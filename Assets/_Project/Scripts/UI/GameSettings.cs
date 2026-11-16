using UnityEngine;

namespace Project05.UI
{
    /// <summary>Where preferences are kept. Injected so tests do not touch the machine.</summary>
    public interface ISettingsStore
    {
        /// <summary>Reads an integer, or the default when the key is absent.</summary>
        int GetInt(string key, int defaultValue);

        /// <summary>Writes an integer.</summary>
        void SetInt(string key, int value);

        /// <summary>Makes pending writes durable.</summary>
        void Save();
    }

    /// <summary>Stores preferences in Unity PlayerPrefs.</summary>
    public sealed class PlayerPrefsStore : ISettingsStore
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// Player preferences that change presentation only (design/gdd/settings.md rules 2 to 6): reduced flashes (default on),
    /// reduced motion, "Skip goes straight to the end" (default off) and the remembered playback speed.
    /// A stand-in for System 37 until it exists: no validation by owners, no unsaved-write feedback, no schema version.
    /// Skip is never stored as a speed.
    /// </summary>
    public static class GameSettings
    {
        private const string FlashesKey = "grimpack.reducedFlashes";
        private const string MotionKey = "grimpack.reducedMotion";
        private const string SkipEndKey = "grimpack.skipToEnd";
        private const string SpeedKey = "grimpack.speed";

        private static ISettingsStore _store = new PlayerPrefsStore();

        /// <summary>Replaces the store, for tests.</summary>
        public static void UseStore(ISettingsStore store) => _store = store;

        /// <summary>Bloom becomes a steady glow and pop-ins lose the tier pulse. Default on (settings.md rule 3).</summary>
        public static bool ReducedFlashes
        {
            get => _store.GetInt(FlashesKey, 1) == 1;
            set => Write(FlashesKey, value);
        }

        /// <summary>Replaces motion by fades of equal duration. Default off.</summary>
        public static bool ReducedMotion
        {
            get => _store.GetInt(MotionKey, 0) == 1;
            set
            {
                Write(MotionKey, value);
                LanternPoolBreathing.ReducedMotion = value;
            }
        }

        /// <summary>Skip jumps straight to the final view instead of stopping at each Detonation. Default off.</summary>
        public static bool SkipGoesToEnd
        {
            get => _store.GetInt(SkipEndKey, 0) == 1;
            set => Write(SkipEndKey, value);
        }

        /// <summary>The remembered speed: 0 for 1x, 1 for 2x, 2 for 4x. Clamped, so Skip can never be stored.</summary>
        public static int SpeedIndex
        {
            get => Mathf.Clamp(_store.GetInt(SpeedKey, 0), 0, 2);
            set
            {
                _store.SetInt(SpeedKey, Mathf.Clamp(value, 0, 2));
                _store.Save();
            }
        }

        /// <summary>Applies stored preferences that live outside this class. Runs before the first scene loads.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            LanternPoolBreathing.ReducedMotion = ReducedMotion;
        }

        private static void Write(string key, bool value)
        {
            _store.SetInt(key, value ? 1 : 0);
            _store.Save();
        }
    }
}
