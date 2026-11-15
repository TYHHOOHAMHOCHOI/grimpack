using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The order strip in the top strip (art bible 7.5): plain chips, a join numeral for a hero and an intent glyph
    /// for an enemy. The first chip is the acting unit and carries the Candle marker. It shows a forecast, so it is
    /// labelled by the caller as "if nothing changes".
    /// </summary>
    public sealed class OrderStripView : MonoBehaviour
    {
        [Serializable]
        private sealed class Chip
        {
            public GameObject root;
            public SpriteNumeralText numeral;
            public Image glyph;
            public GameObject marker;
        }

        [SerializeField] private Chip[] chips = Array.Empty<Chip>();

        /// <summary>Shows up to one chip per entry; extra chips hide.</summary>
        public void Apply(IReadOnlyList<OrderEntryData> entries, HudSpriteSet sprites)
        {
            for (int i = 0; i < chips.Length; i++)
            {
                Chip chip = chips[i];
                bool shown = i < entries.Count;
                chip.root.SetActive(shown);
                if (!shown) continue;

                OrderEntryData e = entries[i];
                chip.numeral.gameObject.SetActive(e.IsHero);
                chip.glyph.gameObject.SetActive(!e.IsHero);
                if (e.IsHero) chip.numeral.Text = e.JoinNumeral.ToString();
                else chip.glyph.sprite = sprites.Intent(e.Intent, 8);
                chip.marker.SetActive(i == 0);
            }
        }
    }
}
