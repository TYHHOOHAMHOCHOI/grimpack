using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project05.UI
{
    /// <summary>The actions the title menu can request.</summary>
    public enum TitleMenuAction
    {
        /// <summary>Start a new run.</summary>
        Play,

        /// <summary>Open the settings screen.</summary>
        Settings,

        /// <summary>Leave the game.</summary>
        Quit,
    }

    /// <summary>
    /// Title menu behaviour (Menus and Screen Flow, design/gdd/menus-and-screen-flow.md).
    /// The menu only presents and routes: it raises <see cref="ActionRequested"/> and does not
    /// create runs, seeds or rewards. Keyboard focus starts on Play so the screen is operable
    /// without a mouse.
    /// </summary>
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Scene loaded by Play until the Run State and Flow system owns run creation. Empty means raise the event only.")]
        [SerializeField] private string playSceneName = "MainGame";

        /// <summary>Raised when the player activates a menu button.</summary>
        public event Action<TitleMenuAction> ActionRequested;

        /// <summary>The button that holds focus when the menu opens.</summary>
        public Button InitialFocus => playButton;

        private void OnEnable()
        {
            Hook(playButton, TitleMenuAction.Play);
            Hook(settingsButton, TitleMenuAction.Settings);
            Hook(quitButton, TitleMenuAction.Quit);
        }

        private void Start()
        {
            if (EventSystem.current != null && playButton != null)
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }
        }

        private void OnDisable()
        {
            Unhook(playButton, TitleMenuAction.Play);
            Unhook(settingsButton, TitleMenuAction.Settings);
            Unhook(quitButton, TitleMenuAction.Quit);
        }

        /// <summary>Requests an action, as if the matching button had been activated.</summary>
        /// <param name="action">The action to request.</param>
        public void Request(TitleMenuAction action)
        {
            ActionRequested?.Invoke(action);

            switch (action)
            {
                case TitleMenuAction.Play:
                    if (!string.IsNullOrEmpty(playSceneName) && Application.CanStreamedLevelBeLoaded(playSceneName))
                    {
                        SceneManager.LoadScene(playSceneName);
                    }

                    break;
                case TitleMenuAction.Quit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        private void Hook(Button button, TitleMenuAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() => Request(action));
        }

        private void Unhook(Button button, TitleMenuAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
        }
    }
}
