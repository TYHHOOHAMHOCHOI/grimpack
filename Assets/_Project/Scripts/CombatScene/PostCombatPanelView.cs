using System;
using System.Collections.Generic;
using Project05.CombatCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// Post-combat breakdown panel rendered in the Stage right column (Art Bible 7.5).
    /// Displays outcome, ranked item contributions, the 1..6 next-stage monster selector,
    /// and Next Stage / Retry actions.
    /// </summary>
    public sealed class PostCombatPanelView : MonoBehaviour
    {
        private RectTransform _root;
        private CombatSceneSpriteCatalog _sprites;
        private TextMeshProUGUI _titleLabel;
        private TextMeshProUGUI _summaryLabel;
        private RectTransform _rowsRoot;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        private readonly Button[] _nextCountButtons = new Button[6];
        private Button _nextStageBtn;
        private TextMeshProUGUI _nextStageBtnLabel;
        private Button _retryBtn;
        private int _selectedNextCount = 2;

        public event Action<int> NextMonsterCountSelected;
        public event Action NextStageClicked;
        public event Action RetryClicked;

        public void Initialize(RectTransform safeFrame, CombatSceneSpriteCatalog sprites)
        {
            _sprites = sprites;

            // Sits in Stage right column (x = 318..634, y = 22..328)
            _root = CombatUiFactory.CreateRect("PostCombatPanel", safeFrame, 318f, 22f, 316f, 306f);
            CombatUiFactory.CreateImage("Bg", _root, _sprites.panelDefault, 0f, 0f, 316f, 306f, sliced: true, raycastTarget: true);

            _titleLabel = CombatUiFactory.CreateLabel(
                "OutcomeTitle",
                _root,
                _sprites.font,
                "KẾT QUẢ",
                12f,
                10f,
                292f,
                14f,
                CombatUiFactory.Candle);

            _summaryLabel = CombatUiFactory.CreateLabel(
                "OutcomeSummary",
                _root,
                _sprites.font,
                string.Empty,
                12f,
                26f,
                292f,
                14f,
                CombatUiFactory.Bone);

            CombatUiFactory.CreateImage("SepTop", _root, _sprites.fillInk3, 12f, 44f, 292f, 1f);
            CombatUiFactory.CreateLabel("ContribHeader", _root, _sprites.font, "Đóng góp trang bị:", 12f, 50f, 200f, 12f, CombatUiFactory.Fog);

            _rowsRoot = CombatUiFactory.CreateRect("ContributionRows", _root, 12f, 66f, 292f, 150f);

            CombatUiFactory.CreateImage("SepBot", _root, _sprites.fillInk3, 12f, 224f, 292f, 1f);

            CombatUiFactory.CreateLabel(
                "NextCountHeader",
                _root,
                _sprites.font,
                "Chọn số quái màn sau (1-6):",
                12f,
                232f,
                220f,
                12f,
                CombatUiFactory.Bone);

            for (int i = 0; i < 6; i++)
            {
                int count = i + 1;
                Button btn = CombatUiFactory.CreateSegmentButton(
                    "PostNextCount_" + count,
                    _root,
                    _sprites,
                    count.ToString(),
                    12f + i * 24f,
                    248f,
                    22f,
                    20f,
                    selected: count == _selectedNextCount);
                btn.onClick.AddListener(() =>
                {
                    SetNextMonsterCount(count);
                    NextMonsterCountSelected?.Invoke(count);
                });
                _nextCountButtons[i] = btn;
            }

            _nextStageBtn = CombatUiFactory.CreateSegmentButton(
                "NextStageButton",
                _root,
                _sprites,
                "Màn sau (2 quái)",
                12f,
                274f,
                148f,
                24f,
                selected: true);
            _nextStageBtnLabel = _nextStageBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            _nextStageBtn.onClick.AddListener(() => NextStageClicked?.Invoke());

            _retryBtn = CombatUiFactory.CreateSegmentButton(
                "RetryButton",
                _root,
                _sprites,
                "Đánh lại",
                168f,
                274f,
                136f,
                24f,
                selected: false);
            _retryBtn.onClick.AddListener(() => RetryClicked?.Invoke());

            Hide();
        }

        public void SetNextMonsterCount(int count)
        {
            _selectedNextCount = Mathf.Clamp(count, 1, 6);
            for (int i = 0; i < _nextCountButtons.Length; i++)
            {
                CombatUiFactory.SetSegmentVisual(_nextCountButtons[i], _sprites, (i + 1) == _selectedNextCount);
            }
            if (_nextStageBtnLabel != null)
            {
                _nextStageBtnLabel.text = "Màn sau (" + _selectedNextCount + " quái)";
            }
        }

        public void Show(CombatResult result, GameCatalog catalog, int stageNumber, int nextMonsterCount)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(true);
            SetNextMonsterCount(nextMonsterCount);

            if (result == null || result.Status == CombatRunStatus.TechnicalStop)
            {
                _titleLabel.text = "LỖI HỆ THỐNG";
                _titleLabel.color = CombatUiFactory.Blood;
                _summaryLabel.text = result != null ? result.StopDiagnostic : "Không có kết quả";
                ClearRows();
                return;
            }

            switch (result.Outcome)
            {
                case CombatOutcome.Victory:
                    _titleLabel.text = "CHIẾN THẮNG!";
                    _titleLabel.color = CombatUiFactory.Candle;
                    break;
                case CombatOutcome.Draw:
                    _titleLabel.text = "HÒA TRẬN";
                    _titleLabel.color = CombatUiFactory.FogHi;
                    break;
                default:
                    _titleLabel.text = "THẤT BẠI";
                    _titleLabel.color = CombatUiFactory.Blood;
                    break;
            }

            CombatTrace trace = result.Trace;
            int actions = 0;
            int totalDealt = 0;
            int totalTaken = 0;

            var byItem = new Dictionary<string, (string name, TierKind tier, string art, string fp, int value)>(StringComparer.Ordinal);

            if (trace != null)
            {
                if (trace.SnapshotsBySeq != null && trace.SnapshotsBySeq.Count > 0)
                {
                    actions = trace.SnapshotsBySeq[trace.SnapshotsBySeq.Count - 1].CompletedActions;
                }

                for (int i = 0; i < trace.Events.Count; i++)
                {
                    CombatEventRecord ev = trace.Events[i];
                    bool isDmg = ev.Kind == CombatEventKind.Damage || ev.Kind == CombatEventKind.DetonationDamage;
                    bool isSustain = ev.Kind == CombatEventKind.ShieldGained || ev.Kind == CombatEventKind.Healed;

                    if (isDmg && ev.Target.Side == UnitSide.Hero)
                    {
                        totalTaken += ev.ResolvedAmount;
                    }
                    else if (isDmg && ev.Target.Side == UnitSide.Enemy)
                    {
                        totalDealt += ev.ResolvedAmount;
                    }

                    if ((isDmg || isSustain) &&
                        ev.ResolvedAmount > 0 &&
                        ev.Origin != null &&
                        ev.Origin.Kind == OriginKind.Item &&
                        !string.IsNullOrEmpty(ev.Origin.ItemDefId))
                    {
                        string id = ev.Origin.ItemDefId;
                        if (!byItem.TryGetValue(id, out var agg))
                        {
                            ItemDef def = catalog != null ? catalog.FindItem(id) : null;
                            string label = def != null ? def.NameVi : id;
                            TierKind tier = def != null ? def.Tier : TierKind.White;
                            string art = def != null ? def.Art : string.Empty;
                            string fp = def != null ? def.Footprint : "1x1";
                            agg = (label, tier, art, fp, 0);
                        }
                        agg.value += ev.ResolvedAmount;
                        byItem[id] = agg;
                    }
                }
            }

            _summaryLabel.text = "Màn " + stageNumber + " · " + actions + " lượt · Gây " + totalDealt + " · Chịu " + totalTaken;
            BuildContributionRows(byItem);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rowObjects.Count; i++)
            {
                CombatUiFactory.DestroySafe(_rowObjects[i]);
            }
            _rowObjects.Clear();
        }

        private void BuildContributionRows(Dictionary<string, (string name, TierKind tier, string art, string fp, int value)> byItem)
        {
            ClearRows();

            var list = new List<(string id, string name, TierKind tier, string art, string fp, int value)>(byItem.Count);
            foreach (var kv in byItem)
            {
                list.Add((kv.Key, kv.Value.name, kv.Value.tier, kv.Value.art, kv.Value.fp, kv.Value.value));
            }
            list.Sort((a, b) =>
            {
                int cmp = b.value.CompareTo(a.value);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.id, b.id);
            });

            int maxVal = list.Count > 0 ? Mathf.Max(1, list[0].value) : 1;
            int rows = Mathf.Min(5, list.Count);
            for (int i = 0; i < rows; i++)
            {
                var entry = list[i];
                float y = i * 28f;
                RectTransform row = CombatUiFactory.CreateRect("Contrib_" + i, _rowsRoot, 0f, y, 292f, 24f);
                _rowObjects.Add(row.gameObject);

                CombatUiFactory.CreateLabel("Rank", row, _sprites.font, (i + 1) + ".", 0f, 4f, 16f, 12f, CombatUiFactory.Fog);

                Sprite plate = _sprites.GetAuthoredPlate(entry.tier.ToString().ToLowerInvariant(), false, "1x1");
                Image plateImg = CombatUiFactory.CreateImage("Plate", row, plate, 18f, 2f, 20f, 20f);
                Sprite icon = _sprites.GetItemIcon(entry.art, entry.fp, compact: true);
                if (icon != null) CombatUiFactory.CreateImage("Icon", plateImg.transform, icon, 2f, 2f, 16f, 16f);

                CombatUiFactory.CreateLabel("Name", row, _sprites.font, entry.name, 44f, 0f, 170f, 12f, CombatUiFactory.Bone);
                CombatUiFactory.CreateImage("Track", row, _sprites.fillInk2, 44f, 16f, 180f, 4f);
                float fillW = Mathf.Max(2f, Mathf.Round(180f * entry.value / maxVal));
                CombatUiFactory.CreateImage("Bar", row, _sprites.fillBone, 44f, 16f, fillW, 4f);

                CombatUiFactory.CreateLabel("Value", row, _sprites.font, entry.value.ToString(), 230f, 4f, 60f, 12f,
                    CombatUiFactory.Candle, TextAlignmentOptions.TopRight);
            }

            if (rows == 0)
            {
                RectTransform emptyRow = CombatUiFactory.CreateRect("ContribEmpty", _rowsRoot, 0f, 4f, 292f, 16f);
                _rowObjects.Add(emptyRow.gameObject);
                CombatUiFactory.CreateLabel("Empty", emptyRow, _sprites.font, "Chưa có sát thương hoặc giáp từ trang bị.", 0f, 0f, 292f, 14f, CombatUiFactory.Fog);
            }
        }
    }
}
