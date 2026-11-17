using System.Collections.Generic;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// The floor's progress as a corridor of doors in the top strip (run-state-and-flow.md UI Requirements; user decision
    /// 2026-11-17: "map" = this strip). Nine nodes: rooms 1..8 and the boss. A cleared room shows the tier of the door
    /// taken, the current room has the Candle bar under it, rooms ahead are empty outlines (their content does not
    /// exist yet), and an armed door previews the next node. Hovering lists the rooms so far. Display only.
    /// </summary>
    public sealed class CorridorView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float Pitch = 12f;
        private CombatSceneSpriteCatalog _sprites;
        private GameCatalog _catalog;
        private readonly List<Image> _nodes = new List<Image>();
        private Image _marker;
        private RectTransform _popup;
        private TextMeshProUGUI _popupText;
        private RunSession _run;
        private TierKind? _preview;

        public void Initialize(RectTransform parent, float x, float y, CombatSceneSpriteCatalog sprites, GameCatalog catalog, RectTransform popupParent)
        {
            _sprites = sprites;
            _catalog = catalog;
            var rt = (RectTransform)transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Pitch * 8f + 16f, 16f);
            Image hit = gameObject.AddComponent<Image>();
            hit.sprite = sprites.fillInk0;
            hit.color = new Color32(0, 0, 0, 0);   // invisible hit area for the hover popup (not drawn)
            hit.raycastTarget = true;
            for (int i = 0; i < 9; i++)
            {
                bool boss = i == 8;
                // a door is 9 px wide in its 10 x 11 canvas: nodes sit at slot + (1, 1); the boss at its slot
                float nx = boss ? Pitch * 8f + 4f : i * Pitch + 1f;
                Image node = CombatUiFactory.CreateImage(boss ? "Boss" : "Room" + (i + 1), rt, null, nx, boss ? 0f : 1f, boss ? 12f : 10f, boss ? 12f : 11f);
                _nodes.Add(node);
            }

            _marker = CombatUiFactory.CreateImage("Now", rt, Sprite("ui_map-marker_now_6x2"), 2f, 13f, 6f, 2f);
            _popup = CombatUiFactory.CreateRect("CorridorPopup", popupParent, x, y + 18f, 190f, 120f);
            CombatUiFactory.CreateImage("Bg", _popup, sprites.panelDefault, 0f, 0f, 190f, 120f, sliced: true);
            _popupText = CombatUiFactory.CreateLabel("Text", _popup, sprites.font, string.Empty, 6f, 4f, 178f, 112f, CombatUiFactory.Bone, TextAlignmentOptions.TopLeft, wrap: true);
            _popup.gameObject.SetActive(false);
        }

        /// <summary>Redraws from the run (call after every run change).</summary>
        public void Show(RunSession run)
        {
            _run = run;
            Redraw();
        }

        /// <summary>Previews the armed door's tier on the next node, or clears it with null.</summary>
        public void Preview(TierKind? tier)
        {
            _preview = tier;
            Redraw();
        }

        private void Redraw()
        {
            if (_run == null || _nodes.Count < 9) return;
            int current = _run.Room;
            var tierOf = new Dictionary<int, TierKind>();
            var fusion = new HashSet<int>();   // item-fusion.md: fusion rooms get their own node
            foreach (RoomRecord rec in _run.History)
            {
                tierOf[rec.Room] = rec.Tier;
                if (rec.Fusion) fusion.Add(rec.Room);
            }

            if (!_run.IsBossRoom && current >= 1) tierOf[current] = _run.CurrentDoor?.Tier ?? TierKind.White;
            if (!_run.IsBossRoom && _run.IsFusionRoom) fusion.Add(current);

            for (int r = 1; r <= 8; r++)
            {
                Image node = _nodes[r - 1];
                string key;
                if (r == 1 && (r <= current || _run.IsBossRoom)) key = "ui_map-node_entrance_10x11";
                else if (r <= current && fusion.Contains(r) && Sprite("ui_map-node_fusion_10x11") != null) key = "ui_map-node_fusion_10x11";
                else if (r <= current && tierOf.TryGetValue(r, out TierKind t)) key = "ui_map-node_" + RunPanelView.TierKey(t) + "_10x11";
                else if (r == current + 1 && _preview.HasValue) key = "ui_map-node-preview_" + RunPanelView.TierKey(_preview.Value) + "_10x11";
                else key = "ui_map-node_ahead_10x11";
                node.sprite = Sprite(key);
                node.enabled = node.sprite != null;
            }

            _nodes[8].sprite = Sprite("ui_map-node_boss_12x12");
            _nodes[8].enabled = _nodes[8].sprite != null;
            float mx = _run.IsBossRoom ? Pitch * 8f + 7f : (current - 1) * Pitch + 3f;   // node x + 2
            _marker.rectTransform.anchoredPosition = new Vector2(mx, -13f);
            _marker.enabled = _run.Phase != RunPhase.Won && _run.Phase != RunPhase.Lost;
            _popupText.text = HistoryText();
        }

        private string HistoryText()
        {
            var lines = new List<string>();
            foreach (RoomRecord rec in _run.History)
            {
                string line = "P" + rec.Room + " " + (rec.Entrance ? "Lối vào" : rec.Fusion ? "Lò Dung Hợp" : RunPanelView.TierName(rec.Tier));
                if (rec.Fusion)
                {
                    ItemDef made = _catalog?.FindItem(_catalog.FindRecipe(rec.RecipeMade)?.ResultId);
                    lines.Add(line + (made != null ? ", luyện " + made.NameVi : ", không luyện"));
                    continue;
                }

                if (rec.HpGranted > 0) line += ", +" + rec.HpGranted + " Máu";
                if (rec.CellsGranted > 0) line += ", +" + rec.CellsGranted + " ô";
                CurseDef curse = _run.Config.FindCurse(rec.CurseId);
                if (curse != null) line += ", nguyền " + curse.NameVi;
                ItemDef item = _catalog?.FindItem(rec.ItemTaken);
                if (item != null) line += ", lấy " + item.NameVi;
                lines.Add(line);
            }

            lines.Add(_run.IsBossRoom ? "Đang ở: phòng boss" : "Đang ở: phòng " + _run.Room + "/8, rồi tới boss");
            return string.Join("\n", lines);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_popup == null) return;
            _popup.gameObject.SetActive(true);
            _popup.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_popup != null) _popup.gameObject.SetActive(false);
        }

        private Sprite Sprite(string key) => _sprites != null ? _sprites.GetRunSprite(key) : null;
    }
}
