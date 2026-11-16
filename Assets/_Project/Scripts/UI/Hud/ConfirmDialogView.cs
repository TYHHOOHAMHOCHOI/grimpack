using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The abandon confirmation (menus-and-screen-flow.md rule 7): "Keep playing" is the safe default and takes the initial
    /// focus; Esc cancels. Confirming abandons through the screen controller; the dialog itself changes nothing.
    /// </summary>
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private Button keepPlaying;
        [SerializeField] private Button abandon;

        /// <summary>Keep playing was activated.</summary>
        public event Action KeepRequested;

        /// <summary>Abandon was activated.</summary>
        public event Action AbandonConfirmed;

        private void OnEnable()
        {
            keepPlaying.onClick.AddListener(() => KeepRequested?.Invoke());
            abandon.onClick.AddListener(() => AbandonConfirmed?.Invoke());
        }

        private void OnDisable()
        {
            keepPlaying.onClick.RemoveAllListeners();
            abandon.onClick.RemoveAllListeners();
        }

        /// <summary>Shows or hides the dialog; the safe choice takes focus when shown.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (visible && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(keepPlaying.gameObject);
        }
    }
}
