using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>One enemy slot: intent tab, body, lit eyes, shadow, turn marker and rows (art bible 7.4a).</summary>
    public sealed class EnemySlotView : MonoBehaviour
    {
        [SerializeField] private RectTransform tab;
        [SerializeField] private Image tabGlyph;
        [SerializeField] private SpriteNumeralText magnitude;
        [SerializeField] private SpriteNumeralText target;
        [SerializeField] private RectTransform bodyRect;
        [SerializeField] private Image body;
        [SerializeField] private Image eyes;
        [SerializeField] private RectTransform shadowRect;
        [SerializeField] private RectTransform chevronRect;
        [SerializeField] private Image chevron;
        [SerializeField] private RectTransform rowsRect;

        /// <summary>The body image, for hit effects.</summary>
        public Image Body => body;

        /// <summary>The intent glyph shown on the tab, which flies to the target when the enemy acts (art bible 5.10.8).</summary>
        public Sprite IntentSprite => tabGlyph != null ? tabGlyph.sprite : null;

        /// <summary>The lit eyes, hidden with the body while it blinks.</summary>
        public Image Eyes => eyes;

        /// <summary>Slot pitch for a body height: intent tab zone 26, body and headroom, shadow row 4, HP row 10, stat row 12.</summary>
        public static int Pitch(int bodyHeight) => bodyHeight + 56;

        /// <summary>Shows one enemy, or hides the slot when no enemy stands here. Slots never reflow (art bible 7.4a).</summary>
        public void Apply(EnemySlotData data, HudSpriteSet sprites)
        {
            gameObject.SetActive(data != null && data.Present);
            if (data == null || !data.Present) return;

            HudSpriteSet.MonsterVisual visual = sprites.monsters[data.Visual];
            int ground = 26 + visual.bodyHeight + 4;
            Sprite pose = visual.poses[(int)data.Pose];
            body.sprite = pose;
            bodyRect.sizeDelta = pose.rect.size;
            bodyRect.anchoredPosition = new Vector2(-pose.rect.width / 2f, -(ground - (pose.rect.height - 4f)));
            eyes.rectTransform.anchoredPosition = bodyRect.anchoredPosition;
            eyes.rectTransform.sizeDelta = bodyRect.sizeDelta;
            int eyeIndex = data.Pose == MonsterPose.Act ? 2 : data.Pose == MonsterPose.Idle2 ? 1 : 0;
            eyes.sprite = visual.eyes[eyeIndex];
            eyes.enabled = data.EyesLit;
            shadowRect.anchoredPosition = new Vector2(-15f, -ground);
            chevronRect.anchoredPosition = new Vector2(-3f, -ground);
            chevron.sprite = sprites.chevronTurnFoe;
            chevron.enabled = data.Acting;
            rowsRect.anchoredPosition = new Vector2(0f, -(ground + 4));

            tabGlyph.sprite = sprites.Intent(data.Intent, 12);
            tabGlyph.enabled = !data.IntentAway && data.Intent != IntentKind.None;   // the glyph slot goes dark while the glyph is away
            magnitude.Text = data.Magnitude;
            target.Text = data.TargetJoinNumeral > 0 ? data.TargetJoinNumeral.ToString() : string.Empty;
            tab.gameObject.SetActive(data.Intent != IntentKind.None || data.IntentAway);
            GetComponentInChildren<UnitRowsView>(true).Apply(data.Hp, data.MaxHp, data.Armour, data.SpeedGauge, data.Statuses, sprites);
        }
    }
}
