using System;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The combat HUD (design/gdd/combat-hud.md, art bible 7.3 to 7.5). It draws one <see cref="CombatHudData"/> and
    /// owns no game state: it never resolves combat, picks targets or advances playback. Enemy slots are laid out
    /// in one or two vertical lines, centred per line, and never reflow when a unit falls (art bible 7.4a).
    /// </summary>
    public sealed class CombatHudView : MonoBehaviour
    {
        /// <summary>One numeral cut used for readouts, in Bone and in Blood (for the minus of damage taken).</summary>
        [Serializable]
        private sealed class ReadoutFont
        {
            public int cut;
            public Sprite boneAtlas;
            public TextAsset boneMetrics;
            public Sprite bloodAtlas;
            public TextAsset bloodMetrics;
        }

        /// <summary>Top of the stage scene area inside the safe frame, in native px.</summary>
        public const int StageTop = 19;

        /// <summary>Height of the stage scene area, in native px.</summary>
        public const int StageHeight = 314;

        /// <summary>Horizontal centre of the enemy column (between the section line at 314 and the frame edge at 637).</summary>
        public const int ColumnCenter = 476;

        /// <summary>Distance between the centres of enemy line 1 and line 2.</summary>
        public const int LineGap = 140;

        [SerializeField] private HudSpriteSet sprites;
        [SerializeField] private HeroSlotView[] heroes = Array.Empty<HeroSlotView>();
        [SerializeField] private EnemySlotView[] enemies = Array.Empty<EnemySlotView>();
        [SerializeField] private OrderStripView order;
        [SerializeField] private SpeedControlView speed;
        [SerializeField] private SpriteNumeralText currency;
        [SerializeField] private SpriteNumeralText room;
        [SerializeField] private RectTransform readoutLayer;
        [SerializeField] private ReadoutFont[] readoutFonts = Array.Empty<ReadoutFont>();

        /// <summary>The speed control, so playback can listen to it.</summary>
        public SpeedControlView Speed => speed;

        /// <summary>The sprite set the HUD draws with.</summary>
        public HudSpriteSet Sprites => sprites;

        /// <summary>Redraws only the currency readout.</summary>
        public void SetCurrency(int value) => currency.Text = value.ToString();

        /// <summary>Draws a full HUD state.</summary>
        public void Apply(CombatHudData data)
        {
            for (int i = 0; i < heroes.Length; i++)
            {
                heroes[i].gameObject.SetActive(i < data.Heroes.Length && data.Heroes[i] != null);
                if (heroes[i].gameObject.activeSelf) heroes[i].Apply(data.Heroes[i], sprites);
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Apply(i < data.Enemies.Length ? data.Enemies[i] : null, sprites);
            }

            LayoutEnemies(data);
            order.Apply(data.Order, sprites);
            speed.Apply(data.SpeedIndex, sprites);
            currency.Text = data.Currency.ToString();
            room.Text = data.Room + "/" + data.RoomCount;
            ApplyReadouts(data);
        }

        /// <summary>
        /// Places enemies in two lines of up to three. Each line is centred in the stage on its own, so slots in
        /// different lines are never aligned on purpose (art bible 7.4a).
        /// </summary>
        public void LayoutEnemies(CombatHudData data)
        {
            // One line stands in the middle of the enemy column; two lines share it, one either side of the middle.
            bool twoLines = Slot(data, 3) != null || Slot(data, 4) != null || Slot(data, 5) != null;
            for (int line = 0; line < 2; line++)
            {
                float centreX = twoLines ? ColumnCenter + LineGap * (line - 0.5f) : ColumnCenter;
                int total = 0;
                for (int k = 0; k < 3; k++)
                {
                    EnemySlotData e = Slot(data, line * 3 + k);
                    if (e != null) total += EnemySlotView.Pitch(sprites.monsters[e.Visual].bodyHeight);
                }

                float y = StageTop + (StageHeight - total) / 2;
                for (int k = 0; k < 3; k++)
                {
                    int index = line * 3 + k;
                    EnemySlotData e = Slot(data, index);
                    if (e == null) continue;
                    var rt = (RectTransform)enemies[index].transform;
                    rt.anchoredPosition = new Vector2(centreX, -y);
                    y += EnemySlotView.Pitch(sprites.monsters[e.Visual].bodyHeight);
                }
            }
        }

        private static EnemySlotData Slot(CombatHudData data, int index)
        {
            return index < data.Enemies.Length && data.Enemies[index] != null && data.Enemies[index].Present ? data.Enemies[index] : null;
        }

        private void ApplyReadouts(CombatHudData data)
        {
            for (int i = readoutLayer.childCount - 1; i >= 0; i--)
            {
                GameObject child = readoutLayer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            foreach (ReadoutData r in data.Readouts)
            {
                ReadoutFont font = Array.Find(readoutFonts, f => f.cut == r.Cut);
                if (font == null) continue;

                Vector2 centre = r.OnHero
                    ? ((RectTransform)heroes[r.Slot].transform).anchoredPosition + new Vector2(0f, -44f)
                    : ((RectTransform)enemies[r.Slot].transform).anchoredPosition + new Vector2(0f, -(26f + sprites.monsters[data.Enemies[r.Slot].Visual].bodyHeight / 2f));
                Spawn(r, font, centre);
            }
        }

        private void Spawn(ReadoutData r, ReadoutFont font, Vector2 centre)
        {
            SpriteNumeralText digits = NewNumeral("digits", font.boneAtlas, font.boneMetrics, r.Text);
            float width = digits.Width;
            SpriteNumeralText minus = null;
            if (r.Taken)
            {
                minus = NewNumeral("minus", font.bloodAtlas, font.bloodMetrics, "-");
                width += minus.Width + 1;
            }

            float x = centre.x - width / 2f;
            if (minus != null)
            {
                Place(minus, x, centre.y);
                x += minus.Width + 1;
            }

            Place(digits, x, centre.y);
        }

        private SpriteNumeralText NewNumeral(string label, Sprite atlas, TextAsset metrics, string text)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(readoutLayer, false);
            var numeral = go.AddComponent<SpriteNumeralText>();
            numeral.Configure(atlas, metrics, SpriteNumeralText.Align.Left);
            numeral.Text = text;
            return numeral;
        }

        private static void Place(SpriteNumeralText numeral, float x, float y)
        {
            var rt = (RectTransform)numeral.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
