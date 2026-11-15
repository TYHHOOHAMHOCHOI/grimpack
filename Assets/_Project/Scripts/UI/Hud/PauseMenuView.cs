using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The pause menu (design/ux/combat-screen-buttons.md): Resume, Settings and Abandon run. It only raises events; the
    /// screen controller decides what they do. The first button takes focus when the menu opens.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button resume;
        [SerializeField] private Button settings;
        [SerializeField] private Button abandon;

        /// <summary>Resume was activated.</summary>
        public event Action ResumeRequested;

        /// <summary>Settings was activated.</summary>
        public event Action SettingsRequested;

        /// <summary>Abandon run was activated.</summary>
        public event Action AbandonRequested;

        private void OnEnable()
        {
            resume.onClick.AddListener(() => ResumeRequested?.Invoke());
            settings.onClick.AddListener(() => SettingsRequested?.Invoke());
            abandon.onClick.AddListener(() => AbandonRequested?.Invoke());
        }

        private void OnDisable()
        {
            resume.onClick.RemoveAllListeners();
            settings.onClick.RemoveAllListeners();
            abandon.onClick.RemoveAllListeners();
        }

        /// <summary>Shows or hides the menu and moves focus to Resume when shown.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (visible && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resume.gameObject);
        }
    }
}
