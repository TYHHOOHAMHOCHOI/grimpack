using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;
using Project05.Combat;
using Project05.Progression;

namespace Project05.UI
{
    /// <summary>
    /// Builds and coordinates the 3-Column Screen Layout:
    /// - Column 1 (Left 1/3): Upper Half = Selected Hero's Personal Backpack; Lower Half = Shared Stash (remains constant on hero switch).
    /// - Column 2 (Middle 1/3): Party Formation (up to 3 Heroes: Frontline, Midline, Backline; starts with 1 Hero) + Combat Controls & Log.
    /// - Column 3 (Right 1/3): Context-sensitive Enemy Battlefield (with Monster Intent) OR 3-Door Selection (6 color tiers + room icons) OR Shop / Recruit / Event.
    /// </summary>
    public class ThreeColumnLayoutManager : MonoBehaviour
    {
        public RunManager Run { get; private set; }

        // Root Canvas & Columns
        private Canvas _mainCanvas;
        private RectTransform _leftCol;
        private RectTransform _midCol;
        private RectTransform _rightCol;

        // Column 1 (Left): Hero Backpack (Top) + Shared Stash (Bottom)
        private Text _heroBackpackTitleText;
        private Text _heroSynergySummaryText;
        private BackpackGridView _heroBackpackView;
        private BackpackGridView _sharedStashView;
        private Text _itemInspectorText;

        // Held / Dragged Item State
        private ItemData _heldItem;
        private BackpackGrid _heldSourceGrid;
        private int _heldRotationSteps;
        private bool _isPointerDragging;
        private BackpackGridView _lastHoveredGridView;
        private int _lastHoverX;
        private int _lastHoverY;
        private RectTransform _dragGhostRoot;
        private Image _dragGhostIcon;
        private Text _dragGhostLabel;
        private Button _sellHeldItemBtn;
        private Text _sellHeldItemBtnLabel;

        // Column 2 (Middle): Header, 3 Hero Formation Cards, Combat Controls & Log
        private Text _topStatusHeaderText;
        private Text _bannerMessageText;
        private RectTransform _partySlotsContainer;
        private readonly HeroCardUI[] _heroCards = new HeroCardUI[RunManager.MaxPartySize];
        private Text _combatLogText;
        private Text _autoBattleBtnLabel;
        private Text _speedScaleBtnLabel;

        // Column 3 (Right): Dynamic Room Content
        private Text _rightColumnHeaderText;
        private RectTransform _rightDynamicContainer;
        private readonly List<MonsterCardUI> _monsterCards = new List<MonsterCardUI>(3);

        // Playback Controls
        private bool _autoBattleEnabled = true;
        private float _battleSpeedScale = 1.6f;

        private struct HeroCardUI
        {
            public GameObject Root;
            public Image FrameImage;
            public ImageHpSpeedBars Bars;
            public Text NameRoleText;
            public Text StatusText;
            public Text SelectedBadgeText;
        }

        private struct MonsterCardUI
        {
            public MonsterCombatant Monster;
            public ImageHpSpeedBars Bars;
            public Text IntentBannerText;
            public Text StatusText;
        }

        private struct ImageHpSpeedBars
        {
            public Image HpFill;
            public Text HpLabel;
            public Text ShieldLabel;
            public Image SpeedFill;
            public Text SpeedLabel;
        }

        private bool _isInitialized;

        private void Awake()
        {
            InitializeImmediate();
        }

        private void Start()
        {
            if (Run != null && Run.Party.Count == 0)
            {
                Run.StartNewRun();
                RefreshAllColumns();
            }
        }

        public void InitializeImmediate()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EnsureCameraAndEventSystem();
            Run = new RunManager();
            BuildThreeColumnCanvas();

            Run.OnRunStateChanged += RefreshAllColumns;
            Run.OnSelectedHeroChanged += OnSelectedHeroChanged;
            Run.OnNotification += OnRunNotification;
            Run.Combat.OnTurnExecuted += OnCombatTurnExecuted;

            Run.StartNewRun();
            RefreshAllColumns();
        }

        private void Update()
        {
            // Check 'R' key to rotate held or hovered item 90 degrees clockwise
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotateHeldOrHoveredItem();
            }

            // Check 'Escape' key to cancel holding an item
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _heldItem != null)
            {
                CancelHeldItem();
            }

            // Update floating drag ghost position while holding an item
            if (_heldItem != null && _dragGhostRoot != null && Mouse.current != null)
            {
                UpdateDragGhostPosition(Mouse.current.position.ReadValue());
            }

            // Advance real-time Speed Gauges when in Combat
            if (Run != null && Run.State == RunState.Combat && _autoBattleEnabled)
            {
                Run.Combat.TickRealTime(Time.deltaTime, _battleSpeedScale);
                UpdateLiveCombatBars();
            }
        }

        private void EnsureCameraAndEventSystem()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.AbyssBg;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void BuildThreeColumnCanvas()
        {
            GameObject canvasGo = new GameObject("ThreeColumnCanvas");
            canvasGo.transform.SetParent(transform, false);

            _mainCanvas = canvasGo.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.pixelPerfect = true;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Fullscreen Abyss Background
            UIBuilderUtility.CreatePanel("AbyssBackground", canvasGo.transform, Palette.AbyssBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);

            // Column 1: Left 1/3 (0.00 -> 0.333)
            Image leftBg = UIBuilderUtility.CreatePanel(
                "Column1_Left_Backpacks",
                canvasGo.transform,
                Palette.ColumnLeftBg,
                new Vector2(0f, 0f),
                new Vector2(0.333f, 1f),
                new Vector2(6f, 6f),
                new Vector2(-3f, -6f),
                true,
                Palette.BorderIron);
            _leftCol = SkinColumnFrame(leftBg);

            // Column 2: Middle 1/3 (0.333 -> 0.666)
            Image midBg = UIBuilderUtility.CreatePanel(
                "Column2_Middle_Party",
                canvasGo.transform,
                Palette.ColumnMidBg,
                new Vector2(0.333f, 0f),
                new Vector2(0.666f, 1f),
                new Vector2(3f, 6f),
                new Vector2(-3f, -6f),
                true,
                Palette.BorderIron);
            _midCol = SkinColumnFrame(midBg);

            // Column 3: Right 1/3 (0.666 -> 1.00)
            Image rightBg = UIBuilderUtility.CreatePanel(
                "Column3_Right_Stage",
                canvasGo.transform,
                Palette.ColumnRightBg,
                new Vector2(0.666f, 0f),
                new Vector2(1f, 1f),
                new Vector2(3f, 6f),
                new Vector2(-6f, -6f),
                true,
                Palette.BorderIron);
            _rightCol = SkinColumnFrame(rightBg);

            BuildLeftColumn();
            BuildMiddleColumn();
            BuildRightColumn();
            BuildFloatingDragGhost(canvasGo.transform);
        }

        /// <summary>
        /// Puts the PixelLab stone frame on a column and returns an inset content rect,
        /// so children laid out with small offsets don't sit on top of the carved border.
        /// </summary>
        private static RectTransform SkinColumnFrame(Image columnBg)
        {
            if (!UISkin.Apply(columnBg, UISkin.PanelFrame, 1f))
            {
                return columnBg.rectTransform;
            }

            const float framePad = 6f;
            return UIBuilderUtility.CreateRect(
                "Content",
                columnBg.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(framePad, framePad),
                new Vector2(-framePad, -framePad));
        }

        /// <summary>
        /// Round PixelLab ring portrait inside the given box; falls back to the old square box + sprite when the ring art is missing.
        /// </summary>
        private static void BuildPortrait(Image box, Sprite portrait, string spriteName)
        {
            if (UISkin.Get(UISkin.AvatarRing) != null)
            {
                box.color = Color.clear;
                Outline outline = box.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
                UISkin.BuildRingPortrait(box.transform, portrait);
                return;
            }

            RectTransform spriteRt = UIBuilderUtility.CreateRect(spriteName, box.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            Image spriteImg = spriteRt.gameObject.AddComponent<Image>();
            spriteImg.sprite = portrait;
            spriteImg.preserveAspect = true;
            spriteImg.raycastTarget = false;
        }

        private void BuildFloatingDragGhost(Transform canvasTransform)
        {
            Image ghostPanel = UIBuilderUtility.CreatePanel(
                "FloatingDragGhost",
                canvasTransform,
                new Color(0.18f, 0.14f, 0.24f, 0.90f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-42f, -42f),
                new Vector2(42f, 42f),
                withOutline: true,
                outlineColor: Palette.TorchGold,
                raycastTarget: false);
            UISkin.Apply(ghostPanel, UISkin.SlotLegendary, 1f, new Color(1f, 1f, 1f, 0.92f));

            _dragGhostRoot = ghostPanel.rectTransform;

            RectTransform iconRt = UIBuilderUtility.CreateRect(
                "GhostIcon",
                _dragGhostRoot,
                new Vector2(0.15f, 0.28f),
                new Vector2(0.85f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            _dragGhostIcon = iconRt.gameObject.AddComponent<Image>();
            _dragGhostIcon.preserveAspect = true;
            _dragGhostIcon.raycastTarget = false;

            _dragGhostLabel = UIBuilderUtility.CreateText(
                "GhostLabel",
                _dragGhostRoot,
                string.Empty,
                10,
                Palette.TorchGold,
                TextAnchor.LowerCenter,
                FontStyle.Bold,
                raycastTarget: false);

            _dragGhostRoot.gameObject.SetActive(false);
        }

        private void BuildLeftColumn()
        {
            // UPPER HALF (0.50 -> 1.00): Selected Hero's Personal Backpack
            Image topHalf = UIBuilderUtility.CreatePanel(
                "TopHalf_HeroBackpack",
                _leftCol,
                Palette.PanelSurface,
                new Vector2(0f, 0.50f),
                new Vector2(1f, 1f),
                new Vector2(8f, 4f),
                new Vector2(-8f, -8f),
                true,
                Palette.BorderGold);
            UISkin.Apply(topHalf, UISkin.SlotLegendary);

            // Header bar for Hero Backpack
            Image topHeader = UIBuilderUtility.CreatePanel(
                "HeroBagHeader",
                topHalf.transform,
                Palette.PanelHeader,
                new Vector2(0f, 0.84f),
                new Vector2(1f, 1f),
                new Vector2(8f, 0f),
                new Vector2(-8f, -8f),
                false);

            _heroBackpackTitleText = UIBuilderUtility.CreateText(
                "HeroBagTitle",
                topHeader.transform,
                "BALO NHÂN VẬT ĐANG CHỌN",
                15,
                Palette.TorchGold,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            _heroBackpackTitleText.rectTransform.offsetMin = new Vector2(10f, 0f);
            _heroBackpackTitleText.rectTransform.offsetMax = new Vector2(-115f, 0f);

            // Rotate [R] Button in Top-Right of Hero Backpack Header
            Button rotateBtn = UIBuilderUtility.CreateButton(
                "RotateRButton",
                topHeader.transform,
                "🔄 Xoay [R]",
                12,
                new Color(0.26f, 0.20f, 0.34f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                RotateHeldOrHoveredItem);
            RectTransform rotRt = rotateBtn.GetComponent<RectTransform>();
            rotRt.anchorMin = new Vector2(1f, 0.12f);
            rotRt.anchorMax = new Vector2(1f, 0.88f);
            rotRt.offsetMin = new Vector2(-108f, 0f);
            rotRt.offsetMax = new Vector2(-8f, 0f);

            // Synergy Summary Subtitle
            _heroSynergySummaryText = UIBuilderUtility.CreateText(
                "SynergySummary",
                topHalf.transform,
                "★ Liên kết vị trí: Đặt Đá Mài / Bình Độc cạnh Vũ Khí để cộng hưởng!",
                12,
                Palette.BoneText,
                TextAnchor.MiddleCenter);
            _heroSynergySummaryText.rectTransform.anchorMin = new Vector2(0f, 0.74f);
            _heroSynergySummaryText.rectTransform.anchorMax = new Vector2(1f, 0.84f);
            _heroSynergySummaryText.rectTransform.offsetMin = new Vector2(8f, 0f);
            _heroSynergySummaryText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            // Grid View Container for Hero Backpack
            RectTransform heroGridHolder = UIBuilderUtility.CreateRect(
                "HeroGridHolder",
                topHalf.transform,
                new Vector2(0f, 0.04f),
                new Vector2(1f, 0.73f),
                Vector2.zero,
                Vector2.zero);
            _heroBackpackView = heroGridHolder.gameObject.AddComponent<BackpackGridView>();
            _heroBackpackView.InitializeLayout(heroGridHolder, cellSize: 54f, cellSpacing: 4f);
            WireGridViewEvents(_heroBackpackView);

            // LOWER HALF (0.00 -> 0.50): Shared Stash (Kho Đồ Chung) + Inspector Bar
            Image bottomHalf = UIBuilderUtility.CreatePanel(
                "BottomHalf_SharedStash",
                _leftCol,
                Palette.PanelSurface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.50f),
                new Vector2(8f, 8f),
                new Vector2(-8f, -4f),
                true,
                Palette.BorderIron);
            UISkin.Apply(bottomHalf, UISkin.SlotCommon);

            Image stashHeader = UIBuilderUtility.CreatePanel(
                "StashHeader",
                bottomHalf.transform,
                Palette.PanelHeader,
                new Vector2(0f, 0.85f),
                new Vector2(1f, 1f),
                new Vector2(8f, 0f),
                new Vector2(-8f, -8f),
                false);

            Text stashTitle = UIBuilderUtility.CreateText(
                "StashTitle",
                stashHeader.transform,
                "📦 KHO ĐỒ CHUNG (GIỮ NGUYÊN KHI ĐỔI HERO)",
                13,
                Palette.BoneText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            stashTitle.rectTransform.offsetMin = new Vector2(10f, 0f);
            stashTitle.rectTransform.offsetMax = new Vector2(-128f, 0f);

            // Sell Held Item Button in Top-Right of Stash Header
            _sellHeldItemBtn = UIBuilderUtility.CreateButton(
                "SellHeldItemButton",
                stashHeader.transform,
                "💰 Bán Đồ Đang Cầm",
                11,
                new Color(0.32f, 0.22f, 0.12f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                SellCurrentlyHeldItem);
            RectTransform sellRt = _sellHeldItemBtn.GetComponent<RectTransform>();
            sellRt.anchorMin = new Vector2(1f, 0.12f);
            sellRt.anchorMax = new Vector2(1f, 0.88f);
            sellRt.offsetMin = new Vector2(-124f, 0f);
            sellRt.offsetMax = new Vector2(-8f, 0f);
            _sellHeldItemBtnLabel = _sellHeldItemBtn.GetComponentInChildren<Text>();

            RectTransform stashGridHolder = UIBuilderUtility.CreateRect(
                "StashGridHolder",
                bottomHalf.transform,
                new Vector2(0f, 0.24f),
                new Vector2(1f, 0.84f),
                Vector2.zero,
                Vector2.zero);
            _sharedStashView = stashGridHolder.gameObject.AddComponent<BackpackGridView>();
            _sharedStashView.InitializeLayout(stashGridHolder, cellSize: 50f, cellSpacing: 4f);
            WireGridViewEvents(_sharedStashView);

            // Item Tooltip & Controls Footer inside Bottom-Left
            Image inspectorPanel = UIBuilderUtility.CreatePanel(
                "ItemInspectorFooter",
                bottomHalf.transform,
                new Color(0.07f, 0.06f, 0.09f, 0.96f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0.23f),
                new Vector2(6f, 6f),
                new Vector2(-6f, -2f),
                true,
                Palette.BorderIron);
            UISkin.Apply(inspectorPanel, UISkin.SlotCommon, 1f, new Color(0.75f, 0.75f, 0.8f, 1f));

            _itemInspectorText = UIBuilderUtility.CreateText(
                "InspectorText",
                inspectorPanel.transform,
                "💡 Mẹo Balo: Kéo Thả hoặc Click Trái để Nhấc/Đặt • Nhấn [R] để Xoay 90° • Click Phải để chuyển nhanh Balo <-> Kho Chung.",
                11,
                Palette.BoneText,
                TextAnchor.MiddleLeft);
            _itemInspectorText.rectTransform.offsetMin = new Vector2(8f, 4f);
            _itemInspectorText.rectTransform.offsetMax = new Vector2(-8f, -4f);
        }

        private void WireGridViewEvents(BackpackGridView view)
        {
            view.OnCellClicked += HandleGridCellClicked;
            view.OnCellHovered += HandleGridCellHovered;
            view.OnGridPointerExited += HandleGridPointerExited;
            view.OnCellBeginDrag += HandleCellBeginDrag;
            view.OnCellDragged += HandleCellDragged;
            view.OnCellDropped += HandleCellDropped;
            view.OnCellEndDrag += HandleCellEndDrag;
        }

        private void BuildMiddleColumn()
        {
            // Top Run Status Header
            Image topHeader = UIBuilderUtility.CreatePanel(
                "MiddleTopHeader",
                _midCol,
                Palette.PanelHeader,
                new Vector2(0f, 0.89f),
                new Vector2(1f, 1f),
                new Vector2(8f, -4f),
                new Vector2(-8f, -8f),
                true,
                Palette.BorderGold);

            _topStatusHeaderText = UIBuilderUtility.CreateText(
                "RunStatsText",
                topHeader.transform,
                "TẦNG 1 • 💰 35 VÀNG • ĐỘI HÌNH: 1/3",
                15,
                Palette.TorchGold,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            _topStatusHeaderText.rectTransform.anchorMin = new Vector2(0.02f, 0.54f);
            _topStatusHeaderText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);

            _bannerMessageText = UIBuilderUtility.CreateText(
                "BannerText",
                topHeader.transform,
                "Click vào nhân vật để xem và sắp xếp Balo cá nhân ở Cột Trái!",
                12,
                Palette.BoneText,
                TextAnchor.MiddleCenter);
            _bannerMessageText.rectTransform.anchorMin = new Vector2(0.02f, 0.04f);
            _bannerMessageText.rectTransform.anchorMax = new Vector2(0.98f, 0.54f);

            // Party Formation Container (3 Slots: Frontline, Midline, Backline)
            _partySlotsContainer = UIBuilderUtility.CreateRect(
                "PartyFormationContainer",
                _midCol,
                new Vector2(0f, 0.25f),
                new Vector2(1f, 0.88f),
                new Vector2(8f, 4f),
                new Vector2(-8f, -4f));

            // Combat Control & Log Box at Bottom of Middle Column
            Image bottomLogPanel = UIBuilderUtility.CreatePanel(
                "CombatControlAndLog",
                _midCol,
                Palette.PanelSurface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.24f),
                new Vector2(8f, 8f),
                new Vector2(-8f, -2f),
                true,
                Palette.BorderIron);
            UISkin.Apply(bottomLogPanel, UISkin.SlotCommon);

            // Control buttons row
            RectTransform ctrlRow = UIBuilderUtility.CreateRect(
                "ControlRow",
                bottomLogPanel.transform,
                new Vector2(0f, 0.70f),
                new Vector2(1f, 1f),
                new Vector2(6f, 2f),
                new Vector2(-6f, -4f));

            Button autoBtn = UIBuilderUtility.CreateButton(
                "AutoBattleBtn",
                ctrlRow,
                "⏯ Tự Đánh: BẬT",
                12,
                new Color(0.18f, 0.36f, 0.24f, 1f),
                Palette.BoneText,
                Palette.BorderIron,
                ToggleAutoBattle);
            RectTransform autoRt = autoBtn.GetComponent<RectTransform>();
            autoRt.anchorMin = new Vector2(0f, 0f);
            autoRt.anchorMax = new Vector2(0.32f, 1f);
            autoRt.offsetMin = Vector2.zero;
            autoRt.offsetMax = new Vector2(-3f, 0f);
            _autoBattleBtnLabel = autoBtn.GetComponentInChildren<Text>();

            Button stepBtn = UIBuilderUtility.CreateButton(
                "StepTurnBtn",
                ctrlRow,
                "⏭ Chạy 1 Lượt",
                12,
                new Color(0.28f, 0.22f, 0.38f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                StepOneCombatTurn);
            RectTransform stepRt = stepBtn.GetComponent<RectTransform>();
            stepRt.anchorMin = new Vector2(0.33f, 0f);
            stepRt.anchorMax = new Vector2(0.66f, 1f);
            stepRt.offsetMin = new Vector2(3f, 0f);
            stepRt.offsetMax = new Vector2(-3f, 0f);

            Button spdBtn = UIBuilderUtility.CreateButton(
                "SpeedToggleBtn",
                ctrlRow,
                "⚡ Tốc Độ: 1x",
                12,
                new Color(0.22f, 0.22f, 0.30f, 1f),
                Palette.BoneText,
                Palette.BorderIron,
                ToggleBattleSpeed);
            RectTransform spdRt = spdBtn.GetComponent<RectTransform>();
            spdRt.anchorMin = new Vector2(0.67f, 0f);
            spdRt.anchorMax = new Vector2(1f, 1f);
            spdRt.offsetMin = new Vector2(3f, 0f);
            spdRt.offsetMax = Vector2.zero;
            _speedScaleBtnLabel = spdBtn.GetComponentInChildren<Text>();

            // Combat Log Text Area
            _combatLogText = UIBuilderUtility.CreateText(
                "CombatLogText",
                bottomLogPanel.transform,
                "Nhật ký chiến đấu sẽ hiển thị tại đây...",
                11,
                Palette.BoneText,
                TextAnchor.LowerLeft);
            _combatLogText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _combatLogText.rectTransform.anchorMax = new Vector2(1f, 0.68f);
            _combatLogText.rectTransform.offsetMin = new Vector2(8f, 6f);
            _combatLogText.rectTransform.offsetMax = new Vector2(-8f, -2f);
        }

        private void BuildRightColumn()
        {
            Image rightHeader = UIBuilderUtility.CreatePanel(
                "RightColumnHeader",
                _rightCol,
                Palette.PanelHeader,
                new Vector2(0f, 0.91f),
                new Vector2(1f, 1f),
                new Vector2(8f, -4f),
                new Vector2(-8f, -8f),
                true,
                Palette.CrimsonAccent);

            _rightColumnHeaderText = UIBuilderUtility.CreateText(
                "RightHeaderTitle",
                rightHeader.transform,
                "⚔ CHIẾN TRƯỜNG KẺ ĐỊCH (HIỂN THỊ Ý ĐỊNH)",
                15,
                Palette.TorchGold,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            _rightDynamicContainer = UIBuilderUtility.CreateRect(
                "RightDynamicStage",
                _rightCol,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.90f),
                new Vector2(8f, 8f),
                new Vector2(-8f, -4f));
        }

        public void RefreshAllColumns()
        {
            if (Run == null) return;
            RefreshLeftColumnBackpacks();
            RefreshMiddleColumnParty();
            RefreshRightColumnStage();
            RefreshCombatLogText();
        }

        private void RefreshLeftColumnBackpacks()
        {
            HeroCombatant selectedHero = Run.SelectedHero;
            if (selectedHero != null)
            {
                selectedHero.RecalculateSynergies(Run.ActiveBuffs);
                string posName = RunManager.GetPositionVietnamese(selectedHero.Position);
                _heroBackpackTitleText.text = $"🎒 BALO: {selectedHero.DisplayName.ToUpper()} ({posName})";

                int linkCount = selectedHero.LatestSynergyReport.ActiveLinks.Count;
                int bonusSpd = selectedHero.LatestSynergyReport.TotalPassiveBonusSpeed;
                _heroSynergySummaryText.text = $"★ Liên kết Synergy: {linkCount} cặp • Tốc độ cộng thêm: +{bonusSpd} • Ô mở: {selectedHero.PersonalBackpack.GetUnlockedSlotCount()}/{selectedHero.PersonalBackpack.Width * selectedHero.PersonalBackpack.Height}";

                _heroBackpackView.BindGrid(selectedHero.PersonalBackpack, selectedHero.LatestSynergyReport);
            }

            // Shared Stash in Bottom-Left stays bound to Run.SharedStash regardless of which Hero is selected!
            _sharedStashView.BindGrid(Run.SharedStash, null);
        }

        private void RefreshMiddleColumnParty()
        {
            string buffsTag = Run.ActiveBuffs.Count > 0 ? $" • 🌟 Buff: {Run.ActiveBuffs.Count}" : string.Empty;
            _topStatusHeaderText.text = $"TẦNG {Run.CurrentFloor} • 💰 {Run.Gold} VÀNG • ĐỘI HÌNH: {Run.Party.Count}/{RunManager.MaxPartySize}{buffsTag}";
            _bannerMessageText.text = Run.LatestBannerMessage;

            for (int i = _partySlotsContainer.childCount - 1; i >= 0; i--)
            {
                UIBuilderUtility.SafeDestroy(_partySlotsContainer.GetChild(i).gameObject);
            }

            string[] slotNames =
            {
                "[VỊ TRÍ 1] TIỀN TUYẾN (FRONTLINE)",
                "[VỊ TRÍ 2] TRUNG TÂM (MIDLINE)",
                "[VỊ TRÍ 3] HẬU TUYẾN (BACKLINE)"
            };

            for (int slotIdx = 0; slotIdx < RunManager.MaxPartySize; slotIdx++)
            {
                float yMax = 1f - (slotIdx * 0.333f);
                float yMin = yMax - 0.32f;

                HeroCombatant hero = slotIdx < Run.Party.Count ? Run.Party[slotIdx] : null;
                bool isSelected = hero != null && slotIdx == Run.SelectedHeroIndex;

                Image cardBg = UIBuilderUtility.CreatePanel(
                    $"PartySlot_{slotIdx}",
                    _partySlotsContainer,
                    hero != null ? Palette.PanelSurface : new Color(0.09f, 0.08f, 0.12f, 0.75f),
                    new Vector2(0f, yMin),
                    new Vector2(1f, yMax),
                    new Vector2(2f, 2f),
                    new Vector2(-2f, -2f),
                    true,
                    isSelected ? Palette.TorchGold : Palette.BorderIron,
                    raycastTarget: true);
                UISkin.Apply(
                    cardBg,
                    isSelected ? UISkin.SlotLegendary : UISkin.SlotCommon,
                    1f,
                    hero != null ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.8f));

                if (hero == null)
                {
                    // Empty party slot waiting to be recruited
                    UIBuilderUtility.CreateText(
                        "EmptySlotLabel",
                        cardBg.transform,
                        $"{slotNames[slotIdx]}\n[Chưa có đồng đội — Chọn Cánh Cửa 🛡 CHIÊU MỘ giữa các ải để thu thập đồng đội!]",
                        12,
                        Palette.AshMutedText,
                        TextAnchor.MiddleCenter,
                        FontStyle.Italic);
                    _heroCards[slotIdx] = default;
                    continue;
                }

                int capturedIdx = slotIdx;
                Button selectHeroBtn = cardBg.gameObject.AddComponent<Button>();
                selectHeroBtn.targetGraphic = cardBg;
                selectHeroBtn.onClick.AddListener(() => Run.SelectHero(capturedIdx));

                // Portrait Icon on Left
                Image portraitBg = UIBuilderUtility.CreatePanel(
                    "PortraitBox",
                    cardBg.transform,
                    new Color(0.08f, 0.07f, 0.11f, 1f),
                    new Vector2(0.02f, 0.12f),
                    new Vector2(0.22f, 0.88f),
                    Vector2.zero,
                    Vector2.zero,
                    true,
                    isSelected ? Palette.TorchGold : Palette.BorderIron);

                BuildPortrait(portraitBg, SpriteFactory.GetSprite(hero.Definition.SpriteName, Palette.TorchGold), "HeroSprite");

                // Name, Position & Selected Indicator
                Text nameTxt = UIBuilderUtility.CreateText(
                    "HeroName",
                    cardBg.transform,
                    $"{slotNames[slotIdx]} — {hero.DisplayName}",
                    13,
                    isSelected ? Palette.TorchGold : Palette.BoneText,
                    TextAnchor.MiddleLeft,
                    FontStyle.Bold);
                nameTxt.rectTransform.anchorMin = new Vector2(0.24f, 0.72f);
                nameTxt.rectTransform.anchorMax = new Vector2(0.75f, 0.95f);

                Text selBadge = UIBuilderUtility.CreateText(
                    "SelectedBadge",
                    cardBg.transform,
                    isSelected ? "◀ ĐANG MỞ BALO" : "[Click xem Balo]",
                    11,
                    isSelected ? Palette.TierGreen : Palette.AshMutedText,
                    TextAnchor.MiddleRight,
                    FontStyle.Bold);
                selBadge.rectTransform.anchorMin = new Vector2(0.75f, 0.72f);
                selBadge.rectTransform.anchorMax = new Vector2(0.98f, 0.95f);

                // HP Bar & Shield Badge
                ImageHpSpeedBars bars = CreateCombatantBars(cardBg.transform, hero);

                // Status Effects & Item Count Row
                Text statusTxt = UIBuilderUtility.CreateText(
                    "StatusRow",
                    cardBg.transform,
                    FormatCombatantStatusLine(hero, includeBagCount: true),
                    11,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft);
                statusTxt.rectTransform.anchorMin = new Vector2(0.24f, 0.04f);
                statusTxt.rectTransform.anchorMax = new Vector2(0.98f, 0.24f);

                _heroCards[slotIdx] = new HeroCardUI
                {
                    Root = cardBg.gameObject,
                    FrameImage = cardBg,
                    Bars = bars,
                    NameRoleText = nameTxt,
                    StatusText = statusTxt,
                    SelectedBadgeText = selBadge
                };
            }
        }

        private void RefreshRightColumnStage()
        {
            for (int i = _rightDynamicContainer.childCount - 1; i >= 0; i--)
            {
                UIBuilderUtility.SafeDestroy(_rightDynamicContainer.GetChild(i).gameObject);
            }
            _monsterCards.Clear();

            switch (Run.State)
            {
                case RunState.Combat:
                    _rightColumnHeaderText.text = $"⚔ {Palette.GetRoomVietnameseName(Run.CurrentRoomType).ToUpper()} (Ý ĐỊNH QUÁI)";
                    BuildRightCombatView();
                    break;

                case RunState.DoorSelection:
                    _rightColumnHeaderText.text = "🚪 CHỌN 1 TRONG 3 CÁNH CỬA (NHẬN BUFF & VÀO ẢI)";
                    BuildRightDoorSelectionView();
                    break;

                case RunState.Shop:
                    _rightColumnHeaderText.text = "💰 CỬA HÀNG THƯƠNG NHÂN HẮC ÁM";
                    BuildRightShopView();
                    break;

                case RunState.Recruit:
                    _rightColumnHeaderText.text = "🛡 TRẠI CHIÊU MỘ ĐỒNG ĐỘI (TỐI ĐA 3 NGƯỜI)";
                    BuildRightRecruitView();
                    break;

                case RunState.Event:
                    _rightColumnHeaderText.text = "🕯 SỰ KIỆN CỔ QUÁI — ĐIỆN THỜ HUYẾT NGUYỆT";
                    BuildRightEventView();
                    break;

                case RunState.Victory:
                    _rightColumnHeaderText.text = "🏆 CHIẾN THẮNG HUYẾT NGUYỆT!";
                    BuildRightVictoryView();
                    break;

                case RunState.Defeat:
                    _rightColumnHeaderText.text = "☠ HÀNH TRÌNH KẾT THÚC";
                    BuildRightDefeatView();
                    break;
            }
        }

        private void BuildRightCombatView()
        {
            IReadOnlyList<MonsterCombatant> monsters = Run.CurrentMonsters;
            int count = monsters != null ? monsters.Count : 0;

            for (int i = 0; i < count; i++)
            {
                MonsterCombatant m = monsters[i];
                float yMax = 1f - (i * 0.42f);
                float yMin = yMax - 0.40f;

                Image cardBg = UIBuilderUtility.CreatePanel(
                    $"MonsterCard_{i}",
                    _rightDynamicContainer,
                    Palette.PanelSurface,
                    new Vector2(0f, yMin),
                    new Vector2(1f, yMax),
                    new Vector2(4f, 4f),
                    new Vector2(-4f, -4f),
                    true,
                    m.Definition.IsBoss ? Palette.TierRed : (m.Definition.IsElite ? Palette.TierPurple : Palette.BorderIron));
                UISkin.Apply(cardBg, m.Definition.IsBoss ? UISkin.SlotCursed : (m.Definition.IsElite ? UISkin.SlotEpic : UISkin.SlotCommon));

                // Intent Banner at the top of Monster Card
                Image intentBanner = UIBuilderUtility.CreatePanel(
                    "IntentBanner",
                    cardBg.transform,
                    new Color(0.24f, 0.12f, 0.16f, 0.96f),
                    new Vector2(0.02f, 0.74f),
                    new Vector2(0.98f, 0.96f),
                    Vector2.zero,
                    Vector2.zero,
                    true,
                    Palette.CrimsonAccent);

                Text intentTxt = UIBuilderUtility.CreateText(
                    "IntentText",
                    intentBanner.transform,
                    $"👁 Ý ĐỊNH LƯỢT TỚI: {m.CurrentIntent.GetSummaryText()}",
                    13,
                    Palette.TorchGold,
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);

                // Monster Pixel Art Sprite on Left
                Image spriteBox = UIBuilderUtility.CreatePanel(
                    "MonsterPortrait",
                    cardBg.transform,
                    new Color(0.07f, 0.06f, 0.09f, 1f),
                    new Vector2(0.02f, 0.10f),
                    new Vector2(0.22f, 0.70f),
                    Vector2.zero,
                    Vector2.zero,
                    true,
                    Palette.BorderIron);

                BuildPortrait(spriteBox, SpriteFactory.GetSprite(m.Definition.SpriteName, Palette.CrimsonAccent), "MonsterSprite");

                // Monster Name & Subtitle
                Text mName = UIBuilderUtility.CreateText(
                    "MonsterName",
                    cardBg.transform,
                    $"{m.DisplayName} ({m.Definition.Subtitle})",
                    14,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft,
                    FontStyle.Bold);
                mName.rectTransform.anchorMin = new Vector2(0.24f, 0.56f);
                mName.rectTransform.anchorMax = new Vector2(0.98f, 0.72f);

                // HP & Speed Bars
                ImageHpSpeedBars bars = CreateCombatantBars(cardBg.transform, m, yOffset: -0.14f);

                Text statusTxt = UIBuilderUtility.CreateText(
                    "MonsterStatus",
                    cardBg.transform,
                    FormatCombatantStatusLine(m, includeBagCount: false),
                    11,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft);
                statusTxt.rectTransform.anchorMin = new Vector2(0.24f, 0.03f);
                statusTxt.rectTransform.anchorMax = new Vector2(0.98f, 0.18f);

                _monsterCards.Add(new MonsterCardUI
                {
                    Monster = m,
                    Bars = bars,
                    IntentBannerText = intentTxt,
                    StatusText = statusTxt
                });
            }

            // Quick tactical tip at the bottom of Right Combat Column
            Image tipBox = UIBuilderUtility.CreatePanel(
                "TacticalTipBox",
                _rightDynamicContainer,
                new Color(0.08f, 0.07f, 0.11f, 0.9f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0.14f),
                new Vector2(4f, 4f),
                new Vector2(-4f, -2f),
                true,
                Palette.BorderIron);
            UISkin.Apply(tipBox, UISkin.SlotCommon, 1f, new Color(0.75f, 0.75f, 0.8f, 1f));

            UIBuilderUtility.CreateText(
                "TipText",
                tipBox.transform,
                "💡 Khi thanh Tốc độ của Nhân vật đầy 100%, toàn bộ trang bị trong Balo của nhân vật đó sẽ TỰ ĐỘNG KÍCH HOẠT theo Cooldown lượt.\nBạn có thể kéo thả đổi trang bị giữa Kho Chung và Balo bất cứ lúc nào để khắc chế Ý Định (Intent) của quái!",
                11,
                Palette.AshMutedText,
                TextAnchor.MiddleCenter);
        }

        private void BuildRightDoorSelectionView()
        {
            IReadOnlyList<DoorCard> doors = Run.CurrentDoorChoices;
            for (int i = 0; i < doors.Count; i++)
            {
                DoorCard door = doors[i];
                int capturedIdx = i;

                float yMax = 1f - (i * 0.33f);
                float yMin = yMax - 0.315f;

                Color tierColor = Palette.GetTierColor(door.Tier);
                Color cardTint = new Color(tierColor.r * 0.22f + 0.08f, tierColor.g * 0.22f + 0.07f, tierColor.b * 0.22f + 0.10f, 0.98f);

                Image cardBg = UIBuilderUtility.CreatePanel(
                    $"DoorCard_{i}_{door.Tier}",
                    _rightDynamicContainer,
                    cardTint,
                    new Vector2(0f, yMin),
                    new Vector2(1f, yMax),
                    new Vector2(4f, 3f),
                    new Vector2(-4f, -3f),
                    true,
                    tierColor);
                UISkin.Apply(cardBg, UISkin.SlotForTier(door.Tier));

                // TOP BANNER: Prominent Room Type Icon Badge (so player sees which room this buff card enters!)
                Image roomBadgeBar = UIBuilderUtility.CreatePanel(
                    "RoomIconBanner",
                    cardBg.transform,
                    new Color(0.06f, 0.05f, 0.08f, 0.95f),
                    new Vector2(0.02f, 0.73f),
                    new Vector2(0.98f, 0.96f),
                    Vector2.zero,
                    Vector2.zero,
                    true,
                    tierColor);

                UIBuilderUtility.CreateText(
                    "RoomBadgeText",
                    roomBadgeBar.transform,
                    $"🚪 DẪN VÀO ẢI: {door.GetRoomBadgeText()}  •  [Bậc {door.GetTierLabelText()}]",
                    13,
                    tierColor,
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);

                // MIDDLE: Buff Name & Description
                string buffTitle = door.BuffReward != null ? door.BuffReward.BuffName : "Chúc Phúc Cổ Xưa";
                string buffDesc = door.BuffReward != null ? door.BuffReward.Description : string.Empty;

                Text buffText = UIBuilderUtility.CreateText(
                    "BuffDescription",
                    cardBg.transform,
                    $"✨ BUFF NHẬN NGAY: [{buffTitle}]\n{buffDesc}",
                    12,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft,
                    FontStyle.Bold);
                buffText.rectTransform.anchorMin = new Vector2(0.04f, 0.28f);
                buffText.rectTransform.anchorMax = new Vector2(0.96f, 0.71f);

                // BOTTOM: Danger Level + Select Door Button
                Text dangerTxt = UIBuilderUtility.CreateText(
                    "DangerText",
                    cardBg.transform,
                    door.GetDangerDescription(),
                    11,
                    Palette.AshMutedText,
                    TextAnchor.MiddleLeft);
                dangerTxt.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
                dangerTxt.rectTransform.anchorMax = new Vector2(0.58f, 0.26f);

                Button chooseDoorBtn = UIBuilderUtility.CreateButton(
                    $"SelectDoorBtn_{i}",
                    cardBg.transform,
                    $"Bước Qua Cửa #{i + 1} ▶",
                    12,
                    new Color(tierColor.r * 0.45f, tierColor.g * 0.45f, tierColor.b * 0.45f, 1f),
                    Palette.BoneText,
                    tierColor,
                    () => Run.SelectDoor(capturedIdx));
                RectTransform btnRt = chooseDoorBtn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.60f, 0.05f);
                btnRt.anchorMax = new Vector2(0.98f, 0.27f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;
            }
        }

        private void BuildRightShopView()
        {
            IReadOnlyList<ShopOffer> offers = Run.Shop.CurrentOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                ShopOffer offer = offers[i];
                int capturedIdx = i;
                float yMax = 1f - (i * 0.165f);
                float yMin = yMax - 0.15f;

                Color tierCol = Palette.GetTierColor(offer.ItemDef.RarityTier);
                Image row = UIBuilderUtility.CreatePanel(
                    $"ShopOffer_{i}",
                    _rightDynamicContainer,
                    Palette.PanelSurface,
                    new Vector2(0f, yMin),
                    new Vector2(1f, yMax),
                    new Vector2(4f, 2f),
                    new Vector2(-4f, -2f),
                    true,
                    tierCol);
                UISkin.Apply(row, UISkin.SlotForTier(offer.ItemDef.RarityTier));

                Text title = UIBuilderUtility.CreateText(
                    "OfferInfo",
                    row.transform,
                    $"[{offer.ItemDef.DisplayName}] (CD:{offer.ItemDef.CooldownTurns})\n{offer.ItemDef.Description}",
                    11,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft);
                title.rectTransform.anchorMin = new Vector2(0.03f, 0.06f);
                title.rectTransform.anchorMax = new Vector2(0.70f, 0.94f);

                string btnLabel = offer.IsSold ? "ĐÃ MUA" : $"Mua ({offer.Price} Vàng)";
                Button buyBtn = UIBuilderUtility.CreateButton(
                    $"BuyBtn_{i}",
                    row.transform,
                    btnLabel,
                    12,
                    offer.IsSold ? Palette.SlotLocked : new Color(0.20f, 0.42f, 0.26f, 1f),
                    Palette.BoneText,
                    tierCol,
                    () =>
                    {
                        int gold = Run.Gold;
                        if (Run.Shop.TryBuyOffer(capturedIdx, ref gold, Run.SharedStash, Run.SelectedHero?.PersonalBackpack, out string msg))
                        {
                            Run.Gold = gold;
                        }
                        _bannerMessageText.text = msg;
                        RefreshAllColumns();
                    });
                buyBtn.interactable = !offer.IsSold;
                RectTransform buyRt = buyBtn.GetComponent<RectTransform>();
                buyRt.anchorMin = new Vector2(0.72f, 0.15f);
                buyRt.anchorMax = new Vector2(0.98f, 0.85f);
                buyRt.offsetMin = Vector2.zero;
                buyRt.offsetMax = Vector2.zero;
            }

            // Shop Utility Buttons (Reroll, Unlock Backpack Slot, Heal Party, Leave Shop)
            Button rerollBtn = UIBuilderUtility.CreateButton(
                "RerollShopBtn",
                _rightDynamicContainer,
                $"🎲 Làm Mới Kệ Hàng ({ShopManager.RerollCost} Vàng)",
                12,
                new Color(0.24f, 0.18f, 0.34f, 1f),
                Palette.BoneText,
                Palette.TierPurple,
                () =>
                {
                    int gold = Run.Gold;
                    if (Run.Shop.TryReroll(Run.CurrentFloor, ref gold, out string msg))
                    {
                        Run.Gold = gold;
                    }
                    _bannerMessageText.text = msg;
                    RefreshAllColumns();
                });
            RectTransform rrRt = rerollBtn.GetComponent<RectTransform>();
            rrRt.anchorMin = new Vector2(0.02f, 0.26f);
            rrRt.anchorMax = new Vector2(0.98f, 0.335f);
            rrRt.offsetMin = Vector2.zero;
            rrRt.offsetMax = Vector2.zero;

            Button unlockSlotBtn = UIBuilderUtility.CreateButton(
                "UnlockBagSlotBtn",
                _rightDynamicContainer,
                $"🔓 Mở +1 Ô Balo Nhân Vật Đang Chọn ({ShopManager.SlotUnlockCost} Vàng)",
                12,
                new Color(0.28f, 0.24f, 0.14f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                () =>
                {
                    HeroCombatant h = Run.SelectedHero;
                    if (h == null) return;
                    if (Run.Gold < ShopManager.SlotUnlockCost)
                    {
                        _bannerMessageText.text = "Không đủ Vàng để mở khóa ô Balo!";
                        return;
                    }
                    if (h.PersonalBackpack.UnlockNextLockedSlots(1) > 0)
                    {
                        Run.Gold -= ShopManager.SlotUnlockCost;
                        _bannerMessageText.text = $"Đã mở thêm +1 Ô Balo cho {h.DisplayName}!";
                        RefreshAllColumns();
                    }
                    else
                    {
                        _bannerMessageText.text = "Balo của nhân vật này đã mở khóa tối đa!";
                    }
                });
            RectTransform uRt = unlockSlotBtn.GetComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0.02f, 0.175f);
            uRt.anchorMax = new Vector2(0.98f, 0.25f);
            uRt.offsetMin = Vector2.zero;
            uRt.offsetMax = Vector2.zero;

            Button healBtn = UIBuilderUtility.CreateButton(
                "ShopHealBtn",
                _rightDynamicContainer,
                $"❤ Hồi 35% HP Toàn Bộ Đội Hình ({ShopManager.PartyHealCost} Vàng)",
                12,
                new Color(0.32f, 0.16f, 0.20f, 1f),
                Palette.BoneText,
                Palette.CrimsonAccent,
                () =>
                {
                    if (Run.Gold < ShopManager.PartyHealCost)
                    {
                        _bannerMessageText.text = "Không đủ Vàng để trị thương!";
                        return;
                    }
                    Run.Gold -= ShopManager.PartyHealCost;
                    for (int i = 0; i < Run.Party.Count; i++)
                    {
                        Run.Party[i].Heal(Mathf.RoundToInt(Run.Party[i].MaxHP * 0.35f));
                    }
                    _bannerMessageText.text = "Đã hồi phục 35% HP cho toàn đội!";
                    RefreshAllColumns();
                });
            RectTransform hRt = healBtn.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.02f, 0.09f);
            hRt.anchorMax = new Vector2(0.98f, 0.165f);
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            Button leaveShopBtn = UIBuilderUtility.CreateButton(
                "LeaveShopBtn",
                _rightDynamicContainer,
                "🚪 Rời Cửa Hàng -> Chọn 3 Cánh Cửa Tiếp Theo",
                13,
                new Color(0.22f, 0.34f, 0.46f, 1f),
                Palette.BoneText,
                Palette.TierBlue,
                () => Run.PrepareDoorSelection());
            RectTransform lRt = leaveShopBtn.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.02f, 0.005f);
            lRt.anchorMax = new Vector2(0.98f, 0.08f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
        }

        private void BuildRightRecruitView()
        {
            IReadOnlyList<HeroDefinitionSO> candidates = Run.RecruitCandidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                HeroDefinitionSO def = candidates[i];
                float yMax = 1f - (i * 0.31f);
                float yMin = yMax - 0.295f;

                Image card = UIBuilderUtility.CreatePanel(
                    $"RecruitCard_{def.HeroId}",
                    _rightDynamicContainer,
                    Palette.PanelSurface,
                    new Vector2(0f, yMin),
                    new Vector2(1f, yMax),
                    new Vector2(4f, 3f),
                    new Vector2(-4f, -3f),
                    true,
                    Palette.BorderGold);
                UISkin.Apply(card, UISkin.SlotLegendary);

                RectTransform iconRt = UIBuilderUtility.CreateRect("Portrait", card.transform, new Vector2(0.03f, 0.15f), new Vector2(0.22f, 0.85f), Vector2.zero, Vector2.zero);
                Image iconImg = iconRt.gameObject.AddComponent<Image>();
                iconImg.sprite = SpriteFactory.GetSprite(def.SpriteName, Palette.TorchGold);
                iconImg.preserveAspect = true;

                Text info = UIBuilderUtility.CreateText(
                    "RecruitInfo",
                    card.transform,
                    $"🛡 {def.HeroName} ({def.RoleTitle})\nHP: {def.MaxHP} • Tốc độ: {def.BaseSpeed} • Có Balo riêng {def.BackpackWidth}x{def.BackpackHeight}\n{def.LoreDescription}",
                    11,
                    Palette.BoneText,
                    TextAnchor.MiddleLeft);
                info.rectTransform.anchorMin = new Vector2(0.24f, 0.26f);
                info.rectTransform.anchorMax = new Vector2(0.97f, 0.94f);

                string btnText = Run.Party.Count < RunManager.MaxPartySize
                    ? $"Chiêu Mộ [{def.HeroName}] Vào Đội Hình ({Run.Party.Count + 1}/{RunManager.MaxPartySize})"
                    : "Đội Đã Đủ 3 Người — Huấn Luyện Toàn Đội (+HP & +Ô Balo)";

                Button recBtn = UIBuilderUtility.CreateButton(
                    $"RecruitBtn_{i}",
                    card.transform,
                    btnText,
                    12,
                    new Color(0.20f, 0.42f, 0.28f, 1f),
                    Palette.TorchGold,
                    Palette.BorderGold,
                    () =>
                    {
                        Run.TryRecruitHero(def, out _);
                        RefreshAllColumns();
                    });
                RectTransform rRt = recBtn.GetComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0.24f, 0.05f);
                rRt.anchorMax = new Vector2(0.97f, 0.25f);
                rRt.offsetMin = Vector2.zero;
                rRt.offsetMax = Vector2.zero;
            }
        }

        private void BuildRightEventView()
        {
            Image descBox = UIBuilderUtility.CreatePanel(
                "EventDescBox",
                _rightDynamicContainer,
                Palette.PanelSurface,
                new Vector2(0f, 0.58f),
                new Vector2(1f, 0.98f),
                new Vector2(4f, 4f),
                new Vector2(-4f, -4f),
                true,
                Palette.TierPurple);
            UISkin.Apply(descBox, UISkin.SlotEpic);

            UIBuilderUtility.CreateText(
                "EventLore",
                descBox.transform,
                "🕯 ĐIỆN THỜ HUYẾT NGUYỆT CỔ ĐẠI\n\nGiữa phế tích u ám, một bàn thờ nhuốm máu tỏa ra năng lượng cổ ngữ. Những thánh vật bị lãng quên nằm bên cạnh ngọn nến leo lét.\nHãy chọn hành động của đội hình:",
                13,
                Palette.BoneText,
                TextAnchor.MiddleCenter);

            string[] choices =
            {
                "🩸 [1] Hiến Tế 12 HP: Nhận [Tượng Tà Thần Cổ Quái (Vàng)] vào Kho Chung & Mở +2 Ô Balo",
                "✨ [2] Khấn Nguyện Ánh Nến: Hồi 40% HP Toàn Bộ Đội Hình & Nhận +25 Vàng",
                "🪓 [3] Lục Soát Hầm Mộ: Nhặt [Rìu Đồ Tể Chữ L (Lam)] vào Kho Đồ Chung"
            };

            for (int i = 0; i < choices.Length; i++)
            {
                int captured = i;
                float yMax = 0.52f - (i * 0.16f);
                float yMin = yMax - 0.14f;

                Button btn = UIBuilderUtility.CreateButton(
                    $"EventChoice_{i}",
                    _rightDynamicContainer,
                    choices[i],
                    12,
                    new Color(0.22f, 0.16f, 0.28f, 1f),
                    Palette.BoneText,
                    Palette.TierPurple,
                    () =>
                    {
                        Run.ResolveEventChoice(captured, out _);
                        RefreshAllColumns();
                    });
                RectTransform rt = btn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.02f, yMin);
                rt.anchorMax = new Vector2(0.98f, yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void BuildRightVictoryView()
        {
            Button contBtn = UIBuilderUtility.CreateButton(
                "ContinueEndlessButton",
                _rightDynamicContainer,
                "⚔ Tiếp Tục Thám Hiểm Hầm Ngục Sâu Hơn",
                14,
                new Color(0.20f, 0.42f, 0.28f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                () =>
                {
                    Run.PrepareDoorSelection("🏆 Đã chinh phục Đại Boss! Tiếp tục dấn thân vào vực thẳm");
                    RefreshAllColumns();
                });
            RectTransform cRt = contBtn.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.1f, 0.55f);
            cRt.anchorMax = new Vector2(0.9f, 0.72f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            Button restartBtn = UIBuilderUtility.CreateButton(
                "RestartVictoryButton",
                _rightDynamicContainer,
                "🔄 Khởi Đầu Hành Trình Mới (Bắt Đầu Với 1 Nhân Vật)",
                14,
                new Color(0.28f, 0.22f, 0.38f, 1f),
                Palette.BoneText,
                Palette.BorderIron,
                () =>
                {
                    Run.StartNewRun();
                    RefreshAllColumns();
                });
            RectTransform rRt = restartBtn.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.1f, 0.32f);
            rRt.anchorMax = new Vector2(0.9f, 0.49f);
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;
        }

        private void BuildRightDefeatView()
        {
            Button restartBtn = UIBuilderUtility.CreateButton(
                "RestartRunButton",
                _rightDynamicContainer,
                "🔄 Khởi Đầu Hành Trình Mới (Bắt Đầu Với 1 Nhân Vật)",
                15,
                new Color(0.45f, 0.16f, 0.20f, 1f),
                Palette.TorchGold,
                Palette.BorderGold,
                () =>
                {
                    Run.StartNewRun();
                    RefreshAllColumns();
                });
            RectTransform rt = restartBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 0.6f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private ImageHpSpeedBars CreateCombatantBars(Transform parent, Combatant unit, float yOffset = 0f)
        {
            // HP Bar
            Image hpBg = UIBuilderUtility.CreatePanel(
                "HpBarBg",
                parent,
                new Color(0.06f, 0.05f, 0.08f, 1f),
                new Vector2(0.24f, 0.48f + yOffset),
                new Vector2(0.76f, 0.68f + yOffset),
                Vector2.zero,
                Vector2.zero,
                true,
                Palette.BorderIron);

            float hpRatio = unit.MaxHP > 0 ? Mathf.Clamp01((float)unit.CurrentHP / unit.MaxHP) : 0f;
            Image hpFill = UIBuilderUtility.CreatePanel(
                "HpFill",
                hpBg.transform,
                Palette.HpBarFill,
                new Vector2(0f, 0f),
                new Vector2(hpRatio, 1f),
                Vector2.zero,
                Vector2.zero,
                false);

            Text hpTxt = UIBuilderUtility.CreateText(
                "HpLabel",
                hpBg.transform,
                $"❤ HP: {unit.CurrentHP}/{unit.MaxHP}",
                11,
                Palette.BoneText,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            // Shield Badge next to HP Bar
            Image shieldBox = UIBuilderUtility.CreatePanel(
                "ShieldBadge",
                parent,
                new Color(0.12f, 0.22f, 0.34f, 1f),
                new Vector2(0.78f, 0.48f + yOffset),
                new Vector2(0.98f, 0.68f + yOffset),
                Vector2.zero,
                Vector2.zero,
                true,
                Palette.ShieldBarFill);

            Text shieldTxt = UIBuilderUtility.CreateText(
                "ShieldLabel",
                shieldBox.transform,
                $"🛡 {unit.Shield}",
                11,
                Palette.BoneText,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            // Speed / Initiative Gauge Bar
            Image spdBg = UIBuilderUtility.CreatePanel(
                "SpeedBarBg",
                parent,
                new Color(0.06f, 0.05f, 0.08f, 1f),
                new Vector2(0.24f, 0.26f + yOffset),
                new Vector2(0.98f, 0.44f + yOffset),
                Vector2.zero,
                Vector2.zero,
                true,
                Palette.BorderIron);

            float gaugeRatio = Mathf.Clamp01(unit.ActionGauge / SpeedGaugeManager.TurnThreshold);
            Image spdFill = UIBuilderUtility.CreatePanel(
                "SpeedFill",
                spdBg.transform,
                Palette.SpeedBarFill,
                new Vector2(0f, 0f),
                new Vector2(gaugeRatio, 1f),
                Vector2.zero,
                Vector2.zero,
                false);

            Text spdTxt = UIBuilderUtility.CreateText(
                "SpeedLabel",
                spdBg.transform,
                $"⚡ Tốc độ: {unit.GetEffectiveSpeed()}  •  Thanh lượt: {Mathf.RoundToInt(unit.ActionGauge)}%",
                11,
                Palette.BoneText,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            return new ImageHpSpeedBars
            {
                HpFill = hpFill,
                HpLabel = hpTxt,
                ShieldLabel = shieldTxt,
                SpeedFill = spdFill,
                SpeedLabel = spdTxt
            };
        }

        private void UpdateLiveCombatBars()
        {
            if (Run == null) return;

            for (int i = 0; i < Run.Party.Count && i < _heroCards.Length; i++)
            {
                HeroCombatant h = Run.Party[i];
                HeroCardUI ui = _heroCards[i];
                if (h == null || ui.Root == null) continue;
                UpdateBarValues(ui.Bars, h);
                if (ui.StatusText != null)
                {
                    ui.StatusText.text = FormatCombatantStatusLine(h, includeBagCount: true);
                }
            }

            for (int i = 0; i < _monsterCards.Count; i++)
            {
                MonsterCardUI mUi = _monsterCards[i];
                if (mUi.Monster == null || mUi.Bars.HpFill == null) continue;
                UpdateBarValues(mUi.Bars, mUi.Monster);
                if (mUi.IntentBannerText != null)
                {
                    mUi.IntentBannerText.text = mUi.Monster.IsAlive
                        ? $"👁 Ý ĐỊNH LƯỢT TỚI: {mUi.Monster.CurrentIntent.GetSummaryText()}"
                        : "💀 ĐÃ BỊ TIÊU DIỆT";
                }
                if (mUi.StatusText != null)
                {
                    mUi.StatusText.text = FormatCombatantStatusLine(mUi.Monster, includeBagCount: false);
                }
            }
        }

        private static void UpdateBarValues(ImageHpSpeedBars bars, Combatant unit)
        {
            if (bars.HpFill != null)
            {
                float hpRatio = unit.MaxHP > 0 ? Mathf.Clamp01((float)unit.CurrentHP / unit.MaxHP) : 0f;
                bars.HpFill.rectTransform.anchorMax = new Vector2(hpRatio, 1f);
            }
            if (bars.HpLabel != null)
            {
                bars.HpLabel.text = $"❤ HP: {unit.CurrentHP}/{unit.MaxHP}";
            }
            if (bars.ShieldLabel != null)
            {
                bars.ShieldLabel.text = $"🛡 {unit.Shield}";
            }
            if (bars.SpeedFill != null)
            {
                float gRatio = Mathf.Clamp01(unit.ActionGauge / SpeedGaugeManager.TurnThreshold);
                bars.SpeedFill.rectTransform.anchorMax = new Vector2(gRatio, 1f);
            }
            if (bars.SpeedLabel != null)
            {
                bars.SpeedLabel.text = $"⚡ Tốc độ: {unit.GetEffectiveSpeed()}  •  Thanh lượt: {Mathf.RoundToInt(unit.ActionGauge)}%";
            }
        }

        private static string FormatCombatantStatusLine(Combatant c, bool includeBagCount)
        {
            string line = $"☣ Độc: {c.PoisonStacks}   🩸 Chảy máu: {c.BleedStacks}   ⚡ Choáng: {c.StunStacks}";
            if (includeBagCount && c is HeroCombatant h && h.PersonalBackpack != null)
            {
                line += $"   •   🎒 Trang bị: {h.PersonalBackpack.PlacedItems.Count}";
            }
            return line;
        }

        public static GridCoord ResolveBestPlacementAnchor(BackpackGrid grid, ItemData item, int rawX, int rawY, int rotationSteps)
        {
            if (grid == null || item == null || !grid.IsSlotUnlocked(rawX, rawY))
            {
                return new GridCoord(rawX, rawY);
            }

            if (grid.CanPlaceItemAt(item, rawX, rawY, rotationSteps, item))
            {
                return new GridCoord(rawX, rawY);
            }

            // If cursor is on an unlocked cell near the right/bottom edge, try aligning another occupied cell of the item onto (rawX, rawY)
            IReadOnlyList<GridCoord> occ = item.Definition.Shape.GetOccupiedOffsets(rotationSteps);
            for (int i = 0; i < occ.Count; i++)
            {
                int candX = rawX - occ[i].X;
                int candY = rawY - occ[i].Y;
                if (grid.CanPlaceItemAt(item, candX, candY, rotationSteps, item))
                {
                    return new GridCoord(candX, candY);
                }
            }

            return new GridCoord(rawX, rawY);
        }

        private void HandleCellBeginDrag(BackpackGridView view, int x, int y, PointerEventData eventData)
        {
            if (view == null || view.BoundGrid == null) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;

            ItemData itemAtCell = view.BoundGrid.GetItemAt(x, y);
            if (itemAtCell == null) return;

            _heldItem = itemAtCell;
            _heldSourceGrid = view.BoundGrid;
            _heldRotationSteps = itemAtCell.RotationSteps;
            _isPointerDragging = true;

            RefreshDragGhostVisual();
            if (eventData != null)
            {
                UpdateDragGhostPosition(eventData.position);
            }

            UpdateInspectorForItem(_heldItem, isHolding: true);
            GridCoord snap = ResolveBestPlacementAnchor(view.BoundGrid, _heldItem, x, y, _heldRotationSteps);
            view.HighlightPreview(_heldItem, snap.X, snap.Y, _heldRotationSteps);
        }

        private void HandleCellDragged(BackpackGridView view, PointerEventData eventData)
        {
            if (_heldItem != null && eventData != null)
            {
                UpdateDragGhostPosition(eventData.position);
            }
        }

        private void HandleCellDropped(BackpackGridView targetView, int x, int y, PointerEventData eventData)
        {
            if (_heldItem == null || targetView == null || targetView.BoundGrid == null) return;

            BackpackGrid targetGrid = targetView.BoundGrid;
            GridCoord snap = ResolveBestPlacementAnchor(targetGrid, _heldItem, x, y, _heldRotationSteps);
            bool placed = Run.TryTransferOrMoveItem(_heldItem, _heldSourceGrid, targetGrid, snap.X, snap.Y, _heldRotationSteps);
            if (placed)
            {
                _itemInspectorText.text = $"✔ Đã kéo thả [{_heldItem.Definition.DisplayName}] tại ({snap.X}, {snap.Y}) trong {targetGrid.GridName}!";
                ClearHeldItemState();
                RefreshLeftColumnBackpacks();
                RefreshMiddleColumnParty();
            }
            else
            {
                _itemInspectorText.text = $"❌ Không thể thả [{_heldItem.Definition.DisplayName}] tại ({x}, {y})! Đã trả về vị trí cũ.";
                ClearHeldItemState();
            }
        }

        private void HandleCellEndDrag(BackpackGridView view, PointerEventData eventData)
        {
            if (_isPointerDragging)
            {
                ClearHeldItemState();
            }
        }

        private void HandleGridCellClicked(BackpackGridView view, int x, int y, PointerEventData.InputButton button)
        {
            if (view == null || view.BoundGrid == null) return;
            BackpackGrid clickedGrid = view.BoundGrid;

            // Right-Click: Cancel held item if holding one, otherwise Quick Transfer between Shared Stash and Selected Hero's Personal Backpack!
            if (button == PointerEventData.InputButton.Right)
            {
                if (_heldItem != null)
                {
                    CancelHeldItem();
                    return;
                }

                ItemData rightClickedItem = clickedGrid.GetItemAt(x, y);
                if (rightClickedItem != null && Run.SelectedHero != null)
                {
                    BackpackGrid targetGrid = clickedGrid == Run.SharedStash
                        ? Run.SelectedHero.PersonalBackpack
                        : Run.SharedStash;

                    clickedGrid.RemoveItem(rightClickedItem);
                    if (!targetGrid.TryAutoPlaceItem(rightClickedItem))
                    {
                        // Put back if no room
                        clickedGrid.TryAutoPlaceItem(rightClickedItem);
                        _itemInspectorText.text = $"⚠ Không đủ ô trống trong {targetGrid.GridName} để chuyển nhanh [{rightClickedItem.Definition.DisplayName}]!";
                    }
                    else
                    {
                        _itemInspectorText.text = $"✔ Đã chuyển [{rightClickedItem.Definition.DisplayName}] sang {targetGrid.GridName}!";
                    }

                    ClearHeldItemState();
                    Run.RecalculateAllPartySynergies();
                    RefreshLeftColumnBackpacks();
                    RefreshMiddleColumnParty();
                }
                return;
            }

            // Left-Click: Pick up item if none held, or Place held item onto (x, y)
            if (_heldItem == null)
            {
                ItemData itemAtCell = clickedGrid.GetItemAt(x, y);
                if (itemAtCell != null)
                {
                    _heldItem = itemAtCell;
                    _heldSourceGrid = clickedGrid;
                    _heldRotationSteps = itemAtCell.RotationSteps;
                    _isPointerDragging = false;
                    RefreshDragGhostVisual();
                    UpdateInspectorForItem(_heldItem, isHolding: true);
                    view.HighlightPreview(_heldItem, itemAtCell.AnchorX, itemAtCell.AnchorY, _heldRotationSteps);
                }
                else if (!clickedGrid.IsSlotUnlocked(x, y))
                {
                    _itemInspectorText.text = "🔒 Ô Balo này đang bị khóa! Mở khóa qua Cửa Hàng hoặc Buff Cánh Cửa.";
                }
            }
            else
            {
                GridCoord snap = ResolveBestPlacementAnchor(clickedGrid, _heldItem, x, y, _heldRotationSteps);
                bool placed = Run.TryTransferOrMoveItem(_heldItem, _heldSourceGrid, clickedGrid, snap.X, snap.Y, _heldRotationSteps);
                if (placed)
                {
                    _itemInspectorText.text = $"✔ Đã đặt [{_heldItem.Definition.DisplayName}] tại ({snap.X}, {snap.Y}) trong {clickedGrid.GridName}!";
                    ClearHeldItemState();
                    RefreshLeftColumnBackpacks();
                    RefreshMiddleColumnParty();
                }
                else
                {
                    _itemInspectorText.text = $"❌ Không thể đặt [{_heldItem.Definition.DisplayName}] tại ({x}, {y})! Nhấn [R] để xoay hoặc Click Phải để hủy.";
                    view.HighlightPreview(_heldItem, snap.X, snap.Y, _heldRotationSteps);
                }
            }
        }

        private void HandleGridCellHovered(BackpackGridView view, int x, int y)
        {
            if (_lastHoveredGridView != null && _lastHoveredGridView != view)
            {
                _lastHoveredGridView.HighlightPreview(null, 0, 0, 0);
            }

            _lastHoveredGridView = view;
            _lastHoverX = x;
            _lastHoverY = y;

            if (_heldItem != null && view != null && view.BoundGrid != null)
            {
                GridCoord snap = ResolveBestPlacementAnchor(view.BoundGrid, _heldItem, x, y, _heldRotationSteps);
                view.HighlightPreview(_heldItem, snap.X, snap.Y, _heldRotationSteps);
                return;
            }

            ItemData hoverItem = view.BoundGrid?.GetItemAt(x, y);
            if (hoverItem != null)
            {
                UpdateInspectorForItem(hoverItem, isHolding: false);
                view.HighlightPreview(hoverItem, hoverItem.AnchorX, hoverItem.AnchorY, hoverItem.RotationSteps);
            }
            else if (view != null)
            {
                view.HighlightPreview(null, 0, 0, 0);
            }
        }

        private void HandleGridPointerExited(BackpackGridView view)
        {
            if (view != null)
            {
                view.HighlightPreview(null, 0, 0, 0);
            }
        }

        public void CancelHeldItem()
        {
            if (_heldItem == null) return;
            string itemName = _heldItem.Definition != null ? _heldItem.Definition.DisplayName : "Trang bị";
            ClearHeldItemState();
            _itemInspectorText.text = $"↩ Đã hủy di chuyển [{itemName}].";
        }

        public void SellCurrentlyHeldItem()
        {
            ItemData targetItem = _heldItem;
            BackpackGrid sourceGrid = _heldSourceGrid;

            if (targetItem == null && _lastHoveredGridView != null && _lastHoveredGridView.BoundGrid != null)
            {
                targetItem = _lastHoveredGridView.BoundGrid.GetItemAt(_lastHoverX, _lastHoverY);
                sourceGrid = _lastHoveredGridView.BoundGrid;
            }

            if (targetItem == null || sourceGrid == null)
            {
                _itemInspectorText.text = "💡 Hãy Click chọn 1 trang bị trong Balo/Kho Chung trước khi nhấn [Bán Đồ]!";
                return;
            }

            if (Run.TrySellItem(targetItem, sourceGrid, out string msg))
            {
                ClearHeldItemState();
                _itemInspectorText.text = msg;
                RefreshAllColumns();
            }
        }

        private void ClearHeldItemState()
        {
            _heldItem = null;
            _heldSourceGrid = null;
            _isPointerDragging = false;
            _heroBackpackView?.HighlightPreview(null, 0, 0, 0);
            _sharedStashView?.HighlightPreview(null, 0, 0, 0);
            RefreshDragGhostVisual();
        }

        private void RefreshDragGhostVisual()
        {
            if (_dragGhostRoot == null) return;

            if (_heldItem == null || _heldItem.Definition == null)
            {
                _dragGhostRoot.gameObject.SetActive(false);
                if (_sellHeldItemBtnLabel != null)
                {
                    _sellHeldItemBtnLabel.text = "💰 Bán Đồ Đang Cầm";
                }
                return;
            }

            ItemDefinitionSO def = _heldItem.Definition;
            Color tierCol = Palette.GetTierColor(def.RarityTier);
            _dragGhostIcon.sprite = SpriteFactory.GetSprite(def.SpriteName, tierCol);
            int w = def.Shape.GetWidth(_heldRotationSteps);
            int h = def.Shape.GetHeight(_heldRotationSteps);
            _dragGhostLabel.text = $"{w}x{h} • {_heldRotationSteps * 90}°";
            _dragGhostRoot.gameObject.SetActive(true);

            if (_sellHeldItemBtnLabel != null)
            {
                int val = Mathf.Max(5, def.GoldCost / 2);
                _sellHeldItemBtnLabel.text = $"💰 Bán (+{val} Vàng)";
            }
        }

        private void UpdateDragGhostPosition(Vector2 screenPos)
        {
            if (_dragGhostRoot == null || _mainCanvas == null) return;
            RectTransform canvasRt = _mainCanvas.transform as RectTransform;
            if (canvasRt == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, null, out Vector2 localPoint))
            {
                _dragGhostRoot.anchoredPosition = localPoint + new Vector2(28f, -28f);
            }
        }

        public void RotateHeldOrHoveredItem()
        {
            if (_heldItem != null)
            {
                _heldRotationSteps = ItemShape.NormalizeRotation(_heldRotationSteps + 1);
                RefreshDragGhostVisual();
                UpdateInspectorForItem(_heldItem, isHolding: true);
                if (_lastHoveredGridView != null && _lastHoveredGridView.BoundGrid != null)
                {
                    GridCoord snap = ResolveBestPlacementAnchor(_lastHoveredGridView.BoundGrid, _heldItem, _lastHoverX, _lastHoverY, _heldRotationSteps);
                    _lastHoveredGridView.HighlightPreview(_heldItem, snap.X, snap.Y, _heldRotationSteps);
                }
                return;
            }

            // If hovering over an already placed item, try rotating it in place or picking it up rotated
            if (_lastHoveredGridView != null && _lastHoveredGridView.BoundGrid != null)
            {
                BackpackGrid g = _lastHoveredGridView.BoundGrid;
                ItemData hovered = g.GetItemAt(_lastHoverX, _lastHoverY);
                if (hovered != null)
                {
                    int nextRot = ItemShape.NormalizeRotation(hovered.RotationSteps + 1);
                    if (g.CanPlaceItemAt(hovered, hovered.AnchorX, hovered.AnchorY, nextRot, hovered))
                    {
                        g.TryPlaceItem(hovered, hovered.AnchorX, hovered.AnchorY, nextRot);
                        Run.RecalculateAllPartySynergies();
                        RefreshLeftColumnBackpacks();
                        RefreshMiddleColumnParty();
                        _itemInspectorText.text = $"🔄 Đã xoay [{hovered.Definition.DisplayName}] 90° ngay tại chỗ!";
                    }
                    else
                    {
                        // Pick it up rotated so the player can place it where it fits
                        _heldItem = hovered;
                        _heldSourceGrid = g;
                        _heldRotationSteps = nextRot;
                        _isPointerDragging = false;
                        RefreshDragGhostVisual();
                        UpdateInspectorForItem(_heldItem, isHolding: true);
                        GridCoord snap = ResolveBestPlacementAnchor(g, _heldItem, _lastHoverX, _lastHoverY, _heldRotationSteps);
                        _lastHoveredGridView.HighlightPreview(_heldItem, snap.X, snap.Y, _heldRotationSteps);
                    }
                }
            }
        }

        private void UpdateInspectorForItem(ItemData item, bool isHolding)
        {
            if (item == null || item.Definition == null) return;
            ItemDefinitionSO d = item.Definition;
            string holdPrefix = isHolding
                ? $"🖐 ĐANG CẦM [{d.DisplayName}] (Góc xoay: {_heldRotationSteps * 90}° — Nhấn R xoay, Click Trái đặt, Click Phải hủy): "
                : $"🔍 [{d.DisplayName}] (Hồi chiêu: {d.CooldownTurns} lượt • Kéo thả / Click Trái di chuyển, Phải chuyển Kho): ";
            _itemInspectorText.text = holdPrefix + d.Description;
        }

        private void ToggleAutoBattle()
        {
            _autoBattleEnabled = !_autoBattleEnabled;
            if (_autoBattleBtnLabel != null)
            {
                _autoBattleBtnLabel.text = _autoBattleEnabled ? "⏯ Tự Đánh: BẬT" : "⏸ Tự Đánh: TẮT";
            }
        }

        private void StepOneCombatTurn()
        {
            if (Run != null && Run.State == RunState.Combat)
            {
                Run.Combat.StepNextTurnInstant();
                RefreshAllColumns();
            }
        }

        private void ToggleBattleSpeed()
        {
            _battleSpeedScale = _battleSpeedScale < 2.2f ? 3.2f : 1.6f;
            if (_speedScaleBtnLabel != null)
            {
                _speedScaleBtnLabel.text = _battleSpeedScale > 2.2f ? "⚡ Tốc Độ: 2x" : "⚡ Tốc Độ: 1x";
            }
        }

        private void OnSelectedHeroChanged(int heroIdx)
        {
            ClearHeldItemState();
            RefreshLeftColumnBackpacks();
            RefreshMiddleColumnParty();
        }

        private void OnRunNotification(string msg)
        {
            if (_bannerMessageText != null)
            {
                _bannerMessageText.text = msg;
            }
        }

        private void OnCombatTurnExecuted(Combatant actor, string turnSummary)
        {
            RefreshLeftColumnBackpacks();
            UpdateLiveCombatBars();
            RefreshCombatLogText();
        }

        private void RefreshCombatLogText()
        {
            if (_combatLogText == null || Run == null) return;
            IReadOnlyList<string> logs = Run.Combat.CombatLogs;
            if (logs.Count == 0)
            {
                _combatLogText.text = "Chưa có giao tranh.";
                return;
            }

            int start = Mathf.Max(0, logs.Count - 4);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = start; i < logs.Count; i++)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(logs[i]);
            }
            _combatLogText.text = sb.ToString();
        }
    }
}
