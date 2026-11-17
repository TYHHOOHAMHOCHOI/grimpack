using System;
using Project05.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Wires the buttons of the Combat screen to their behaviour (design/ux/combat-screen-buttons.md): Start, the speed
    /// segments and Skip, the menu, the pause menu, the abandon dialog and the settings panel, and the Esc priority.
    /// Presentation only. Playback is the <see cref="DemoPlayback"/> fixture until the combat presentation exists, so no
    /// button here creates a run, resolves a fight or changes a reward.
    /// </summary>
    public sealed class CombatScreenController : MonoBehaviour
    {
        /// <summary>How long the "skipped" state stays on screen before the final view, in seconds (provisional orient hold).</summary>
        public const float SkipOrientHold = 0.3f;

        private static readonly HudFixture[] Beats =
        {
            HudFixture.PartyActing, HudFixture.EnemyActing, HudFixture.PartyActing, HudFixture.EnemyActing,
        };

        [SerializeField] private CombatHudView hud;
        [SerializeField] private ItemBoardController board;
        [SerializeField] private ItemPopupController popup;
        [SerializeField] private ActionStripView strip;
        [SerializeField] private Button menuButton;
        [SerializeField] private PauseMenuView pause;
        [SerializeField] private ConfirmDialogView confirm;
        [SerializeField] private SettingsPanelView settings;
        [SerializeField] private GameObject stageDim;
        [SerializeField] private CanvasGroup[] screenGroups = Array.Empty<CanvasGroup>();
        [SerializeField] private string titleScene = "Title";
        [SerializeField] private PixelCursor cursor;
        [SerializeField] private Project05.CombatScene.CombatSceneController liveCombat;

        private readonly CombatScreenModel _model = new CombatScreenModel();
        private readonly DemoPlayback _playback = new DemoPlayback(new[] { 1.6f, 1.6f, 1.6f, 1.6f });
        private float _skipLandAt = -1f;
        private GameObject _returnFocus;
        private int _earned;          // money from items sold this session, added on top of the fixture currency

        /// <summary>Raised when the player confirms abandoning the run, before the Title scene loads.</summary>
        public event Action AbandonConfirmed;

        /// <summary>The rules behind the buttons, for tests and captures.</summary>
        public CombatScreenModel Model => _model;

        private void OnEnable()
        {
            strip.StartRequested += TryStart;
            hud.Speed.SpeedRequested += OnSpeedSegment;
            menuButton.onClick.AddListener(OpenMenuFromButton);
            pause.ResumeRequested += CloseMenu;
            pause.SettingsRequested += OpenSettings;
            pause.AbandonRequested += AskAbandon;
            confirm.KeepRequested += CancelAbandon;
            confirm.AbandonConfirmed += Abandon;
            settings.BackRequested += CloseSettings;
            board.ItemSold += OnItemSold;
            board.BoardChanged += OnBoardChanged;
            if (liveCombat != null) liveCombat.StageReturnedToPlanning += HandleLiveStageReturned;
        }

        private void OnDisable()
        {
            strip.StartRequested -= TryStart;
            hud.Speed.SpeedRequested -= OnSpeedSegment;
            menuButton.onClick.RemoveListener(OpenMenuFromButton);
            pause.ResumeRequested -= CloseMenu;
            pause.SettingsRequested -= OpenSettings;
            pause.AbandonRequested -= AskAbandon;
            confirm.KeepRequested -= CancelAbandon;
            confirm.AbandonConfirmed -= Abandon;
            settings.BackRequested -= CloseSettings;
            board.ItemSold -= OnItemSold;
            board.BoardChanged -= OnBoardChanged;
            if (liveCombat != null) liveCombat.StageReturnedToPlanning -= HandleLiveStageReturned;
        }

        private void OnItemSold(Project05.Items.ItemRecord record, int price)
        {
            _earned += price;
            if (liveCombat != null) liveCombat.AddCurrency(price);
            else hud.SetCurrency(CombatHudFixtures.Create(HudFixture.Planning).Currency + _earned);
        }

        private void OnBoardChanged()
        {
            if (liveCombat != null) liveCombat.RefreshFromExternalBoard();
        }

        private void Start()
        {
            _model.RestoreSpeed(GameSettings.SpeedIndex);
            if (liveCombat != null)
            {
                liveCombat.InitializeIntegrated(board, hud, strip, popup, (RectTransform)safeFrameForLive(), _model.SpeedIndex);
                strip.SetPhase(ScreenPhase.Planning);
                hud.Speed.SetSkipArmed(false);
            }
            else
            {
                ShowPlanning();
            }
            SyncModals();
        }

        private Transform safeFrameForLive() => hud != null ? hud.transform.parent : transform;

        // ------------------------------------------------------------------ per-frame
        private void Update()
        {
            _model.ItemHeld = board.Held != null;
            _model.PopupOpen = popup.Current != null;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) HandleEscape();
                else if (!_model.Paused) HandleShortcuts(kb);
            }

            if (_model.Phase == ScreenPhase.Playback && liveCombat == null)
            {
                if (_skipLandAt >= 0f)
                {
                    if (Time.unscaledTime >= _skipLandAt) EndPlayback();
                }
                else if (_playback.Advance(Time.unscaledDeltaTime, _model.SpeedIndex, _model.Paused))
                {
                    if (_playback.Finished) EndPlayback();
                    else ShowBeat();
                }
            }

            if (liveCombat != null)
            {
                liveCombat.SetMonsterSelectorVisible(_model.Phase == ScreenPhase.Planning && !_model.ItemHeld);
            }

            // a won room's reward steps (buff, item, door) come before the next Start
            strip.SetStartEnabled(_model.CanStart && (liveCombat == null || liveCombat.CanStartFight), _model.ItemHeld);
            if (cursor != null) cursor.Shape = board.Held != null ? CursorShape.Grab : CursorShape.Arrow;
        }

        /// <summary>Selling is a planning action: open in Planning with no modal, closed otherwise. Set on every change.</summary>
        private void UpdateSellGate()
        {
            bool canSell = _model.Phase == ScreenPhase.Planning && !_model.Paused && (liveCombat == null || liveCombat.CanStartFight);
            if (board.SellEnabled != canSell) board.SellEnabled = canSell;
        }

        private void HandleShortcuts(Keyboard kb)
        {
            if (_model.Phase == ScreenPhase.Planning)
            {
                if (kb.ctrlKey.isPressed && kb.enterKey.wasPressedThisFrame) TryStart();
                return;
            }

            if (kb.digit1Key.wasPressedThisFrame) SelectSpeed(0);
            else if (kb.digit2Key.wasPressedThisFrame) SelectSpeed(1);
            else if (kb.digit3Key.wasPressedThisFrame) SelectSpeed(2);
            else if (kb.kKey.wasPressedThisFrame) ArmSkip();
        }

        // ------------------------------------------------------------------ Esc
        /// <summary>One Esc press, in the documented priority.</summary>
        public void HandleEscape()
        {
            _model.ItemHeld = board.Held != null;
            _model.PopupOpen = popup.Current != null;
            switch (_model.Escape())
            {
                case EscResult.ClosedPopup:
                    popup.Close();
                    break;
                case EscResult.CancelledHeldItem:
                    board.Cancel();
                    break;
                case EscResult.OpenedMenu:
                    _returnFocus = menuButton.gameObject;
                    break;
            }

            SyncModals();
        }

        // ------------------------------------------------------------------ Start and speed
        /// <summary>Start was requested by the button or Ctrl+Enter.</summary>
        public void TryStart()
        {
            if (!_model.RequestStart(Time.unscaledTime)) return;
            UpdateSellGate();
            _skipLandAt = -1f;
            if (liveCombat != null)
            {
                board.InputLocked = true;
                strip.SetPhase(ScreenPhase.Playback);
                hud.Speed.SetSkipArmed(false);
                hud.Speed.Focus(_model.SpeedIndex);
                if (!liveCombat.StartIntegratedFight(_model.SpeedIndex))
                {
                    board.InputLocked = false;
                    _model.FinishPlayback();
                    UpdateSellGate();
                    strip.SetPhase(ScreenPhase.Planning);
                }
                return;
            }

            _playback.Restart();
            ShowBeat();
            strip.SetPhase(ScreenPhase.Playback);
            hud.Speed.SetSkipArmed(false);
            hud.Speed.Focus(_model.SpeedIndex);   // the current speed has focus on entering playback
        }

        private void OnSpeedSegment(int index)
        {
            if (index <= 2) SelectSpeed(index);
            else ArmSkip();
        }

        private void SelectSpeed(int index)
        {
            if (!_model.SelectSpeed(index)) return;
            GameSettings.SpeedIndex = index;      // the speed is remembered; Skip never is
            hud.Speed.SetSkipArmed(false);
            hud.Speed.Apply(_model.SpeedIndex, SpritesOf(hud));
            if (liveCombat != null) liveCombat.SetPlaybackSpeed(index);
        }

        private void ArmSkip()
        {
            if (!_model.ArmSkip(Time.unscaledTime)) return;
            hud.Speed.SetSkipArmed(true);
            if (liveCombat != null)
            {
                liveCombat.ArmSkip();
                return;
            }

            _playback.SkipToEnd();
            ShowBeat();
            _skipLandAt = Time.unscaledTime + SkipOrientHold;
        }

        private void HandleLiveStageReturned()
        {
            _skipLandAt = -1f;
            _model.FinishPlayback();
            board.InputLocked = false;
            UpdateSellGate();
            strip.SetPhase(ScreenPhase.Planning);
            hud.Speed.SetSkipArmed(false);
        }

        private void ShowBeat()
        {
            CombatHudData data = CombatHudFixtures.Create(Beats[Mathf.Clamp(_playback.Beat, 0, Beats.Length - 1)]);
            data.SpeedIndex = _model.SpeedIndex;
            data.Currency += _earned;
            hud.Apply(data);
        }

        private void ShowPlanning()
        {
            CombatHudData data = CombatHudFixtures.Create(HudFixture.Planning);
            data.SpeedIndex = _model.SpeedIndex;
            data.Currency += _earned;
            hud.Apply(data);
            strip.SetPhase(ScreenPhase.Planning);
            hud.Speed.SetSkipArmed(false);
        }

        private void EndPlayback()
        {
            _skipLandAt = -1f;
            _model.FinishPlayback();
            UpdateSellGate();
            ShowPlanning();
        }

        // ------------------------------------------------------------------ menu, dialog, settings
        private void OpenMenuFromButton()
        {
            if (_model.MenuOpen) return;
            board.Cancel();                       // a held item goes back before the screen freezes
            popup.Close();
            _returnFocus = menuButton.gameObject;
            _model.OpenMenu();
            SyncModals();
        }

        private void CloseMenu()
        {
            _model.CloseMenu();
            SyncModals();
        }

        private void OpenSettings()
        {
            _model.OpenSettings();
            SyncModals();
        }

        private void CloseSettings()
        {
            _model.CloseSettings();
            SyncModals();
        }

        private void AskAbandon()
        {
            _model.AskAbandon();
            SyncModals();
        }

        private void CancelAbandon()
        {
            _model.CancelAbandon();
            SyncModals();
        }

        private void Abandon()
        {
            AbandonConfirmed?.Invoke();
            if (Application.CanStreamedLevelBeLoaded(titleScene)) SceneManager.LoadScene(titleScene);
        }

        /// <summary>Shows only the top modal, dims the stage, and freezes the screen behind while any modal is open.</summary>
        private void SyncModals()
        {
            confirm.SetVisible(_model.ConfirmOpen);
            settings.SetVisible(_model.SettingsOpen && !_model.ConfirmOpen);
            pause.SetVisible(_model.MenuOpen && !_model.SettingsOpen && !_model.ConfirmOpen);
            stageDim.SetActive(_model.Paused);
            if (liveCombat != null) liveCombat.SetPlaybackPaused(_model.Paused);
            UpdateSellGate();
            foreach (CanvasGroup g in screenGroups)
            {
                g.interactable = !_model.Paused;
                g.blocksRaycasts = !_model.Paused;
            }

            if (!_model.Paused && _returnFocus != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_returnFocus);
            }
        }

        private static HudSpriteSet SpritesOf(CombatHudView view) => view.Sprites;
    }
}
