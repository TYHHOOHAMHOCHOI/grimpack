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
    /// Builds the Combat scene from the combat art set (art bible 7.3 to 7.5, 8.1), the HUD views,
    /// and the runtime <see cref="CombatSceneController"/>. Rebuilt on every run; preserves EditMode
    /// fixture capture via <see cref="CombatHudDemo"/>.
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
        private const string LabelHero = "Kỵ Sĩ Hắc Quang";
        private const string LabelStash = "Kho đồ";
        private const string LabelForecast = "nếu không có gì đổi";
        private const string LabelSkip = "Bỏ qua";
        private const string LabelKey = "Tab";

        private static TMP_FontAsset _font;
        private static HudSpriteSet _set;

        /// <summary>Menu entry: rebuild the Combat scene.</summary>
        [MenuItem("Project05/Combat/Build Combat Scene")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigureBorders();
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
            RectTransform workbenchRoot = BuildWorkbench(safe, popup);
            CombatHudView hudView = BuildHud(safe);
            BuildPopupView(safe, popup);

            CombatSceneSpriteCatalog spriteCatalog = BuildCombatSceneSpriteCatalog();
            WireSceneController(canvasGo, cam, safe, workbenchRoot, hudView, popup, spriteCatalog);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[CombatSceneBuilder] Built " + ScenePath);
        }

        // ------------------------------------------------------------------ assets
        private static void ConfigureBorders()
        {
            Border(Art + "UI/ui_panel_default_9s.png", new Vector4(4, 4, 4, 4));
            Border(Art + "UI/ui_panel_void_9s.png", new Vector4(4, 4, 4, 4));
            Border(Art + "UI/ui_chip_base_9s.png", new Vector4(3, 0, 3, 0));
            foreach (string s in new[] { "default", "selected" }) Border(Art + "UI/ui_segment_" + s + "_9s.png", new Vector4(6, 6, 6, 6));
        }

        private static void Border(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
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

            string[] glyphIds = { "attack", "multi", "sweep", "windup", "heart", "shield", "speed", "damage", "droplet", "seal", "menu" };
            string[] tiers = { "white", "green", "blue", "purple", "gold", "red" };
            set.heroPoses = new[]
            {
                S("Characters/char_tank_idle_01.png"), S("Characters/char_tank_idle_02.png"), S("Characters/char_tank_hurt_01.png"),
                S("Characters/char_tank_hurt_02.png"), S("Characters/char_tank_down_01.png"), S("Characters/char_tank_down_02.png"),
            };
            set.heroNarrow = S("Characters/char_tank_narrow_01.png");
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
            EditorUtility.SetDirty(set);
            return set;
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

            catalog.panelDefault = S("UI/ui_panel_default_9s.png");
            catalog.panelVoid = S("UI/ui_panel_void_9s.png");
            catalog.chipBase = S("UI/ui_chip_base_9s.png");
            catalog.segmentDefault = S("UI/ui_segment_default_9s.png");
            catalog.segmentSelected = S("UI/ui_segment_selected_9s.png");
            catalog.btnDefault = S("UI/ui_btn_default_120x28.png");
            catalog.btnHover = S("UI/ui_btn_hover_120x28.png");
            catalog.btnPressed = S("UI/ui_btn_pressed_120x28.png");
            catalog.btnFocus = S("UI/ui_btn_focus_120x28.png");
            catalog.btnDisabled = S("UI/ui_btn_disabled_120x28.png");

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

            catalog.InvalidateCache();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireSceneController(
            GameObject canvasGo,
            Camera cam,
            RectTransform safe,
            RectTransform workbenchRoot,
            CombatHudView hudView,
            ItemPopupController popup,
            CombatSceneSpriteCatalog spriteCatalog)
        {
            var controller = canvasGo.AddComponent<CombatSceneController>();
            var so = new SerializedObject(controller);
            so.FindProperty("itemsJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/items_mvp.json");
            so.FindProperty("heroesJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/heroes_mvp.json");
            so.FindProperty("monstersJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/monsters_mvp.json");
            so.FindProperty("spriteCatalog").objectReferenceValue = spriteCatalog;
            so.FindProperty("hudView").objectReferenceValue = hudView;
            so.FindProperty("popupController").objectReferenceValue = popup;
            so.FindProperty("safeFrame").objectReferenceValue = safe;
            so.FindProperty("workbenchRoot").objectReferenceValue = workbenchRoot;
            so.FindProperty("uiCamera").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
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
            Fill("TopStrip", safe, "ink-1", 0, 0, 640, 16, TopLeft);
            Img("WorkbenchPanel", safe, S("UI/ui_panel_default_9s.png"), 0, 16, TopLeft, 174, 344, true);
            Img("StagePanel", safe, S("UI/ui_panel_void_9s.png"), 174, 16, TopLeft, 466, 344, true);
            Fill("WallPlane", safe, "ink-1", 177, 19, 137, 314, TopLeft);
            for (int y = 33; y < 333; y += 20) Fill("Course", safe, "ink-2", 177, y, 137, 1, TopLeft);
            Fill("SectionLine", safe, "ink-3", 314, 19, 1, 314, TopLeft);
            Fill("ActionStrip", safe, "ink-1", 177, 333, 460, 24, TopLeft);
            Fill("ActionStripEdge", safe, "ink-3", 177, 333, 460, 1, TopLeft);
        }

        private static RectTransform BuildWorkbench(RectTransform safe, ItemPopupController popup)
        {
            RectTransform wb = Rect("Workbench", safe, TopLeft, TopLeft);
            wb.anchoredPosition = Vector2.zero;
            Img("NameChip", wb, S("UI/ui_chip_base_9s.png"), 7, 22, TopLeft, 14, 12, true);
            Num("NameJoin", wb, 8, "num", 12, 24, "1", TopLeft);
            Img("NameChevron", wb, S("UI/ui_chevron-turn_hero_7x4.png"), 24, 26, TopLeft);
            Label("NameLabel", wb, LabelHero, 34, 22, 120, Bone);
            Fill("GridBed", wb, "ink-2", 7, 40, 80, 80, TopLeft);
            for (int i = 1; i < 4; i++)
            {
                Fill("GridV", wb, "ink-1", 7 + 20 * i, 40, 1, 80, TopLeft);
                Fill("GridH", wb, "ink-1", 7, 40 + 20 * i, 80, 1, TopLeft);
            }

            PlaceItems(wb, popup, true);
            Fill("SectionLine", wb, "ink-3", 7, 128, 160, 1, TopLeft);
            Label("StashLabel", wb, LabelStash, 7, 134, 100, Bone);
            Fill("StashBed", wb, "ink-2", 7, 152, 160, 40, TopLeft);
            for (int i = 1; i < 8; i++) Fill("StashV", wb, "ink-1", 7 + 20 * i, 152, 1, 40, TopLeft);
            Fill("StashH", wb, "ink-1", 7, 172, 160, 1, TopLeft);
            PlaceItems(wb, popup, false);
            return wb;
        }

        // The arrangement shown in the workbench: the items of hero 2 grid (4 x 4, origin at 7, 40) and the stash
        // (8 x 2, origin at 7, 152). Chosen so that every synergy rule has a visible neighbour.
        private static readonly (string id, int x, int y)[] GridLayout =
        {
            ("item_iron_sword", 0, 0), ("item_whetstone", 1, 0), ("item_dagger", 1, 1), ("item_war_drum", 2, 0),
            ("item_seal_stamp", 3, 0), ("item_hourglass", 3, 2), ("item_echo_charm", 2, 2),
        };

        private static readonly (string id, int x, int y)[] StashLayout =
        {
            ("item_breaking_bell", 0, 0), ("item_iron_helm", 1, 0), ("item_seal_ink", 1, 1), ("item_scholar_lens", 2, 0),
            ("item_heart_charm", 2, 1), ("item_swift_boots", 3, 0), ("item_crossbow", 5, 0),
        };

        private static ItemCatalog LoadCatalog()
        {
            return ItemCatalog.Load(File.ReadAllText("Assets/_Project/Data/items_mvp.json"));
        }

        /// <summary>Places each item as its plate plus its art, at the true size of its footprint, with a hover target.</summary>
        private static void PlaceItems(Transform parent, ItemPopupController popup, bool grid)
        {
            ItemCatalog catalog = LoadCatalog();
            (string id, int x, int y)[] layout = grid ? GridLayout : StashLayout;
            float originX = 7, originY = grid ? 40 : 152;

            ItemGridContext context = null;
            if (grid)
            {
                var ctxGo = new GameObject("HeroGrid", typeof(RectTransform));
                ctxGo.transform.SetParent(parent, false);
                context = ctxGo.AddComponent<ItemGridContext>();
            }

            var targets = new List<ItemHoverTarget>();
            foreach ((string id, int x, int y) in layout)
            {
                ItemRecord item = catalog.Find(id);
                string plateRel = "Items/Plates/plate_" + item.tier.ToLowerInvariant() + "_rest_" + item.footprint + ".png";
                MakeReadable(Art + plateRel);
                Image plate = Img("Item_" + item.art, parent, S(plateRel), originX + 20 * x, originY + 20 * y, TopLeft);
                plate.raycastTarget = true;
                plate.alphaHitTestMinimumThreshold = 0.5f;
                Img("Art", plate.transform, S("Items/Icons/item_" + item.art + "_d_" + item.footprint + ".png"), 2, 2, TopLeft);
                plate.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                var hover = plate.gameObject.AddComponent<ItemHoverTarget>();
                hover.Configure(item.id, x, y, context, popup);
                targets.Add(hover);
            }

            if (context != null) context.SetItems(targets.ToArray());
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
        private static CombatHudView BuildHud(RectTransform safe)
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
            SpriteNumeralText room = Num("Room", hudRt, 8, "num-fog", 52, 4, "1/8", TopLeft);
            Img("Menu", hudRt, S("UI/ui_glyph-menu_bone_8x8.png"), 624, 4, TopLeft);
            Label("ForecastLabel", hudRt, LabelForecast, 276, 2, 150, Fog);
            OrderStripView orderView = BuildOrderStrip(hudRt);

            HeroSlotView[] heroes = new HeroSlotView[3];
            for (int k = 0; k < 3; k++) heroes[k] = BuildHeroSlot(hudRt, k);
            EnemySlotView[] enemies = new EnemySlotView[6];
            for (int i = 0; i < 6; i++) enemies[i] = BuildEnemySlot(hudRt, i);
            SpeedControlView speedView = BuildSpeedControl(hudRt);
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
            return view;
        }

        private static void Assign<T>(SerializedProperty array, T[] items) where T : Object
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static OrderStripView BuildOrderStrip(Transform parent)
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
                SerializedProperty c = chips.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("root").objectReferenceValue = chip.gameObject;
                c.FindPropertyRelative("numeral").objectReferenceValue = numeral;
                c.FindPropertyRelative("glyph").objectReferenceValue = glyph;
                c.FindPropertyRelative("marker").objectReferenceValue = marker.gameObject;
            }

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
            int[] widths = { 28, 28, 28, 52 };
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
                Label("Label", seg.transform, labels[i], 0, 5, widths[i] + 8, Bone, TextAlignmentOptions.Top);
                buttons[i] = button;
                images[i] = seg;
                x += widths[i] + 8;
            }

            Image key = Img("KeyChip", strip, S("UI/ui_chip_base_9s.png"), 598, 338, TopLeft, 30, 14, true);
            Label("KeyLabel", key.transform, LabelKey, 0, 1, 30, Fog, TextAlignmentOptions.Top);

            var so = new SerializedObject(view);
            Assign(so.FindProperty("buttons"), buttons);
            Assign(so.FindProperty("segments"), images);
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
                foreach (string id in new[] { "item_iron_sword", "item_dagger", "item_hourglass", "item_breaking_bell", "item_echo_charm" })
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
