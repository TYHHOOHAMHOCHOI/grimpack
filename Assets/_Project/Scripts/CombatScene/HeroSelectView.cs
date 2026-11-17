using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI;
using Project05.UI.Motion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// The full-screen hero pick after Play (design/ux/hero-select.md): a hero on a pedestal, an info panel with the role,
    /// stats, innate ability, the 3 starting items and the core item, a row of hero cards, Back and Confirm. Built at
    /// runtime from the generated UI kit (design/art/ui-kit-generated.md). Display and input only: it raises
    /// <see cref="Picked"/>, the scene does the rest.
    /// TEMPORARY: Vietnamese literals until the localization system exists.
    /// </summary>
    public sealed class HeroSelectView : MonoBehaviour
    {
        private sealed class Card
        {
            public RectTransform Rect;
            public Image Ring;
            public Image Corner;
        }

        private sealed class Relay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            public Action Enter;
            public Action Exit;
            public Action<int> Click;

            public void OnPointerEnter(PointerEventData e) => Enter?.Invoke();
            public void OnPointerExit(PointerEventData e) => Exit?.Invoke();
            public void OnPointerClick(PointerEventData e) => Click?.Invoke(e.clickCount);
        }

        private const float InputLock = 0.25f;

        private CombatSceneSpriteCatalog _sprites;
        private GameCatalog _catalog;
        private UiMotion _motion;
        private Func<HeroDef, Sprite> _portrait;
        private RectTransform _root;
        private CanvasGroup _group;
        private Image _hero;
        private RectTransform _heroRect;
        private TextMeshProUGUI _name, _role, _stats, _innateHead, _innate, _coreHead, _coreName, _coreText;
        private RectTransform _kit;
        private RectTransform _arrowL, _arrowR;
        private readonly List<Card> _cards = new List<Card>();
        private IReadOnlyList<HeroDef> _heroes = Array.Empty<HeroDef>();
        private int _selected;
        private int _hover = -1;
        private float _readyAt;
        private bool _committing;

        /// <summary>Raised with the chosen hero's index when Confirm is accepted.</summary>
        public event Action<int> Picked;

        /// <summary>True while the screen is shown.</summary>
        public bool IsVisible => _root != null && _root.gameObject.activeSelf;

        /// <summary>The selected hero index.</summary>
        public int Selected => _selected;

        /// <summary>Builds the screen (hidden) on the safe frame.</summary>
        public void Initialize(RectTransform safeFrame, CombatSceneSpriteCatalog sprites, GameCatalog catalog, UiMotion motion)
        {
            _sprites = sprites;
            _catalog = catalog;
            _motion = motion;
            TMP_FontAsset font = sprites.font;
            _root = CombatUiFactory.CreateRect("HeroSelect", safeFrame, 0f, 0f, 640f, 360f);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            Image back = CombatUiFactory.CreateImage("Backdrop", _root, Gen("env_heroselect_bg_640x360"), 0f, 0f, 640f, 360f, raycastTarget: true);
            back.color = Color.white;

            // H2 title, H7 pedestal, H6 hero, H8 name
            Image title = CombatUiFactory.CreateImage("Title", _root, Gen("ui_board-wood_default_9s"), 232f, 7f, 176f, 19f, sliced: true);
            CombatUiFactory.CreateLabel("Text", title.transform, font, "Chọn nhân vật", 0f, 3f, 176f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            CombatUiFactory.CreateImage("Pedestal", _root, Gen("prop_plinth_base_109x21"), 105f, 180f);
            _hero = CombatUiFactory.CreateImage("Hero", _root, null, 112f, 94f, 96f, 96f);
            _heroRect = _hero.rectTransform;
            Image nameBoard = CombatUiFactory.CreateImage("NameBoard", _root, Gen("ui_board-wood_default_9s"), 64f, 213f, 192f, 19f, sliced: true);
            _name = CombatUiFactory.CreateLabel("Name", nameBoard.transform, font, string.Empty, 0f, 3f, 192f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);

            // H4 / H5 arrows
            _arrowL = Arrow("Prev", 40f, "<", -1);
            _arrowR = Arrow("Next", 264f, ">", 1);

            // H9 info panel and its rows
            CombatUiFactory.CreateImage("Info", _root, Gen("ui_panel-wood_default_9s"), 316f, 34f, 312f, 216f, sliced: true);
            _role = CombatUiFactory.CreateLabel("Role", _root, font, string.Empty, 324f, 42f, 296f, 12f, CombatUiFactory.Fog);
            _stats = CombatUiFactory.CreateLabel("Stats", _root, font, string.Empty, 324f, 58f, 296f, 12f, CombatUiFactory.Bone);
            CombatUiFactory.CreateImage("Divider1", _root, Gen("ui_divider-wood_default_9s"), 324f, 80f, 296f, 3f, sliced: true);
            _innateHead = CombatUiFactory.CreateLabel("InnateHead", _root, font, string.Empty, 324f, 88f, 296f, 12f, CombatUiFactory.Bone);
            _innate = CombatUiFactory.CreateLabel("Innate", _root, font, string.Empty, 324f, 102f, 296f, 42f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            CombatUiFactory.CreateImage("Divider2", _root, Gen("ui_divider-wood_default_9s"), 324f, 148f, 296f, 3f, sliced: true);
            CombatUiFactory.CreateLabel("KitHead", _root, font, "Đồ khởi đầu", 324f, 156f, 140f, 12f, CombatUiFactory.Fog);
            CombatUiFactory.CreateImage("KitTray", _root, Gen("ui_tray-leather_default_9s"), 324f, 170f, 140f, 72f, sliced: true);
            _kit = CombatUiFactory.CreateRect("Kit", _root, 328f, 174f, 132f, 64f);
            _coreHead = CombatUiFactory.CreateLabel("CoreHead", _root, font, "Đồ chủ lực", 476f, 156f, 144f, 12f, CombatUiFactory.Fog);
            _coreName = CombatUiFactory.CreateLabel("CoreName", _root, font, string.Empty, 476f, 172f, 144f, 12f, CombatUiFactory.Candle);
            _coreText = CombatUiFactory.CreateLabel("CoreText", _root, font, string.Empty, 476f, 188f, 144f, 56f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);

            // H23 to H26
            Image esc = CombatUiFactory.CreateImage("EscChip", _root, Gen("ui_plate-iron_default_9s"), 12f, 334f, 28f, 14f, sliced: true);
            CombatUiFactory.CreateLabel("Text", esc.transform, font, "Esc", 0f, 1f, 28f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            Button backBtn = Board("Back", 44f, 331f, 88f, "Quay lại", false);
            backBtn.onClick.AddListener(Back);
            Image enter = CombatUiFactory.CreateImage("EnterChip", _root, Gen("ui_plate-iron_default_9s"), 432f, 334f, 48f, 14f, sliced: true);
            CombatUiFactory.CreateLabel("Text", enter.transform, font, "Enter", 0f, 1f, 48f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
            Button confirm = Board("Confirm", 484f, 331f, 144f, "Vào hầm ngục", true);
            confirm.onClick.AddListener(Confirm);

            _root.gameObject.SetActive(false);
        }

        /// <summary>Shows the screen with the pickable heroes; slot <paramref name="preselect"/> starts selected.</summary>
        public void Show(IReadOnlyList<HeroDef> heroes, Func<HeroDef, Sprite> portrait, int preselect = 0)
        {
            _heroes = heroes ?? Array.Empty<HeroDef>();
            _portrait = portrait;
            _committing = false;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            BuildCards();
            _selected = Mathf.Clamp(preselect, 0, Mathf.Max(0, _heroes.Count - 1));
            _arrowL.gameObject.SetActive(_heroes.Count > 1);
            _arrowR.gameObject.SetActive(_heroes.Count > 1);
            Apply(0);
            _readyAt = Time.unscaledTime + InputLock;

            // HS-01 screen enter: the hall rises out of ink in 4 steps, the hero steps onto the pedestal
            _group.alpha = 0f;
            _motion?.Run(0.3f, u => _group.alpha = Mathf.Floor(u * 4f) / 4f, () => _group.alpha = 1f);
            if (_motion != null) _motion.MoveFrom(_heroRect, new Vector2(112f, -90f), new Vector2(112f, -94f), 0.25f, Ease.OutQuad);
        }

        /// <summary>Hides the screen at once.</summary>
        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>Selects hero i (keyboard, card click, arrows and tests).</summary>
        public void Select(int index)
        {
            if (_heroes.Count == 0 || _committing) return;
            index = ((index % _heroes.Count) + _heroes.Count) % _heroes.Count;
            if (index == _selected) return;
            int dir = index > _selected ? 1 : -1;
            _selected = index;
            Apply(dir);
        }

        /// <summary>Confirms the selected hero (Enter, the Confirm button, a double-click and tests).</summary>
        public void Confirm()
        {
            if (_committing || _heroes.Count == 0 || (Application.isPlaying && Time.unscaledTime < _readyAt)) return;
            _committing = true;
            int pick = _selected;
            // HS-10 / HS-11: the other cards fall to ink, the hero steps forward, then the hall fades into planning
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i == pick) continue;
                var cg = _cards[i].Rect.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = _cards[i].Rect.gameObject.AddComponent<CanvasGroup>();   // Unity null: no ?? on components
                cg.alpha = 0.35f;
            }

            if (_motion == null)
            {
                Finish(pick);
                return;
            }

            _motion.MoveFrom(_heroRect, new Vector2(112f, -94f), new Vector2(116f, -94f), 0.1f, Ease.OutQuad);
            _motion.Run(0.35f, u => _group.alpha = 1f - Mathf.Floor(u * 4f) / 4f, () => Finish(pick));
        }

        private void Finish(int pick)
        {
            Hide();
            _group.alpha = 1f;
            Picked?.Invoke(pick);
        }

        private void Back()
        {
            if (_committing || Time.unscaledTime < _readyAt) return;
            // Esc / Back returns to the Title when it is in the build (hero-select.md H24); nothing was committed
            if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Title.unity") >= 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/_Project/Scenes/Title.unity");
        }

        private void Update()
        {
            if (!IsVisible || _committing) return;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) Select(_selected - 1);
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) Select(_selected + 1);
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Confirm();
                else if (kb.escapeKey.wasPressedThisFrame) Back();
                else
                {
                    for (int i = 0; i < _heroes.Count && i < 4; i++)
                    {
                        if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Select(i);
                    }
                }
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame) Select(_selected - 1);
                else if (pad.dpad.right.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame) Select(_selected + 1);
                else if (pad.buttonSouth.wasPressedThisFrame) Confirm();
                else if (pad.buttonEast.wasPressedThisFrame) Back();
            }

            // HS-02 hero idle: breathing only, one pixel every half second (no tilt)
            if (!UiMotion.Reduced && _motion != null && _motion.Running == 0)
            {
                float y = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.6f ? -94f : -93f;
                _heroRect.anchoredPosition = new Vector2(112f, y);
            }
        }

        // ------------------------------------------------------------------ content
        private void Apply(int dir)
        {
            if (_heroes.Count == 0) return;
            HeroDef h = _heroes[_selected];
            Sprite showcase = Gen("char_" + (h.Art == "alchemist" ? "alchemist" : "tank") + "_showcase_96");
            _hero.sprite = showcase != null ? showcase : _portrait?.Invoke(h);
            _name.text = h.NameVi;
            _role.text = h.RoleVi;
            _stats.text = "Máu " + h.BaseMaxHp + "    Tốc độ " + h.BaseSpeed + (h.InitialShield > 0 ? "    Giáp " + h.InitialShield : string.Empty);
            _innateHead.text = h.InnateItem != null ? "<color=#8795B3>Nội tại:</color> " + h.InnateItem.NameVi : string.Empty;
            _innate.text = h.InnateItem != null ? h.InnateItem.DescVi : string.Empty;
            ShowItem(_catalog?.FindItem(h.SignatureItemId), core: true);
            BuildKit(h);
            RefreshCards();

            // HS-05 showcase swap: the incoming hero slides in on the pedestal (never blocks input)
            if (dir != 0 && _motion != null)
                _motion.MoveFrom(_heroRect, new Vector2(112f + 8f * dir, -94f), new Vector2(112f, -94f), 0.12f, Ease.OutQuad);
        }

        private void ShowItem(ItemDef def, bool core)
        {
            _coreHead.text = core ? "Đồ chủ lực" : "Món đồ";
            _coreName.text = def != null ? def.NameVi : string.Empty;
            _coreText.text = def != null ? RunPanelView.Describe(def) : string.Empty;
        }

        private void BuildKit(HeroDef h)
        {
            for (int i = _kit.childCount - 1; i >= 0; i--) CombatUiFactory.DestroySafe(_kit.GetChild(i).gameObject);
            var ids = new List<string>(h.StartingItems);
            int core = ids.IndexOf(h.SignatureItemId);
            if (core > 0)
            {
                ids.RemoveAt(core);
                ids.Insert(0, h.SignatureItemId);   // core item first (H17)
            }

            float x = 0f;
            for (int i = 0; i < ids.Count; i++)
            {
                ItemDef def = _catalog?.FindItem(ids[i]);
                if (def == null) continue;
                string footprint = Footprint(def);
                Sprite plate = _sprites.GetAuthoredPlate(def.Tier.ToString().ToLowerInvariant(), false, footprint);
                Sprite icon = _sprites.GetItemIcon(def.Art, footprint, compact: false);
                if (plate == null) continue;
                Image img = CombatUiFactory.CreateImage("Plate" + i, _kit, plate, x, 0f, raycastTarget: true);
                if (icon != null) CombatUiFactory.CreateImage("Art", img.transform, icon, 2f, 2f);
                ItemDef captured = def;
                var relay = img.gameObject.AddComponent<Relay>();
                relay.Enter = () => ShowItem(captured, captured.Id == h.SignatureItemId);
                relay.Exit = () => ShowItem(_catalog?.FindItem(h.SignatureItemId), core: true);
                if (def.Id == h.SignatureItemId)
                    CombatUiFactory.CreateImage("CoreBar", _kit, _sprites.fillBone, x, 62f, plate.rect.width, 2f);   // K16 core marker

                // HS-07 kit seat: the plates set down in order, core first
                if (_motion != null)
                    _motion.MoveFrom(img.rectTransform, new Vector2(x, 4f), new Vector2(x, 0f), 0.08f + 0.05f * i, Ease.OutQuad);
                x += plate.rect.width + 6f;
            }
        }

        private static string Footprint(ItemDef def)
        {
            int w = 0, h = 0;
            foreach (GridCell c in def.Cells)
            {
                w = Mathf.Max(w, c.X + 1);
                h = Mathf.Max(h, c.Y + 1);
            }

            return string.IsNullOrEmpty(def.Footprint) ? w + "x" + h : def.Footprint;
        }

        // ------------------------------------------------------------------ cards
        private void BuildCards()
        {
            foreach (Card c in _cards) CombatUiFactory.DestroySafe(c.Rect.gameObject);
            _cards.Clear();
            int n = _heroes.Count;
            float x0 = 320f - (64f * n + 8f * (n - 1)) / 2f;   // H22
            for (int i = 0; i < n; i++)
            {
                int index = i;
                var card = new Card { Rect = CombatUiFactory.CreateRect("Card" + i, _root, x0 + 72f * i, 258f, 64f, 60f) };
                card.Ring = CombatUiFactory.CreateImage("Ring", card.Rect, Gen("ui_ring-gen_focus_9s"), -3f, -3f, 70f, 66f, sliced: true);
                Image frame = CombatUiFactory.CreateImage("Frame", card.Rect, Gen("ui_panel-wood_default_9s"), 0f, 0f, 64f, 60f, sliced: true, raycastTarget: true);
                Sprite face = _portrait?.Invoke(_heroes[i]);
                if (face != null) CombatUiFactory.CreateImage("Portrait", card.Rect, face, 8f, 6f, 48f, 48f);
                Image chip = CombatUiFactory.CreateImage("Key", card.Rect, Gen("ui_plate-iron_default_9s"), 3f, 3f, 12f, 12f, sliced: true);
                CombatUiFactory.CreateLabel("Digit", chip.transform, _sprites.font, (i + 1).ToString(), 0f, 0f, 12f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
                card.Corner = CombatUiFactory.CreateImage("Corner", card.Rect, _sprites.fillCandle, -3f, -3f, 4f, 4f);
                var relay = frame.gameObject.AddComponent<Relay>();
                relay.Enter = () => { _hover = index; RefreshCards(); };
                relay.Exit = () => { if (_hover == index) _hover = -1; RefreshCards(); };
                relay.Click = clicks =>
                {
                    Select(index);
                    if (clicks >= 2) Confirm();
                };
                _cards.Add(card);
            }
        }

        private void RefreshCards()
        {
            Sprite hover = Gen("ui_ring-gen_hover_9s"), focus = Gen("ui_ring-gen_focus_9s");
            for (int i = 0; i < _cards.Count; i++)
            {
                bool sel = i == _selected;
                _cards[i].Ring.enabled = sel || i == _hover;
                _cards[i].Ring.sprite = sel ? focus : hover;
                _cards[i].Corner.enabled = sel;   // selection is form too: a 4 x 4 corner square (greyscale-safe)
            }
        }

        // ------------------------------------------------------------------ helpers
        private RectTransform Arrow(string name, float x, string glyph, int dir)
        {
            Image img = CombatUiFactory.CreateImage(name, _root, Gen("ui_btn-mini_default_9s"), x, 124f, 16f, 24f, sliced: true, raycastTarget: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { highlightedSprite = Gen("ui_btn-mini_hover_9s"), pressedSprite = Gen("ui_btn-mini_pressed_9s"), selectedSprite = Gen("ui_btn-mini_hover_9s") };
            btn.onClick.AddListener(() => Select(_selected + dir));
            CombatUiFactory.CreateLabel("Glyph", img.transform, _sprites.font, glyph, 0f, 6f, 16f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            return img.rectTransform;
        }

        private Button Board(string name, float x, float y, float w, string label, bool primary)
        {
            Image img = CombatUiFactory.CreateImage(name, _root, Gen(primary ? "ui_board-wood_primary_9s" : "ui_board-wood_default_9s"), x, y, w, 19f, sliced: true, raycastTarget: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = Gen("ui_board-wood_hover_9s"),
                pressedSprite = Gen("ui_board-wood_pressed_9s"),
                selectedSprite = Gen("ui_board-wood_hover_9s"),
                disabledSprite = Gen("ui_board-wood_disabled_9s"),
            };
            CombatUiFactory.CreateLabel("Label", img.transform, _sprites.font, label, 0f, 4f, w, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            return btn;
        }

        private Sprite Gen(string key) => _sprites != null ? _sprites.GetRunSprite(key) : null;
    }
}
