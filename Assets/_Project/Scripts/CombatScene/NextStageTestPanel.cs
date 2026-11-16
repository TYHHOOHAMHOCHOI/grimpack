using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// In-scene Next-Stage Monster Count Test System ("Hệ thống test chọn số lượng quái cho màn sau").
    /// Allows picking 1..6 monsters for the next encounter (or applying immediately during Planning).
    /// </summary>
    public sealed class NextStageTestPanel : MonoBehaviour
    {
        private readonly Button[] _countButtons = new Button[6];
        private CombatSceneSpriteCatalog _sprites;
        private Button _applyNowButton;
        private TextMeshProUGUI _statusLabel;

        public int SelectedMonsterCount { get; private set; } = 2;

        public event Action<int> MonsterCountChanged;
        public event Action<int> ApplyNowRequested;

        public void Initialize(RectTransform planningActionStrip, CombatSceneSpriteCatalog sprites, int initialCount = 2)
        {
            _sprites = sprites;
            SelectedMonsterCount = Mathf.Clamp(initialCount, 1, 6);

            CombatUiFactory.CreateLabel(
                "NextCountLabel",
                planningActionStrip,
                _sprites.font,
                "Quái sau:",
                182f,
                339f,
                60f,
                12f,
                CombatUiFactory.Fog);

            for (int i = 0; i < 6; i++)
            {
                int count = i + 1;
                Button btn = CombatUiFactory.CreateSegmentButton(
                    "MonsterCount_" + count,
                    planningActionStrip,
                    _sprites,
                    count.ToString(),
                    242f + i * 20f,
                    335f,
                    19f,
                    20f,
                    selected: count == SelectedMonsterCount);

                btn.onClick.AddListener(() => SelectCount(count));
                _countButtons[i] = btn;
            }

            _applyNowButton = CombatUiFactory.CreateSegmentButton(
                "ApplyMonstersNowButton",
                planningActionStrip,
                _sprites,
                "Đổi ngay",
                366f,
                335f,
                62f,
                20f);
            _applyNowButton.onClick.AddListener(() => ApplyNowRequested?.Invoke(SelectedMonsterCount));

            _statusLabel = CombatUiFactory.CreateLabel(
                "StageSeedInfo",
                planningActionStrip,
                _sprites.font,
                "Màn 1 · 2 quái",
                434f,
                339f,
                114f,
                12f,
                CombatUiFactory.Fog);
        }

        public void SelectCount(int count)
        {
            int clamped = Mathf.Clamp(count, 1, 6);
            SelectedMonsterCount = clamped;
            RefreshSelectionVisuals();
            MonsterCountChanged?.Invoke(clamped);
        }

        public void SetCountSilently(int count)
        {
            SelectedMonsterCount = Mathf.Clamp(count, 1, 6);
            RefreshSelectionVisuals();
        }

        public void UpdateSummary(int stage, int currentEnemyCount)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = "Màn " + stage + " · " + currentEnemyCount + " quái";
            }
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < _countButtons.Length; i++)
            {
                CombatUiFactory.SetSegmentVisual(_countButtons[i], _sprites, (i + 1) == SelectedMonsterCount);
            }
        }
    }
}
