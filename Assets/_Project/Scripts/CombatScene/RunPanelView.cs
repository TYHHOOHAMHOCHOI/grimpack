using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// The post-victory panel in the right column (door-and-room-panels-ui.md, System 28): three steps of one flow,
    /// BuffClaim, Reward shelf and DoorChoice, sharing one card mould, one arm-then-Confirm grammar and one detail box.
    /// Display only: it shows what the run session offers and raises commands; it decides nothing. The fusion room's
    /// step (item-fusion.md) lives in RunPanelView.Fusion.cs.
    /// Vietnamese literals are TEMPORARY until the localisation pass (System 7).
    /// </summary>
    public sealed partial class RunPanelView : MonoBehaviour
    {
        /// <summary>Which step the panel shows.</summary>
        public enum Step { Hidden, Claim, Reward, Doors, HeroPick, Fusion }

        // planning-screen.md section 5: an overlay on the enemy zone (P24, x 316..637, y 19..333); R rects relative to it
        private const float PanelX = 316f, PanelY = 19f, PanelW = 321f, PanelH = 314f;
        private const float CardW = 90f, CardH = 140f, CardGap = 12f, CardX0 = 9f, CardY = 32f;
        private const float DetailY = 180f, DetailH = 98f;

        public event Action ClaimConfirmed;
        public event Action<int> RewardTaken;
        public event Action RewardSkipped;
        public event Action<int> DoorChosen;
        /// <summary>A hero was chosen at run start (index into the list shown).</summary>
        public event Action<int> HeroPicked;

        /// <summary>A line shown after the title (e.g. the gold paid for the room), cleared by the caller.</summary>
        public string TitleNote { get; set; } = string.Empty;
        /// <summary>The armed door changed (index, or -1): the corridor previews it.</summary>
        public event Action<int> DoorArmed;

        private CombatSceneSpriteCatalog _sprites;
        private GameCatalog _catalog;
        private RectTransform _root;
        private RectTransform _content;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _steps;
        private TextMeshProUGUI _detail;
        private TextMeshProUGUI _message;
        private Button _confirm;
        private Button _skip;
        private TextMeshProUGUI _skipLabel;
        private TextMeshProUGUI _confirmLabel;
        private RectTransform _actions;
        private Image _skipKey;
        private readonly List<Card> _cards = new List<Card>();
        private int _armed = -1;
        private int _hover = -1;
        private float _readyAt;
        private IReadOnlyList<DoorCard> _doors;
        private IReadOnlyList<string> _offer;

        public Step Current { get; private set; } = Step.Hidden;

        /// <summary>Card i's rect, or null (the reward flight starts from it, feedback-motion.md R-01).</summary>
        public RectTransform CardRect(int index) => index >= 0 && index < _cards.Count ? _cards[index].Rect : null;

        /// <summary>Reactions (feedback-motion.md U-04); null in tests.</summary>
        public Project05.UI.Motion.FeedbackFx Feedback { get; set; }
        public int Armed => _armed;
        public bool IsVisible => _root != null && _root.gameObject.activeSelf;

        private sealed class Card
        {
            public RectTransform Rect;
            public Image Ring;
            public Image Corner;
            public string Detail = string.Empty;
        }

        /// <summary>Relays pointer enter/exit to the panel.</summary>
        private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Action Enter;
            public Action Exit;
            public void OnPointerEnter(PointerEventData eventData) => Enter?.Invoke();
            public void OnPointerExit(PointerEventData eventData) => Exit?.Invoke();
        }

        public void Initialize(RectTransform safeFrame, CombatSceneSpriteCatalog sprites, GameCatalog catalog)
        {
            _sprites = sprites;
            _catalog = catalog;
            _root = CombatUiFactory.CreateRect("RunPanel", safeFrame, PanelX, PanelY, PanelW, PanelH);
            // R1: one ink step over the enemy zone (checker, no partial alpha); the hero column and workbench stay usable
            Image dim = CombatUiFactory.CreateImage("Dim", _root, Run("ui_dim-mask_default_4x4") ?? sprites.fillInk0, 0f, 0f, PanelW, PanelH, raycastTarget: true);
            dim.type = Image.Type.Tiled;
            // R3: step header plate with the title and the tracker
            CombatUiFactory.CreateImage("Header", _root, Run("ui_board-wood_default_9s") ?? sprites.panelDefault, 7f, 6f, 296f, 19f, sliced: true);
            _title = CombatUiFactory.CreateLabel("Title", _root, sprites.font, string.Empty, 26f, 9f, 170f, 12f, CombatUiFactory.Bone);
            _steps = CombatUiFactory.CreateLabel("Steps", _root, sprites.font, string.Empty, 150f, 9f, 134f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.TopRight);
            _content = CombatUiFactory.CreateRect("Content", _root, 0f, 0f, PanelW, PanelH);
            // R6 detail plate, R7 message line
            CombatUiFactory.CreateImage("DetailBg", _root, sprites.panelDefault, CardX0, DetailY, 294f, DetailH, sliced: true);
            _detail = CombatUiFactory.CreateLabel("Detail", _root, sprites.font, string.Empty, CardX0 + 7f, DetailY + 6f, 280f, DetailH - 12f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            _message = CombatUiFactory.CreateLabel("Message", _root, sprites.font, string.Empty, CardX0, 284f, 294f, 24f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);

            // R8 to R11 live in the action strip (art bible 7.10): a strip of their own over the Start group while shown
            _actions = CombatUiFactory.CreateRect("RunActions", safeFrame, 177f, 333f, 460f, 24f);
            CombatUiFactory.CreateImage("Strip", _actions, Run("ui_fill-gen_ink-1_4x4") ?? sprites.fillInk1, 0f, 0f, 460f, 24f, raycastTarget: true);
            CombatUiFactory.CreateImage("Rail", _actions, Run("ui_fill-gen_umber-2_4x4") ?? sprites.fillCandleD, 0f, 0f, 460f, 1f);
            _skipKey = CombatUiFactory.CreateImage("SkipKey", _actions, Run("ui_plate-iron_default_9s") ?? sprites.panelDefault, 160f, 5f, 14f, 14f, sliced: true);
            CombatUiFactory.CreateLabel("Text", _skipKey.transform, sprites.font, "K", 0f, 1f, 14f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            _skip = CombatUiFactory.CreateSegmentButton("Skip", _actions, sprites, "Bỏ qua", 176f, 2f, 76f, 19f);
            _skipLabel = _skip.GetComponentInChildren<TextMeshProUGUI>(true);
            _skip.onClick.AddListener(Skip);
            Image enterKey = CombatUiFactory.CreateImage("ConfirmKey", _actions, Run("ui_plate-iron_default_9s") ?? sprites.panelDefault, 258f, 5f, 46f, 14f, sliced: true);
            CombatUiFactory.CreateLabel("Text", enterKey.transform, sprites.font, "Enter", 0f, 1f, 46f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            _confirm = CombatUiFactory.CreateSegmentButton("Confirm", _actions, sprites, "Xác nhận", 308f, 2f, 148f, 19f, selected: true);
            _confirmLabel = _confirm.GetComponentInChildren<TextMeshProUGUI>(true);
            _confirm.onClick.AddListener(Confirm);
            _actions.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Commit presses are ignored for a moment after a step appears (System 28 input lock).</summary>
        private bool Ready => Time.unscaledTime >= _readyAt;

        public void Hide()
        {
            ClearFusionDetail();
            Current = Step.Hidden;
            if (_root != null) _root.gameObject.SetActive(false);
            if (_actions != null) _actions.gameObject.SetActive(false);
            SetArmed(-1);
        }

        // ------------------------------------------------------------------ steps
        /// <summary>BuffClaim: the door's certain buff, and a Gold door's curse, go to the one hero.</summary>
        public void ShowClaim(int room, TierKind tier, DoorBuff buff, int hpNow, int cellsNow, CurseDef curse, string heroName)
        {
            Begin(Step.Claim, $"Thắng phòng {room}", 0);
            var box = CombatUiFactory.CreateRect("ClaimBox", _content, CardX0, CardY, 294f, CardH);
            CombatUiFactory.CreateImage("Frame", box, DoorFrame(tier), 0f, 0f, 294f, CardH, sliced: true);
            Pips(box, tier, 147f, 8f);
            CombatUiFactory.CreateLabel("Head", box, _sprites.font, "Phần thưởng cửa " + TierName(tier), 0f, 18f, 294f, 12f, CombatUiFactory.Candle, TextAlignmentOptions.Top);
            float y = 40f;
            Line(box, _sprites.glyphHeartBone8, $"+{hpNow} Máu tối đa" + (hpNow < buff.Hp ? $" (giới hạn, gốc +{buff.Hp})" : string.Empty), 20f, ref y);
            if (buff.Cells > 0)
            {
                Line(box, Run("ui_glyph-cell_bone_8x8"), cellsNow > 0 ? "+1 ô balo" : "+1 ô balo: mất (không còn ô khóa)", 20f, ref y);
            }

            if (curse != null)
            {
                y += 4f;
                CurseTag(box, curse, 20f, y);
                CombatUiFactory.CreateLabel("CurseLine", box, _sprites.font, curse.NameVi + ": " + curse.LineVi, 64f, y - 1f, 220f, 12f, CombatUiFactory.Bone);
                y += 14f;
                CombatUiFactory.CreateLabel("Lasts", box, _sprites.font, "Nguyền kéo dài cả lượt chơi.", 64f, y, 220f, 12f, CombatUiFactory.Fog);
            }

            SetDetail("Trao cho: " + heroName + "\nNhận ngay, không ngẫu nhiên. Có hiệu lực từ trận sau." +
                      (curse != null && curse.Kind == CurseKind.Pinned ? "\nNeo cần 2 ô trống liền nhau trong balo." : string.Empty));
            _confirmLabel.text = "Nhận";
            SetButtons(confirm: true, skip: false);
            SetArmed(0);
        }

        /// <summary>Reward shelf: take one of the offered items or skip.</summary>
        public void ShowReward(int room, TierKind doorTier, IReadOnlyList<string> offer)
        {
            Begin(Step.Reward, $"Thắng phòng {room}", 1);
            _offer = offer;
            for (int i = 0; i < offer.Count; i++)
            {
                ItemDef def = _catalog?.FindItem(offer[i]);
                if (def == null) continue;
                int index = i;
                Card card = NewCard(i, DoorFrame(def.Tier), () => Arm(index));
                Pips(card.Rect, def.Tier, CardW / 2f, 8f);
                Sprite icon = _sprites.GetItemIcon(def.Art, def.Footprint, compact: false);
                if (icon != null)
                {
                    float w = icon.rect.width, h = icon.rect.height;
                    float scale = Mathf.Min(1f, Mathf.Min(76f / w, 80f / h));
                    w = Mathf.Floor(w * scale);
                    h = Mathf.Floor(h * scale);
                    CombatUiFactory.CreateImage("Icon", card.Rect, icon, Mathf.Floor((CardW - w) / 2f), 18f + Mathf.Floor((80f - h) / 2f), w, h);
                }

                CombatUiFactory.CreateLabel("Name", card.Rect, _sprites.font, def.NameVi, 2f, 104f, CardW - 4f, 24f, CombatUiFactory.Bone, TextAlignmentOptions.Top, wrap: true);
                card.Detail = def.NameVi + " (" + TierName(def.Tier) + ", " + def.Footprint + ")\n" + Describe(def) + (string.IsNullOrEmpty(def.FlavorVi) ? string.Empty : "\n" + def.FlavorVi);
            }

            SetDetail(offer.Count > 0 ? "Chọn 1 món (vào kho đồ) hoặc bỏ qua.\nĐộ hiếm theo cửa " + TierName(doorTier) + "." : "Không có món nào.");
            _confirmLabel.text = "Lấy";
            if (_skipLabel != null) _skipLabel.text = "Bỏ qua";
            SetButtons(confirm: true, skip: true);
        }

        /// <summary>DoorChoice: three cards; arm one, then Confirm.</summary>
        public void ShowDoors(int fromRoom, IReadOnlyList<DoorCard> row)
        {
            Begin(Step.Doors, fromRoom + 1 > 8 ? "Phòng boss" : $"Chọn cửa vào phòng {fromRoom + 1}", 2);
            _doors = row;
            for (int i = 0; i < row.Count; i++)
            {
                DoorCard door = row[i];
                int index = i;
                Card card = NewCard(i, door.IsFusion ? FusionFrame : DoorFrame(door.Tier), () => Arm(index));
                if (door.IsFusion)
                {
                    BuildFusionDoorFace(card.Rect);
                    card.Detail = FusionDoorDetail;
                }
                else
                {
                    BuildDoorFace(card.Rect, door);
                    card.Detail = DoorDetail(door);
                }
            }

            SetDetail("Rê chuột để xem, bấm để chọn, rồi Xác nhận. Cửa đã chọn là cố định.");
            _confirmLabel.text = "Vào cửa";
            SetButtons(confirm: true, skip: false);
        }

        /// <summary>
        /// Arms card index (or the claim) and confirms in one step, without the input lock: for tests and automation.
        /// </summary>
        public void Command(int index)
        {
            if (Current != Step.Claim) Arm(index);
            _readyAt = 0f;
            Confirm();
        }

        /// <summary>Skips the item shelf, or leaves the fusion room, without the input lock (tests and automation).</summary>
        public void CommandSkip()
        {
            if (Current == Step.Reward) RewardSkipped?.Invoke();
            else if (Current == Step.Fusion) FusionLeft?.Invoke();
        }

        private void Skip()
        {
            if (!Ready) return;
            if (Current == Step.Reward) RewardSkipped?.Invoke();
            else if (Current == Step.Fusion) FusionLeft?.Invoke();
        }

        /// <summary>Run start: pick the hero (debuffs-and-detonation.md: the Alchemist carries the detonation core).</summary>
        public void ShowHeroPick(IReadOnlyList<HeroDef> heroes, Func<HeroDef, Sprite> portrait)
        {
            Begin(Step.HeroPick, "Chọn nhân vật", -1);
            _steps.text = string.Empty;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroDef h = heroes[i];
                int index = i;
                float x = 6f + i * (150f + 10f);
                var card = new Card { Rect = CombatUiFactory.CreateRect("Hero" + i, _content, x, CardY, 150f, CardH) };
                card.Ring = CombatUiFactory.CreateImage("Ring", card.Rect, Run("ui_door-card_ring-hover_9s"), -2f, -2f, 154f, CardH + 4f, sliced: true);
                card.Ring.enabled = false;
                Image bg = CombatUiFactory.CreateImage("Frame", card.Rect, _sprites.panelDefault, 0f, 0f, 150f, CardH, sliced: true, raycastTarget: true);
                var btn = bg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Arm(index));
                var relay = bg.gameObject.AddComponent<HoverRelay>();
                relay.Enter = () => Hover(index);
                relay.Exit = () => Hover(-1);
                card.Corner = CombatUiFactory.CreateImage("Corner", card.Rect, Run("ui_door-card_armed-corner_4x4"), -3f, -3f, 4f, 4f);
                card.Corner.enabled = false;
                Sprite face = portrait != null ? portrait(h) : null;
                if (face != null) CombatUiFactory.CreateImage("Portrait", card.Rect, face, 51f, 6f, 48f, 48f);
                CombatUiFactory.CreateLabel("Name", card.Rect, _sprites.font, h.NameVi, 4f, 58f, 142f, 12f, CombatUiFactory.Candle, TextAlignmentOptions.Top);
                CombatUiFactory.CreateLabel("Role", card.Rect, _sprites.font, h.RoleVi, 4f, 72f, 142f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
                CombatUiFactory.CreateLabel("Stats", card.Rect, _sprites.font, "Máu " + h.BaseMaxHp + " · Tốc độ " + h.BaseSpeed, 4f, 88f, 142f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
                ItemDef sig = _catalog?.FindItem(h.SignatureItemId);
                if (sig != null) CombatUiFactory.CreateLabel("Sig", card.Rect, _sprites.font, "Đồ chính: " + sig.NameVi, 4f, 104f, 142f, 24f, CombatUiFactory.Bone, TextAlignmentOptions.Top, wrap: true);
                var kit = new List<string>();
                foreach (string id in h.StartingItems)
                {
                    ItemDef d = _catalog?.FindItem(id);
                    if (d != null) kit.Add(d.NameVi);
                }

                card.Detail = h.NameVi + ": " + h.RoleVi + "\n" + (h.InnateItem != null ? h.InnateItem.NameVi + ": " + h.InnateItem.DescVi + "\n" : string.Empty) +
                              "Đồ khởi đầu: " + string.Join(", ", kit) + ".";
                _cards.Add(card);
            }

            SetDetail("Chọn người bước vào hầm ngục. Dược Sư mang Xúc Tác Dịch Hạch: kích nổ mọi debuff trên quái.");
            _confirmLabel.text = "Chọn";
            SetButtons(confirm: true, skip: false);
        }

        /// <summary>Shows a refusal line (e.g. no space for a pinned curse) without changing the step.</summary>
        public void ShowMessage(string text) => _message.text = text ?? string.Empty;

        // ------------------------------------------------------------------ door card face
        private void BuildDoorFace(RectTransform card, DoorCard door)
        {
            Pips(card, door.Tier, CardW / 2f, 8f);
            CombatUiFactory.CreateLabel("Tier", card, _sprites.font, TierName(door.Tier), 0f, 16f, CardW, 12f, TierColour(door.Tier), TextAlignmentOptions.Top);

            // the promise: certain buff
            float x = 10f;
            CombatUiFactory.CreateImage("Heart", card, _sprites.glyphHeartBone8, x, 36f, 8f, 8f);
            CombatUiFactory.CreateLabel("Hp", card, _sprites.font, "+" + door.Buff.Hp, x + 10f, 34f, 30f, 12f, CombatUiFactory.Bone);
            if (door.Buff.Cells > 0)
            {
                Sprite cell = Run("ui_glyph-cell_bone_8x8");
                if (cell != null) CombatUiFactory.CreateImage("Cell", card, cell, 46f, 36f, 8f, 8f);
                CombatUiFactory.CreateLabel("Cells", card, _sprites.font, "+" + door.Buff.Cells, 56f, 34f, 24f, 12f, CombatUiFactory.Bone);
            }

            // the price: danger and the class counts
            EncounterSpec e = door.Encounter;
            CombatUiFactory.CreateLabel("Danger", card, _sprites.font, "Nguy hiểm " + e.Danger, 0f, 48f, CardW, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            CombatUiFactory.CreateLabel("DangerWord", card, _sprites.font, DangerWord(e.Danger), 0f, 60f, CardW, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            float cx = 8f;
            Count(card, "ui_glyph-size-s_bone_10x10", e.SmallCount, ref cx);
            Count(card, "ui_glyph-size-m_bone_10x10", e.MediumCount, ref cx);
            Count(card, "ui_glyph-size-l_bone_10x10", e.LargeCount, ref cx);
            if (e.EliteCount > 0)
            {
                Sprite elite = Run("ui_glyph-elite_bone_6x6");
                if (elite != null) CombatUiFactory.CreateImage("Elite", card, elite, cx + 1f, 80f, 6f, 6f);
            }

            CombatUiFactory.CreateLabel("Quality", card, _sprites.font, "Đồ: " + QualityWord(door.Tier), 0f, 94f, CardW, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            if (door.Curse != null)
            {
                CurseTag(card, door.Curse, CardW - 30f, CardH - 16f);
                CombatUiFactory.CreateLabel("CurseName", card, _sprites.font, door.Curse.NameVi, 2f, 110f, CardW - 4f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            }
        }

        private void Count(RectTransform card, string glyph, int n, ref float x)
        {
            if (n <= 0) return;
            Sprite s = Run(glyph);
            if (s != null) CombatUiFactory.CreateImage("Size", card, s, x, 78f, 10f, 10f);
            CombatUiFactory.CreateLabel("N", card, _sprites.font, n.ToString(), x + 11f, 77f, 10f, 12f, CombatUiFactory.Bone);
            x += 24f;
        }

        private string DoorDetail(DoorCard door)
        {
            EncounterSpec e = door.Encounter;
            var roster = new List<string>();
            var counts = new Dictionary<string, int>();
            foreach (string id in e.Roster)
            {
                if (!counts.ContainsKey(id))
                {
                    counts[id] = 0;
                    roster.Add(id);
                }

                counts[id]++;
            }

            var parts = new List<string>();
            foreach (string id in roster)
            {
                MonsterDef m = _catalog?.FindMonster(id);
                parts.Add((counts[id] > 1 ? counts[id] + "× " : string.Empty) + (m != null ? m.NameVi : id) + (m != null ? " (" + RoleWord(m) + ")" : string.Empty));
            }

            string text = "Cửa " + TierName(door.Tier) + ": thắng phòng này nhận +" + door.Buff.Hp + " Máu tối đa" +
                          (door.Buff.Cells > 0 ? ", +" + door.Buff.Cells + " ô balo" : string.Empty) + ".\n" +
                          "Quái: " + string.Join(", ", parts) + ".";
            if (door.Curse != null) text += "\nNguyền " + door.Curse.NameVi + ": " + door.Curse.LineVi + ". Kéo dài cả lượt chơi.";
            return text;
        }

        // ------------------------------------------------------------------ cards and arming
        private Card NewCard(int index, Sprite frame, Action onClick)
        {
            float x = CardX0 + index * (CardW + CardGap);
            var card = new Card { Rect = CombatUiFactory.CreateRect("Card" + index, _content, x, CardY, CardW, CardH) };
            card.Ring = CombatUiFactory.CreateImage("Ring", card.Rect, Run("ui_door-card_ring-hover_9s"), -2f, -2f, CardW + 4f, CardH + 4f, sliced: true);
            card.Ring.enabled = false;
            Image bg = CombatUiFactory.CreateImage("Frame", card.Rect, frame, 0f, 0f, CardW, CardH, sliced: true, raycastTarget: true);
            Sprite slab = Run("ui_card-slab_default_9s");   // K7: the generated stone slab is the card body inside its tier edge
            if (slab != null) CombatUiFactory.CreateImage("Slab", card.Rect, slab, 3f, 3f, CardW - 6f, CardH - 6f, sliced: true);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick());
            var relay = bg.gameObject.AddComponent<HoverRelay>();
            relay.Enter = () => Hover(index);
            relay.Exit = () => Hover(-1);
            card.Corner = CombatUiFactory.CreateImage("Corner", card.Rect, Run("ui_door-card_armed-corner_4x4"), -3f, -3f, 4f, 4f);
            card.Corner.enabled = false;
            _cards.Add(card);
            return card;
        }

        private void Hover(int index)
        {
            _hover = index;
            RefreshRings();
            int show = index >= 0 ? index : _armed;
            if (Current == Step.Fusion) ShowFusionDetail(show);
            else if (show >= 0 && show < _cards.Count) _detail.text = _cards[show].Detail;
        }

        private void Arm(int index)
        {
            SetArmed(index);
            if (Current == Step.Fusion) ShowFusionDetail(index);
            else if (index >= 0 && index < _cards.Count) _detail.text = _cards[index].Detail;
        }

        private void SetArmed(int index)
        {
            _armed = index;
            RefreshRings();
            if (Current == Step.Doors) DoorArmed?.Invoke(index);
            if (Current == Step.Fusion) FusionArmed?.Invoke(index);
            if (_confirm != null) _confirm.interactable = Current == Step.Claim || (Current == Step.Fusion ? CanConfirmFusion : index >= 0);
        }

        private void RefreshRings()
        {
            Sprite armed = Run("ui_door-card_ring-armed_9s");
            Sprite hover = Run("ui_door-card_ring-hover_9s");
            for (int i = 0; i < _cards.Count; i++)
            {
                bool isArmed = i == _armed;
                _cards[i].Ring.enabled = isArmed || i == _hover;
                _cards[i].Ring.sprite = isArmed ? armed : hover;
                float grow = isArmed ? 3f : 2f;   // the hover ring sits 2 px outside the card, the armed ring 3 px
                RectTransform rr = _cards[i].Ring.rectTransform;
                Vector2 size = _cards[i].Rect.sizeDelta;
                rr.anchoredPosition = new Vector2(-grow, grow);
                rr.sizeDelta = new Vector2(size.x + grow * 2f, size.y + grow * 2f);
                _cards[i].Corner.enabled = isArmed;
            }
        }

        private void Confirm()
        {
            if (!Ready) return;
            switch (Current)
            {
                case Step.Claim:
                    ClaimConfirmed?.Invoke();
                    break;
                case Step.Reward:
                    if (_armed >= 0) RewardTaken?.Invoke(_armed);
                    break;
                case Step.Doors:
                    if (_armed >= 0) DoorChosen?.Invoke(_armed);
                    break;
                case Step.HeroPick:
                    if (_armed >= 0) HeroPicked?.Invoke(_armed);
                    break;
                case Step.Fusion:
                    ConfirmFusion();
                    break;
            }
        }

        private void Update()
        {
            if (!IsVisible || Current == Step.Hidden) return;
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool left = (kb != null && kb.leftArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.left.wasPressedThisFrame);
            bool right = (kb != null && kb.rightArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.right.wasPressedThisFrame);
            bool up = (kb != null && kb.upArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && kb.downArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.down.wasPressedThisFrame);
            bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool skip = (kb != null && kb.kKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            // the fusion book is two columns: left/right step one recipe, up/down step a row (item-fusion.md UI)
            int step = left ? -1 : right ? 1 : Current == Step.Fusion && up ? -2 : Current == Step.Fusion && down ? 2 : 0;
            if (_cards.Count > 0 && step != 0)
            {
                int next = _armed < 0 ? (step > 0 ? 0 : _cards.Count - 1) : _armed + step;
                if (next >= 0 && next < _cards.Count) Arm(next);
            }
            else if (confirm)
            {
                Confirm();
            }
            else if (skip && (Current == Step.Reward || Current == Step.Fusion))
            {
                Skip();
            }
        }

        // ------------------------------------------------------------------ helpers
        private void Begin(Step step, string title, int stepIndex)
        {
            Current = step;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            bool wasHidden = !_actions.gameObject.activeSelf;
            _actions.gameObject.SetActive(true);
            _actions.SetAsLastSibling();
            if (Feedback != null && wasHidden)
            {
                Feedback.SlideIn(_root, new Vector2(24f, 0f));   // U-04: the overlay slides in from the right
                if (step == Step.Claim || step == Step.Reward) Banner(title);
            }
            else if (Feedback != null) Feedback.SlideIn(_content, new Vector2(8f, 0f), 0.12f);  // U-05: the next step steps in
            for (int i = _content.childCount - 1; i >= 0; i--) CombatUiFactory.DestroySafe(_content.GetChild(i).gameObject);
            ClearFusionDetail();
            _cards.Clear();
            _hover = -1;
            _message.text = string.Empty;
            _title.text = string.IsNullOrEmpty(TitleNote) ? title : title + " · " + TitleNote;
            string[] names = step == Step.Fusion ? new[] { "Dung hợp", "Cửa" } : new[] { "Buff", "Đồ", "Cửa" };
            for (int i = 0; i < names.Length; i++) names[i] = i == stepIndex ? "<color=#EFC186>" + names[i] + "</color>" : names[i];
            _steps.text = string.Join(" · ", names);
            _readyAt = Time.unscaledTime + 0.25f;
            SetArmed(-1);
        }

        /// <summary>
        /// R2 / C-16: the victory banner grows from a 16 px strip to its full 240 px in 8 steps (240 ms), holds, then goes.
        /// Input is never blocked by it.
        /// </summary>
        private void Banner(string title)
        {
            Sprite board = Run("ui_banner-wood_default_9s");
            if (board == null || Feedback == null) return;
            const float w = 240f, h = 29f, cx = 147f + CardX0, y = 92f;
            Image img = CombatUiFactory.CreateImage("VictoryBanner", _root, board, cx - w / 2f, y, w, h, sliced: true);
            TextMeshProUGUI text = CombatUiFactory.CreateLabel("Text", img.transform, _sprites.font, title, 0f, 8f, w, 12f, CombatUiFactory.Candle, TextAlignmentOptions.Top);
            RectTransform rt = img.rectTransform;
            text.enabled = false;
            Feedback.Motion.Run(0.24f, u =>
            {
                if (rt == null) return;
                float width = Mathf.Round(Mathf.Lerp(16f, w, Mathf.Floor(u * 8f) / 8f));
                rt.sizeDelta = new Vector2(width, h);
                rt.anchoredPosition = new Vector2(cx - width / 2f, -y);
            }, () =>
            {
                if (text != null) text.enabled = true;
                Feedback.Motion.Run(0.9f, null, () => { if (img != null) CombatUiFactory.DestroySafe(img.gameObject); });
            });
        }

        private void SetButtons(bool confirm, bool skip)
        {
            _confirm.gameObject.SetActive(confirm);
            _skip.gameObject.SetActive(skip);
            _skipKey.gameObject.SetActive(skip);
        }

        private void SetDetail(string text) => _detail.text = text;

        private void Line(RectTransform parent, Sprite glyph, string text, float x, ref float y)
        {
            if (glyph != null) CombatUiFactory.CreateImage("Glyph", parent, glyph, x, y + 2f, 8f, 8f);
            CombatUiFactory.CreateLabel("Line", parent, _sprites.font, text, x + 12f, y, PanelW - 60f, 12f, CombatUiFactory.Bone);
            y += 16f;
        }

        private void Pips(RectTransform parent, TierKind tier, float centreX, float y)
        {
            Sprite pip = Run("ui_pip-rank_" + TierKey(tier) + "_5x5");
            if (pip == null) return;
            int n = (int)tier + 1;
            float x0 = Mathf.Floor(centreX - (n * 6f - 1f) / 2f);
            for (int i = 0; i < n; i++) CombatUiFactory.CreateImage("Pip", parent, pip, x0 + i * 6f, y - 4f, 5f, 5f);
        }

        private void CurseTag(RectTransform parent, CurseDef curse, float x, float y)
        {
            Sprite tag = Run("ui_curse-tag_card_24x10");
            if (tag != null) CombatUiFactory.CreateImage("CurseTag", parent, tag, x, y, 24f, 10f);
            Sprite glyph = Run(CurseGlyph(curse.Kind, 8));
            if (glyph != null) CombatUiFactory.CreateImage("CurseGlyph", parent, glyph, x + 10f, y + 1f, 8f, 8f);
        }

        private Sprite Run(string key) => _sprites != null ? _sprites.GetRunSprite(key) : null;

        private Sprite DoorFrame(TierKind tier) => Run("ui_door-card_" + TierKey(tier) + "_9s") ?? _sprites.panelDefault;

        /// <summary>The curse glyph sprite key for a kind at 12 or 8 px.</summary>
        public static string CurseGlyph(CurseKind kind, int size) =>
            "ui_glyph-curse-" + kind.ToString().ToLowerInvariant() + "_bone_" + size + "x" + size;

        public static string TierKey(TierKind tier) => tier.ToString().ToLowerInvariant();

        public static string TierName(TierKind tier)
        {
            switch (tier)
            {
                case TierKind.White: return "Trắng";
                case TierKind.Green: return "Xanh lá";
                case TierKind.Blue: return "Xanh dương";
                case TierKind.Purple: return "Tím";
                case TierKind.Gold: return "Vàng";
                default: return "Đỏ";
            }
        }

        private static Color32 TierColour(TierKind tier)
        {
            switch (tier)
            {
                case TierKind.Green: return new Color32(0x6F, 0xB0, 0x7A, 0xFF);
                case TierKind.Blue: return new Color32(0x6F, 0x9F, 0xD8, 0xFF);
                case TierKind.Purple: return new Color32(0xA9, 0x86, 0xD6, 0xFF);
                case TierKind.Gold: return CombatUiFactory.Candle;
                default: return CombatUiFactory.Bone;
            }
        }

        /// <summary>The danger word for 1..5 (System 16 Rule 10; wording TEMPORARY).</summary>
        public static string DangerWord(int danger)
        {
            switch (danger)
            {
                case 1: return "Dễ";
                case 2: return "Vừa";
                case 3: return "Khó";
                case 4: return "Rất khó";
                default: return "Chí mạng";
            }
        }

        /// <summary>A rank of the offer, never an item rarity (System 17 Rule 10; wording TEMPORARY).</summary>
        private static string QualityWord(TierKind tier)
        {
            switch (tier)
            {
                case TierKind.White: return "thường";
                case TierKind.Green: return "khá";
                case TierKind.Blue: return "tốt";
                default: return "tốt nhất";
            }
        }

        private static string RoleWord(MonsterDef m)
        {
            bool multi = false, sweep = false, windup = false;
            foreach (MonsterMoveDef mv in m.Moves)
            {
                multi |= mv.HitCount > 1;
                sweep |= mv.TargetMode == MoveTargetMode.AllHeroes;
                windup |= mv.Glyph == MoveGlyphKind.Windup;
            }

            string role = windup ? "gồng rồi đập" : sweep ? "quét cả đội" : multi ? "đánh nhiều lần" : "đánh thường";
            return m.Classification == MonsterClassKind.Elite ? "tinh anh, " + role : role;
        }

        public static string Describe(ItemDef def)
        {
            var parts = new List<string>();
            foreach (EffectDef e in def.Effects)
            {
                switch (e.Kind)
                {
                    case EffectKind.Damage: parts.Add("Sát thương " + e.Base + (e.Target == TargetKind.AllEnemies ? " mọi quái" : string.Empty)); break;
                    case EffectKind.ShieldGain: parts.Add("Giáp +" + e.Amount); break;
                    case EffectKind.Heal: parts.Add("Hồi " + e.Amount); break;
                    case EffectKind.ApplyStatus: parts.Add(Project05.Items.ItemTooltipText.StatusName(e.Status) + " " + e.Amount + (e.Target == TargetKind.AllEnemies ? " mọi quái" : string.Empty)); break;
                    case EffectKind.ConsumeDetonate: parts.Add("Kích nổ"); break;
                    case EffectKind.DetonateAll: parts.Add("Kích nổ mọi debuff"); break;
                }
            }

            if (def.Trigger != null && def.Trigger.Kind == TriggerKind.Cooldown) parts.Add("hồi " + def.Trigger.N + " lượt");
            if (def.HpBonus != 0) parts.Add("Máu +" + def.HpBonus);
            if (def.SpeedBonus != 0) parts.Add("Tốc độ " + (def.SpeedBonus > 0 ? "+" : string.Empty) + def.SpeedBonus);
            if (def.Trigger != null && def.Trigger.Kind == TriggerKind.Passive && def.Synergy.Count > 0) parts.Add("Bị động: cộng hưởng món kề bên");
            return parts.Count > 0 ? string.Join(", ", parts) : "Bị động";
        }
    }
}
