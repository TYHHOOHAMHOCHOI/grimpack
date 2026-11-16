using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The floating sell drawer on the right edge of the stage (user request 2026-11-16). A tab with an arrow sits at the top
    /// of the edge; activating it springs the drawer out with a small bounce, activating it again tucks it away. With reduced
    /// motion the drawer moves at once. The drawer only shows the sell zone; the board controller decides what is sold.
    /// </summary>
    public sealed class SellDrawerView : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button tab;
        [SerializeField] private Image arrow;
        [SerializeField] private Sprite arrowOpen;
        [SerializeField] private Sprite arrowClose;
        [Tooltip("Panel x (anchored, native px) when open.")]
        [SerializeField] private float openX;
        [Tooltip("Panel x (anchored, native px) when closed: the panel sits past the stage edge, only the tab shows.")]
        [SerializeField] private float closedX;
        [Tooltip("Spring frequency in Hz when opening.")]
        [SerializeField] private float openFrequency = 3.2f;
        [Tooltip("Damping ratio when opening; below 1 gives the bounce.")]
        [SerializeField] private float openDamping = 0.55f;
        [Tooltip("Spring frequency in Hz when closing.")]
        [SerializeField] private float closeFrequency = 4f;
        [Tooltip("Damping ratio when closing; 1 tucks it away without a bounce.")]
        [SerializeField] private float closeDamping = 1f;

        private DrawerSpring _spring;

        /// <summary>Raised when the drawer starts opening or closing.</summary>
        public event Action<bool> Toggled;

        /// <summary>True while the drawer is open or opening.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The tab, so the board can open the drawer when a held item is brought over it.</summary>
        public RectTransform TabRect => (RectTransform)tab.transform;

        private void Awake()
        {
            _spring = new DrawerSpring(closedX);
            Draw();
        }

        private void OnEnable() => tab.onClick.AddListener(Toggle);

        private void OnDisable() => tab.onClick.RemoveListener(Toggle);

        /// <summary>Opens if closed, closes if open.</summary>
        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Opens the drawer.</summary>
        public void Open() => SetOpen(true);

        /// <summary>Closes the drawer.</summary>
        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            arrow.sprite = open ? arrowClose : arrowOpen;
            if (GameSettings.ReducedMotion)
            {
                _spring.Snap(Target);
                Draw();
            }

            Toggled?.Invoke(open);
        }

        private float Target => IsOpen ? openX : closedX;

        /// <summary>Opens the drawer at once, without the spring (edit-mode captures and tests).</summary>
        public void SnapOpenForCapture()
        {
            _spring ??= new DrawerSpring(closedX);
            IsOpen = true;
            arrow.sprite = arrowClose;
            _spring.Snap(openX);
            Draw();
        }

        private void Update()
        {
            if (_spring.Settled && Mathf.Approximately(_spring.Position, Target)) return;
            // A long frame (a hitch, or the first frame after a load) is capped, so the motion is still seen, not skipped.
            _spring.Step(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f), Target, IsOpen ? openFrequency : closeFrequency, IsOpen ? openDamping : closeDamping);
            Draw();
        }

        // Whole pixels only, so the pixel art never shimmers while the drawer moves.
        private void Draw() => panel.anchoredPosition = new Vector2(Mathf.Round(_spring.Position), panel.anchoredPosition.y);
    }
}
