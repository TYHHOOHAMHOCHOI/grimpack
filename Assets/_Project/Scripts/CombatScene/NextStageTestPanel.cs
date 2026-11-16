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
        private GameObject _panelRoot;
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

            var rootRect = CombatUiFactory.CreateRect("NextStageTestGroup", planningActionStrip, 0f, 0f, 640f, 360f);
            _panelRoot = rootRect.gameObject;

            CombatUiFactory.CreateLabel(
                "NextCountLabel",
                rootRect,
                _sprites.font,
                "Quái:",
                182f,
                339f,
                34f,
                12f,
                CombatUiFactory.Fog);

            for (int i = 0; i < 6; i++)
            {
                int count = i + 1;
                Button btn = CombatUiFactory.CreateSegmentButton(
                    "MonsterCount_" + count,
                    rootRect,
                    _sprites,
                    count.ToString(),
                    216f + i * 19f,
                    335f,
                    18f,
                    20f,
                    selected: count == SelectedMonsterCount);

                btn.onClick.AddListener(() => SelectCount(count));
                _countButtons[i] = btn;
            }

            _applyNowButton = CombatUiFactory.CreateSegmentButton(
                "ApplyMonstersNowButton",
                rootRect,
                _sprites,
                "Đổi ngay",
                333f,
                335f,
                54f,
                20f);
            _applyNowButton.onClick.AddListener(() => ApplyNowRequested?.Invoke(SelectedMonsterCount));

            _statusLabel = CombatUiFactory.CreateLabel(
                "StageSeedInfo",
                rootRect,
                _sprites.font,
                "M1",
                390f,
                339f,
                24f,
                12f,
                CombatUiFactory.Fog);
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null && _panelRoot.activeSelf != visible)
            {
                _panelRoot.SetActive(visible);
            }
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
                _statusLabel.text = "M" + stage + "·" + currentEnemyCount;
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
