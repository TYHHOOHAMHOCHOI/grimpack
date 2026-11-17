using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>One recipe as the fusion room shows it (item-fusion.md UI), built by the scene from FusionRules.Plan.</summary>
    public sealed class FusionRow
    {
        public RecipeDef Recipe;
        public ItemDef Result;
        public ItemDef[] Ingredients = Array.Empty<ItemDef>();
        /// <summary>Per ingredient: an instance is owned (grid or stash).</summary>
        public bool[] Owned = Array.Empty<bool>();
        public FusionStatus Status;
        public int Cost;
        public int Shortfall;
        /// <summary>Names of the + items the fusion would consume (their upgrade is lost, Rule 9).</summary>
        public string[] LosesUpgrade = Array.Empty<string>();

        public bool Makeable => Status == FusionStatus.Ok;
    }

    /// <summary>
    /// The Fusion Door's card face and the fusion room's step (design/gdd/item-fusion.md, "Fusion Door and Fusion Room
    /// UI"): a two-column recipe book over the enemy zone, a detail plate for the armed recipe, "Dung hợp" and "Rời lò"
    /// in the action strip. Display only: the scene plans and applies the fusion.
    /// </summary>
    public sealed partial class RunPanelView
    {
        private const float RowW = 144f, RowH = 22f, RowGap = 2f, ColGap = 6f, RowY0 = 30f;

        /// <summary>The armed recipe changed (index into the rows, or -1): the scene marks its ingredients.</summary>
        public event Action<int> FusionArmed;
        /// <summary>"Dung hợp" was pressed on a makeable recipe.</summary>
        public event Action<int> FusionConfirmed;
        /// <summary>"Rời lò" was pressed.</summary>
        public event Action FusionLeft;

        /// <summary>The recipe rows, re-read on every refresh (the scene plans them against the board and the gold).</summary>
        public Func<IReadOnlyList<FusionRow>> FusionRows { get; set; }

        /// <summary>The gold held, for the cost line.</summary>
        public Func<int> Gold { get; set; }

        /// <summary>Recipes makeable now, for the fusion door card ("Làm được: N").</summary>
        public Func<int> MakeableCount { get; set; }

        /// <summary>True once the visit's one fusion is made: the book is read-only.</summary>
        public bool FusionLocked { get; private set; }

        /// <summary>The recipe rows shown (tests).</summary>
        public IReadOnlyList<FusionRow> ShownRows => _rows;

        private IReadOnlyList<FusionRow> _rows = Array.Empty<FusionRow>();
        private readonly List<GameObject> _detailParts = new List<GameObject>();
        private readonly List<TextMeshProUGUI> _rowNames = new List<TextMeshProUGUI>();
        private readonly List<Image> _rowDims = new List<Image>();
        private string _madeRecipe = string.Empty;

        private Sprite FusionFrame => Run("ui_plate-iron_default_9s") ?? _sprites.panelDefault;

        private const string FusionDoorDetail =
            "Lò Dung Hợp: phòng không có quái. Không vàng, không buff, không món thưởng; vẫn tính là 1 phòng.\n" +
            "Dung hợp 1 lần: 2 món 6 vàng, 3 món 10 vàng. Món mới mạnh hơn và chiếm ít ô hơn.";

        // ------------------------------------------------------------------ the door card
        private void BuildFusionDoorFace(RectTransform card)
        {
            Sprite emblem = Run("ui_emblem-crucible_32x32");
            if (emblem != null) CombatUiFactory.CreateImage("Emblem", card, emblem, Mathf.Floor((CardW - 32f) / 2f), 10f, 32f, 32f);
            CombatUiFactory.CreateLabel("Title", card, _sprites.font, "Lò Dung Hợp", 0f, 46f, CardW, 12f, CombatUiFactory.Candle, TextAlignmentOptions.Top);
            CombatUiFactory.CreateLabel("NoFight", card, _sprites.font, "Không có quái", 0f, 62f, CardW, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            CombatUiFactory.CreateLabel("Once", card, _sprites.font, "Dung hợp 1 lần", 0f, 76f, CardW, 12f, CombatUiFactory.Bone, TextAlignmentOptions.Top);
            int n = MakeableCount != null ? MakeableCount() : 0;
            CombatUiFactory.CreateLabel("Makeable", card, _sprites.font, "Làm được: " + n, 0f, 94f, CardW, 12f,
                n > 0 ? CombatUiFactory.Candle : CombatUiFactory.Fog, TextAlignmentOptions.Top);
            CombatUiFactory.CreateLabel("Cost", card, _sprites.font, "Giá 6 / 10 vàng", 0f, 110f, CardW, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
        }

        // ------------------------------------------------------------------ the fusion room
        /// <summary>The fusion room's step: the recipe book, the detail plate, "Dung hợp" and "Rời lò".</summary>
        public void ShowFusion(int room)
        {
            Begin(Step.Fusion, "Lò Dung Hợp · Phòng " + room, 0);
            FusionLocked = false;
            _madeRecipe = string.Empty;
            BuildFusionRows();
            _confirmLabel.text = "Dung hợp";
            if (_skipLabel != null) _skipLabel.text = "Rời lò";
            SetButtons(confirm: true, skip: true);
            int first = -1;
            for (int i = 0; i < _rows.Count && first < 0; i++)
            {
                if (_rows[i].Makeable) first = i;
            }

            if (first >= 0) Arm(first);
            else ShowFusionDetail(-1);
        }

        /// <summary>Re-reads the rows (the board or the gold changed) and redraws states and the detail plate.</summary>
        public void RefreshFusion()
        {
            if (Current != Step.Fusion) return;
            _rows = FusionRows?.Invoke() ?? Array.Empty<FusionRow>();
            for (int i = 0; i < _rowNames.Count && i < _rows.Count; i++)
            {
                _rowNames[i].color = RowColour(i);
                if (i < _rowDims.Count && _rowDims[i] != null) _rowDims[i].enabled = !RowLit(i);
            }
            ShowFusionDetail(_hover >= 0 ? _hover : _armed);
            if (_confirm != null) _confirm.interactable = CanConfirmFusion;
        }

        /// <summary>The fusion is made: the book locks and the message says so.</summary>
        public void FusionDone(string recipeId, string resultName)
        {
            FusionLocked = true;
            _madeRecipe = recipeId ?? string.Empty;
            RefreshFusion();
            ShowMessage("Đã luyện " + resultName + " (vào kho). Rời lò để đi tiếp.");
        }

        /// <summary>Leaves the fusion room without the input lock (tests and automation).</summary>
        public void CommandLeave()
        {
            if (Current == Step.Fusion) FusionLeft?.Invoke();
        }

        /// <summary>The armed row's rect (the crucible point the ingredients fly to), or the detail plate's centre.</summary>
        public Vector3 CrucibleWorld
        {
            get
            {
                RectTransform r = _root;
                Vector2 local = new Vector2(CardX0 + 147f, -(DetailY + 14f));
                return r != null ? r.TransformPoint(local) : Vector3.zero;
            }
        }

        private bool CanConfirmFusion => !FusionLocked && _armed >= 0 && _armed < _rows.Count && _rows[_armed].Makeable;

        private void ConfirmFusion()
        {
            if (FusionLocked)
            {
                ShowMessage("Mỗi lần ghé lò chỉ dung hợp 1 lần. Rời lò để đi tiếp.");
                return;
            }

            if (_armed < 0 || _armed >= _rows.Count)
            {
                ShowMessage("Chọn một công thức trước.");
                return;
            }

            FusionRow row = _rows[_armed];
            if (!row.Makeable)
            {
                ShowMessage(Reason(row));
                return;
            }

            FusionConfirmed?.Invoke(_armed);
        }

        /// <summary>Why a recipe cannot be fused now, in player words.</summary>
        public static string Reason(FusionRow row)
        {
            switch (row.Status)
            {
                case FusionStatus.MissingIngredient:
                    var missing = new List<string>();
                    for (int i = 0; i < row.Ingredients.Length; i++)
                    {
                        if (i >= row.Owned.Length || !row.Owned[i]) missing.Add(row.Ingredients[i] != null ? row.Ingredients[i].NameVi : "?");
                    }

                    return "Thiếu: " + string.Join(", ", missing) + ".";
                case FusionStatus.NotEnoughGold: return "Thiếu " + row.Shortfall + " vàng.";
                case FusionStatus.AlreadyFused: return "Đã dung hợp lần này.";
                case FusionStatus.NoSpace: return "Kho và balo đã đầy: bán hoặc dời bớt đồ.";
                default: return string.Empty;
            }
        }

        /// <summary>A row is lit when it can be fused now (or, once locked, when it is the one made).</summary>
        private bool RowLit(int i) => FusionLocked ? _rows[i].Recipe != null && _rows[i].Recipe.Id == _madeRecipe : _rows[i].Makeable;

        private Color RowColour(int i) => RowLit(i) ? (Color)CombatUiFactory.Candle : CombatUiFactory.Fog;

        /// <summary>Removes the detail plate's parts (they sit on the panel root, above the plate's background).</summary>
        private void ClearFusionDetail()
        {
            foreach (GameObject go in _detailParts)
            {
                if (go != null) CombatUiFactory.DestroySafe(go);
            }

            _detailParts.Clear();
        }

        private void BuildFusionRows()
        {
            _rows = FusionRows?.Invoke() ?? Array.Empty<FusionRow>();
            _rowNames.Clear();
            _rowDims.Clear();
            Sprite plate = Run("ui_plate-iron_default_9s") ?? _sprites.panelDefault;
            Sprite dim = Run("ui_dim-mask_default_4x4") ?? _sprites.fillInk0;
            for (int i = 0; i < _rows.Count; i++)
            {
                FusionRow row = _rows[i];
                int index = i;
                float x = CardX0 + (i % 2) * (RowW + ColGap);
                float y = RowY0 + (i / 2) * (RowH + RowGap);
                var card = new Card { Rect = CombatUiFactory.CreateRect("Recipe" + i, _content, x, y, RowW, RowH) };
                card.Ring = CombatUiFactory.CreateImage("Ring", card.Rect, Run("ui_door-card_ring-hover_9s"), -2f, -2f, RowW + 4f, RowH + 4f, sliced: true);
                card.Ring.enabled = false;
                Image bg = CombatUiFactory.CreateImage("Plate", card.Rect, plate, 0f, 0f, RowW, RowH, sliced: true, raycastTarget: true);
                var btn = bg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Arm(index));
                var relay = bg.gameObject.AddComponent<HoverRelay>();
                relay.Enter = () => Hover(index);
                relay.Exit = () => Hover(-1);
                card.Corner = CombatUiFactory.CreateImage("Corner", card.Rect, Run("ui_door-card_armed-corner_4x4"), -3f, -3f, 4f, 4f);
                card.Corner.enabled = false;
                if (row.Result != null) Glyph(card.Rect, row.Result, 3f, 3f);
                // a recipe that cannot be fused now has its glyph under the one-step ink checker (no partial alpha)
                Image dimmed = CombatUiFactory.CreateImage("Dim", card.Rect, dim, 3f, 3f, 16f, 16f);
                dimmed.type = Image.Type.Tiled;
                dimmed.enabled = !RowLit(i);
                _rowDims.Add(dimmed);
                _rowNames.Add(CombatUiFactory.CreateLabel("Name", card.Rect, _sprites.font, row.Result != null ? row.Result.NameVi : "?", 21f, 5f, 108f, 12f, RowColour(i)));
                CombatUiFactory.CreateLabel("Cost", card.Rect, _sprites.font, row.Cost.ToString(), RowW - 16f, 5f, 13f, 12f, CombatUiFactory.Bone, TextAlignmentOptions.TopRight);
                _cards.Add(card);
            }
        }

        /// <summary>An item's 16 x 16 glyph (the compact avatar of a multi-cell item, the art of a 1x1 one).</summary>
        private Image Glyph(RectTransform parent, ItemDef def, float x, float y)
        {
            Sprite icon = _sprites.GetItemIcon(def.Art, def.Footprint, compact: true);
            if (icon == null) return null;
            float w = icon.rect.width, h = icon.rect.height;
            float scale = Mathf.Min(1f, Mathf.Min(16f / w, 16f / h));
            w = Mathf.Max(1f, Mathf.Floor(w * scale));
            h = Mathf.Max(1f, Mathf.Floor(h * scale));
            return CombatUiFactory.CreateImage("Glyph", parent, icon, x + Mathf.Floor((16f - w) / 2f), y + Mathf.Floor((16f - h) / 2f), w, h);
        }

        /// <summary>The detail plate for recipe index (or the help line for -1).</summary>
        private void ShowFusionDetail(int index)
        {
            ClearFusionDetail();
            if (index < 0 || index >= _rows.Count)
            {
                SetDetail("Chọn một công thức. Nguyên liệu lấy từ balo và kho (món thường trước món +); món mới vào kho.");
                return;
            }

            SetDetail(string.Empty);
            FusionRow row = _rows[index];
            RectTransform box = CombatUiFactory.CreateRect("FusionDetail", _root, CardX0 + 7f, DetailY + 6f, 280f, DetailH - 10f);
            box.SetSiblingIndex(_detail.transform.GetSiblingIndex());   // above the plate's background, under the message
            _detailParts.Add(box.gameObject);
            float x = 0f;
            Sprite dim = Run("ui_dim-mask_default_4x4") ?? _sprites.fillInk0;
            var have = new List<string>();
            for (int i = 0; i < row.Ingredients.Length; i++)
            {
                ItemDef ing = row.Ingredients[i];
                bool owned = i < row.Owned.Length && row.Owned[i];
                if (ing != null) Glyph(box, ing, x, 0f);
                if (!owned)
                {
                    Image mask = CombatUiFactory.CreateImage("Missing", box, dim, x, 0f, 16f, 16f);
                    mask.type = Image.Type.Tiled;
                }

                have.Add((ing != null ? ing.NameVi : "?") + (owned ? " (có)" : " (thiếu)"));
                x += 18f;
                CombatUiFactory.CreateLabel("Op", box, _sprites.font, i < row.Ingredients.Length - 1 ? "+" : "=", x, 2f, 8f, 12f, CombatUiFactory.Fog, TextAlignmentOptions.Top);
                x += 10f;
            }

            if (row.Result != null)
            {
                Glyph(box, row.Result, x, 0f);
                x += 22f;
                CombatUiFactory.CreateLabel("Result", box, _sprites.font, row.Result.NameVi, x, -1f, 280f - x, 12f, CombatUiFactory.Candle);
                CombatUiFactory.CreateLabel("Kind", box, _sprites.font, "Huyền thoại · " + row.Result.Footprint, x, 10f, 280f - x, 12f, CombatUiFactory.Fog);
                CombatUiFactory.CreateLabel("Effect", box, _sprites.font, Describe(row.Result), 0f, 22f, 280f, 24f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            }

            if (FusionLocked && row.Recipe != null && row.Recipe.Id == _madeRecipe)
            {
                CombatUiFactory.CreateLabel("Made", box, _sprites.font, "Đã luyện: món mới nằm trong kho.", 0f, 48f, 280f, 12f, CombatUiFactory.Candle);
                return;
            }

            CombatUiFactory.CreateLabel("Need", box, _sprites.font, "Cần: " + string.Join(" · ", have), 0f, 46f, 280f, 26f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            int gold = Gold != null ? Gold() : 0;
            string cost = "Giá: " + row.Cost + " vàng · đang có " + gold + (row.Status == FusionStatus.NotEnoughGold ? " · thiếu " + row.Shortfall : string.Empty);
            Sprite coin = Run("ui_glyph-coin_gen_10x10");
            if (coin != null) CombatUiFactory.CreateImage("Coin", box, coin, 0f, 73f, 10f, 10f);
            CombatUiFactory.CreateLabel("CostLine", box, _sprites.font, cost, 13f, 72f, 267f, 12f, row.Status == FusionStatus.NotEnoughGold ? CombatUiFactory.Fog : CombatUiFactory.Bone);
            if (row.LosesUpgrade.Length > 0 && !FusionLocked)
            {
                CombatUiFactory.CreateLabel("Warn", box, _sprites.font, "Mất + của " + string.Join(", ", row.LosesUpgrade), 0f, 85f, 280f, 12f, CombatUiFactory.Candle);
            }
        }
    }
}
