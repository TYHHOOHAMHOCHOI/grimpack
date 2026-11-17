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
    /// Display only: it shows what the run session offers and raises commands; it decides nothing.
    /// Vietnamese literals are TEMPORARY until the localisation pass (System 7).
    /// </summary>
    public sealed class RunPanelView : MonoBehaviour
    {
        /// <summary>Which step the panel shows.</summary>
        public enum Step { Hidden, Claim, Reward, Doors, HeroPick }

        private const float PanelX = 315f, PanelY = 19f, PanelW = 322f, PanelH = 314f;
        private const float CardW = 90f, CardH = 140f, CardGap = 20f, CardX0 = 6f, CardY = 24f;
        private const float DetailY = 170f, DetailH = 108f;

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
        private TextMeshProUGUI _confirmLabel;
        private readonly List<Card> _cards = new List<Card>();
        private int _armed = -1;
        private int _hover = -1;
        private float _readyAt;
        private IReadOnlyList<DoorCard> _doors;
        private IReadOnlyList<string> _offer;

        public Step Current { get; private set; } = Step.Hidden;
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
            CombatUiFactory.CreateImage("Bg", _root, sprites.panelVoid, 0f, 0f, PanelW, PanelH, sliced: true, raycastTarget: true);
            _title = CombatUiFactory.CreateLabel("Title", _root, sprites.font, string.Empty, 8f, 6f, 200f, 12f, CombatUiFactory.Bone);
            _steps = CombatUiFactory.CreateLabel("Steps", _root, sprites.font, string.Empty, 170f, 6f, 146f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.TopRight);
            _content = CombatUiFactory.CreateRect("Content", _root, 0f, 0f, PanelW, PanelH);
            CombatUiFactory.CreateImage("DetailBg", _root, sprites.panelDefault, 6f, DetailY, PanelW - 12f, DetailH, sliced: true);
            _detail = CombatUiFactory.CreateLabel("Detail", _root, sprites.font, string.Empty, 12f, DetailY + 6f, PanelW - 24f, DetailH - 12f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            _message = CombatUiFactory.CreateLabel("Message", _root, sprites.font, string.Empty, 8f, 290f, 150f, 12f, CombatUiFactory.Blood, TextAlignmentOptions.TopLeft, wrap: true);
            _skip = CombatUiFactory.CreateSegmentButton("Skip", _root, sprites, "Bỏ qua", 166f, 286f, 64f, 20f);
            _skip.onClick.AddListener(() => { if (Current == Step.Reward && Ready) RewardSkipped?.Invoke(); });
            _confirm = CombatUiFactory.CreateSegmentButton("Confirm", _root, sprites, "Xác nhận", 236f, 286f, 80f, 20f, selected: true);
            _confirmLabel = _confirm.GetComponentInChildren<TextMeshProUGUI>(true);
            _confirm.onClick.AddListener(Confirm);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Commit presses are ignored for a moment after a step appears (System 28 input lock).</summary>
        private bool Ready => Time.unscaledTime >= _readyAt;

        public void Hide()
        {
            Current = Step.Hidden;
            if (_root != null) _root.gameObject.SetActive(false);
            SetArmed(-1);
        }

        // ------------------------------------------------------------------ steps
        /// <summary>BuffClaim: the door's certain buff, and a Gold door's curse, go to the one hero.</summary>
        public void ShowClaim(int room, TierKind tier, DoorBuff buff, int hpNow, int cellsNow, CurseDef curse, string heroName)
        {
            Begin(Step.Claim, $"Thắng phòng {room}", 0);
            var box = CombatUiFactory.CreateRect("ClaimBox", _content, 6f, CardY, PanelW - 12f, CardH);
            CombatUiFactory.CreateImage("Frame", box, DoorFrame(tier), 0f, 0f, PanelW - 12f, CardH, sliced: true);
            Pips(box, tier, (PanelW - 12f) / 2f, 8f);
            CombatUiFactory.CreateLabel("Head", box, _sprites.font, "Phần thưởng cửa " + TierName(tier), 0f, 18f, PanelW - 12f, 12f, CombatUiFactory.Candle, TextAlignmentOptions.Top);
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
                CombatUiFactory.CreateLabel("CurseLine", box, _sprites.font, curse.NameVi + ": " + curse.LineVi, 64f, y - 1f, PanelW - 90f, 12f, CombatUiFactory.Bone);
                y += 14f;
                CombatUiFactory.CreateLabel("Lasts", box, _sprites.font, "Nguyền kéo dài cả lượt chơi.", 64f, y, PanelW - 90f, 12f, CombatUiFactory.Fog);
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
                Card card = NewCard(i, DoorFrame(door.Tier), () => Arm(index));
                BuildDoorFace(card.Rect, door);
                card.Detail = DoorDetail(door);
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

        /// <summary>Skips the item shelf, without the input lock (tests and automation).</summary>
        public void CommandSkip()
        {
            if (Current == Step.Reward) RewardSkipped?.Invoke();
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
            if (show >= 0 && show < _cards.Count) _detail.text = _cards[show].Detail;
        }

        private void Arm(int index)
        {
            SetArmed(index);
            if (index >= 0 && index < _cards.Count) _detail.text = _cards[index].Detail;
        }

        private void SetArmed(int index)
        {
            _armed = index;
            RefreshRings();
            if (Current == Step.Doors) DoorArmed?.Invoke(index);
            if (_confirm != null) _confirm.interactable = Current == Step.Claim || index >= 0;
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
                rr.anchoredPosition = new Vector2(-grow, grow);
                rr.sizeDelta = new Vector2(CardW + grow * 2f, CardH + grow * 2f);
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
            }
        }

        private void Update()
        {
            if (!IsVisible || Current == Step.Hidden) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (_cards.Count > 0 && (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame))
            {
                int dir = kb.rightArrowKey.wasPressedThisFrame ? 1 : -1;
                int next = _armed < 0 ? (dir > 0 ? 0 : _cards.Count - 1) : Mathf.Clamp(_armed + dir, 0, _cards.Count - 1);
                Arm(next);
            }
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                Confirm();
            }
        }

        // ------------------------------------------------------------------ helpers
        private void Begin(Step step, string title, int stepIndex)
        {
            Current = step;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            for (int i = _content.childCount - 1; i >= 0; i--) CombatUiFactory.DestroySafe(_content.GetChild(i).gameObject);
            _cards.Clear();
            _hover = -1;
            _message.text = string.Empty;
            _title.text = string.IsNullOrEmpty(TitleNote) ? title : title + " · " + TitleNote;
            string[] names = { "Buff", "Đồ", "Cửa" };
            for (int i = 0; i < names.Length; i++) names[i] = i == stepIndex ? "<color=#EFC186>" + names[i] + "</color>" : names[i];
            _steps.text = string.Join(" · ", names);
            _readyAt = Time.unscaledTime + 0.25f;
            SetArmed(-1);
        }

        private void SetButtons(bool confirm, bool skip)
        {
            _confirm.gameObject.SetActive(confirm);
            _skip.gameObject.SetActive(skip);
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

        private static string Describe(ItemDef def)
        {
            var parts = new List<string>();
            foreach (EffectDef e in def.Effects)
            {
                switch (e.Kind)
                {
                    case EffectKind.Damage: parts.Add("Sát thương " + e.Base); break;
                    case EffectKind.ShieldGain: parts.Add("Giáp +" + e.Amount); break;
                    case EffectKind.Heal: parts.Add("Hồi " + e.Amount); break;
                    case EffectKind.ApplyStatus: parts.Add("Ấn " + e.Amount); break;
                    case EffectKind.ConsumeDetonate: parts.Add("Kích nổ Ấn"); break;
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
