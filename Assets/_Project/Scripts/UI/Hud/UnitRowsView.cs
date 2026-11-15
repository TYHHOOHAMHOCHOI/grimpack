using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The HP row and the stat row shared by hero and enemy slots (art bible 7.4, 7.4a): HP bar, armour,
    /// speed gauge and up to three status chips. The stat row clears when the unit is down.
    /// </summary>
    public sealed class UnitRowsView : MonoBehaviour
    {
        /// <summary>Width in native pixels of the speed gauge fill.</summary>
        public const int GaugeWidth = 18;

        [SerializeField] private HpBarView hp;
        [SerializeField] private GameObject statRow;
        [SerializeField] private SpriteNumeralText armour;
        [SerializeField] private RectTransform gaugeFill;
        [SerializeField] private StatusChipView[] chips = Array.Empty<StatusChipView>();

        /// <summary>Shows HP, armour, speed and statuses.</summary>
        public void Apply(int hpNow, int hpMax, int armourNow, int speedGauge, IReadOnlyList<StatusChipData> statuses, HudSpriteSet sprites)
        {
            hp.SetHp(hpNow, hpMax);
            statRow.SetActive(hpNow > 0);
            armour.Text = armourNow.ToString();
            gaugeFill.sizeDelta = new Vector2(Mathf.Clamp(speedGauge, 0, GaugeWidth), gaugeFill.sizeDelta.y);
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i].Apply(statuses != null && i < statuses.Count ? statuses[i] : null, sprites);
            }
        }
    }
}
