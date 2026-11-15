using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>How the sell zone looks.</summary>
    public enum SellZoneState
    {
        /// <summary>Nothing held: quiet chrome and a hint.</summary>
        Idle,

        /// <summary>An item is held: the zone lights up and shows its price.</summary>
        Ready,

        /// <summary>The held item is over the zone: releasing sells it.</summary>
        Over,

        /// <summary>Selling is not allowed now (playback): quiet chrome, no price.</summary>
        Locked,
    }

    /// <summary>
    /// The sell zone under the stash: a scale, a framed drop area and the price of the held item. Display only; the board
    /// controller decides when an item is sold. State is shown by frame, text and price, never by colour alone.
    /// TEMPORARY: the texts are Vietnamese literals set by the scene builder until the localization system exists.
    /// </summary>
    public sealed class SellZoneView : MonoBehaviour
    {
        [SerializeField] private Image scale;
        [SerializeField] private Image frame;
        [SerializeField] private Sprite scaleIdle;
        [SerializeField] private Sprite scaleActive;
        [SerializeField] private Sprite frameIdle;
        [SerializeField] private Sprite frameReady;
        [SerializeField] private Sprite frameOver;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private GameObject priceRow;
        [SerializeField] private SpriteNumeralText price;
        [SerializeField] private string idleText = string.Empty;
        [SerializeField] private string readyText = string.Empty;
        [SerializeField] private string overText = string.Empty;
        [SerializeField] private string lockedText = string.Empty;

        /// <summary>The state shown now.</summary>
        public SellZoneState State { get; private set; }

        /// <summary>Shows a state, with the price of the held item when one is held.</summary>
        public void Show(SellZoneState state, int itemPrice)
        {
            State = state;
            bool lit = state == SellZoneState.Ready || state == SellZoneState.Over;
            scale.sprite = lit ? scaleActive : scaleIdle;
            frame.sprite = state == SellZoneState.Over ? frameOver : lit ? frameReady : frameIdle;
            hint.text = state switch
            {
                SellZoneState.Ready => readyText,
                SellZoneState.Over => overText,
                SellZoneState.Locked => lockedText,
                _ => idleText,
            };
            priceRow.SetActive(lit);
            if (lit) price.Text = itemPrice.ToString();
        }
    }
}
