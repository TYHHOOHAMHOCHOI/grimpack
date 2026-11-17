using System.Collections.Generic;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using Project05.UI.Hud;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// The run flow of the live Combat scene (System 5, MVP slice): rooms 1..8 then the boss. After a win the right
    /// column shows the buff claim, the item shelf and the three doors (System 28); the corridor in the top strip shows
    /// the floor (user decision 2026-11-17). Rules live in the engine-free <see cref="RunSession"/>; this part only
    /// shows them and applies a claim to the board (cells, pinned item).
    /// </summary>
    public sealed partial class CombatSceneController
    {
        [Header("Run (doors, buffs, curses)")]
        [SerializeField] private TextAsset runJson;
        [Tooltip("Debug policy (System 19 Rule 1): card 3 is Gold whenever Gold is allowed, so curses can be tested.")]
        [SerializeField] private bool forceGoldDoor;
        [Tooltip("Debug policy (item-fusion.md): every eligible row (rooms 3..8, not two in a row, max 2) holds a Fusion Door.")]
        [SerializeField] private bool forceFusionDoor;
        [Tooltip("A new seed every run; off uses Stage Seed so a run can be replayed.")]
        [SerializeField] private bool randomRunSeed = true;
        [Tooltip("Keep the old 1..6 monster test selector instead of the run's rooms.")]
        [SerializeField] private bool monsterTestSelector;

        private RunSession _run;
        private bool _heroPicked;
        private RunPanelView _runPanel;
        private CorridorView _corridor;
        private RectTransform _curseTag;

        /// <summary>The current run, or null when the run config is missing (the old stage loop is used then).</summary>
        public RunSession Run => _run;

        /// <summary>True when Start may begin the room's fight: planning, and no reward step pending.</summary>
        public bool CanStartFight => Phase == CombatScenePhase.Planning && (_run == null || (_run.Phase == RunPhase.Planning && _heroPicked));

        /// <summary>The heroes offered at run start (the Crusader and the Plague Alchemist).</summary>
        public IReadOnlyList<HeroDef> PickableHeroes
        {
            get
            {
                var list = new List<HeroDef>();
                foreach (string id in new[] { "hero_crusader", "hero_alchemist" })
                {
                    HeroDef h = Catalog?.FindHero(id);
                    if (h != null) list.Add(h);
                }

                return list;
            }
        }

        /// <summary>The post-victory panel (tests and captures).</summary>
        public RunPanelView RunPanel => _runPanel;

        private bool RunActive => _run != null;

        /// <summary>Loads the run config and starts a run; leaves the run null (old loop) when it cannot.</summary>
        private void InitRun()
        {
            _run = null;
            if (monsterTestSelector || runJson == null || Catalog == null) return;
            RunConfig cfg = RunConfig.Load(runJson.text, Catalog, out IReadOnlyList<string> errors);
            if (cfg == null)
            {
                foreach (string e in errors) Debug.LogError("[Run] " + e);
                return;
            }

            ulong seed = randomRunSeed ? (ulong)System.DateTime.UtcNow.Ticks : stageSeed;
            _run = new RunSession(cfg, Catalog, seed == 0 ? 1UL : seed, heroCount: 1) { ForceGold = forceGoldDoor, ForceFusion = forceFusionDoor };
            CurrentStage = _run.Room;
            Currency = cfg.StartGold;   // item-upgrades.md: start 5, then gold by door
            _heroPicked = false;
            if (hudView != null && hudView.Sprites != null) hudView.Sprites.ActiveHeroArt = HeroDef != null ? HeroDef.Art : "tank";
        }

        private void BuildRunPanels()
        {
            if (!RunActive) return;
            _runPanel = gameObject.AddComponent<RunPanelView>();
            _runPanel.Initialize(safeFrame, spriteCatalog, Catalog);
            _runPanel.ClaimConfirmed += HandleClaim;
            _runPanel.RewardTaken += HandleRewardTaken;
            _runPanel.RewardSkipped += HandleRewardSkipped;
            _runPanel.DoorChosen += HandleDoorChosen;
            _runPanel.DoorArmed += i => _corridor?.Preview(i >= 0 && i < _run.Row.Count && !_run.Row[i].IsFusion ? _run.Row[i].Tier : (TierKind?)null);
            _runPanel.HeroPicked += HandleHeroPicked;

            var hudRt = (RectTransform)hudView.transform;
            Transform roomNum = hudRt.Find("Room");
            if (roomNum != null) roomNum.gameObject.SetActive(false);   // the corridor replaces the "3/8" counter
            var go = new GameObject("Corridor", typeof(RectTransform));
            _corridor = go.AddComponent<CorridorView>();
            _corridor.Initialize(hudRt, 48f, 2f, spriteCatalog, Catalog, safeFrame);
            _corridor.Show(_run);
            if (_externalBoard != null)
            {
                _externalBoard.PinnedRefused += () => _runPanel?.ShowMessage("Món bị nguyền ghim chặt, không thể nhấc ra.");
                _externalBoard.QuoteForge = QuoteUpgrade;
                _externalBoard.ItemUpgraded += HandleItemUpgraded;
            }

            WireFusion();   // item-fusion.md: the fusion room's step

            // hero-select.md: the hero pick is its own full-screen step after Play
            _motion = safeFrame.GetComponent<Project05.UI.Motion.UiMotion>();
            if (_motion == null) _motion = safeFrame.gameObject.AddComponent<Project05.UI.Motion.UiMotion>();
            _feedback = Project05.UI.Motion.FeedbackFx.Create(safeFrame, _motion, key => spriteCatalog.GetRunSprite(key));
            _runPanel.Feedback = _feedback;
            if (Playback != null) Playback.Feedback = _feedback;
            if (_externalBoard != null)
            {
                _externalBoard.Feedback = _feedback;
                _externalBoard.hoverRingSprite = spriteCatalog.GetRunSprite("ui_ring-gen_hover_9s");
            }
            var heroSelectGo = new GameObject("HeroSelectView");
            heroSelectGo.transform.SetParent(transform, false);
            _heroSelect = heroSelectGo.AddComponent<HeroSelectView>();
            _heroSelect.Initialize(safeFrame, spriteCatalog, Catalog, _motion);
            _heroSelect.Picked += HandleHeroPicked;
            _heroSelect.Show(PickableHeroes, h => h.Art == "alchemist" ? hudView.Sprites.alchemistPoses[0] : hudView.Sprites.heroPoses[0]);
        }

        private TMPro.TextMeshProUGUI _roomPlateText;
        private RectTransform _roomPlate;

        /// <summary>planning-screen.md P25: "Phòng 3/8 · Nguy hiểm 2: Vừa" over the enemy zone, in planning only.</summary>
        private void UpdateRoomPlate()
        {
            if (!RunActive || safeFrame == null || spriteCatalog == null) return;
            if (_roomPlate == null)
            {
                _roomPlate = CombatUiFactory.CreateRect("RoomPlate", safeFrame, 324f, 24f, 200f, 19f);
                CombatUiFactory.CreateImage("Board", _roomPlate, spriteCatalog.GetRunSprite("ui_board-wood_default_9s") ?? spriteCatalog.panelDefault, 0f, 0f, 200f, 19f, sliced: true);
                _roomPlateText = CombatUiFactory.CreateLabel("Text", _roomPlate, spriteCatalog.font, string.Empty, 0f, 3f, 200f, 12f, CombatUiFactory.Bone, TMPro.TextAlignmentOptions.Top);
            }

            bool show = Phase == CombatScenePhase.Planning && _heroPicked && (_runPanel == null || !_runPanel.IsVisible) &&
                        (_heroSelect == null || !_heroSelect.IsVisible);
            if (_roomPlate.gameObject.activeSelf != show) _roomPlate.gameObject.SetActive(show);
            if (!show) return;
            string text = _run.IsBossRoom
                ? "Phòng boss"
                : _run.CurrentEncounter == null
                    ? "Phòng " + _run.Room + "/" + _run.Config.Rooms + " · Lò Dung Hợp"
                    : "Phòng " + _run.Room + "/" + _run.Config.Rooms + " · Nguy hiểm " + _run.CurrentEncounter.Danger + ": " + RunPanelView.DangerWord(_run.CurrentEncounter.Danger);
            if (_roomPlateText.text != text) _roomPlateText.text = text;
        }

        private HeroSelectView _heroSelect;
        private Project05.UI.Motion.UiMotion _motion;

        /// <summary>The full-screen hero pick (tests and probes).</summary>
        public HeroSelectView HeroSelect => _heroSelect;

        // ------------------------------------------------------------------ hero pick
        private void HandleHeroPicked(int index)
        {
            IReadOnlyList<HeroDef> heroes = PickableHeroes;
            if (index < 0 || index >= heroes.Count) return;
            HeroDef pick = heroes[index];
            _heroPicked = true;
            HeroDef = pick;
            // balance-three-item-start.md: the run starts with the hero's kit only (3 items) and an empty stash, whatever
            // the scene had laid out for the editor preview.
            {
                if (_externalBoard != null && popupController != null)
                {
                    _externalBoard.RemoveAllIn("grid");
                    _externalBoard.RemoveAllIn("stash");
                    // the authored starting layout (cells and turns) so the kit's adjacency links hold from turn 1
                    foreach (StartingPlacement at in pick.StartingLayout)
                    {
                        Project05.Items.ItemRecord record = popupController.Catalog.Find(at.ItemId);
                        if (record == null) continue;
                        Sprite plate = spriteCatalog.GetAuthoredPlate(record.tier.ToLowerInvariant(), false, record.footprint);
                        Sprite icon = spriteCatalog.GetItemIcon(record.art, record.footprint, compact: false);
                        _externalBoard.TrySpawnItem(record, plate, icon, "grid", out _, at.X, at.Y, at.Rotation);
                    }
                }
            }

            if (hudView != null && hudView.Sprites != null) hudView.Sprites.ActiveHeroArt = pick.Art;
            Transform title = safeFrame != null ? safeFrame.Find("Workbench/NameLabel") : null;
            string[] words = pick.NameVi.Split(' ');
            if (title != null && title.GetComponent<TMPro.TMP_Text>() != null)
                title.GetComponent<TMPro.TMP_Text>().text = words.Length >= 2 ? words[0] + " " + words[1] : pick.NameVi;   // "Kỵ Sĩ", "Dược Sư"
            Transform innate = safeFrame != null ? safeFrame.Find("Workbench/InnateText") : null;
            if (innate != null && innate.GetComponent<TMPro.TMP_Text>() != null && pick.InnateItem != null)
                innate.GetComponent<TMPro.TMP_Text>().text = pick.InnateItem.NameVi + ". " + pick.InnateItem.DescVi;   // planning-screen.md P18
            _runPanel.Hide();
            SyncArrangementFromExternalBoard();
            PrepareStage(CurrentStage, NextMonsterCount);
            StageReturnedToPlanning?.Invoke();
        }

        // ------------------------------------------------------------------ forge (item-upgrades.md)
        private static readonly int[] FallbackUpgradeCost = { 4, 6, 9, 14, 0, 0 };

        private int UpgradeCostOf(ItemDef def) =>
            _run != null ? _run.Config.UpgradeCostFor(def.Tier) : FallbackUpgradeCost[Mathf.Clamp((int)def.Tier, 0, FallbackUpgradeCost.Length - 1)];

        /// <summary>Whether the held item can be upgraded now, for how much, and the before -> after line.</summary>
        private Project05.UI.Hud.ForgeQuote QuoteUpgrade(Project05.Items.ItemRecord record)
        {
            var q = new Project05.UI.Hud.ForgeQuote();
            if (record == null || Catalog == null) return q;
            if (record.IsUpgraded)
            {
                q.Reason = "Đã rèn +";
                return q;
            }

            ItemDef def = Catalog.FindItem(record.id);
            ItemDef up = def != null ? Catalog.FindItem(def.UpgradeId) : null;
            if (up == null || popupController == null)
            {
                q.Reason = "Không rèn được";
                return q;
            }

            q.Upgrade = popupController.Catalog.Find(up.Id);
            q.Cost = UpgradeCostOf(def);
            q.Preview = UpgradePreview(def, up);
            if (q.Upgrade == null)
            {
                q.Reason = "Không rèn được";
                return q;
            }

            if (Currency < q.Cost)
            {
                q.Reason = "Thiếu " + (q.Cost - Currency) + " vàng";
                return q;
            }

            q.Ok = true;
            return q;
        }

        private void HandleItemUpgraded(Project05.Items.ItemRecord from, Project05.Items.ItemRecord to, int cost)
        {
            AddCurrency(-cost);
            RefreshFromExternalBoard();
        }

        /// <summary>The numbers an upgrade changes, "old → new" (item-upgrades.md).</summary>
        public static string UpgradePreview(ItemDef a, ItemDef b)
        {
            var parts = new List<string>();
            for (int i = 0; i < a.Effects.Count && i < b.Effects.Count; i++)
            {
                EffectDef x = a.Effects[i], y = b.Effects[i];
                switch (x.Kind)
                {
                    case EffectKind.Damage:
                        if (x.Base != y.Base) parts.Add("Sát thương " + x.Base + ">" + y.Base);
                        break;
                    case EffectKind.ApplyStatus:
                        if (x.Amount != y.Amount) parts.Add(Project05.Items.ItemTooltipText.StatusName(x.Status) + " " + x.Amount + ">" + y.Amount);
                        break;
                    case EffectKind.ShieldGain:
                        if (x.Amount != y.Amount) parts.Add("Giáp " + x.Amount + ">" + y.Amount);
                        break;
                    case EffectKind.Heal:
                        if (x.Amount != y.Amount) parts.Add("Hồi " + x.Amount + ">" + y.Amount);
                        break;
                }
            }

            if (a.Trigger.N != b.Trigger.N) parts.Add("Hồi chiêu " + a.Trigger.N + ">" + b.Trigger.N);
            if (a.HpBonus != b.HpBonus) parts.Add("Máu " + a.HpBonus + ">" + b.HpBonus);
            if (a.SpeedBonus != b.SpeedBonus) parts.Add("Tốc độ " + a.SpeedBonus + ">" + b.SpeedBonus);
            if (a.GoldOnWin != b.GoldOnWin) parts.Add("Vàng " + a.GoldOnWin + ">" + b.GoldOnWin);
            for (int i = 0; i < Mathf.Max(a.Synergy.Count, b.Synergy.Count); i++)
            {
                if (i >= a.Synergy.Count) parts.Add("+ năng lực mới");
                else if (i < b.Synergy.Count && a.Synergy[i].Amount != b.Synergy[i].Amount) parts.Add("Cộng hưởng " + a.Synergy[i].Amount + ">" + b.Synergy[i].Amount);
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "Mạnh hơn";
        }

        /// <summary>Gold for winning the current room: the door's tier (room 1 counts as White; the boss pays 0) plus Coin Purses.</summary>
        private int RoomGold()
        {
            if (_run == null || _run.IsBossRoom) return 0;
            int gold = _run.Config.GoldFor(_run.CurrentDoor?.Tier ?? TierKind.White);
            if (_externalBoard != null && Catalog != null)
            {
                foreach (Project05.Items.BoardItem bi in _externalBoard.ExportContainerItems("grid"))
                {
                    ItemDef def = bi?.Record != null ? Catalog.FindItem(bi.Record.id) : null;
                    if (def != null) gold += def.GoldOnWin;
                }
            }

            return gold;
        }

        /// <summary>The hero's baseline for the next fight: door HP and curse terms from the run.</summary>
        private HeroBaselineInput RunBaseline(ArrangementGridView grid, GridSynergyView synergy) =>
            RunActive
                ? _run.Baseline(0, HeroDef, grid, synergy)
                : new HeroBaselineInput(0, HeroDef, 0, grid, synergy);

        // ------------------------------------------------------------------ after a fight
        private void HandleRunPlaybackCompleted(CombatResult result)
        {
            LastResult = result;
            if (result != null && result.Outcome == CombatOutcome.Victory)
            {
                int gold = RoomGold();
                if (gold > 0) AddCurrency(gold, new Vector2(476f, 150f));   // R-10: coins hop from the enemy zone to the counter
                if (_runPanel != null) _runPanel.TitleNote = gold > 0 ? "+" + gold + " vàng" : string.Empty;
                _run.OnVictory();
            }
            else
            {
                _run.OnDefeat();
            }
            _corridor?.Show(_run);

            if (_run.Phase == RunPhase.Won || _run.Phase == RunPhase.Lost)
            {
                SetPhase(CombatScenePhase.PostCombat);
                PostCombatPanel?.Show(result, Catalog, CurrentStage, NextMonsterCount);
                PostCombatPanel?.SetRunOver(_run.Phase == RunPhase.Won ? "Đã hạ boss! Lượt chơi thắng." : "Lượt chơi kết thúc. Không có chơi lại phòng.", "Chơi lượt mới");
                return;
            }

            Arrangement?.Thaw();
            SetPhase(CombatScenePhase.RunReward);
            ShowRunStep();
            Playback?.SetSpeedIndex(_playbackSpeedIndex);
            StageReturnedToPlanning?.Invoke();   // the grid stays editable through the reward steps (System 17 Rule 8)
        }

        private void ShowRunStep()
        {
            _corridor?.Show(_run);
            switch (_run.Phase)
            {
                case RunPhase.BuffClaim:
                    _runPanel.ShowClaim(_run.Room, _run.CurrentDoor.Tier, _run.PendingBuff, _run.HpFor(0),
                        _run.CellsFor(_externalBoard != null ? _externalBoard.LockedHeroCellsLeft : 0), _run.PendingCurse, HeroDef != null ? HeroDef.NameVi : "?");
                    break;
                case RunPhase.Reward:
                    if (_run.Offer.Count == 0)
                    {
                        _run.SkipReward();
                        ShowRunStep();
                        return;
                    }

                    _runPanel.ShowReward(_run.Room, _run.CurrentDoor?.Tier ?? TierKind.White, _run.Offer);
                    break;
                case RunPhase.DoorChoice:
                    _runPanel.ShowDoors(_run.Room, _run.Row);
                    break;
                case RunPhase.Fusion:
                    _runPanel.ShowFusion(_run.Room);
                    MarkFusionIngredients(_runPanel.Armed);
                    break;
                default:
                    EnterRoom();   // after room 8 the boss follows with no door
                    break;
            }
        }

        private void HandleClaim()
        {
            if (_run.Phase != RunPhase.BuffClaim) return;
            CurseDef curse = _run.PendingCurse;
            Project05.Items.ItemRecord pinnedRecord = curse != null && curse.Kind == CurseKind.Pinned && popupController != null
                ? popupController.Catalog.Find(curse.ItemId)
                : null;
            bool fits = pinnedRecord == null || (_externalBoard != null && _externalBoard.CanPlaceInHeroGrid(pinnedRecord));
            int locked = _externalBoard != null ? _externalBoard.LockedHeroCellsLeft : 0;
            ClaimResult r = _run.ClaimBuff(0, locked, fits);
            switch (r.Status)
            {
                case ClaimStatus.NeedsSpace:
                    _runPanel.ShowMessage("Cần 2 ô trống liền nhau cho Neo: dời đồ ra kho rồi Nhận lại.");
                    return;
                case ClaimStatus.HeroHasCurse:
                    _runPanel.ShowMessage("Nhân vật này đã mang một lời nguyền.");
                    return;
                case ClaimStatus.Ok:
                    break;
                default:
                    return;
            }

            if (r.CellsGranted > 0) _externalBoard?.OpenHeroCells(r.CellsGranted);
            if (pinnedRecord != null && _externalBoard != null)
            {
                Sprite plate = spriteCatalog.GetAuthoredPlate(pinnedRecord.tier.ToLowerInvariant(), false, pinnedRecord.footprint);
                Sprite icon = spriteCatalog.GetItemIcon(pinnedRecord.art, pinnedRecord.footprint, compact: false);
                if (!_externalBoard.TrySpawnPinned(pinnedRecord, plate, icon, spriteCatalog.GetRunSprite("ui_fill_plum_4x4"),
                        spriteCatalog.GetRunSprite(RunPanelView.CurseGlyph(CurseKind.Pinned, 8)), out _))
                {
                    Debug.LogError("[Run] The pinned item did not fit after the claim was accepted.");
                }
            }

            RefreshCurseTag();
            ShowRunStep();
        }

        private void HandleRewardTaken(int index)
        {
            if (_run.Phase != RunPhase.Reward || index < 0 || index >= _run.Offer.Count) return;
            Project05.Items.ItemRecord record = popupController != null ? popupController.Catalog.Find(_run.Offer[index]) : null;
            if (record == null || _externalBoard == null) return;
            Sprite plate = spriteCatalog.GetAuthoredPlate(record.tier.ToLowerInvariant(), false, record.footprint);
            Sprite icon = spriteCatalog.GetItemIcon(record.art, record.footprint, compact: false);
            RectTransform card = _runPanel.CardRect(index);
            Vector3? cardWorld = card != null ? card.TransformPoint(card.rect.center) : (Vector3?)null;
            if (!_externalBoard.TrySpawnItem(record, plate, icon, "stash", out Project05.UI.Hud.ItemHoverTarget spawned))
            {
                _runPanel.ShowMessage("Kho và balo đã đầy: bán hoặc dời bớt đồ.");
                return;
            }

            // R-01: the taken item flies from its card to its cell, then lands (thunk and ring)
            if (_feedback != null && spawned != null && cardWorld.HasValue)
            {
                RectTransform rt = spawned.Rect;
                Vector2 to = rt.anchoredPosition;
                Vector2 from = ((RectTransform)rt.parent).InverseTransformPoint(cardWorld.Value);
                _feedback.Motion.MoveFrom(rt, from, to, 0.28f, Project05.UI.Motion.Ease.InOutQuad, () =>
                {
                    _feedback.Thunk(rt);
                    _feedback.RingPulse(rt, 0.15f);
                });
            }

            _run.TakeReward(index);
            ShowRunStep();
        }

        private void HandleRewardSkipped()
        {
            if (_run.SkipReward()) ShowRunStep();
        }

        private void HandleDoorChosen(int index)
        {
            if (!_run.ChooseDoor(index)) return;
            if (_run.Phase == RunPhase.Fusion) EnterFusionRoom();   // item-fusion.md Rule 6: no fight
            else EnterRoom();
        }

        /// <summary>Room entry: the chosen door's room (or the boss) becomes the next fight.</summary>
        private void EnterRoom()
        {
            if (_runPanel != null) _runPanel.TitleNote = string.Empty;
            _runPanel?.Hide();
            _corridor?.Preview(null);
            CurrentStage = _run.Room;
            PrepareStage(CurrentStage, NextMonsterCount);
            _corridor?.Show(_run);
            Playback?.SetSpeedIndex(_playbackSpeedIndex);
            StageReturnedToPlanning?.Invoke();
        }

        /// <summary>A new run starts by reloading the scene (no retry, no reroll; System 5).</summary>
        private void RestartRun()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        /// <summary>The Plum curse tag beside the hero (System 19 Rule 13), with the curse's glyph.</summary>
        private void RefreshCurseTag()
        {
            ActiveCurse active = _run?.CurseOf(0);
            Transform hero = hudView != null ? hudView.transform.Find("HeroSlot1") : null;
            if (active == null || hero == null)
            {
                if (_curseTag != null) _curseTag.gameObject.SetActive(false);
                return;
            }

            if (_curseTag == null)
            {
                Image chip = CombatUiFactory.CreateImage("CurseTag", hero, spriteCatalog.GetRunSprite("ui_curse-chip_9s"), 4f, 2f, 54f, 12f, sliced: true);
                _curseTag = chip.rectTransform;
                CombatUiFactory.CreateImage("Glyph", _curseTag, null, 4f, 2f, 8f, 8f);
                CombatUiFactory.CreateLabel("Name", _curseTag, spriteCatalog.font, string.Empty, 14f, 0f, 40f, 12f, CombatUiFactory.Bone);
            }

            _curseTag.gameObject.SetActive(true);
            _curseTag.Find("Glyph").GetComponent<Image>().sprite = spriteCatalog.GetRunSprite(RunPanelView.CurseGlyph(active.Curse.Kind, 8));
            _curseTag.Find("Name").GetComponent<TMPro.TextMeshProUGUI>().text = active.Curse.NameVi;
        }
    }
}
