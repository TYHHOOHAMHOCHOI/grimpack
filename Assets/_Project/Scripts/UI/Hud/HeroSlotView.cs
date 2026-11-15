using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>One hero slot: pedestal, body, avatars, join numeral, turn marker and rows (art bible 7.4).</summary>
    public sealed class HeroSlotView : MonoBehaviour
    {
        [SerializeField] private Image pedestal;
        [SerializeField] private RectTransform bodyRect;
        [SerializeField] private Image body;
        [SerializeField] private Image narrow;
        [SerializeField] private Image chevron;
        [SerializeField] private SpriteNumeralText join;
        [SerializeField] private UnitRowsView rows;
        [SerializeField] private AvatarView[] avatars = Array.Empty<AvatarView>();

        /// <summary>Body Y position inside the slot when standing, in native px (the ready state lifts it by 1).</summary>
        [SerializeField] private float bodyRestY = -20f;

        /// <summary>Shows one hero. A fallen hero holds the last down frame on a dim pedestal with its pool out.</summary>
        public void Apply(HeroSlotData data, HudSpriteSet sprites)
        {
            HeroPose pose = data.Fallen ? HeroPose.Down2 : data.Pose;
            body.sprite = sprites.heroPoses[(int)pose];
            narrow.sprite = sprites.heroNarrow;
            narrow.enabled = data.Ready && !data.Fallen;
            bodyRect.anchoredPosition = new Vector2(bodyRect.anchoredPosition.x, bodyRestY + (data.Ready ? 1f : 0f));
            pedestal.sprite = data.Fallen ? sprites.pedestalDim : sprites.pedestalDefault;
            join.Text = data.JoinNumeral.ToString();
            chevron.sprite = sprites.chevronTurnHero;
            chevron.enabled = data.Acting && !data.Fallen;
            rows.Apply(data.Hp, data.MaxHp, data.Armour, data.SpeedGauge, data.Statuses, sprites);
            for (int i = 0; i < avatars.Length; i++)
            {
                avatars[i].Apply(i < data.Avatars.Count ? data.Avatars[i] : null, sprites);
            }
        }
    }
}
