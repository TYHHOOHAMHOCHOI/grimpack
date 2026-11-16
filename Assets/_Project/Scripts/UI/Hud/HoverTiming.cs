namespace Project05.UI.Hud
{
    /// <summary>Timing rules of the item popup, free of the engine so they can be tested.</summary>
    public static class HoverTiming
    {
        /// <summary>True when a popup closed recently enough that the next hover should open at once.</summary>
        /// <param name="now">Current time, seconds.</param>
        /// <param name="closedAt">When the last popup closed, seconds; negative infinity if none has closed yet.</param>
        /// <param name="warmWindow">Length of the warm window, seconds.</param>
        public static bool IsWarm(float now, float closedAt, float warmWindow) => now - closedAt <= warmWindow;
    }
}
