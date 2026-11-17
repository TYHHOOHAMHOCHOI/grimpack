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

        [Header("Forge tab (item-upgrades.md): one panel, two modes")]
        [SerializeField] private Button forgeTab;
        [SerializeField] private GameObject sellContent;
        [SerializeField] private GameObject forgeContent;

        private DrawerSpring _spring;

        /// <summary>What the open drawer shows.</summary>
        public enum DrawerMode { Sell, Forge }

        /// <summary>The mode shown when the drawer is open.</summary>
        public DrawerMode Mode { get; private set; } = DrawerMode.Sell;

        /// <summary>The forge tab, so the board can open the forge when a held item is brought over it.</summary>
        public RectTransform ForgeTabRect => forgeTab != null ? (RectTransform)forgeTab.transform : null;

        /// <summary>Opens the drawer in a mode; the same mode's tab on an open drawer closes it.</summary>
        public void Select(DrawerMode mode)
        {
            if (IsOpen && Mode == mode)
            {
                Close();
                return;
            }

            SetMode(mode);
            Open();
        }

        /// <summary>Opens in a mode without closing (a held item brought over a tab).</summary>
        public void OpenIn(DrawerMode mode)
        {
            SetMode(mode);
            Open();
        }

        private void SetMode(DrawerMode mode)
        {
            Mode = mode;
            if (sellContent != null) sellContent.SetActive(mode == DrawerMode.Sell);
            if (forgeContent != null) forgeContent.SetActive(mode == DrawerMode.Forge);
        }

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

        private void OnEnable()
        {
            tab.onClick.AddListener(SelectSell);
            if (forgeTab != null) forgeTab.onClick.AddListener(SelectForge);
            SetMode(Mode);
        }

        private void OnDisable()
        {
            tab.onClick.RemoveListener(SelectSell);
            if (forgeTab != null) forgeTab.onClick.RemoveListener(SelectForge);
        }

        private void SelectSell() => Select(DrawerMode.Sell);

        private void SelectForge() => Select(DrawerMode.Forge);

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
