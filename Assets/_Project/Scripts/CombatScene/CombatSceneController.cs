using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    public enum CombatScenePhase
    {
        Planning = 0,
        Playing = 1,
        PostCombat = 2,

        /// <summary>A room was won: buff claim, item shelf and door choice (the run's reward steps).</summary>
        RunReward = 3
    }

    /// <summary>
    /// Top-level runtime orchestrator for <c>Combat.unity</c> in temporary 1-hero mode (Hero0 active).
    /// Connects the interactive Workbench + Equipment Picker, live Turn Order Forecast,
    /// deterministic <see cref="CombatCore"/> simulation, HUD playback, Post-Combat breakdown,
    /// and the Next-Stage Monster Count Test System (1..6).
    /// </summary>
    public sealed partial class CombatSceneController : MonoBehaviour
    {
        [Header("Data Catalogs")]
        [SerializeField] private TextAsset itemsJson;
        [SerializeField] private TextAsset heroesJson;
        [SerializeField] private TextAsset monstersJson;
        [SerializeField] private CombatSceneSpriteCatalog spriteCatalog;

        [Header("Scene References")]
        [SerializeField] private CombatHudView hudView;
        [SerializeField] private ItemPopupController popupController;
        [SerializeField] private RectTransform safeFrame;
        [SerializeField] private RectTransform workbenchRoot;
        [SerializeField] private Camera uiCamera;

        [Header("Run & Test Defaults")]
        [SerializeField] private ulong stageSeed = 12345UL;
        [SerializeField] [Range(1, 6)] private int defaultMonsterCount = 2;
        [SerializeField] private int roomCount = 8;
        [SerializeField] private int startingCurrency = 120;

        private EncounterPresetCatalog _presetCatalog;
        private SequentialItemIdSource _idSource;
        private List<EnemySpawnInput> _currentSpawns = new List<EnemySpawnInput>();
        private RectTransform _planningActionStripRoot;
        private Button _fightButton;

        // Integrated mode fields (when wired with CombatScreenController + ItemBoardController)
        private ItemBoardController _externalBoard;
        private ActionStripView _externalStrip;
        private bool _integratedMode;
        private RectTransform _pickerOverlay;
        private EquipmentPickerView _integratedPicker;
        private Button _pickerToggleBtn;
        private TextMeshProUGUI _pickerToggleLabel;
        private int _playbackSpeedIndex;

        public event Action StageReturnedToPlanning;

        public GameCatalog Catalog { get; private set; }
        public HeroDef HeroDef { get; private set; }
        public BackpackArrangement Arrangement { get; private set; }
        public PreparedEncounter Prepared { get; private set; }
        public CombatResult LastResult { get; private set; }

        public WorkbenchController Workbench { get; private set; }
        public NextStageTestPanel NextStagePanel { get; private set; }
        public CombatPlaybackController Playback { get; private set; }
        public PostCombatPanelView PostCombatPanel { get; private set; }

        public CombatScenePhase Phase { get; private set; } = CombatScenePhase.Planning;
        public int CurrentStage { get; private set; } = 1;
        public int NextMonsterCount { get; private set; } = 2;
        public int Currency { get; private set; }
        public bool IsFighting => Phase == CombatScenePhase.Playing;
        public string LastPrepareError { get; private set; }

        private void Start()
        {
            if (!_integratedMode && FindAnyObjectByType<CombatScreenController>() == null)
            {
                InitializeRuntime();
            }
        }

        /// <summary>
        /// Integrated initialization driven by <see cref="CombatScreenController"/>: keeps <c>main</c>'s
        /// <see cref="ItemBoardController"/>, <see cref="SellDrawerView"/>, pause/settings modals, and pixel cursor,
        /// while replacing fixture playback with real <see cref="CombatCore"/> simulation, 1-hero centering,
        /// an equipment picker ("Chọn đồ"), and the 1..6 monster count test selector.
        /// </summary>
        public void InitializeIntegrated(
            ItemBoardController externalBoard,
            CombatHudView externalHud,
            ActionStripView externalStrip,
            ItemPopupController externalPopup,
            Transform externalSafeFrame,
            int initialSpeedIndex)
        {
            if (Catalog != null) return;

            _integratedMode = true;
            _externalBoard = externalBoard;
            hudView = externalHud;
            _externalStrip = externalStrip;
            popupController = externalPopup;
            if (externalSafeFrame is RectTransform rt) safeFrame = rt;
            _playbackSpeedIndex = Mathf.Clamp(initialSpeedIndex, 0, 2);

            if (!LoadCatalogs()) return;

            CurrentStage = 1;
            NextMonsterCount = Mathf.Clamp(defaultMonsterCount, 1, 6);
            Currency = startingCurrency;
            _idSource = new SequentialItemIdSource(1);
            InitRun();

            CenterSingleHeroSlot();
            BuildIntegratedPanels();
            BuildRunPanels();
            SyncArrangementFromExternalBoard();
            PrepareStage(CurrentStage, NextMonsterCount);
        }

        private bool LoadCatalogs()
        {
            if (itemsJson == null || heroesJson == null || monstersJson == null || spriteCatalog == null || hudView == null)
            {
                Debug.LogError("[CombatSceneController] Missing serialized catalog or scene references.");
                return false;
            }

            Catalog = GameCatalog.LoadFromJson(
                itemsJson.text,
                heroesJson.text,
                monstersJson.text,
                out IReadOnlyList<string> validationErrors);

            if (validationErrors != null && validationErrors.Count > 0)
            {
                for (int i = 0; i < validationErrors.Count; i++)
                {
                    Debug.LogWarning("[CombatSceneController] Catalog validation: " + validationErrors[i]);
                }
            }

            _presetCatalog = new EncounterPresetCatalog(monstersJson.text);
            HeroDef = Catalog.FindHero("hero_crusader") ?? (Catalog.Heroes.Count > 0 ? Catalog.Heroes[0] : null);
            if (HeroDef == null)
            {
                Debug.LogError("[CombatSceneController] No hero definition available.");
                return false;
            }

            return true;
        }

        public void AddCurrency(int delta) => AddCurrency(delta, null);

        /// <summary>
        /// Changes the gold at once (game state) and presents it: the counter ticks, and with a source point the coins first
        /// hop from there to the counter (feedback-motion.md P-10, P-11, R-10).
        /// </summary>
        public void AddCurrency(int delta, Vector2? coinsFrom)
        {
            int before = Currency;
            Currency = Mathf.Max(0, Currency + delta);
            if (hudView == null) return;
            if (_feedback == null || !Application.isPlaying)
            {
                hudView.SetCurrency(Currency);
                return;
            }

            int after = Currency;
            if (coinsFrom.HasValue && delta > 0)
                _feedback.CoinFlight(coinsFrom.Value, new Vector2(10f, 8f), Mathf.Clamp(delta, 3, 10), () => _feedback.Tick(hudView.SetCurrencyText, before, after));
            else
                _feedback.Tick(hudView.SetCurrencyText, before, after);
        }

        private Project05.UI.Motion.FeedbackFx _feedback;

        /// <summary>The reactions layer (tests and probes).</summary>
        public Project05.UI.Motion.FeedbackFx Feedback => _feedback;

        public void SetMonsterSelectorVisible(bool visible)
        {
            if (_integratedMode && NextStagePanel != null)
            {
                NextStagePanel.SetVisible(visible);
            }
        }

        public void SetPlaybackSpeed(int index)
        {
            _playbackSpeedIndex = Mathf.Clamp(index, 0, 2);
            Playback?.SetSpeedIndex(_playbackSpeedIndex);
        }

        public void ArmSkip()
        {
            Playback?.SetSpeedIndex(3);
        }

        public void SetPlaybackPaused(bool paused)
        {
            Playback?.SetPaused(paused);
        }

        public void RefreshFromExternalBoard()
        {
            if (!_integratedMode || Phase != CombatScenePhase.Planning) return;
            SyncArrangementFromExternalBoard();
            RefreshPlanningHud();
        }

        public bool StartIntegratedFight(int speedIndex)
        {
            if (!_integratedMode || Phase != CombatScenePhase.Planning || Catalog == null || !CanStartFight) return false;

            SetPickerOpen(false);
            SyncArrangementFromExternalBoard();
            Arrangement.Freeze();
            if (!TryPrepareCurrentArrangement(out PreparedEncounter prepared, out string error))
            {
                LastPrepareError = error;
                Debug.LogWarning("[CombatSceneController] Cannot start fight: " + error);
                Arrangement.Thaw();
                return false;
            }

            Prepared = prepared;
            LastPrepareError = null;
            LastResult = CombatRunner.RunEncounter(Catalog, Prepared);

            SetPhase(CombatScenePhase.Playing);
            PostCombatPanel?.Hide();

            _playbackSpeedIndex = Mathf.Clamp(speedIndex, 0, 2);
            if (Playback != null && Playback.SpeedIndex != 3)
            {
                Playback.SetSpeedIndex(_playbackSpeedIndex);
            }

            CombatHudData startData = CombatHudMapper.MapPlanning(
                Catalog,
                HeroDef,
                Arrangement.CaptureView().HeroGrids[0],
                null,
                Prepared,
                CurrentStage,
                roomCount,
                Currency,
                _playbackSpeedIndex,
                RunBaseline(Arrangement.CaptureView().HeroGrids[0], null));
            hudView.Apply(startData);

            Playback.Play(Catalog, Prepared, LastResult, CurrentStage, roomCount, Currency);
            return true;
        }

        // Simulated item instance -> the board item it was made from (integrated mode), for backpack effects.
        private readonly Dictionary<long, Project05.Items.BoardItem> _simToBoard = new Dictionary<long, Project05.Items.BoardItem>();

        /// <summary>The backpack plate showing a simulated item instance, or null.</summary>
        private RectTransform PlateOfSimItem(long instanceId)
        {
            if (_integratedMode)
            {
                return _externalBoard != null && _simToBoard.TryGetValue(instanceId, out Project05.Items.BoardItem bi) ? _externalBoard.PlateOf(bi) : null;
            }

            return Workbench != null ? Workbench.PlateOf(instanceId) : null;
        }

        /// <summary>The 1x1 tier plate and the glyph an item's avatar is drawn with (art bible 5.10.2).</summary>
        private (Sprite plate, Sprite glyph) AvatarSprites(string itemDefId)
        {
            ItemDef def = Catalog?.FindItem(itemDefId);
            if (def == null || spriteCatalog == null) return (null, null);
            Sprite plate = spriteCatalog.GetAuthoredPlate(def.Tier.ToString().ToLowerInvariant(), false, "1x1");
            Sprite glyph = spriteCatalog.GetItemIcon(def.Art, def.Footprint, compact: true);
            return (plate, glyph);
        }

        private void SyncArrangementFromExternalBoard()
        {
            _simToBoard.Clear();
            if (Catalog == null || HeroDef == null) return;

            _idSource = new SequentialItemIdSource(1);
            Arrangement = new BackpackArrangement(
                Catalog,
                heroCount: 1,
                heroProfiles: new[] { HeroDef.BackpackProfile },
                idSource: _idSource,
                stashWidth: WorkbenchLayout.Stash,
                stashHeight: WorkbenchLayout.StashRows,
                rotationEnabled: true);

            if (_externalBoard == null) return;

            List<Project05.Items.BoardItem> gridItems = _externalBoard.ExportContainerItems("grid");
            for (int i = 0; i < gridItems.Count; i++)
            {
                Project05.Items.BoardItem bi = gridItems[i];
                if (bi == null || bi.Record == null) continue;
                if (!Arrangement.CreateInTray(bi.Record.id, out long itemId).IsOk) continue;
                if (!Arrangement.Place(itemId, ContainerKind.Hero0, bi.X, bi.Y, bi.Rotation).IsOk)
                {
                    Arrangement.DestroyTrayItem(itemId);
                    continue;
                }

                _simToBoard[itemId] = bi;
            }
        }

        private void BuildIntegratedPanels()
        {
            // Next-stage 1..6 monster test selector inside ActionStrip's StartGroup
            Transform startGroupTf = _externalStrip != null ? _externalStrip.transform.parent.Find("StartGroup") : null;
            if (startGroupTf is RectTransform startGroupRt && !RunActive)
            {
                NextStagePanel = gameObject.AddComponent<NextStageTestPanel>();
                NextStagePanel.Initialize(startGroupRt, spriteCatalog, NextMonsterCount);
                NextStagePanel.MonsterCountChanged += SetNextStageMonsterCount;
                NextStagePanel.ApplyNowRequested += ApplyMonsterCountImmediately;
            }

            // "Chọn đồ" button in Workbench header (right of hero name) + picker overlay
            Transform wbTf = safeFrame != null ? safeFrame.Find("Workbench") : null;
            if (wbTf is RectTransform wbRt)
            {
                _pickerToggleBtn = CombatUiFactory.CreateSegmentButton(
                    "PickerToggleButton",
                    wbRt,
                    spriteCatalog,
                    "Chọn đồ",
                    116f,
                    20f,
                    52f,
                    16f,
                    selected: false);
                _pickerToggleLabel = _pickerToggleBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                _pickerToggleBtn.onClick.AddListener(TogglePickerOverlay);

                _pickerOverlay = CombatUiFactory.CreateRect("EquipmentPickerOverlay", wbRt, 4f, 38f, 166f, 174f);
                CombatUiFactory.CreateImage("PickerBg", _pickerOverlay, spriteCatalog.panelDefault, 0f, 0f, 166f, 174f, sliced: true, raycastTarget: true);
                CombatUiFactory.CreateLabel("PickerTitle", _pickerOverlay, spriteCatalog.font, "Chọn trang bị (34):", 6f, 5f, 120f, 12f, CombatUiFactory.Bone);
                Button closePickerBtn = CombatUiFactory.CreateSegmentButton("ClosePickerBtn", _pickerOverlay, spriteCatalog, "X", 142f, 4f, 18f, 14f);
                closePickerBtn.onClick.AddListener(() => SetPickerOpen(false));

                RectTransform pickerBody = CombatUiFactory.CreateRect("PickerBody", _pickerOverlay, 4f, 20f, 158f, 150f);
                _integratedPicker = pickerBody.gameObject.AddComponent<EquipmentPickerView>();
                _integratedPicker.Initialize(pickerBody, spriteCatalog, popupController);
                _integratedPicker.Populate(Catalog);
                _integratedPicker.ItemClicked += HandleIntegratedPickerClicked;
                _pickerOverlay.gameObject.SetActive(false);

                // planning-screen.md P19: a developer tool, not a player feature. Hidden in a run unless F9 shows it;
                // absent from release builds.
                _pickerDebugShown = !RunActive && (Application.isEditor || Debug.isDebugBuild);
                _pickerToggleBtn.gameObject.SetActive(_pickerDebugShown);
            }

            // Playback & Post-Combat panels
            Playback = gameObject.AddComponent<CombatPlaybackController>();
            Playback.Initialize(hudView);
            Playback.SetPresentation(PlateOfSimItem, AvatarSprites);
            Playback.PlaybackCompleted += HandlePlaybackCompleted;

            PostCombatPanel = gameObject.AddComponent<PostCombatPanelView>();
            PostCombatPanel.Initialize(safeFrame, spriteCatalog);
            PostCombatPanel.NextMonsterCountSelected += SetNextStageMonsterCount;
            PostCombatPanel.NextStageClicked += ProceedToNextStage;
            PostCombatPanel.RetryClicked += RetryStage;

            // Keep ItemPopup topmost so hover tooltips stay above PostCombatPanel and PickerOverlay.
            Transform popupViewTf = safeFrame != null ? safeFrame.Find("ItemPopup") : null;
            if (popupViewTf != null) popupViewTf.SetAsLastSibling();
        }

        private void TogglePickerOverlay()
        {
            if (_pickerOverlay == null) return;
            SetPickerOpen(!_pickerOverlay.gameObject.activeSelf);
        }

        private void SetPickerOpen(bool open)
        {
            if (_pickerOverlay != null) _pickerOverlay.gameObject.SetActive(open);
            if (_pickerToggleBtn != null) CombatUiFactory.SetSegmentVisual(_pickerToggleBtn, spriteCatalog, open);
            if (!open && popupController != null) popupController.Close();
        }

        private void HandleIntegratedPickerClicked(string itemId)
        {
            if (_externalBoard == null || popupController == null || spriteCatalog == null) return;
            Project05.Items.ItemRecord record = popupController.Catalog.Find(itemId);
            if (record == null) return;

            string tierLower = string.IsNullOrEmpty(record.tier) ? "white" : record.tier.ToLowerInvariant();
            Sprite plateSprite = spriteCatalog.GetAuthoredPlate(tierLower, false, record.footprint);
            Sprite iconSprite = spriteCatalog.GetItemIcon(record.art, record.footprint, compact: false);
            if (!_externalBoard.TrySpawnItem(record, plateSprite, iconSprite, "stash", out _))
            {
                Debug.LogWarning("[CombatSceneController] Stash and Grid are full; sell or remove an item first.");
            }
        }

        private void OnDestroy()
        {
            if (Workbench != null) Workbench.ArrangementChanged -= HandleArrangementChanged;
            if (Playback != null) Playback.PlaybackCompleted -= HandlePlaybackCompleted;
            if (NextStagePanel != null)
            {
                NextStagePanel.MonsterCountChanged -= SetNextStageMonsterCount;
                NextStagePanel.ApplyNowRequested -= ApplyMonsterCountImmediately;
            }
            if (PostCombatPanel != null)
            {
                PostCombatPanel.NextMonsterCountSelected -= SetNextStageMonsterCount;
                PostCombatPanel.NextStageClicked -= ProceedToNextStage;
                PostCombatPanel.RetryClicked -= RetryStage;
            }
        }

        /// <summary>
        /// Initializes catalogs, 1-hero arrangement, starter items, and runtime UI modules.
        /// Safe to call explicitly from integration tests immediately after scene load.
        /// </summary>
        public void InitializeRuntime()
        {
            if (Catalog != null) return;

            if (itemsJson == null || heroesJson == null || monstersJson == null || spriteCatalog == null || hudView == null)
            {
                Debug.LogError("[CombatSceneController] Missing serialized catalog or scene references.");
                return;
            }

            Catalog = GameCatalog.LoadFromJson(
                itemsJson.text,
                heroesJson.text,
                monstersJson.text,
                out IReadOnlyList<string> validationErrors);

            if (validationErrors != null && validationErrors.Count > 0)
            {
                for (int i = 0; i < validationErrors.Count; i++)
                {
                    Debug.LogWarning("[CombatSceneController] Catalog validation: " + validationErrors[i]);
                }
            }

            _presetCatalog = new EncounterPresetCatalog(monstersJson.text);
            HeroDef = Catalog.FindHero("hero_crusader") ?? (Catalog.Heroes.Count > 0 ? Catalog.Heroes[0] : null);
            if (HeroDef == null)
            {
                Debug.LogError("[CombatSceneController] No hero definition available.");
                return;
            }

            CurrentStage = 1;
            NextMonsterCount = Mathf.Clamp(defaultMonsterCount, 1, 6);
            Currency = startingCurrency;
            _idSource = new SequentialItemIdSource(1);

            Arrangement = new BackpackArrangement(
                Catalog,
                heroCount: 1,
                heroProfiles: new[] { HeroDef.BackpackProfile },
                idSource: _idSource,
                stashWidth: 8,
                stashHeight: 2,
                rotationEnabled: true);

            PopulateInitialLoadout();
            CenterSingleHeroSlot();
            BuildRuntimePanels();

            PrepareStage(CurrentStage, NextMonsterCount);
        }

        public void SetNextStageMonsterCount(int count)
        {
            NextMonsterCount = Mathf.Clamp(count, 1, 6);
            NextStagePanel?.SetCountSilently(NextMonsterCount);
            PostCombatPanel?.SetNextMonsterCount(NextMonsterCount);
        }

        public void ApplyMonsterCountImmediately(int count)
        {
            SetNextStageMonsterCount(count);
            if (Phase != CombatScenePhase.Planning) return;

            PrepareStage(CurrentStage, NextMonsterCount);
        }

        public void StartFight()
        {
            if (Phase != CombatScenePhase.Planning || Catalog == null || Arrangement == null) return;

            Workbench?.ForceClearDragAndTray();
            ClearLooseTrayItems();

            Arrangement.Freeze();
            if (!TryPrepareCurrentArrangement(out PreparedEncounter prepared, out string error))
            {
                LastPrepareError = error;
                Debug.LogWarning("[CombatSceneController] Cannot start fight: " + error);
                Arrangement.Thaw();
                Workbench?.ShowFeedback(string.IsNullOrEmpty(error) ? "Không thể bắt đầu!" : error, isError: true);
                return;
            }

            Prepared = prepared;
            LastPrepareError = null;
            LastResult = CombatRunner.RunEncounter(Catalog, Prepared);

            SetPhase(CombatScenePhase.Playing);
            Workbench?.SetInteractive(false);
            PostCombatPanel?.Hide();

            int speedIndex = _integratedMode ? _playbackSpeedIndex : (Playback != null ? Playback.SpeedIndex : 0);
            CombatHudData startData = CombatHudMapper.MapPlanning(
                Catalog,
                HeroDef,
                Arrangement.CaptureView().HeroGrids[0],
                Workbench != null ? Workbench.CurrentSynergy : null,
                Prepared,
                CurrentStage,
                roomCount,
                Currency,
                speedIndex,
                RunBaseline(Arrangement.CaptureView().HeroGrids[0], null));
            hudView.Apply(startData);

            Playback.Play(Catalog, Prepared, LastResult, CurrentStage, roomCount, Currency);
        }

        public void ProceedToNextStage()
        {
            if (Phase != CombatScenePhase.PostCombat) return;
            if (RunActive)
            {
                RestartRun();   // the run is over: a new run
                return;
            }


            Playback?.StopPlayback();
            PostCombatPanel?.Hide();
            Arrangement.Thaw();

            CurrentStage += 1;
            Currency += 25 * (_currentSpawns != null ? _currentSpawns.Count : 1);

            PrepareStage(CurrentStage, NextMonsterCount);
            if (_integratedMode)
            {
                Playback?.SetSpeedIndex(_playbackSpeedIndex);
                StageReturnedToPlanning?.Invoke();
            }
        }

        public void RetryStage()
        {
            if (Phase != CombatScenePhase.PostCombat) return;
            if (RunActive)
            {
                RestartRun();   // no retry of a room in a run (System 5)
                return;
            }


            Playback?.StopPlayback();
            PostCombatPanel?.Hide();
            Arrangement.Thaw();

            int count = _currentSpawns != null && _currentSpawns.Count > 0 ? _currentSpawns.Count : NextMonsterCount;
            PrepareStage(CurrentStage, count);
            if (_integratedMode)
            {
                Playback?.SetSpeedIndex(_playbackSpeedIndex);
                StageReturnedToPlanning?.Invoke();
            }
        }

        public void RefreshPlanningHud()
        {
            if (Phase != CombatScenePhase.Planning || Catalog == null || Arrangement == null) return;

            ArrangementView view = Arrangement.CaptureView();
            ArrangementGridView heroGrid = view.HeroGrids != null && view.HeroGrids.Count > 0 ? view.HeroGrids[0] : null;
            GridSynergyView synergy = Workbench != null
                ? Workbench.CurrentSynergy
                : AdjacencySynergyEvaluator.Evaluate(heroGrid, Catalog);

            if (TryPrepareCurrentArrangement(out PreparedEncounter prepared, out string error))
            {
                Prepared = prepared;
                LastPrepareError = null;
            }
            else
            {
                LastPrepareError = error;
            }

            int speedIndex = _integratedMode ? _playbackSpeedIndex : (Playback != null ? Playback.SpeedIndex : 0);
            CombatHudData data = CombatHudMapper.MapPlanning(
                Catalog,
                HeroDef,
                heroGrid,
                synergy,
                Prepared,
                CurrentStage,
                roomCount,
                Currency,
                speedIndex,
                RunBaseline(heroGrid, synergy));
            hudView.Apply(data);
        }

        private void PrepareStage(int stage, int monsterCount)
        {
            int count = Mathf.Clamp(monsterCount, 1, 6);
            _currentSpawns = RunActive ? _run.BuildSpawns() : _presetCatalog.BuildSpawns(Catalog, count, stage);
            SetPhase(CombatScenePhase.Planning);

            Workbench?.SetInteractive(true);
            Workbench?.RefreshAll();
            NextStagePanel?.SetCountSilently(NextMonsterCount);
            NextStagePanel?.UpdateSummary(stage, _currentSpawns.Count);

            RefreshPlanningHud();
        }

        private bool TryPrepareCurrentArrangement(out PreparedEncounter prepared, out string error)
        {
            ArrangementView view = Arrangement.CaptureView();
            ArrangementGridView heroGrid = view.HeroGrids[0];
            GridSynergyView synergy = AdjacencySynergyEvaluator.Evaluate(heroGrid, Catalog);

            HeroBaselineInput heroBaseline = RunBaseline(heroGrid, synergy);   // door HP and curse terms (Systems 17, 19)

            var setup = new EncounterSetup(
                encounterId: CurrentStage,
                stageNumber: CurrentStage,
                heroes: new[] { heroBaseline },
                enemies: _currentSpawns);

            ulong encounterSeed = DeriveStageSeed(RunActive ? _run.Seed : stageSeed, CurrentStage, _currentSpawns.Count);
            var rng = new SeededRng(encounterSeed);
            return CombatRunner.TryPrepare(Catalog, setup, rng, out prepared, out error);
        }

        private bool _pickerDebugShown;

        private void Update()
        {
            UpdateRoomPlate();
            if (_pickerToggleBtn == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if ((Application.isEditor || Debug.isDebugBuild) && kb != null && kb.f9Key.wasPressedThisFrame)
            {
                _pickerDebugShown = !_pickerDebugShown;   // planning-screen.md P19: F9 shows the debug item picker
                if (!_pickerDebugShown) SetPickerOpen(false);
            }

            bool show = _pickerDebugShown && (Application.isEditor || Debug.isDebugBuild);
            if (_pickerToggleBtn.gameObject.activeSelf != show) _pickerToggleBtn.gameObject.SetActive(show);
        }

        private void PopulateInitialLoadout()
        {
            if (HeroDef.StartingLayout != null)
            {
                foreach (StartingPlacement at in HeroDef.StartingLayout)
                {
                    if (at.X >= 0 && Catalog.FindItem(at.ItemId) != null && Arrangement.CreateInTray(at.ItemId, out long itemId).IsOk)
                    {
                        if (Arrangement.Place(itemId, ContainerKind.Hero0, at.X, at.Y, at.Rotation).IsOk) continue;
                        Arrangement.DestroyTrayItem(itemId);
                    }

                    PlaceNewItem(at.ItemId, ContainerKind.Hero0);
                }
            }

            // balance-three-item-start.md: no stash spares; the run starts with the hero's 3-item kit only.
        }

        private void PlaceNewItem(string defId, ContainerKind container)
        {
            if (string.IsNullOrEmpty(defId) || Catalog.FindItem(defId) == null) return;
            if (!Arrangement.CreateInTray(defId, out long itemId).IsOk) return;
            if (!Arrangement.AutoPlace(itemId, container).IsOk)
            {
                Arrangement.DestroyTrayItem(itemId);
            }
        }

        private void ClearLooseTrayItems()
        {
            ArrangementView view = Arrangement.CaptureView();
            if (view.Tray == null || view.Tray.Count == 0) return;

            var ids = new List<long>(view.Tray.Count);
            for (int i = 0; i < view.Tray.Count; i++) ids.Add(view.Tray[i].InstanceId);
            for (int i = 0; i < ids.Count; i++) Arrangement.DestroyTrayItem(ids[i]);
        }

        private void CenterSingleHeroSlot()
        {
            Transform hero1 = hudView.transform.Find("HeroSlot1");
            if (hero1 is RectTransform rt)
            {
                // Center single active hero vertically inside the 137x314 WallPlane (y = 19..333)
                rt.anchoredPosition = new Vector2(247f, -129f);
            }
        }

        private void BuildRuntimePanels()
        {
            // Planning action strip (overlay sharing the ActionStrip band y=333..357)
            _planningActionStripRoot = CombatUiFactory.CreateRect("PlanningActionStrip", safeFrame, 0f, 0f, 640f, 360f);

            NextStagePanel = gameObject.AddComponent<NextStageTestPanel>();
            NextStagePanel.Initialize(_planningActionStripRoot, spriteCatalog, NextMonsterCount);
            NextStagePanel.MonsterCountChanged += SetNextStageMonsterCount;
            NextStagePanel.ApplyNowRequested += ApplyMonsterCountImmediately;

            _fightButton = CombatUiFactory.CreateSegmentButton(
                "FightButton",
                _planningActionStripRoot,
                spriteCatalog,
                "Chiến đấu",
                550f,
                335f,
                82f,
                20f,
                selected: true);
            TextMeshProUGUI fightLabel = _fightButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (fightLabel != null) fightLabel.color = CombatUiFactory.Candle;
            _fightButton.onClick.AddListener(StartFight);

            // Workbench
            Workbench = gameObject.AddComponent<WorkbenchController>();
            Workbench.Initialize(safeFrame, workbenchRoot, uiCamera, spriteCatalog, popupController, Catalog, HeroDef, Arrangement);
            Workbench.ArrangementChanged += HandleArrangementChanged;

            // Playback
            Playback = gameObject.AddComponent<CombatPlaybackController>();
            Playback.Initialize(hudView);
            Playback.SetPresentation(PlateOfSimItem, AvatarSprites);
            Playback.PlaybackCompleted += HandlePlaybackCompleted;

            // Post-combat breakdown panel
            PostCombatPanel = gameObject.AddComponent<PostCombatPanelView>();
            PostCombatPanel.Initialize(safeFrame, spriteCatalog);
            PostCombatPanel.NextMonsterCountSelected += SetNextStageMonsterCount;
            PostCombatPanel.NextStageClicked += ProceedToNextStage;
            PostCombatPanel.RetryClicked += RetryStage;

            // Keep ItemPopup topmost so hover tooltips stay above PostCombatPanel and Workbench.
            Transform popupViewTf = safeFrame.Find("ItemPopup");
            if (popupViewTf != null) popupViewTf.SetAsLastSibling();
        }

        private void HandleArrangementChanged()
        {
            if (Phase == CombatScenePhase.Planning)
            {
                RefreshPlanningHud();
            }
        }

        private void HandlePlaybackCompleted(CombatResult result)
        {
            if (RunActive)
            {
                HandleRunPlaybackCompleted(result);
                return;
            }

            LastResult = result;
            SetPhase(CombatScenePhase.PostCombat);
            PostCombatPanel?.Show(result, Catalog, CurrentStage, NextMonsterCount);
        }

        private void SetPhase(CombatScenePhase nextPhase)
        {
            Phase = nextPhase;
            bool inPlanning = nextPhase == CombatScenePhase.Planning;
            bool inCombat = nextPhase == CombatScenePhase.Playing;

            if (_planningActionStripRoot != null)
            {
                _planningActionStripRoot.gameObject.SetActive(inPlanning);
            }

            if (!_integratedMode && hudView != null && hudView.Speed != null)
            {
                hudView.Speed.gameObject.SetActive(inCombat);
            }
        }

        private static ulong DeriveStageSeed(ulong baseSeed, int stage, int monsterCount)
        {
            unchecked
            {
                ulong x = baseSeed ^ ((ulong)(uint)stage * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)monsterCount * 0xBF58476D1CE4E5B9UL);
                x ^= x >> 30;
                x *= 0xBF58476D1CE4E5B9UL;
                x ^= x >> 27;
                x *= 0x94D049BB133111EBUL;
                x ^= x >> 31;
                return x == 0 ? 1UL : x;
            }
        }
    }
}
