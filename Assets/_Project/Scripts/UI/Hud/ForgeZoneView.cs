using Project05.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>What the forge says about the held item (item-upgrades.md): can it be upgraded, for how much, into what.</summary>
    public struct ForgeQuote
    {
        /// <summary>True when dropping the item upgrades it now.</summary>
        public bool Ok;
        /// <summary>Gold the upgrade costs (0 when there is no upgrade).</summary>
        public int Cost;
        /// <summary>The upgraded (+) record, or null.</summary>
        public ItemRecord Upgrade;
        /// <summary>Why it cannot be upgraded ("Thiếu 4 vàng", "Đã rèn +"), or empty.</summary>
        public string Reason;
        /// <summary>The before -> after line shown while the item hovers the frame.</summary>
        public string Preview;
    }

    /// <summary>How the forge zone looks.</summary>
    public enum ForgeZoneState { Idle, Ready, Over, Refused, Locked }

    /// <summary>
    /// The forge ("Rèn") zone inside the drawer, twin of the sell zone: an anvil, a framed drop area, the cost and, while a
    /// held item hovers it, the before -> after numbers. Display only; the board decides and the scene pays.
    /// TEMPORARY: Vietnamese literals until the localization system exists.
    /// </summary>
    public sealed class ForgeZoneView : MonoBehaviour
    {
        [SerializeField] private Image anvil;
        [SerializeField] private Image frame;
        [SerializeField] private Sprite anvilIdle;
        [SerializeField] private Sprite anvilActive;
        [SerializeField] private Sprite anvilStrike;
        [SerializeField] private Sprite frameIdle;
        [SerializeField] private Sprite frameReady;
        [SerializeField] private Sprite frameOver;
        [SerializeField] private Sprite frameInvalid;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private TMP_Text preview;
        [SerializeField] private GameObject costRow;
        [SerializeField] private SpriteNumeralText cost;

        private float _strikeUntil = -1f;

        /// <summary>The state shown now.</summary>
        public ForgeZoneState State { get; private set; }

        /// <summary>Shows a state with the quote of the held item.</summary>
        public void Show(ForgeZoneState state, ForgeQuote quote)
        {
            State = state;
            bool held = state == ForgeZoneState.Ready || state == ForgeZoneState.Over || state == ForgeZoneState.Refused;
            if (Time.unscaledTime >= _strikeUntil) anvil.sprite = held ? anvilActive : anvilIdle;
            frame.sprite = state == ForgeZoneState.Over && quote.Ok ? frameOver
                : (state == ForgeZoneState.Refused || (held && !quote.Ok)) ? frameInvalid
                : held ? frameReady : frameIdle;
            hint.text = state switch
            {
                ForgeZoneState.Locked => "Chỉ rèn khi xếp đồ",
                ForgeZoneState.Idle => "Kéo đồ vào đây",
                _ when !quote.Ok => quote.Reason,
                ForgeZoneState.Over => "Thả để rèn",
                _ => "Thả vào khung",
            };
            preview.text = state == ForgeZoneState.Over || (held && !quote.Ok && quote.Upgrade != null) ? quote.Preview : string.Empty;
            costRow.SetActive(held && quote.Cost > 0);
            if (quote.Cost > 0) cost.Text = quote.Cost.ToString();
        }

        /// <summary>The hammer strikes once (an upgrade happened).</summary>
        public void Strike()
        {
            anvil.sprite = anvilStrike;
            _strikeUntil = Time.unscaledTime + 0.25f;
        }

        private void Update()
        {
            if (_strikeUntil > 0f && Time.unscaledTime >= _strikeUntil)
            {
                _strikeUntil = -1f;
                anvil.sprite = anvilIdle;
            }
        }
    }
}
