using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>An item avatar: a lifted tier plate with its icon (art bible 5.10.2).</summary>
    public sealed class AvatarView : MonoBehaviour
    {
        [SerializeField] private Image plate;
        [SerializeField] private Image icon;

        /// <summary>Shows or hides the avatar.</summary>
        public void Apply(AvatarData data, HudSpriteSet sprites)
        {
            gameObject.SetActive(data != null);
            if (data == null) return;
            plate.sprite = data.Bright ? sprites.plateLiftHi[(int)data.Tier] : sprites.plateLift[(int)data.Tier];
            icon.sprite = sprites.Glyph12(data.Icon);
        }
    }
}
