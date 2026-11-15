using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The settings panel (design/gdd/settings.md rules 2 to 6, MVP subset): three toggles and Back. A toggle states its
    /// value in words, so it never relies on colour. Values go through <see cref="GameSettings"/>.
    /// TEMPORARY: the labels are Vietnamese literals set by the scene builder until the localization system exists.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Button flashesButton;
        [SerializeField] private Button motionButton;
        [SerializeField] private Button skipEndButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text flashesLabel;
        [SerializeField] private TMP_Text motionLabel;
        [SerializeField] private TMP_Text skipEndLabel;
        [SerializeField] private string flashesName = string.Empty;
        [SerializeField] private string motionName = string.Empty;
        [SerializeField] private string skipEndName = string.Empty;
        [SerializeField] private string onText = string.Empty;
        [SerializeField] private string offText = string.Empty;

        /// <summary>Back was activated.</summary>
        public event Action BackRequested;

        private void OnEnable()
        {
            flashesButton.onClick.AddListener(() => { GameSettings.ReducedFlashes = !GameSettings.ReducedFlashes; Refresh(); });
            motionButton.onClick.AddListener(() => { GameSettings.ReducedMotion = !GameSettings.ReducedMotion; Refresh(); });
            skipEndButton.onClick.AddListener(() => { GameSettings.SkipGoesToEnd = !GameSettings.SkipGoesToEnd; Refresh(); });
            backButton.onClick.AddListener(() => BackRequested?.Invoke());
            Refresh();
        }

        private void OnDisable()
        {
            flashesButton.onClick.RemoveAllListeners();
            motionButton.onClick.RemoveAllListeners();
            skipEndButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }

        /// <summary>Writes the current values into the labels.</summary>
        public void Refresh()
        {
            flashesLabel.text = flashesName + ": " + (GameSettings.ReducedFlashes ? onText : offText);
            motionLabel.text = motionName + ": " + (GameSettings.ReducedMotion ? onText : offText);
            skipEndLabel.text = skipEndName + ": " + (GameSettings.SkipGoesToEnd ? onText : offText);
        }

        /// <summary>Shows or hides the panel; the first toggle takes focus when shown.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (visible && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(flashesButton.gameObject);
        }
    }
}
