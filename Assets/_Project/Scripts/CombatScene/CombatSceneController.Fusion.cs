using System.Collections.Generic;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using Project05.UI.Hud;
using UnityEngine;

namespace Project05.CombatScene
{
    /// <summary>
    /// The fusion room of the live Combat scene (design/gdd/item-fusion.md): entering it from a Fusion Door, planning
    /// the recipes against the board and the gold, applying one fusion to the board, and leaving. The rules are the
    /// engine-free <see cref="FusionRules"/> and <see cref="RunSession"/>; this part only shows them and moves plates.
    /// </summary>
    public sealed partial class CombatSceneController
    {
        /// <summary>Wires the fusion step of the run panel (called from BuildRunPanels).</summary>
        private void WireFusion()
        {
            if (_runPanel == null) return;
            _runPanel.FusionRows = BuildFusionRows;
            _runPanel.Gold = () => Currency;
            _runPanel.MakeableCount = () => _run != null ? FusionRules.CountMakeable(Catalog, _run.Config, OwnedItems(out _), Currency) : 0;
            _runPanel.FusionArmed += MarkFusionIngredients;
            _runPanel.FusionConfirmed += HandleFuse;
            _runPanel.FusionLeft += HandleLeaveFusion;
            if (_externalBoard != null)
            {
                _externalBoard.BoardChanged += () =>
                {
                    if (_run == null || _run.Phase != RunPhase.Fusion || _runPanel == null) return;
                    _runPanel.RefreshFusion();
                    MarkFusionIngredients(_runPanel.Armed);
                };
            }
        }

        /// <summary>Room entry through a Fusion Door: no fight, an empty stage, the recipe book over the enemy zone.</summary>
        private void EnterFusionRoom()
        {
            if (_runPanel != null) _runPanel.TitleNote = string.Empty;
            _corridor?.Preview(null);
            CurrentStage = _run.Room;
            _currentSpawns = new List<EnemySpawnInput>();
            if (hudView != null) hudView.ClearEnemies();
            Arrangement?.Thaw();
            SetPhase(CombatScenePhase.RunReward);   // the grid stays editable; selling and the forge work as in the reward steps
            ShowRunStep();
            Playback?.SetSpeedIndex(_playbackSpeedIndex);
            StageReturnedToPlanning?.Invoke();
        }

        /// <summary>Every owned, movable item in the hero grid and the stash, as the fusion rules see it.</summary>
        private List<OwnedItem> OwnedItems(out Dictionary<long, Project05.Items.BoardItem> byId)
        {
            var list = new List<OwnedItem>();
            byId = new Dictionary<long, Project05.Items.BoardItem>();
            if (_externalBoard == null) return list;
            foreach (string container in new[] { "grid", "stash" })
            {
                foreach (Project05.Items.BoardItem bi in _externalBoard.ExportContainerItems(container))
                {
                    if (bi?.Record == null || bi.Pinned) continue;
                    list.Add(new OwnedItem(bi.Id, bi.Record.id, container == "stash"));
                    byId[bi.Id] = bi;
                }
            }

            return list;
        }

        /// <summary>The recipe book's rows, in the catalog's recipe order.</summary>
        private IReadOnlyList<FusionRow> BuildFusionRows()
        {
            var rows = new List<FusionRow>();
            if (Catalog == null || _run == null) return rows;
            List<OwnedItem> owned = OwnedItems(out Dictionary<long, Project05.Items.BoardItem> byId);
            FusionStatus phase = _run.CanFuse;
            foreach (RecipeDef r in Catalog.Recipes)
            {
                FusionPlan plan = FusionRules.Plan(Catalog, _run.Config, r, owned, Currency);
                var ingredients = new ItemDef[r.Ingredients.Count];
                for (int i = 0; i < ingredients.Length; i++) ingredients[i] = Catalog.FindItem(r.Ingredients[i]);
                var owns = new bool[plan.Owned.Count];
                for (int i = 0; i < owns.Length; i++) owns[i] = plan.Owned[i];
                var loses = new List<string>();
                foreach (long id in plan.LosesUpgrade)
                {
                    if (byId.TryGetValue(id, out Project05.Items.BoardItem bi)) loses.Add(bi.Record.nameVi);
                }

                rows.Add(new FusionRow
                {
                    Recipe = r,
                    Result = Catalog.FindItem(r.ResultId),
                    Ingredients = ingredients,
                    Owned = owns,
                    Status = phase != FusionStatus.Ok ? phase : plan.Status,
                    Cost = plan.Cost,
                    Shortfall = plan.Shortfall,
                    LosesUpgrade = loses.ToArray(),
                });
            }

            return rows;
        }

        /// <summary>Rings the instances the armed recipe would consume (item-fusion.md UI); clears when none.</summary>
        private void MarkFusionIngredients(int index)
        {
            if (_externalBoard == null) return;
            if (_run == null || Catalog == null || _run.Phase != RunPhase.Fusion || _run.FusedThisRoom || index < 0 || index >= Catalog.Recipes.Count)
            {
                _externalBoard.MarkItems(null);
                return;
            }

            List<OwnedItem> owned = OwnedItems(out Dictionary<long, Project05.Items.BoardItem> byId);
            FusionPlan plan = FusionRules.Plan(Catalog, _run.Config, Catalog.Recipes[index], owned, int.MaxValue);
            var marked = new List<Project05.Items.BoardItem>();
            foreach (long id in plan.Consumed)
            {
                if (byId.TryGetValue(id, out Project05.Items.BoardItem bi)) marked.Add(bi);
            }

            _externalBoard.MarkItems(marked);
        }

        /// <summary>
        /// Fuse(recipe), item-fusion.md Rule 8: the phase checks, the plan (ingredients, gold), then space. On success the
        /// ingredients leave the board, the gold is paid, the result is created in the stash (or the grid) and the run
        /// records the fusion. On refusal nothing changes.
        /// </summary>
        private void HandleFuse(int index)
        {
            if (_run == null || Catalog == null || _externalBoard == null || index < 0 || index >= Catalog.Recipes.Count) return;
            RecipeDef recipe = Catalog.Recipes[index];
            FusionStatus phase = _run.CanFuse;
            if (phase != FusionStatus.Ok)
            {
                _runPanel.ShowMessage(phase == FusionStatus.AlreadyFused ? "Mỗi lần ghé lò chỉ dung hợp 1 lần." : "Chỉ dung hợp được trong Lò Dung Hợp.");
                return;
            }

            List<OwnedItem> owned = OwnedItems(out Dictionary<long, Project05.Items.BoardItem> byId);
            FusionPlan plan = FusionRules.Plan(Catalog, _run.Config, recipe, owned, Currency);
            if (!plan.IsOk)
            {
                _runPanel.ShowMessage(plan.Status == FusionStatus.NotEnoughGold ? "Thiếu " + plan.Shortfall + " vàng." : "Thiếu nguyên liệu.");
                return;
            }

            Project05.Items.ItemRecord result = popupController != null ? popupController.Catalog.Find(recipe.ResultId) : null;
            if (result == null) return;

            // the ingredients leave the board first (their cells free up), their plates kept for the flight
            var taken = new List<Project05.Items.BoardItem>();
            var plates = new List<RectTransform>();
            foreach (long id in plan.Consumed)
            {
                if (!byId.TryGetValue(id, out Project05.Items.BoardItem bi)) continue;
                taken.Add(bi);
                plates.Add(_externalBoard.TakeOff(bi, keepPlate: true));
            }

            Sprite plate = spriteCatalog.GetAuthoredPlate(result.tier.ToLowerInvariant(), false, result.footprint);
            Sprite icon = spriteCatalog.GetItemIcon(result.art, result.footprint, compact: false);
            if (!_externalBoard.TrySpawnItem(result, plate, icon, "stash", out ItemHoverTarget spawned))
            {
                // NoSpace: everything goes back where it was
                foreach (RectTransform p in plates)
                {
                    if (p != null) CombatUiFactory.DestroySafe(p.gameObject);
                }

                foreach (Project05.Items.BoardItem bi in taken)
                {
                    Sprite bp = spriteCatalog.GetAuthoredPlate(bi.Record.tier.ToLowerInvariant(), false, bi.Record.footprint);
                    Sprite bic = spriteCatalog.GetItemIcon(bi.Record.art, bi.Record.footprint, compact: false);
                    if (_externalBoard.TrySpawnItem(bi.Record, bp, bic, bi.Container, out ItemHoverTarget back, bi.X, bi.Y, bi.Rotation) &&
                        bi.Record.IsUpgraded) _externalBoard.AddPlusBadge(back);
                }

                _runPanel.ShowMessage("Kho và balo đã đầy: bán hoặc dời bớt đồ.");
                RefreshFromExternalBoard();
                return;
            }

            _externalBoard.MarkItems(null);
            _run.RecordFusion(recipe.Id);
            AddCurrency(-plan.Cost);
            RefreshFromExternalBoard();
            _corridor?.Show(_run);
            PlayFusionMotion(plates, spawned);
            _runPanel.FusionDone(recipe.Id, result.nameVi);
        }

        /// <summary>
        /// The fusion's motion (item-fusion.md UI): the cost's coins hop to the crucible, each ingredient plate flies in
        /// and vanishes, sparks and a ring burst there, then the result flies to its stash cell and lands. Skippable by
        /// reduced motion (every tween jumps to its end).
        /// </summary>
        private void PlayFusionMotion(List<RectTransform> plates, ItemHoverTarget spawned)
        {
            Vector3 crucible = _runPanel.CrucibleWorld;
            if (_feedback == null || !Application.isPlaying || safeFrame == null)
            {
                foreach (RectTransform p in plates)
                {
                    if (p != null) CombatUiFactory.DestroySafe(p.gameObject);
                }

                return;
            }

            Vector2 crucibleFrame = _feedback.FromWorld(crucible);
            _feedback.CoinFlight(new Vector2(10f, 8f), crucibleFrame, 4, null);
            // the result waits in the crucible (under the overlay) until the burst, then flies to its cell
            Vector2 resultCell = Vector2.zero, resultStart = Vector2.zero;
            if (spawned != null)
            {
                RectTransform srt = spawned.Rect;
                resultCell = srt.anchoredPosition;
                resultStart = ((RectTransform)srt.parent).InverseTransformPoint(crucible);
                srt.anchoredPosition = resultStart;
            }
            int left = 0;
            foreach (RectTransform p in plates)
            {
                if (p != null) left++;
            }

            void Burst()
            {
                _feedback.Sparks(crucibleFrame);
                _feedback.Pixels(crucibleFrame, "bone", 10);
                if (spawned == null) return;
                RectTransform rt = spawned.Rect;
                _feedback.Motion.MoveFrom(rt, resultStart, resultCell, 0.3f, Project05.UI.Motion.Ease.InOutQuad, () =>
                {
                    _feedback.Thunk(rt);
                    _feedback.RingPulse(rt, 0.15f, 2);
                });
            }

            if (left == 0)
            {
                Burst();
                return;
            }

            float delay = 0f;
            foreach (RectTransform p in plates)
            {
                if (p == null) continue;
                RectTransform plate = p;
                plate.SetParent(safeFrame, true);
                plate.SetAsLastSibling();
                Vector2 centre = safeFrame.InverseTransformPoint(plate.TransformPoint(plate.rect.center));
                Vector2 target = safeFrame.InverseTransformPoint(crucible);
                Vector2 start = plate.anchoredPosition;
                Vector2 end = start + (target - centre);
                _feedback.Motion.Run(delay, null, () =>
                {
                    if (plate == null) return;
                    _feedback.Motion.MoveFrom(plate, start, end, 0.24f, Project05.UI.Motion.Ease.InQuad, () =>
                    {
                        if (plate != null) CombatUiFactory.DestroySafe(plate.gameObject);
                        if (--left == 0) Burst();
                    });
                });
                delay += 0.06f;
            }

            _feedback.BringToFront();
        }

        /// <summary>"Rời lò": the next door row, or the boss after room 8 (item-fusion.md Rule 7).</summary>
        private void HandleLeaveFusion()
        {
            _externalBoard?.MarkItems(null);
            if (_run == null || !_run.LeaveFusion()) return;
            _corridor?.Show(_run);
            if (_run.Phase == RunPhase.DoorChoice) ShowRunStep();
            else EnterRoom();
        }
    }
}
