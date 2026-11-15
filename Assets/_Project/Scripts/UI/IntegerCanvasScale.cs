using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI
{
    /// <summary>
    /// Keeps a 640 x 360 native-resolution canvas at a whole-number zoom (art bible 8.1):
    /// z = max(1, min(floor(W / 640), floor(H / 360))). The scale is never fractional, so a
    /// point-filtered pixel is always a whole number of screen pixels. Anything outside the
    /// 640 x 360 safe frame shows the camera clear colour (INK-0), not black bars.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class IntegerCanvasScale : MonoBehaviour
    {
        /// <summary>Native width of the safe frame in pixels.</summary>
        public const int NativeWidth = 640;

        /// <summary>Native height of the safe frame in pixels.</summary>
        public const int NativeHeight = 360;

        private CanvasScaler _scaler;
        private int _lastWidth;
        private int _lastHeight;

        /// <summary>
        /// Computes the integer zoom for a display of the given size. Pure, so it can be tested
        /// without a running scene.
        /// </summary>
        /// <param name="displayWidth">Display or window width in pixels.</param>
        /// <param name="displayHeight">Display or window height in pixels.</param>
        /// <returns>A zoom of at least 1.</returns>
        public static int ComputeZoom(int displayWidth, int displayHeight)
        {
            int byWidth = displayWidth / NativeWidth;
            int byHeight = displayHeight / NativeHeight;
            return Mathf.Max(1, Mathf.Min(byWidth, byHeight));
        }

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

        private void Apply(bool force)
        {
            int width = Screen.width;
            int height = Screen.height;
            if (!force && width == _lastWidth && height == _lastHeight) return;

            _lastWidth = width;
            _lastHeight = height;
            _scaler.scaleFactor = ComputeZoom(width, height);
        }
    }
}
