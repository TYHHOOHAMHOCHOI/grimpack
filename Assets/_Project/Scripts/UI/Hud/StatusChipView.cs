using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>One status chip: glyph and amount on a plain chip (art bible 5.11, 7.4).</summary>
    public sealed class StatusChipView : MonoBehaviour
    {
        [SerializeField] private Image glyph;
        [SerializeField] private SpriteNumeralText amount;
        private Sprite _plain;

        /// <summary>Shows or hides the chip and sets its contents.</summary>
        public void Apply(StatusChipData data, HudSpriteSet sprites)
        {
            gameObject.SetActive(data != null);
            if (data == null) return;
            glyph.sprite = sprites.Status(data.Kind);
            amount.Text = data.Amount.ToString();
            var frame = GetComponent<Image>();
            if (frame != null)
            {
                if (_plain == null) _plain = frame.sprite;
                Sprite tinted = sprites.StatusChip(data.Kind);
                frame.sprite = tinted != null ? tinted : _plain;   // the debuff's colour frames its chip (art bible 4.6 amendment)
            }
        }
    }
}
