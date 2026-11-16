using System;
using UnityEngine;

namespace Project05.UI
{
    /// <summary>The cursor shapes the game uses.</summary>
    public enum CursorShape
    {
        /// <summary>The default pointer.</summary>
        Arrow,

        /// <summary>A closed hand, while an item is held.</summary>
        Grab,
    }

    /// <summary>
    /// Shows the palette-locked pixel cursor at the same whole-number zoom as the canvas (art bible 8.1). Hardware cursors are
    /// not scaled by the canvas, so each shape comes pre-scaled for zoom 1 to 4. Display only.
    /// </summary>
    public sealed class PixelCursor : MonoBehaviour
    {
        /// <summary>One cursor shape at zoom 1 to 4, with its hot spot in zoom-1 pixels.</summary>
        [Serializable]
        private sealed class ShapeSet
        {
            public Texture2D[] byZoom = Array.Empty<Texture2D>();
            public Vector2 hotSpot;
        }

        [SerializeField] private ShapeSet arrow = new ShapeSet();
        [SerializeField] private ShapeSet grab = new ShapeSet();

        private CursorShape _shape;
        private int _zoom = -1;
        private bool _dirty = true;

        /// <summary>The shape to show. Changing it applies on the next frame.</summary>
        public CursorShape Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                _dirty = true;
            }
        }

        /// <summary>Picks the pre-scaled texture for a zoom: zoom 1 to 4, larger zooms use the zoom-4 cursor.</summary>
        public static int TextureIndex(int zoom, int available) => Mathf.Clamp(zoom, 1, Mathf.Max(1, available)) - 1;

        private void OnDisable() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        private void Update()
        {
            int zoom = IntegerCanvasScale.ComputeZoom(Screen.width, Screen.height);
            if (zoom != _zoom)
            {
                _zoom = zoom;
                _dirty = true;
            }

            if (!_dirty) return;
            _dirty = false;
            ShapeSet set = _shape == CursorShape.Grab ? grab : arrow;
            if (set.byZoom.Length == 0) return;
            int index = TextureIndex(_zoom, set.byZoom.Length);
            int scale = index + 1;
            Cursor.SetCursor(set.byZoom[index], set.hotSpot * scale, CursorMode.Auto);
        }
    }
}
