using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project05.CombatScene;
using Project05.Items;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Editor
{
    /// <summary>
    /// Builds the Combat scene from the combat art set (art bible 7.3 to 7.5, 8.1) and the HUD views. The scene is
    /// a generated output, rebuilt on every run. It shows fixture states, not live combat: the combat presentation
    /// (Systems 27 and 29) does not exist yet.
    /// </summary>
    public static class CombatSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat.unity";
        private const string SetPath = "Assets/_Project/ScriptableObjects/HudSpriteSet.asset";
        private const string SpriteCatalogPath = "Assets/_Project/ScriptableObjects/CombatSceneSpriteCatalog.asset";
        private const string Art = "Assets/_Project/Art/";
        private const string FontAssetPath = Art + "Fonts/grimpack-pixel/font_grimpack-pixel_regular_12_tmp.asset";
        private const string NumeralFolder = Art + "Fonts/grimpack-numeral/font_grimpack-numeral_";
        private const string EvidenceFolder = "production/qa/evidence";

        private static readonly Color Ink0 = new Color32(0x0B, 0x0F, 0x1C, 0xFF);
        private static readonly Color Bone = new Color32(0xE4, 0xDA, 0xC0, 0xFF);
        private static readonly Color Fog = new Color32(0x87, 0x95, 0xB3, 0xFF);

        // TEMPORARY: user-facing strings must come from the localization system (System 7), which does not exist
        // in code yet. Replace these with keyed lookups when it does.
        private const string LabelHero = "Kỵ Sĩ";
        private const string LabelStash = "Kho đồ";
        private const string LabelForecast = "Lượt tới (dự kiến):";
        private const string LabelEsc = "Esc";
        private const string LabelSkip = "Bỏ qua";
        private const string LabelSkipArmed = "Đang bỏ qua";
        private const string LabelStart = "Bắt đầu trận";
        private const string LabelStartKey = "Ctrl+Enter";
        private const string LabelStartHeld = "Đang cầm đồ: đặt xuống trước";
        private const string LabelSell = "Bán";
        private const string LabelSellIdle = "Kéo đồ vào đây để bán";
        private const string LabelSellReady = "Thả vào đây để bán";
        private const string LabelSellOver = "Thả để bán";
        private const string LabelSellLocked = "Chỉ bán khi xếp đồ";

        // The sell drawer on the right edge of the stage: the stage clip, the drawer panel inside it (open and closed x are
        // relative to the clip), and the sell zone inside the panel. Native px, x right, y down.
        private static readonly Rect StageClip = new Rect(177, 19, 460, 314);
        private const float DrawerWidth = 168f;
        private const float DrawerHeight = 160f;
        private const float DrawerTop = 4f;
        private const float TabWidth = 14f;
        private static readonly Rect SellRect = new Rect(12, 40, 144, 92);   // inside the drawer panel

        private static TMP_FontAsset _font;
        private static HudSpriteSet _set;

        /// <summary>Menu entry: rebuild the Combat scene.</summary>
        [MenuItem("Project05/Combat/Build Combat Scene")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigureBorders();
            ModalKit.ConfigureBorders();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (_font == null) throw new FileNotFoundException("Missing TMP font asset; build the Title scene first", FontAssetPath);
            _set = BuildSpriteSet();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ink0;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject("CombatCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.pixelPerfect = true;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<IntegerCanvasScale>();

            RectTransform safe = Rect("SafeFrame", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            safe.sizeDelta = new Vector2(IntegerCanvasScale.NativeWidth, IntegerCanvasScale.NativeHeight);
            safe.anchoredPosition = Vector2.zero;

            BuildFrame(safe);
            ItemPopupController popup = BuildPopupController(safe);
            BuildWorkbench(safe, popup);
            HudParts hudParts = BuildHud(safe);
            BuildPopupView(safe, popup);
            CombatSceneSpriteCatalog spriteCatalog = BuildCombatSceneSpriteCatalog();
            CombatSceneController liveCombat = BuildLiveCombatController(canvasGo, cam, safe, hudParts.View, popup, spriteCatalog);
            BuildScreenController(safe, hudParts, popup, liveCombat);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[CombatSceneBuilder] Built " + ScenePath);
        }

        // ------------------------------------------------------------------ assets
        private static void ConfigureBorders()
        {
            ApplyGeneratedBorders(Art + "UI/Gen");
            Border(Art + "UI/ui_panel_default_9s.png", new Vector4(4, 4, 4, 4));
            Border(Art + "UI/ui_panel_void_9s.png", new Vector4(4, 4, 4, 4));
            Border(Art + "UI/ui_chip_base_9s.png", new Vector4(3, 0, 3, 0));
            foreach (string s in new[] { "default", "selected" }) Border(Art + "UI/ui_segment_" + s + "_9s.png", new Vector4(6, 6, 6, 6));
            foreach (string s in new[] { "default", "active", "hover" }) Border(Art + "UI/ui_sell-frame_" + s + "_9s.png", new Vector4(8, 8, 8, 8));
            // run UI (tools/art/build_run_ui.py): door cards, rings, curse chip
            foreach (string t in new[] { "white", "green", "blue", "purple", "gold", "disabled" }) Border(Art + "UI/ui_door-card_" + t + "_9s.png", new Vector4(8, 8, 8, 8));
            Border(Art + "UI/ui_door-card_ring-hover_9s.png", new Vector4(2, 2, 2, 2));
            Border(Art + "UI/ui_door-card_ring-armed_9s.png", new Vector4(3, 3, 3, 3));
            Border(Art + "UI/ui_curse-chip_9s.png", new Vector4(3, 0, 3, 0));
            // debuff chips and the forge frame (tools/art/build_status_ui.py)
            foreach (string k in new[] { "poison", "burn", "frost", "slow", "weaken" }) Border(Art + "UI/ui_chip-status_" + k + "_9s.png", new Vector4(3, 0, 3, 0));
            foreach (string st in new[] { "default", "active", "hover", "invalid" }) Border(Art + "UI/ui_upgrade-frame_" + st + "_9s.png", new Vector4(8, 8, 8, 8));
        }

        private static void Border(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        [System.Serializable]
        private sealed class GenBorderEntry { public string name; public int l, t, r, b; }

        [System.Serializable]
        private sealed class GenBorderFile { public List<GenBorderEntry> entries = new List<GenBorderEntry>(); }

        /// <summary>Sets the 9-slice borders listed in a generated folder's borders.json (written by build_generated_ui.py).</summary>
        private static void ApplyGeneratedBorders(string dir)
        {
            string file = dir + "/borders.json";
            if (!File.Exists(file)) return;
            GenBorderFile data = JsonUtility.FromJson<GenBorderFile>(File.ReadAllText(file));
            if (data?.entries == null) return;
            foreach (GenBorderEntry e in data.entries)
            {
                string path = dir + "/" + e.name + ".png";
                if (File.Exists(path)) Border(path, new Vector4(e.l, e.b, e.r, e.t));   // Unity order: left, bottom, right, top
            }
        }

        /// <summary>Imports a generated PNG if Unity has not seen it yet (the pixel-art postprocessor sets the rest).</summary>
        private static void MakePixelSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>A sprite of the generated kit (Art/UI/Gen), imported with its 9-slice borders.</summary>
        private static Sprite Gen(string name)
        {
            string path = Art + "UI/Gen/" + name + ".png";
            MakePixelSprite(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new FileNotFoundException("Missing generated sprite (run tools/art/build_generated_ui.py)", path);
            return sprite;
        }

        private static Sprite S(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
            if (sprite == null) throw new FileNotFoundException("Missing sprite (not imported?)", Art + path);
            return sprite;
        }

        private static Sprite[] Many(string pattern, params string[] ids)
        {
            var list = new List<Sprite>();
            foreach (string id in ids) list.Add(S(string.Format(pattern, id)));
            return list.ToArray();
        }

        private static HudSpriteSet BuildSpriteSet()
        {
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects");
            var set = AssetDatabase.LoadAssetAtPath<HudSpriteSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<HudSpriteSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            // GlyphKind order; the debuff, detonate and anvil glyphs come from tools/art/build_status_ui.py
            string[] glyphIds = { "attack", "multi", "sweep", "windup", "heart", "shield", "speed", "damage", "droplet", "seal", "menu",
                "burn", "frost", "slow", "weaken", "detonate", "anvil" };
            string[] tiers = { "white", "green", "blue", "purple", "gold", "red" };
            set.heroPoses = new[]
            {
                S("Characters/char_tank_idle_01.png"), S("Characters/char_tank_idle_02.png"), S("Characters/char_tank_hurt_01.png"),
                S("Characters/char_tank_hurt_02.png"), S("Characters/char_tank_down_01.png"), S("Characters/char_tank_down_02.png"),
            };
            set.heroNarrow = S("Characters/char_tank_narrow_01.png");
            set.heroAttack = new[] { S("Characters/char_tank_attack_01.png"), S("Characters/char_tank_attack_02.png") };
            set.alchemistPoses = new[]
            {
                S("Characters/char_alchemist_idle_01.png"), S("Characters/char_alchemist_idle_02.png"), S("Characters/char_alchemist_hurt_01.png"),
                S("Characters/char_alchemist_hurt_02.png"), S("Characters/char_alchemist_down_01.png"), S("Characters/char_alchemist_down_02.png"),
            };
            set.alchemistNarrow = S("Characters/char_alchemist_narrow_01.png");
            set.alchemistAttack = new[] { S("Characters/char_alchemist_attack_01.png"), S("Characters/char_alchemist_attack_02.png") };
            set.pedestalDefault = S("Props/prop_pedestal_default_52x12.png");
            set.pedestalDim = S("Props/prop_pedestal_dim_52x12.png");
            set.shadowContact = S("Props/prop_shadow-contact_default_30x4.png");
            set.monsters = new[] { Monster("swarmer", 26), Monster("brute", 44) };
            set.glyphBone12 = Many("UI/ui_glyph-{0}_bone_12x12.png", glyphIds);
            set.glyphBone8 = Many("UI/ui_glyph-{0}_bone_8x8.png", glyphIds);
            set.glyphBlood8 = new Sprite[glyphIds.Length];
            set.glyphFog8 = new Sprite[glyphIds.Length];
            set.glyphBlood8[4] = S("UI/ui_glyph-heart_blood_8x8.png");
            set.glyphFog8[5] = S("UI/ui_glyph-shield_fog_8x8.png");
            set.intentFogHi12 = new[] { null, S("UI/ui_glyph-attack_fog-hi_12x12.png"), S("UI/ui_glyph-multi_fog-hi_12x12.png"), S("UI/ui_glyph-sweep_fog-hi_12x12.png"), S("UI/ui_glyph-windup_fog-hi_12x12.png") };
            set.intentFogHi8 = new[] { null, S("UI/ui_glyph-attack_fog-hi_8x8.png"), S("UI/ui_glyph-multi_fog-hi_8x8.png"), S("UI/ui_glyph-sweep_fog-hi_8x8.png"), S("UI/ui_glyph-windup_fog-hi_8x8.png") };
            set.plateLift = Many("Items/Plates/plate_{0}_lift_1x1.png", tiers);
            set.plateLiftHi = Many("Items/Plates/plate_{0}_lifthi_1x1.png", tiers);
            set.chevronTurnHero = S("UI/ui_chevron-turn_hero_7x4.png");
            set.chevronTurnFoe = S("UI/ui_chevron-turn_foe_7x4.png");
            set.segmentDefault = S("UI/ui_segment_default_9s.png");
            set.segmentSelected = S("UI/ui_segment_selected_9s.png");
            set.vfxSmear = Many("VFX/vfx_swing_smear_{0}.png", "01", "02", "03");
            set.vfxSmearFoe = Many("VFX/vfx_swing_smear-foe_{0}.png", "01", "02", "03");
            set.vfxSpark = Many("VFX/vfx_impact_spark_{0}.png", "01", "02");
            set.vfxSparkFoe = Many("VFX/vfx_impact_spark-foe_{0}.png", "01", "02");
            set.vfxBurst = Many("VFX/vfx_detonate_burst_{0}.png", "01", "02", "03", "04");
            set.vfxBurstFoe = Many("VFX/vfx_detonate_burst-foe_{0}.png", "01", "02", "03", "04");
            set.vfxBurstCalm = S("VFX/vfx_detonate_burst-calm_01.png");
            set.vfxBurstFoeCalm = S("VFX/vfx_detonate_burst-foe-calm_01.png");
            set.vfxTrail = Many("VFX/vfx_throw_trail_{0}.png", "01", "02");
            set.vfxTrailFoe = Many("VFX/vfx_throw_trail-foe_{0}.png", "01", "02");
            set.vfxMote = Many("VFX/vfx_status_mote_{0}.png", "01", "02");
            set.vfxMoteFoe = Many("VFX/vfx_status_mote-foe_{0}.png", "01", "02");
            set.dimMask = S("UI/ui_dim-mask_default_4x4.png");
            // debuff chips by StatusKind: Droplet, Seal, Poison, Burn, Frost, Slow, Weaken, Stun (frozen shows the frost chip)
            set.statusChips = new[]
            {
                S("UI/ui_chip-status_poison_9s.png"), null, S("UI/ui_chip-status_poison_9s.png"), S("UI/ui_chip-status_burn_9s.png"),
                S("UI/ui_chip-status_frost_9s.png"), S("UI/ui_chip-status_slow_9s.png"), S("UI/ui_chip-status_weaken_9s.png"),
                S("UI/ui_chip-status_frost_9s.png"),
            };
            set.vfxMotePoison = Many("VFX/vfx_status_mote-poison_{0}.png", "01", "02");
            set.vfxMoteBurn = Many("VFX/vfx_status_mote-burn_{0}.png", "01", "02");
            set.vfxMoteFrost = Many("VFX/vfx_status_mote-frost_{0}.png", "01", "02");
            set.vfxMoteSlow = Many("VFX/vfx_status_mote-slow_{0}.png", "01", "02");
            set.vfxMoteWeaken = Many("VFX/vfx_status_mote-weaken_{0}.png", "01", "02");
            EditorUtility.SetDirty(set);
            return set;
        }

        private static HudSpriteSet.MonsterVisual Monster(string id, int body)
        {
            return new HudSpriteSet.MonsterVisual
            {
                name = id,
                bodyHeight = body,
                poses = Many("Monsters/mon_" + id + "_{0}.png", "idle_01", "idle_02", "act_01", "hurt_01", "die_01", "die_02"),
                eyes = Many("Monsters/mon_" + id + "_eyes_{0}.png", "01", "02", "03"),
            };
        }

        // ------------------------------------------------------------------ layout helpers
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>A sprite placed by its top-left corner; (x, y) is measured from the parent top-left or top-centre.</summary>
        private static Image Img(string name, Transform parent, Sprite sprite, float x, float y, Vector2 anchor, float w = 0, float h = 0, bool sliced = false)
        {
            RectTransform rt = Rect(name, parent, anchor, TopLeft);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w > 0 ? w : sprite.rect.width, h > 0 ? h : sprite.rect.height);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static Image Fill(string name, Transform parent, string colour, float x, float y, float w, float h, Vector2 anchor)
        {
            return Img(name, parent, S("UI/ui_fill_" + colour + "_4x4.png"), x, y, anchor, w, h);
        }

        private static SpriteNumeralText Num(string name, Transform parent, int cut, string variant, float x, float y, string text, Vector2 anchor)
        {
            RectTransform rt = Rect(name, parent, anchor, TopLeft);
            rt.anchoredPosition = new Vector2(x, -y);
            var numeral = rt.gameObject.AddComponent<SpriteNumeralText>();
            numeral.Configure(AtlasSprite(cut, variant), Metrics(cut), SpriteNumeralText.Align.Left);
            numeral.Text = text;
            return numeral;
        }

        private static Sprite AtlasSprite(int cut, string variant)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NumeralFolder + variant + "_" + cut + ".png");
            if (sprite == null) throw new FileNotFoundException("Missing numeral atlas", NumeralFolder + variant + "_" + cut + ".png");
            return sprite;
        }

        private static TextAsset Metrics(int cut) => AssetDatabase.LoadAssetAtPath<TextAsset>(NumeralFolder + "num_" + cut + ".json");

        private static TextMeshProUGUI Label(string name, Transform parent, string text, float x, float y, float w, Color colour,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            RectTransform rt = Rect(name, parent, TopLeft, TopLeft);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, 12);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.fontSize = 12;
            tmp.color = colour;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = Vector4.zero;
            tmp.raycastTarget = false;
            tmp.text = text;
            return tmp;
        }

        // ------------------------------------------------------------------ frame and workbench
        private static void BuildFrame(RectTransform safe)
        {
            // planning-screen.md P1, P8, P20 to P26 with the generated kit (design/art/ui-kit-generated.md)
            Img("TopStrip", safe, Gen("ui_fill-gen_ink-1_4x4"), 0, 0, TopLeft, 640, 16);
            Img("TopStripRail", safe, Gen("ui_fill-gen_umber-2_4x4"), 0, 14, TopLeft, 640, 1);
            Img("TopStripRailShade", safe, Gen("ui_fill-gen_ink-0_4x4"), 0, 15, TopLeft, 640, 1);
            Img("StageBackdrop", safe, Gen("env_stage_bg_460x316"), 177, 19, TopLeft);
            Img("WorkbenchPanel", safe, Gen("ui_panel-wood_default_9s"), 0, 16, TopLeft, 174, 344, true);
            Img("SectionLine", safe, Gen("ui_fill-gen_ink-1_4x4"), 313, 19, TopLeft, 3, 314);
            Img("SectionLineLit", safe, Gen("ui_fill-gen_ink-3_4x4"), 313, 19, TopLeft, 1, 314);
            Img("ActionStrip", safe, Gen("ui_fill-gen_ink-1_4x4"), 177, 333, TopLeft, 460, 24);
            Img("ActionStripRail", safe, Gen("ui_fill-gen_umber-2_4x4"), 177, 333, TopLeft, 460, 1);
            Img("ActionStripRailShade", safe, Gen("ui_fill-gen_ink-0_4x4"), 177, 334, TopLeft, 460, 1);
            Img("StagePanel", safe, Gen("ui_panel-stone_default_9s"), 174, 16, TopLeft, 466, 344, true);
        }

        private static void BuildWorkbench(RectTransform safe, ItemPopupController popup)
        {
            RectTransform wb = Rect("Workbench", safe, TopLeft, TopLeft);
            wb.anchoredPosition = Vector2.zero;
            Img("NameChip", wb, S("UI/ui_chip_base_9s.png"), 7, 22, TopLeft, 14, 12, true);
            Num("NameJoin", wb, 8, "num", 12, 24, "1", TopLeft);
            Img("NameChevron", wb, S("UI/ui_chevron-turn_hero_7x4.png"), 24, 26, TopLeft);
            Label("NameLabel", wb, LabelHero, 34, 22, 80, Bone);
            // Beds are tiled cells, so the hero grid can grow from 4 x 4 to 7 x 7 by a size change; the 8 x 4 stash follows it.
            // Each sits in a leather tray (K2) 2 px around its cells (planning-screen.md P13, P17).
            Image gridTray = Img("GridTray", wb, Gen("ui_tray-leather_default_9s"), 0, 0, TopLeft, 84, 84, true);
            Image gridBed = Img("GridBed", wb, S("UI/ui_cell-bed_default_20x20.png"), 0, 0, TopLeft, 20, 20);
            gridBed.type = Image.Type.Tiled;
            Image line = Img("SectionLine", wb, Gen("ui_divider-wood_default_9s"), 0, 0, TopLeft, 164, 3, true);
            Image stashTab = Img("StashTab", wb, Gen("ui_plate-iron_default_9s"), 0, 0, TopLeft, 54, 14, true);
            TextMeshProUGUI stashLabel = Label("StashLabel", stashTab.transform, LabelStash, 0, 1, 54, Bone, TextAlignmentOptions.Top);
            Image stashTray = Img("StashTray", wb, Gen("ui_tray-leather_default_9s"), 0, 0, TopLeft, 164, 84, true);
            Image stashBed = Img("StashBed", wb, S("UI/ui_cell-bed_default_20x20.png"), 0, 0, TopLeft, 20, 20);
            stashBed.type = Image.Type.Tiled;
            TextMeshProUGUI stashHint = Label("StashHint", wb, "Đồ thưởng sẽ vào đây", 0, 0, 160, Fog, TextAlignmentOptions.Top);

            // P18: the hero's innate ability, filled in when a hero is picked
            Img("InnatePlate", wb, Gen("ui_plate-iron_default_9s"), 5, 288, TopLeft, 164, 66, true);
            Label("InnateTitle", wb, "Nội tại:", 12, 294, 150, Fog);
            TextMeshProUGUI innate = Label("InnateText", wb, string.Empty, 12, 308, 150, Bone);
            innate.textWrappingMode = TextWrappingModes.Normal;
            innate.rectTransform.sizeDelta = new Vector2(150, 42);

            var view = wb.gameObject.AddComponent<WorkbenchView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("gridBed").objectReferenceValue = gridBed.rectTransform;
            viewSo.FindProperty("sectionLine").objectReferenceValue = line.rectTransform;
            viewSo.FindProperty("stashLabel").objectReferenceValue = stashLabel.rectTransform;
            viewSo.FindProperty("stashBed").objectReferenceValue = stashBed.rectTransform;
            viewSo.FindProperty("gridTray").objectReferenceValue = gridTray.rectTransform;
            viewSo.FindProperty("stashTray").objectReferenceValue = stashTray.rectTransform;
            viewSo.FindProperty("stashHint").objectReferenceValue = stashHint.rectTransform;
            viewSo.FindProperty("stashLabel").objectReferenceValue = stashTab.rectTransform;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            view.Layout(WorkbenchLayout.StartGrid);

            PlaceAllItems(wb, popup);
            var board = Object.FindAnyObjectByType<ItemBoardController>();
            var so = new SerializedObject(board);
            so.FindProperty("workbench").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The sell zone: a scale, a framed drop area with a dotted floor, a hint, and the held item's price.</summary>
        private static SellZoneView BuildSellZone(RectTransform panel, out RectTransform area)
        {
            RectTransform root = Rect("SellZone", panel, TopLeft, TopLeft);
            root.anchoredPosition = Vector2.zero;
            Image scale = Img("Scale", root, S("UI/ui_sell-scale_default_48x28.png"), (DrawerWidth - 48) / 2, 8, TopLeft);
            Image frame = Img("Frame", root, S("UI/ui_sell-frame_default_9s.png"), SellRect.x, SellRect.y, TopLeft, SellRect.width, SellRect.height, true);
            Image floor = Img("Floor", root, S("UI/ui_sell-tile_default_20x20.png"), SellRect.x + 2, SellRect.y + 2, TopLeft, SellRect.width - 4, SellRect.height - 4);
            floor.type = Image.Type.Tiled;
            Label("Title", root, LabelSell, SellRect.x, SellRect.y + 8, SellRect.width, Bone, TextAlignmentOptions.Top);
            TextMeshProUGUI hint = Label("Hint", root, LabelSellIdle, SellRect.x, SellRect.y + 72, SellRect.width, Fog, TextAlignmentOptions.Top);

            RectTransform priceRow = Rect("Price", root, TopLeft, TopLeft);
            priceRow.anchoredPosition = Vector2.zero;
            Img("Coin", priceRow, S("UI/ui_glyph-coin_default_12x12.png"), 69, SellRect.yMax + 6, TopLeft);
            SpriteNumeralText price = Num("Amount", priceRow, 12, "num", 85, SellRect.yMax + 6, "0", TopLeft);
            priceRow.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<SellZoneView>();
            var so = new SerializedObject(view);
            so.FindProperty("scale").objectReferenceValue = scale;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("scaleIdle").objectReferenceValue = S("UI/ui_sell-scale_default_48x28.png");
            so.FindProperty("scaleActive").objectReferenceValue = S("UI/ui_sell-scale_active_48x28.png");
            so.FindProperty("frameIdle").objectReferenceValue = S("UI/ui_sell-frame_default_9s.png");
            so.FindProperty("frameReady").objectReferenceValue = S("UI/ui_sell-frame_active_9s.png");
            so.FindProperty("frameOver").objectReferenceValue = S("UI/ui_sell-frame_hover_9s.png");
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("priceRow").objectReferenceValue = priceRow.gameObject;
            so.FindProperty("price").objectReferenceValue = price;
            so.FindProperty("idleText").stringValue = LabelSellIdle;
            so.FindProperty("readyText").stringValue = LabelSellReady;
            so.FindProperty("overText").stringValue = LabelSellOver;
            so.FindProperty("lockedText").stringValue = LabelSellLocked;
            so.ApplyModifiedPropertiesWithoutUndo();
            area = frame.rectTransform;
            return view;
        }

        /// <summary>The forge zone ("Rèn"), the sell zone's twin: an anvil, a framed drop area, the cost and the before -> after line.</summary>
        private static ForgeZoneView BuildForgeZone(RectTransform panel, out RectTransform area)
        {
            RectTransform root = Rect("ForgeZone", panel, TopLeft, TopLeft);
            root.anchoredPosition = Vector2.zero;
            Image anvil = Img("Anvil", root, S("UI/ui_upgrade-anvil_default_48x28.png"), (DrawerWidth - 48) / 2, 8, TopLeft);
            Image frame = Img("Frame", root, S("UI/ui_upgrade-frame_default_9s.png"), SellRect.x, SellRect.y, TopLeft, SellRect.width, SellRect.height, true);
            Label("Title", root, "Rèn +", SellRect.x, SellRect.y + 8, SellRect.width, Bone, TextAlignmentOptions.Top);
            TextMeshProUGUI preview = Label("Preview", root, string.Empty, SellRect.x + 4, SellRect.y + 30, SellRect.width - 8, Bone, TextAlignmentOptions.Top);
            preview.textWrappingMode = TextWrappingModes.Normal;
            preview.rectTransform.sizeDelta = new Vector2(SellRect.width - 8, 40);
            TextMeshProUGUI hint = Label("Hint", root, "Kéo đồ vào đây", SellRect.x, SellRect.y + 72, SellRect.width, Fog, TextAlignmentOptions.Top);

            RectTransform costRow = Rect("Cost", root, TopLeft, TopLeft);
            costRow.anchoredPosition = Vector2.zero;
            Img("Coin", costRow, S("UI/ui_glyph-coin_default_12x12.png"), 69, SellRect.yMax + 6, TopLeft);
            SpriteNumeralText cost = Num("Amount", costRow, 12, "num", 85, SellRect.yMax + 6, "0", TopLeft);
            costRow.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<ForgeZoneView>();
            var so = new SerializedObject(view);
            so.FindProperty("anvil").objectReferenceValue = anvil;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("anvilIdle").objectReferenceValue = S("UI/ui_upgrade-anvil_default_48x28.png");
            so.FindProperty("anvilActive").objectReferenceValue = S("UI/ui_upgrade-anvil_active_48x28.png");
            so.FindProperty("anvilStrike").objectReferenceValue = S("UI/ui_upgrade-anvil_strike_48x28.png");
            so.FindProperty("frameIdle").objectReferenceValue = S("UI/ui_upgrade-frame_default_9s.png");
            so.FindProperty("frameReady").objectReferenceValue = S("UI/ui_upgrade-frame_active_9s.png");
            so.FindProperty("frameOver").objectReferenceValue = S("UI/ui_upgrade-frame_hover_9s.png");
            so.FindProperty("frameInvalid").objectReferenceValue = S("UI/ui_upgrade-frame_invalid_9s.png");
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("preview").objectReferenceValue = preview;
            so.FindProperty("costRow").objectReferenceValue = costRow.gameObject;
            so.FindProperty("cost").objectReferenceValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            area = frame.rectTransform;
            return view;
        }

        /// <summary>
        /// The sell drawer: a clip over the stage, a panel that springs out from the right edge, the pull tab with its arrow at
        /// the top of the edge, and the sell zone inside. Wires the board to it. Returns the clip's group, which pauses with the screen.
        /// </summary>
        private static CanvasGroup BuildSellDrawer(RectTransform safe)
        {
            RectTransform clip = ModalKit.Rect("StageClip", safe, StageClip.x, StageClip.y, StageClip.width, StageClip.height);
            clip.gameObject.AddComponent<RectMask2D>();
            CanvasGroup group = clip.gameObject.AddComponent<CanvasGroup>();

            float openX = StageClip.width - DrawerWidth;
            float closedX = StageClip.width;
            RectTransform panel = ModalKit.Rect("SellDrawer", clip, closedX, DrawerTop, DrawerWidth, DrawerHeight);
            Image body = panel.gameObject.AddComponent<Image>();
            body.sprite = S("UI/ui_panel_default_9s.png");
            body.type = Image.Type.Sliced;
            body.raycastTarget = true;
            SellZoneView zone = BuildSellZone(panel, out RectTransform area);
            ForgeZoneView forge = BuildForgeZone(panel, out RectTransform forgeArea);
            forge.gameObject.SetActive(false);

            RectTransform tabRt = ModalKit.Rect("Tab", panel, -TabWidth, 6, TabWidth, 28);
            Image tabImage = tabRt.gameObject.AddComponent<Image>();
            tabImage.sprite = S("UI/ui_drawer-tab_default_14x28.png");
            var tab = tabRt.gameObject.AddComponent<Button>();
            tab.targetGraphic = tabImage;
            tab.transition = Selectable.Transition.SpriteSwap;
            tab.spriteState = new SpriteState
            {
                highlightedSprite = S("UI/ui_drawer-tab_hover_14x28.png"),
                pressedSprite = S("UI/ui_drawer-tab_pressed_14x28.png"),
                selectedSprite = S("UI/ui_drawer-tab_focus_14x28.png"),
            };
            Image arrow = Img("Arrow", tabRt, S("UI/ui_glyph-arrow-left_bone_5x7.png"), 5, 4, TopLeft);
            Img("Scale", tabRt, S("UI/ui_glyph-scale_bone_8x8.png"), 3, 15, TopLeft);

            // the forge tab under the sell tab (item-upgrades.md): same chrome, an anvil glyph
            RectTransform forgeTabRt = ModalKit.Rect("ForgeTab", panel, -TabWidth, 38, TabWidth, 28);
            Image forgeTabImage = forgeTabRt.gameObject.AddComponent<Image>();
            forgeTabImage.sprite = S("UI/ui_drawer-tab_default_14x28.png");
            var forgeTab = forgeTabRt.gameObject.AddComponent<Button>();
            forgeTab.targetGraphic = forgeTabImage;
            forgeTab.transition = Selectable.Transition.SpriteSwap;
            forgeTab.spriteState = tab.spriteState;
            Img("Arrow", forgeTabRt, S("UI/ui_glyph-arrow-left_bone_5x7.png"), 5, 4, TopLeft);
            Img("Anvil", forgeTabRt, S("UI/ui_glyph-anvil_bone_8x8.png"), 3, 15, TopLeft);

            var drawer = panel.gameObject.AddComponent<SellDrawerView>();
            var so = new SerializedObject(drawer);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("tab").objectReferenceValue = tab;
            so.FindProperty("arrow").objectReferenceValue = arrow;
            so.FindProperty("arrowOpen").objectReferenceValue = S("UI/ui_glyph-arrow-left_bone_5x7.png");
            so.FindProperty("arrowClose").objectReferenceValue = S("UI/ui_glyph-arrow-right_bone_5x7.png");
            so.FindProperty("openX").floatValue = openX;
            so.FindProperty("closedX").floatValue = closedX;
            so.FindProperty("forgeTab").objectReferenceValue = forgeTab;
            so.FindProperty("sellContent").objectReferenceValue = zone.gameObject;
            so.FindProperty("forgeContent").objectReferenceValue = forge.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            // A held plate moves to this layer, so it draws above the stage and the drawer while it is carried.
            RectTransform dragLayer = Rect("DragLayer", safe, TopLeft, TopLeft);
            dragLayer.anchoredPosition = Vector2.zero;

            var board = Object.FindAnyObjectByType<ItemBoardController>();
            var boardSo = new SerializedObject(board);
            boardSo.FindProperty("sellZone").objectReferenceValue = zone;
            boardSo.FindProperty("sellArea").objectReferenceValue = area;
            boardSo.FindProperty("drawer").objectReferenceValue = drawer;
            boardSo.FindProperty("forgeZone").objectReferenceValue = forge;
            boardSo.FindProperty("forgeArea").objectReferenceValue = forgeArea;
            boardSo.FindProperty("plusBadge").objectReferenceValue = S("UI/ui_item-plus_bone_7x7.png");
            boardSo.FindProperty("dragLayer").objectReferenceValue = dragLayer;
            boardSo.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        private static readonly WorkbenchRects Start = WorkbenchLayout.For(WorkbenchLayout.StartGrid);

        // The arrangement shown in the workbench: hero 1 grid (4 x 4) and 8 x 8 stash with Crusader starter & synergy items.
        private static readonly (string id, int x, int y)[] GridLayout =
        {
            ("item_iron_sword", 0, 0), ("item_whetstone", 1, 0), ("item_spiked_buckler", 1, 1),
            ("item_venom_vial", 2, 0), ("item_drowsy_bell", 2, 1), ("item_guard_sigil", 3, 0),
            ("item_field_bandage", 0, 3),
        };

        // balance-three-item-start.md: the stash starts empty.
        private static readonly (string id, int x, int y)[] StashLayout = System.Array.Empty<(string, int, int)>();

        private static ItemCatalog LoadCatalog()
        {
            return ItemCatalog.Load(File.ReadAllText("Assets/_Project/Data/items_mvp.json"));
        }

        /// <summary>
        /// Places every item as its plate plus its art, at the true size of its footprint, each with a hover and drag handle,
        /// and wires the board controller that owns dragging, rotating and the ghost footprint.
        /// </summary>
        private static void PlaceAllItems(Transform parent, ItemPopupController popup)
        {
            ItemCatalog catalog = LoadCatalog();

            var boardGo = new GameObject("ItemBoard", typeof(RectTransform));
            boardGo.transform.SetParent(parent, false);
            var board = boardGo.AddComponent<ItemBoardController>();

            var targets = new List<ItemHoverTarget>();
            foreach ((string container, (string id, int x, int y)[] layout, float ox, float oy) in new[]
            {
                ("grid", GridLayout, Start.GridOrigin.x, Start.GridOrigin.y), ("stash", StashLayout, Start.StashOrigin.x, Start.StashOrigin.y),
            })
            {
                foreach ((string id, int x, int y) in layout)
                {
                    ItemRecord item = catalog.Find(id);
                    string plateRel = "Items/Plates/plate_" + item.tier.ToLowerInvariant() + "_rest_" + item.footprint + ".png";
                    MakeReadable(Art + plateRel);
                    Image plate = Img("Item_" + item.art, parent, S(plateRel), ox + 20 * x, oy + 20 * y, TopLeft);
                    plate.raycastTarget = true;
                    plate.alphaHitTestMinimumThreshold = 0.5f;
                    Img("Art", plate.transform, S("Items/Icons/item_" + item.art + "_d_" + item.footprint + ".png"), 2, 2, TopLeft);
                    plate.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                    var hover = plate.gameObject.AddComponent<ItemHoverTarget>();
                    hover.Configure(item.id, container, x, y, 0, board, popup);
                    targets.Add(hover);
                }
            }

            RectTransform ghostLayer = Rect("GhostLayer", parent, TopLeft, TopLeft);
            ghostLayer.anchoredPosition = Vector2.zero;

            board.SetItems(targets.ToArray());
            var so = new SerializedObject(board);
            so.FindProperty("safeFrame").objectReferenceValue = parent.parent;
            so.FindProperty("popup").objectReferenceValue = popup;
            so.FindProperty("ghostLayer").objectReferenceValue = ghostLayer;
            so.FindProperty("ghostValid").objectReferenceValue = S("UI/ui_ghost_valid_20x20.png");
            so.FindProperty("ghostInvalid").objectReferenceValue = S("UI/ui_ghost_invalid_20x20.png");
            so.FindProperty("ghostCross").objectReferenceValue = S("UI/ui_ghost_cross_20x20.png");
            so.FindProperty("separatorSprite").objectReferenceValue = S("UI/ui_fill_ink-1_4x4.png");
            so.FindProperty("lockedCellSprite").objectReferenceValue = S("UI/ui_dim-mask_default_4x4.png");
            SerializedProperty views = so.FindProperty("containers");
            views.arraySize = 2;
            SetContainer(views.GetArrayElementAtIndex(0), "grid", Start.GridOrigin.x, Start.GridOrigin.y, WorkbenchLayout.StartGrid, WorkbenchLayout.StartGrid, true);
            SetContainer(views.GetArrayElementAtIndex(1), "stash", Start.StashOrigin.x, Start.StashOrigin.y, WorkbenchLayout.Stash, WorkbenchLayout.StashRows, false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetContainer(SerializedProperty p, string id, float x, float y, int cols, int rows, bool grid)
        {
            p.FindPropertyRelative("id").stringValue = id;
            p.FindPropertyRelative("origin").vector2Value = new Vector2(x, y);
            p.FindPropertyRelative("cols").intValue = cols;
            p.FindPropertyRelative("rows").intValue = rows;
            p.FindPropertyRelative("grid").boolValue = grid;
        }

        private static void MakeReadable(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.isReadable) return;
            importer.isReadable = true;   // alpha hit testing needs the plate pixels
            importer.SaveAndReimport();
        }

        private static ItemPopupController BuildPopupController(RectTransform safe)
        {
            var go = new GameObject("ItemPopupController");
            go.transform.SetParent(safe, false);
            var controller = go.AddComponent<ItemPopupController>();
            var so = new SerializedObject(controller);
            so.FindProperty("catalogJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/items_mvp.json");
            so.FindProperty("safeFrame").objectReferenceValue = safe;
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void BuildPopupView(RectTransform safe, ItemPopupController controller)
        {
            RectTransform root = Rect("ItemPopup", safe, TopLeft, TopLeft);
            root.anchoredPosition = Vector2.zero;
            RectTransform panelRt = Rect("Panel", root, TopLeft, TopLeft);
            panelRt.sizeDelta = new Vector2(ItemPopupView.Width, 60);
            var panelImage = panelRt.gameObject.AddComponent<Image>();
            panelImage.sprite = S("UI/ui_panel_default_9s.png");
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;
            panelRt.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<ItemPopupView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("panel").objectReferenceValue = panelRt;
            viewSo.FindProperty("font").objectReferenceValue = _font;
            viewSo.FindProperty("sprites").objectReferenceValue = _set;
            viewSo.FindProperty("separator").objectReferenceValue = S("UI/ui_fill_ink-3_4x4.png");
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(controller);
            so.FindProperty("view").objectReferenceValue = view;
            so.FindProperty("bounds").objectReferenceValue = safe.Find("WorkbenchPanel");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ HUD
        /// <summary>The pieces of the HUD the screen controller is wired to.</summary>
        private sealed class HudParts
        {
            public CombatHudView View;
            public ActionStripView Strip;
            public Button MenuButton;
            public CanvasGroup HudGroup;
        }

        private static HudParts BuildHud(RectTransform safe)
        {
            var hudGo = new GameObject("CombatHud", typeof(RectTransform));
            hudGo.transform.SetParent(safe, false);
            var hudRt = (RectTransform)hudGo.transform;
            hudRt.anchorMin = TopLeft; hudRt.anchorMax = TopLeft; hudRt.pivot = TopLeft;
            hudRt.anchoredPosition = Vector2.zero;
            hudRt.sizeDelta = new Vector2(640, 360);

            // top strip: coin, currency, room counter, menu glyph, order strip and its forecast label
            Img("Coin", hudRt, S("UI/ui_glyph-coin_default_12x12.png"), 3, 2, TopLeft);
            SpriteNumeralText currency = Num("Currency", hudRt, 8, "num", 18, 4, "120", TopLeft);
            SpriteNumeralText room = Num("Room", hudRt, 8, "num-fog", 52, 4, "3/8", TopLeft);
            Button menuButton = BuildMenuButton(hudRt);
            Image esc = Img("EscChip", hudRt, S("UI/ui_chip_base_9s.png"), 596, 2, TopLeft, 22, 12, true);
            Label("EscLabel", esc.transform, LabelEsc, 0, 0, 22, Fog, TextAlignmentOptions.Top);
            TextMeshProUGUI forecast = Label("ForecastLabel", hudRt, LabelForecast, 181, 2, 150, Fog);
            forecast.ForceMeshUpdate();
            OrderStripView orderView = BuildOrderStrip(hudRt, 181 + Mathf.Ceil(forecast.preferredWidth) + 5);

            HeroSlotView[] heroes = new HeroSlotView[3];
            for (int k = 0; k < 3; k++) heroes[k] = BuildHeroSlot(hudRt, k);
            EnemySlotView[] enemies = new EnemySlotView[6];
            for (int i = 0; i < 6; i++) enemies[i] = BuildEnemySlot(hudRt, i);
            SpeedControlView speedView = BuildSpeedControl(hudRt);
            ActionStripView strip = BuildActionStrip(hudRt, speedView);
            RectTransform readouts = Rect("Readouts", hudRt, TopLeft, TopLeft);
            readouts.anchoredPosition = Vector2.zero;

            var view = hudGo.AddComponent<CombatHudView>();
            var so = new SerializedObject(view);
            so.FindProperty("sprites").objectReferenceValue = _set;
            Assign(so.FindProperty("heroes"), heroes);
            Assign(so.FindProperty("enemies"), enemies);
            so.FindProperty("order").objectReferenceValue = orderView;
            so.FindProperty("speed").objectReferenceValue = speedView;
            so.FindProperty("currency").objectReferenceValue = currency;
            so.FindProperty("room").objectReferenceValue = room;
            so.FindProperty("readoutLayer").objectReferenceValue = readouts;
            SerializedProperty fonts = so.FindProperty("readoutFonts");
            int[] cuts = { 12, 16, 24 };
            fonts.arraySize = cuts.Length;
            for (int i = 0; i < cuts.Length; i++)
            {
                SerializedProperty f = fonts.GetArrayElementAtIndex(i);
                f.FindPropertyRelative("cut").intValue = cuts[i];
                f.FindPropertyRelative("boneAtlas").objectReferenceValue = AtlasSprite(cuts[i], "num");
                f.FindPropertyRelative("boneMetrics").objectReferenceValue = Metrics(cuts[i]);
                f.FindPropertyRelative("bloodAtlas").objectReferenceValue = AtlasSprite(cuts[i], "num-blood");
                f.FindPropertyRelative("bloodMetrics").objectReferenceValue = Metrics(cuts[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var demo = hudGo.AddComponent<CombatHudDemo>();
            var demoSo = new SerializedObject(demo);
            demoSo.FindProperty("hud").objectReferenceValue = view;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

            return new HudParts { View = view, Strip = strip, MenuButton = menuButton, HudGroup = hudGo.AddComponent<CanvasGroup>() };
        }

        /// <summary>The menu button in the top strip: a 16 px chip with a cog on it; the Esc chip to its left names its key.</summary>
        private static Button BuildMenuButton(Transform parent)
        {
            RectTransform rt = ModalKit.Rect("MenuButton", parent, 620, 0, 16, 16);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = ModalKit.Sprite("ui_chipbtn_default_9s.png");
            image.type = Image.Type.Sliced;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = ModalKit.Sprite("ui_chipbtn_hover_9s.png"),
                pressedSprite = ModalKit.Sprite("ui_chipbtn_pressed_9s.png"),
                selectedSprite = ModalKit.Sprite("ui_chipbtn_focus_9s.png"),
                disabledSprite = ModalKit.Sprite("ui_chipbtn_disabled_9s.png"),
            };
            Img("Glyph", rt, S("UI/ui_glyph-cog_bone_8x8.png"), 4, 4, TopLeft);
            return button;
        }

        /// <summary>The Start button and the speed control share the action strip; only one shows at a time.</summary>
        private static ActionStripView BuildActionStrip(Transform parent, SpeedControlView speed)
        {
            // Start sits at the right end of the strip (art bible 7.3), its key chip to its left, the reason it is off at the left.
            RectTransform group = ModalKit.Rect("StartGroup", parent, 0, 0, 0, 0);
            Button start = ModalKit.Primary("StartButton", group, _font, LabelStart, 485, 335, 148, "wood-primary", Bone, out TMP_Text startLabel);
            Image keyChip = Img("KeyChip", group, Gen("ui_plate-iron_default_9s"), 415, 338, TopLeft, 66, 14, true);
            Label("KeyLabel", keyChip.transform, LabelStartKey, 0, 1, 66, Fog, TextAlignmentOptions.Top);
            TextMeshProUGUI reason = Label("Reason", group, string.Empty, 185, 339, 220, Fog);

            RectTransform root = ModalKit.Rect("ActionStrip", parent, 0, 0, 0, 0);
            var view = root.gameObject.AddComponent<ActionStripView>();
            var so = new SerializedObject(view);
            so.FindProperty("startGroup").objectReferenceValue = group.gameObject;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("speedGroup").objectReferenceValue = speed.gameObject;
            so.FindProperty("startLabel").objectReferenceValue = startLabel;
            so.FindProperty("reasonLabel").objectReferenceValue = reason;
            so.FindProperty("labelColour").colorValue = Bone;
            so.FindProperty("labelDisabledColour").colorValue = Fog;
            so.FindProperty("heldReason").stringValue = LabelStartHeld;
            so.ApplyModifiedPropertiesWithoutUndo();
            speed.gameObject.SetActive(false);   // the scene opens in Planning; the controller swaps groups
            return view;
        }

        /// <summary>The modals, the stage dim and the controller that wires every button.</summary>
        private static void BuildScreenController(RectTransform safe, HudParts hud, ItemPopupController popup, CombatSceneController liveCombat)
        {
            // Modals sit in the middle of the screen (user decision 2026-11-16), so the whole frame is dimmed behind them.
            const float CentreX = IntegerCanvasScale.NativeWidth / 2f;
            const float CentreY = IntegerCanvasScale.NativeHeight / 2f;
            CanvasGroup drawerGroup = BuildSellDrawer(safe);
            GameObject dim = ModalKit.DimMask(safe, 0, 0, IntegerCanvasScale.NativeWidth, IntegerCanvasScale.NativeHeight);
            PauseMenuView pause = ModalKit.Pause(safe, _font, CentreX, CentreY);
            ConfirmDialogView confirm = ModalKit.Confirm(safe, _font, CentreX, CentreY);
            SettingsPanelView settings = ModalKit.Settings(safe, _font, CentreX, CentreY);

            Transform workbench = safe.Find("Workbench");
            CanvasGroup workbenchGroup = workbench.gameObject.AddComponent<CanvasGroup>();

            var go = new GameObject("CombatScreen");
            var controller = go.AddComponent<CombatScreenController>();
            PixelCursor cursor = ModalKit.AddCursor(go);
            var so = new SerializedObject(controller);
            so.FindProperty("cursor").objectReferenceValue = cursor;
            so.FindProperty("hud").objectReferenceValue = hud.View;
            so.FindProperty("board").objectReferenceValue = Object.FindAnyObjectByType<ItemBoardController>();
            so.FindProperty("popup").objectReferenceValue = popup;
            so.FindProperty("strip").objectReferenceValue = hud.Strip;
            so.FindProperty("menuButton").objectReferenceValue = hud.MenuButton;
            so.FindProperty("pause").objectReferenceValue = pause;
            so.FindProperty("confirm").objectReferenceValue = confirm;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("stageDim").objectReferenceValue = dim;
            so.FindProperty("liveCombat").objectReferenceValue = liveCombat;
            Assign(so.FindProperty("screenGroups"), new[] { hud.HudGroup, workbenchGroup, drawerGroup });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CombatSceneSpriteCatalog BuildCombatSceneSpriteCatalog()
        {
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects");
            var catalog = AssetDatabase.LoadAssetAtPath<CombatSceneSpriteCatalog>(SpriteCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CombatSceneSpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, SpriteCatalogPath);
            }

            catalog.font = _font;
            catalog.num8BoneAtlas = AtlasSprite(8, "num");
            catalog.num8FogHiAtlas = AtlasSprite(8, "num-fog-hi");
            catalog.num8Metrics = Metrics(8);

            catalog.panelDefault = Gen("ui_plate-iron_default_9s");   // popups, detail boxes (K9)
            catalog.panelVoid = S("UI/ui_panel_void_9s.png");
            catalog.chipBase = S("UI/ui_chip_base_9s.png");
            catalog.segmentDefault = Gen("ui_board-wood_default_9s");
            catalog.segmentSelected = Gen("ui_board-wood_primary_9s");
            // generated kit: wooden boards for every runtime button (ui-kit-generated.md K4/K5)
            catalog.btnDefault = Gen("ui_board-wood_default_9s");
            catalog.btnHover = Gen("ui_board-wood_hover_9s");
            catalog.btnPressed = Gen("ui_board-wood_pressed_9s");
            catalog.btnFocus = Gen("ui_board-wood_hover_9s");
            catalog.btnDisabled = Gen("ui_board-wood_disabled_9s");

            catalog.fillInk0 = S("UI/ui_fill_ink-0_4x4.png");
            catalog.fillInk1 = S("UI/ui_fill_ink-1_4x4.png");
            catalog.fillInk2 = S("UI/ui_fill_ink-2_4x4.png");
            catalog.fillInk3 = S("UI/ui_fill_ink-3_4x4.png");
            catalog.fillInk4 = S("UI/ui_fill_ink-4_4x4.png");
            catalog.fillCandle = S("UI/ui_fill_candle_4x4.png");
            catalog.fillCandleD = S("UI/ui_fill_candle-d_4x4.png");
            catalog.fillFog = S("UI/ui_fill_fog_4x4.png");
            catalog.fillBone = S("UI/ui_fill_bone_4x4.png");
            catalog.fillBlood = S("UI/ui_fill_blood_4x4.png");

            catalog.glyphHeartBlood8 = S("UI/ui_glyph-heart_blood_8x8.png");
            catalog.glyphHeartBone8 = S("UI/ui_glyph-heart_bone_8x8.png");
            catalog.glyphShieldFog8 = S("UI/ui_glyph-shield_fog_8x8.png");
            catalog.glyphSpeedBone8 = S("UI/ui_glyph-speed_bone_8x8.png");
            catalog.glyphAttackBone8 = S("UI/ui_glyph-attack_bone_8x8.png");
            catalog.glyphSealBone8 = S("UI/ui_glyph-seal_bone_8x8.png");
            catalog.glyphDamageBone8 = S("UI/ui_glyph-damage_bone_8x8.png");
            catalog.glyphDropletBone8 = S("UI/ui_glyph-droplet_bone_8x8.png");

            catalog.itemIcons.Clear();
            string iconsDir = Art + "Items/Icons";
            if (Directory.Exists(iconsDir))
            {
                string[] files = Directory.GetFiles(iconsDir, "item_*.png");
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    if (!fileName.StartsWith("item_", System.StringComparison.Ordinal)) continue;
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;
                    catalog.itemIcons.Add(new CombatSceneSpriteCatalog.SpriteEntry
                    {
                        key = fileName.Substring("item_".Length),
                        sprite = sprite
                    });
                }
            }

            catalog.itemPlates.Clear();
            string platesDir = Art + "Items/Plates";
            if (Directory.Exists(platesDir))
            {
                string[] files = Directory.GetFiles(platesDir, "plate_*.png");
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    if (!fileName.StartsWith("plate_", System.StringComparison.Ordinal)) continue;
                    if (!fileName.Contains("_rest_") && !fileName.Contains("_hi_")) continue;

                    MakeReadable(assetPath);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;
                    catalog.itemPlates.Add(new CombatSceneSpriteCatalog.SpriteEntry
                    {
                        key = fileName.Substring("plate_".Length),
                        sprite = sprite
                    });
                }
            }

            // Run UI (doors, corridor, buffs, curses): every sprite of tools/art/build_run_ui.py, keyed by file name.
            catalog.runUi.Clear();
            string[] runPrefixes = { "ui_door-card", "ui_pip-rank", "ui_curse", "ui_fill_plum", "ui_glyph-curse", "ui_glyph-cell", "ui_glyph-size", "ui_glyph-elite", "ui_map-", "ui_dim-mask" };
            string uiDir = Art + "UI";
            if (Directory.Exists(uiDir))
            {
                string[] files = Directory.GetFiles(uiDir, "ui_*.png");
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    string assetPath = file.Replace(Path.DirectorySeparatorChar, '/');
                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    if (System.Array.FindIndex(runPrefixes, pre => fileName.StartsWith(pre, System.StringComparison.Ordinal)) < 0) continue;
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;
                    catalog.runUi.Add(new CombatSceneSpriteCatalog.SpriteEntry { key = fileName, sprite = sprite });
                }
            }

            // Generated UI kit (design/art/ui-kit-generated.md, tools/art/build_generated_ui.py): every sprite under
            // Art/UI/Gen and Art/VFX/Gen, keyed by file name, with the 9-slice borders of Gen/borders.json.
            foreach (string genDir in new[] { Art + "UI/Gen", Art + "VFX/Gen" })
            {
                if (!Directory.Exists(genDir)) continue;
                ApplyGeneratedBorders(genDir);
                string[] genFiles = Directory.GetFiles(genDir, "*.png");
                System.Array.Sort(genFiles, System.StringComparer.OrdinalIgnoreCase);
                foreach (string file in genFiles)
                {
                    string assetPath = file.Replace(Path.DirectorySeparatorChar, '/');
                    MakePixelSprite(assetPath);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;
                    catalog.runUi.Add(new CombatSceneSpriteCatalog.SpriteEntry { key = Path.GetFileNameWithoutExtension(assetPath), sprite = sprite });
                }
            }

            catalog.InvalidateCache();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static CombatSceneController BuildLiveCombatController(
            GameObject canvasGo,
            Camera cam,
            RectTransform safe,
            CombatHudView hudView,
            ItemPopupController popup,
            CombatSceneSpriteCatalog spriteCatalog)
        {
            var controller = canvasGo.AddComponent<CombatSceneController>();
            var so = new SerializedObject(controller);
            so.FindProperty("itemsJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/items_mvp.json");
            so.FindProperty("heroesJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/heroes_mvp.json");
            so.FindProperty("monstersJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/monsters_mvp.json");
            so.FindProperty("runJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/run_mvp.json");
            so.FindProperty("spriteCatalog").objectReferenceValue = spriteCatalog;
            so.FindProperty("hudView").objectReferenceValue = hudView;
            so.FindProperty("popupController").objectReferenceValue = popup;
            so.FindProperty("safeFrame").objectReferenceValue = safe;
            so.FindProperty("workbenchRoot").objectReferenceValue = safe.Find("Workbench");
            so.FindProperty("uiCamera").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void Assign<T>(SerializedProperty array, T[] items) where T : Object
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static OrderStripView BuildOrderStrip(Transform parent, float startX)
        {
            RectTransform strip = Rect("OrderStrip", parent, TopLeft, TopLeft);
            strip.anchoredPosition = Vector2.zero;
            var view = strip.gameObject.AddComponent<OrderStripView>();
            var so = new SerializedObject(view);
            SerializedProperty chips = so.FindProperty("chips");
            chips.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                Image chip = Img("Chip" + i, strip, S("UI/ui_chip_base_9s.png"), 182 + 15 * i, 2, TopLeft, 12, 12, true);
                SpriteNumeralText numeral = Num("Join", chip.transform, 8, "num", 4, 2, "1", TopLeft);
                Image glyph = Img("Intent", chip.transform, S("UI/ui_glyph-attack_fog-hi_8x8.png"), 1, 1, TopLeft);
                Image marker = Fill("Active", chip.transform, "candle", 3, 12, 6, 2, TopLeft);
                TextMeshProUGUI letter = Label("Letter", chip.transform, "A", 10, 0, 9, Bone);
                SerializedProperty c = chips.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("letter").objectReferenceValue = letter;
                c.FindPropertyRelative("root").objectReferenceValue = chip.gameObject;
                c.FindPropertyRelative("numeral").objectReferenceValue = numeral;
                c.FindPropertyRelative("glyph").objectReferenceValue = glyph;
                c.FindPropertyRelative("marker").objectReferenceValue = marker.gameObject;
            }

            so.FindProperty("startX").floatValue = startX;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>HP row and stat row, shared by hero and enemy slots. Coordinates are local to the slot centre line.</summary>
        private static UnitRowsView BuildRows(Transform parent, float y)
        {
            RectTransform rows = Rect("Rows", parent, TopCentre, TopLeft);
            rows.anchoredPosition = new Vector2(0, -y);
            var view = rows.gameObject.AddComponent<UnitRowsView>();

            // HP row (10 px): heart, frame, trail, fill, numerals
            RectTransform hpRoot = Rect("HpRow", rows, TopCentre, TopLeft);
            hpRoot.anchoredPosition = Vector2.zero;
            Img("Heart", hpRoot, S("UI/ui_glyph-heart_blood_8x8.png"), -39, 0, TopCentre);
            Image frame = Img("Frame", hpRoot, S("UI/ui_hp-bar_frame_36x8.png"), -28, 1, TopCentre);
            Image trail = Img("Trail", hpRoot, S("UI/ui_hp-bar_trail_34x6.png"), -27, 2, TopCentre);
            Image fill = Img("Fill", hpRoot, S("UI/ui_hp-bar_fill_34x6.png"), -27, 2, TopCentre);
            SpriteNumeralText value = Num("Value", hpRoot, 8, "num", 11, 1, "1/1", TopCentre);
            var hp = hpRoot.gameObject.AddComponent<HpBarView>();
            var hpSo = new SerializedObject(hp);
            hpSo.FindProperty("frame").objectReferenceValue = frame;
            hpSo.FindProperty("fill").objectReferenceValue = fill;
            hpSo.FindProperty("trail").objectReferenceValue = trail;
            hpSo.FindProperty("frameNormal").objectReferenceValue = S("UI/ui_hp-bar_frame_36x8.png");
            hpSo.FindProperty("frameLow").objectReferenceValue = S("UI/ui_hp-bar_frame-low_36x8.png");
            hpSo.FindProperty("value").objectReferenceValue = value;
            hpSo.ApplyModifiedPropertiesWithoutUndo();

            // stat row (12 px): armour, speed gauge, up to three status chips
            RectTransform stat = Rect("StatRow", rows, TopCentre, TopLeft);
            stat.anchoredPosition = new Vector2(0, -10);
            Img("Shield", stat, S("UI/ui_glyph-shield_fog_8x8.png"), -37, 1, TopCentre);
            SpriteNumeralText armour = Num("Armour", stat, 8, "num", -26, 2, "0", TopCentre);
            Img("GaugeFrame", stat, S("UI/ui_speed-gauge_frame_20x6.png"), -16, 3, TopCentre);
            Image gauge = Img("GaugeFill", stat, S("UI/ui_speed-gauge_fill_18x2.png"), -15, 6, TopCentre);
            var chips = new StatusChipView[3];
            for (int i = 0; i < 3; i++)
            {
                Image bg = Img("Chip" + i, stat, S("UI/ui_chip_base_9s.png"), 8 + 22 * i, 0, TopCentre, 20, 12, true);
                Image glyph = Img("Glyph", bg.transform, S("UI/ui_glyph-droplet_bone_8x8.png"), 1, 1, TopLeft);
                SpriteNumeralText amount = Num("Amount", bg.transform, 8, "num", 11, 2, "0", TopLeft);
                var chip = bg.gameObject.AddComponent<StatusChipView>();
                var chipSo = new SerializedObject(chip);
                chipSo.FindProperty("glyph").objectReferenceValue = glyph;
                chipSo.FindProperty("amount").objectReferenceValue = amount;
                chipSo.ApplyModifiedPropertiesWithoutUndo();
                chips[i] = chip;
            }

            var so = new SerializedObject(view);
            so.FindProperty("hp").objectReferenceValue = hp;
            so.FindProperty("statRow").objectReferenceValue = stat.gameObject;
            so.FindProperty("armour").objectReferenceValue = armour;
            so.FindProperty("gaugeFill").objectReferenceValue = gauge.rectTransform;
            Assign(so.FindProperty("chips"), chips);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static HeroSlotView BuildHeroSlot(Transform parent, int k)
        {
            RectTransform slot = Rect("HeroSlot" + (k + 1), parent, TopLeft, TopCentre);
            slot.anchoredPosition = new Vector2(247, -(35 + 94 * k));
            slot.sizeDelta = new Vector2(140, 94);

            Image pedestal = Img("Pedestal", slot, _set.pedestalDefault, -26, 62, TopCentre);
            Image body = Img("Body", slot, _set.heroPoses[0], -24, 20, TopCentre);
            Image narrow = Img("NarrowEyes", body.transform, _set.heroNarrow, 0, 0, TopLeft);
            Image chevron = Img("TurnChevron", slot, _set.chevronTurnHero, -34, 65, TopCentre);
            SpriteNumeralText join = Num("Join", slot, 8, "num", -2, 64, (k + 1).ToString(), TopCentre);
            UnitRowsView rows = BuildRows(slot, 72);
            var avatars = new AvatarView[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform a = Rect("Avatar" + i, slot, TopCentre, TopLeft);
                a.anchoredPosition = new Vector2(i == 0 ? -11 : 11, -1);
                a.sizeDelta = new Vector2(22, 22);
                Image plate = Img("Plate", a, _set.plateLift[0], 0, 0, TopLeft);
                Image icon = Img("Icon", a, _set.glyphBone12[0], 4, 4, TopLeft);
                var view = a.gameObject.AddComponent<AvatarView>();
                var aSo = new SerializedObject(view);
                aSo.FindProperty("plate").objectReferenceValue = plate;
                aSo.FindProperty("icon").objectReferenceValue = icon;
                aSo.ApplyModifiedPropertiesWithoutUndo();
                avatars[i] = view;
            }

            var hero = slot.gameObject.AddComponent<HeroSlotView>();
            var so = new SerializedObject(hero);
            so.FindProperty("pedestal").objectReferenceValue = pedestal;
            so.FindProperty("bodyRect").objectReferenceValue = body.rectTransform;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("narrow").objectReferenceValue = narrow;
            so.FindProperty("chevron").objectReferenceValue = chevron;
            so.FindProperty("join").objectReferenceValue = join;
            so.FindProperty("rows").objectReferenceValue = rows;
            Assign(so.FindProperty("avatars"), avatars);
            so.ApplyModifiedPropertiesWithoutUndo();
            return hero;
        }

        private static EnemySlotView BuildEnemySlot(Transform parent, int i)
        {
            RectTransform slot = Rect("EnemySlot" + (i + 1), parent, TopLeft, TopCentre);
            slot.anchoredPosition = new Vector2(384, -50);
            slot.sizeDelta = new Vector2(140, 82);

            Image shadow = Img("Shadow", slot, _set.shadowContact, -15, 0, TopCentre);
            Image body = Img("Body", slot, _set.monsters[0].poses[0], 0, 0, TopCentre);
            Image eyes = Img("LitEyes", slot, _set.monsters[0].eyes[0], 0, 0, TopCentre);
            Image chevron = Img("TurnChevron", slot, _set.chevronTurnFoe, -3, 0, TopCentre);
            RectTransform tab = Img("Tab", slot, S("UI/ui_tab-intent_default_46x24.png"), -23, 0, TopCentre).rectTransform;
            Image glyph = Img("IntentGlyph", tab, _set.intentFogHi12[1], 3, 3, TopLeft);
            SpriteNumeralText magnitude = Num("Magnitude", tab, 8, "num-fog-hi", 17, 5, "0", TopLeft);
            Img("TargetChip", tab, S("UI/ui_chip_base_9s.png"), 34, 3, TopLeft, 8, 11, true);
            SpriteNumeralText target = Num("Target", tab, 8, "num-fog-hi", 35, 5, "1", TopLeft);
            Image letterChip = Img("LetterChip", slot, S("UI/ui_chip_base_9s.png"), -38, 6, TopCentre, 12, 12, true);
            Label("Letter", letterChip.transform, OrderStripView.Letter(i), 0, 0, 12, Bone, TextAlignmentOptions.Top);
            UnitRowsView rows = BuildRows(slot, 60);

            var enemy = slot.gameObject.AddComponent<EnemySlotView>();
            var so = new SerializedObject(enemy);
            so.FindProperty("tab").objectReferenceValue = tab;
            so.FindProperty("tabGlyph").objectReferenceValue = glyph;
            so.FindProperty("magnitude").objectReferenceValue = magnitude;
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("bodyRect").objectReferenceValue = body.rectTransform;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("eyes").objectReferenceValue = eyes;
            so.FindProperty("shadowRect").objectReferenceValue = shadow.rectTransform;
            so.FindProperty("chevronRect").objectReferenceValue = chevron.rectTransform;
            so.FindProperty("chevron").objectReferenceValue = chevron;
            so.FindProperty("rowsRect").objectReferenceValue = rows.GetComponent<RectTransform>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private static SpeedControlView BuildSpeedControl(Transform parent)
        {
            RectTransform strip = Rect("SpeedControl", parent, TopLeft, TopLeft);
            strip.anchoredPosition = Vector2.zero;
            var view = strip.gameObject.AddComponent<SpeedControlView>();
            string[] labels = { "1x", "2x", "4x", LabelSkip };
            int[] widths = { 28, 28, 28, 76 };
            TextMeshProUGUI skipLabel = null;
            var buttons = new Button[4];
            var images = new Image[4];
            float x = 189;
            for (int i = 0; i < 4; i++)
            {
                Image seg = Img("Segment" + i, strip, _set.segmentDefault, x - 4, 334, TopLeft, widths[i] + 8, 22, true);
                seg.raycastTarget = true;
                Button button = seg.gameObject.AddComponent<Button>();
                button.targetGraphic = seg;
                button.transition = Selectable.Transition.None;
                TextMeshProUGUI segLabel = Label("Label", seg.transform, labels[i], 0, 5, widths[i] + 8, Bone, TextAlignmentOptions.Top);
                if (i == 3) skipLabel = segLabel;
                buttons[i] = button;
                images[i] = seg;
                x += widths[i] + 8;
            }

            var so = new SerializedObject(view);
            Assign(so.FindProperty("buttons"), buttons);
            Assign(so.FindProperty("segments"), images);
            so.FindProperty("skipLabel").objectReferenceValue = skipLabel;
            so.FindProperty("skipIdleText").stringValue = LabelSkip;
            so.FindProperty("skipArmedText").stringValue = LabelSkipArmed;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ evidence
        /// <summary>
        /// Menu entry: capture each fixture of the saved Combat scene at zoom 1, and the acting-party fixture at zoom 2,
        /// then reload the scene from disk so the temporary changes never persist. Refuses to run on unsaved changes.
        /// </summary>
        [MenuItem("Project05/Combat/Capture Evidence")]
        public static void CaptureEvidence()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[CombatSceneBuilder] The open scene has unsaved changes; save or discard them, then capture again.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                Capture(HudFixture.PartyActing, 1);
                Capture(HudFixture.PartyActing, 2);
                Capture(HudFixture.EnemyActing, 1);
                Capture(HudFixture.Planning, 1);
                Capture(HudFixture.SixEnemies, 1);
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Menu entry: capture the saved Combat scene at zoom 2 with the popup open on a placed item, a synergy-rich item
        /// and a stash item, then reload the scene from disk. Refuses to run on unsaved changes.
        /// </summary>
        [MenuItem("Project05/Combat/Capture Item Popups")]
        public static void CaptureItemPopups()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[CombatSceneBuilder] The open scene has unsaved changes; save or discard them, then capture again.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                foreach (string id in new[] { "item_iron_sword", "item_dagger", "item_hourglass", "item_drowsy_bell", "item_echo_charm" })
                {
                    ItemHoverTarget target = null;
                    foreach (ItemHoverTarget t in Object.FindObjectsByType<ItemHoverTarget>()) if (t.ItemId == id) target = t;
                    if (target == null) continue;
                    Object.FindAnyObjectByType<ItemPopupController>().ShowNow(target);
                    Capture(HudFixture.Planning, 2, "popup_" + id);
                    Object.FindAnyObjectByType<ItemPopupController>().Close();
                }
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Menu entry: capture the saved Combat scene at zoom 2 while the sword is held, rotated, over a free place
        /// and over a blocked one, then reload the scene from disk. Refuses to run on unsaved changes.
        /// </summary>
        [MenuItem("Project05/Combat/Capture Drag Preview")]
        public static void CaptureDragPreview()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[CombatSceneBuilder] The open scene has unsaved changes; save or discard them, then capture again.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var board = Object.FindAnyObjectByType<ItemBoardController>();
                ItemHoverTarget sword = null;
                foreach (ItemHoverTarget t in Object.FindObjectsByType<ItemHoverTarget>()) if (t.ItemId == "item_war_drum") sword = t;
                if (sword == null) return;

                board.Pickup(sword, false);
                board.SimulatePointer(new Vector2(157f, 172f));         // over free stash cells
                Capture(HudFixture.Planning, 2, "drag_valid");
                board.SimulatePointer(new Vector2(37f, 70f));           // over placed items: blocked
                Capture(HudFixture.Planning, 2, "drag_blocked");
                board.RotateHeld();
                board.SimulatePointer(new Vector2(147f, 182f));         // turned a quarter, over free stash cells
                Capture(HudFixture.Planning, 2, "drag_rotated");
                Object.FindAnyObjectByType<SellDrawerView>().SnapOpenForCapture();
                board.SimulatePointer(StageClip.position + new Vector2(StageClip.width - DrawerWidth, DrawerTop) + SellRect.center);
                Capture(HudFixture.Planning, 2, "drag_sell");
                board.Cancel();
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// Menu entry: capture the saved Combat scene at zoom 2 in Planning (Start shown), in Playback (speed control with
        /// Skip armed), and with the pause menu, the abandon dialog and the settings panel open, then reload the scene.
        /// </summary>
        [MenuItem("Project05/Combat/Capture Button States")]
        public static void CaptureButtonStates()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[CombatSceneBuilder] The open scene has unsaved changes; save or discard them, then capture again.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var pause = Object.FindAnyObjectByType<PauseMenuView>(FindObjectsInactive.Include);
                var confirm = Object.FindAnyObjectByType<ConfirmDialogView>(FindObjectsInactive.Include);
                var settings = Object.FindAnyObjectByType<SettingsPanelView>(FindObjectsInactive.Include);
                var dim = GameObject.Find("CombatCanvas").transform.Find("SafeFrame/StageDim")?.gameObject;
                var speed = Object.FindAnyObjectByType<SpeedControlView>(FindObjectsInactive.Include);

                Capture(HudFixture.Planning, 2, "buttons_planning");

                speed.SetSkipArmed(true);
                Capture(HudFixture.PartyActing, 2, "buttons_skip-armed");
                speed.SetSkipArmed(false);

                dim.SetActive(true);
                pause.gameObject.SetActive(true);
                Capture(HudFixture.PartyActing, 2, "buttons_pause-menu");
                pause.gameObject.SetActive(false);

                confirm.gameObject.SetActive(true);
                Capture(HudFixture.PartyActing, 2, "buttons_confirm");
                confirm.gameObject.SetActive(false);

                settings.Refresh();
                settings.gameObject.SetActive(true);
                Capture(HudFixture.PartyActing, 2, "buttons_settings");
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static void Capture(HudFixture fixture, int zoom, string name = null)
        {
            Camera cam = Object.FindAnyObjectByType<Camera>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            canvas.GetComponent<IntegerCanvasScale>().enabled = false;
            canvas.GetComponent<CanvasScaler>().scaleFactor = zoom;

            var demo = Object.FindAnyObjectByType<CombatHudDemo>();
            var so = new SerializedObject(demo);
            so.FindProperty("fixture").enumValueIndex = (int)fixture;
            so.ApplyModifiedPropertiesWithoutUndo();
            demo.Show();
            var strip = Object.FindAnyObjectByType<ActionStripView>();
            if (strip != null) strip.SetPhase(fixture == HudFixture.Planning ? ScreenPhase.Planning : ScreenPhase.Playback);

            int w = IntegerCanvasScale.NativeWidth * zoom;
            int h = IntegerCanvasScale.NativeHeight * zoom;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            rt.Create();
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;

            Directory.CreateDirectory(EvidenceFolder);
            string label = name ?? fixture.ToString().ToLowerInvariant();
            string file = Path.Combine(EvidenceFolder, "combat_scene_" + label + "_z" + zoom + "_2026-11-15.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Debug.Log("[CombatSceneBuilder] Captured " + file);
        }
    }
}
