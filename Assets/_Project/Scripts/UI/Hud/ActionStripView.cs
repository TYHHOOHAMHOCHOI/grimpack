using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The action strip at the bottom of the stage (art bible 7.3): the primary Start action while planning, the speed control
    /// while watching. Only one group is shown at a time.
    /// </summary>
    public sealed class ActionStripView : MonoBehaviour
    {
        [SerializeField] private GameObject startGroup;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject speedGroup;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private Color labelColour = Color.black;
        [SerializeField] private Color labelDisabledColour = Color.gray;
        [Tooltip("Why Start is off while an item is held. TEMPORARY literal until localization exists.")]
        [SerializeField] private string heldReason = string.Empty;

        /// <summary>Raised when the Start button is activated by mouse or keyboard.</summary>
        public event Action StartRequested;

        private void OnEnable() => startButton.onClick.AddListener(OnStart);

        private void OnDisable() => startButton.onClick.RemoveListener(OnStart);

        private void OnStart() => StartRequested?.Invoke();

        /// <summary>Shows the group for a phase.</summary>
        public void SetPhase(ScreenPhase phase)
        {
            startGroup.SetActive(phase == ScreenPhase.Planning);
            speedGroup.SetActive(phase == ScreenPhase.Playback);
        }

        /// <summary>
        /// Enables or disables Start, and says why it is off in words at the left of the strip, so the state never relies
        /// on colour (art bible 7.11).
        /// </summary>
        public void SetStartEnabled(bool enabled, bool itemHeld)
        {
            if (startButton.interactable != enabled) startButton.interactable = enabled;
            if (startLabel != null) startLabel.color = enabled ? labelColour : labelDisabledColour;
            if (reasonLabel != null) reasonLabel.text = !enabled && itemHeld ? heldReason : string.Empty;
        }

        /// <summary>Gives keyboard focus to Start.</summary>
        public void FocusStart()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
    }
}
