using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The order strip in the top strip (art bible 7.5): plain chips, a join numeral for a hero, an intent glyph and the
    /// enemy's letter for an enemy (the same letter its stage slot shows, so two enemies are never confused). The first chip
    /// is the acting unit and carries the Candle marker. Chips are packed left to right after the strip label.
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
            public TMP_Text letter;
        }

        /// <summary>Width of a hero chip and of an enemy chip, and the gap between chips, in native px.</summary>
        public const int HeroWidth = 12, EnemyWidth = 20, Gap = 3;

        [SerializeField] private Chip[] chips = Array.Empty<Chip>();
        [Tooltip("x of the first chip, native px from the safe-frame left.")]
        [SerializeField] private float startX;

        /// <summary>Letter of an enemy slot: 0 is A.</summary>
        public static string Letter(int slot) => ((char)('A' + Mathf.Clamp(slot, 0, 25))).ToString();

        /// <summary>Shows up to one chip per entry; extra chips hide.</summary>
        public void Apply(IReadOnlyList<OrderEntryData> entries, HudSpriteSet sprites)
        {
            float x = startX;
            for (int i = 0; i < chips.Length; i++)
            {
                Chip chip = chips[i];
                bool shown = i < entries.Count;
                chip.root.SetActive(shown);
                if (!shown) continue;

                OrderEntryData e = entries[i];
                chip.numeral.gameObject.SetActive(e.IsHero);
                chip.glyph.gameObject.SetActive(!e.IsHero);
                if (chip.letter != null) chip.letter.gameObject.SetActive(!e.IsHero);
                if (e.IsHero) chip.numeral.Text = e.JoinNumeral.ToString();
                else
                {
                    chip.glyph.sprite = sprites.Intent(e.Intent, 8);
                    if (chip.letter != null) chip.letter.text = Letter(e.EnemySlot);
                }

                chip.marker.SetActive(i == 0);
                var rt = (RectTransform)chip.root.transform;
                float width = e.IsHero ? HeroWidth : EnemyWidth;
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
                ((RectTransform)chip.marker.transform).anchoredPosition = new Vector2(Mathf.Floor((width - 6) / 2f), -12f);
                x += width + Gap;
            }
        }
    }
}
