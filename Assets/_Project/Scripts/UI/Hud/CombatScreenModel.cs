namespace Project05.UI.Hud
{
    /// <summary>The two phases of the action strip (art bible 7.3).</summary>
    public enum ScreenPhase
    {
        /// <summary>Arranging: the strip shows the primary Start action.</summary>
        Planning,

        /// <summary>Watching: the strip shows the speed control.</summary>
        Playback,
    }

    /// <summary>What one Esc press did (input-and-focus.md rule 4).</summary>
    public enum EscResult
    {
        None,
        ClosedConfirm,
        ClosedSettings,
        ClosedPopup,
        CancelledHeldItem,
        ClosedMenu,
        OpenedMenu,
    }

    /// <summary>
    /// The rules behind the screen buttons, free of the engine so they can be tested: which button is allowed when,
    /// the Esc priority, the speed and Skip rules. Presentation only; nothing here touches a run, a fight or a reward.
    /// </summary>
    public sealed class CombatScreenModel
    {
        /// <summary>Minimum time after Start before Skip is accepted, in seconds (input-and-focus.md rule 7e).</summary>
        public const float InputLock = 0.25f;

        private float _startedAt;

        /// <summary>Current phase.</summary>
        public ScreenPhase Phase { get; private set; } = ScreenPhase.Planning;

        /// <summary>0 for 1x, 1 for 2x, 2 for 4x. Remembered; Skip is not a speed.</summary>
        public int SpeedIndex { get; private set; }

        /// <summary>True while Skip is armed and has not yet landed.</summary>
        public bool SkipArmed { get; private set; }

        /// <summary>The pause menu is open.</summary>
        public bool MenuOpen { get; private set; }

        /// <summary>The settings panel is open (above the pause menu or the Title).</summary>
        public bool SettingsOpen { get; private set; }

        /// <summary>The abandon confirmation is open.</summary>
        public bool ConfirmOpen { get; private set; }

        /// <summary>Set by the controller before it asks for an Esc result: an item is held on the workbench.</summary>
        public bool ItemHeld { get; set; }

        /// <summary>Set by the controller before it asks for an Esc result: the item popup is open.</summary>
        public bool PopupOpen { get; set; }

        /// <summary>True while any modal is open: the clock is frozen and the screen behind takes no input.</summary>
        public bool Paused => MenuOpen || SettingsOpen || ConfirmOpen;

        /// <summary>Start is allowed: Planning, nothing held, no modal (input-and-focus.md rule 6).</summary>
        public bool CanStart => Phase == ScreenPhase.Planning && !ItemHeld && !Paused;

        /// <summary>The speed segments and Skip are live: Playback and no modal.</summary>
        public bool PlaybackControlsLive => Phase == ScreenPhase.Playback && !Paused;

        /// <summary>Sets the remembered speed without changing phase, for the saved value at startup.</summary>
        public void RestoreSpeed(int index) => SpeedIndex = index < 0 ? 0 : index > 2 ? 2 : index;

        /// <summary>Starts playback if allowed.</summary>
        /// <param name="now">Seconds, from an unscaled clock.</param>
        /// <returns>True when playback started.</returns>
        public bool RequestStart(float now)
        {
            if (!CanStart) return false;
            Phase = ScreenPhase.Playback;
            SkipArmed = false;
            _startedAt = now;
            return true;
        }

        /// <summary>Selects 1x, 2x or 4x. Cancels an armed Skip. Ignored outside live playback.</summary>
        /// <returns>True when the selection was accepted.</returns>
        public bool SelectSpeed(int index)
        {
            if (!PlaybackControlsLive || index < 0 || index > 2) return false;
            SpeedIndex = index;
            SkipArmed = false;
            return true;
        }

        /// <summary>Arms Skip. A press inside the input lock is refused, not buffered.</summary>
        /// <param name="now">Seconds, from the same clock as <see cref="RequestStart"/>.</param>
        /// <returns>True when Skip was armed.</returns>
        public bool ArmSkip(float now)
        {
            if (!PlaybackControlsLive || SkipArmed || now - _startedAt < InputLock) return false;
            SkipArmed = true;
            return true;
        }

        /// <summary>Playback reached its end: back to Planning, Skip disarmed, the speed kept.</summary>
        public void FinishPlayback()
        {
            Phase = ScreenPhase.Planning;
            SkipArmed = false;
        }

        /// <summary>Opens the pause menu.</summary>
        public void OpenMenu() => MenuOpen = true;

        /// <summary>Closes the pause menu, and anything opened above it.</summary>
        public void CloseMenu()
        {
            MenuOpen = false;
            SettingsOpen = false;
            ConfirmOpen = false;
        }

        /// <summary>Opens the settings panel.</summary>
        public void OpenSettings() => SettingsOpen = true;

        /// <summary>Closes the settings panel.</summary>
        public void CloseSettings() => SettingsOpen = false;

        /// <summary>Opens the abandon confirmation.</summary>
        public void AskAbandon() => ConfirmOpen = true;

        /// <summary>Closes the abandon confirmation (Keep playing).</summary>
        public void CancelAbandon() => ConfirmOpen = false;

        /// <summary>
        /// One Esc press, in the documented priority: close a dialog, close the settings panel, close the popup, cancel a held
        /// item, close the menu, then open the menu. The caller performs the matching action on its views.
        /// </summary>
        public EscResult Escape()
        {
            if (ConfirmOpen)
            {
                ConfirmOpen = false;
                return EscResult.ClosedConfirm;
            }

            if (SettingsOpen)
            {
                SettingsOpen = false;
                return EscResult.ClosedSettings;
            }

            if (PopupOpen) return EscResult.ClosedPopup;
            if (ItemHeld) return EscResult.CancelledHeldItem;
            if (MenuOpen)
            {
                MenuOpen = false;
                return EscResult.ClosedMenu;
            }

            MenuOpen = true;
            return EscResult.OpenedMenu;
        }
    }
}
