using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The speed control in the action strip (art bible 7.3, 7.11): four plain segments, 1x, 2x, 4x and Skip.
    /// The selected one carries a ring and a 4 x 4 Candle square. Selecting is presentation only; it raises an
    /// event and the playback system decides what to do with it.
    /// </summary>
    public sealed class SpeedControlView : MonoBehaviour
    {
        [SerializeField] private Button[] buttons = Array.Empty<Button>();
        [SerializeField] private Image[] segments = Array.Empty<Image>();
        [SerializeField] private TMP_Text skipLabel;
        [SerializeField] private string skipIdleText = string.Empty;
        [SerializeField] private string skipArmedText = string.Empty;

        /// <summary>Raised with the segment index when the player activates a segment.</summary>
        public event Action<int> SpeedRequested;

        /// <summary>Index of the segment currently shown as selected.</summary>
        public int Current { get; private set; }

        private void OnEnable()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => SpeedRequested?.Invoke(index));
            }
        }

        private void OnDisable()
        {
            foreach (Button b in buttons) b.onClick.RemoveAllListeners();
        }

        /// <summary>Shows whether Skip is armed. The label changes, so it is never mistaken for a selected speed.</summary>
        public void SetSkipArmed(bool armed)
        {
            if (skipLabel != null) skipLabel.text = armed ? skipArmedText : skipIdleText;
        }

        /// <summary>Gives keyboard focus to a segment (the current speed has focus on entering playback).</summary>
        public void Focus(int index)
        {
            if (EventSystem.current != null && index >= 0 && index < buttons.Length) EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        }

        /// <summary>Marks one segment selected.</summary>
        public void Apply(int selected, HudSpriteSet sprites)
        {
            Current = selected;
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].sprite = i == selected ? sprites.segmentSelected : sprites.segmentDefault;
            }
        }
    }
}
