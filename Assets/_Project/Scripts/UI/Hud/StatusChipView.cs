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

        /// <summary>Shows or hides the chip and sets its contents.</summary>
        public void Apply(StatusChipData data, HudSpriteSet sprites)
        {
            gameObject.SetActive(data != null);
            if (data == null) return;
            glyph.sprite = sprites.Status(data.Kind);
            amount.Text = data.Amount.ToString();
        }
    }
}
