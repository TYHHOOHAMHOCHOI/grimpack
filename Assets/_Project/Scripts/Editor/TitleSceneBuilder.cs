using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Editor
{
    /// <summary>
    /// Builds the Title scene from the menu art (art bible 2.3.1, 7.3, 7.11, 8.1) and the project's
    /// pixel face. Rebuilds <c>Title.unity</c> and the TMP font asset on every run, because both are
    /// generated outputs. Batch entry points: <see cref="ImportDependencies"/> then
    /// <see cref="BuildAndCapture"/> (two separate runs, since the TMP resources import first).
    /// </summary>
    public static class TitleSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Title.unity";
        private const string FontFolder = "Assets/_Project/Art/Fonts/grimpack-pixel";
        private const string FontTtfPath = FontFolder + "/font_grimpack-pixel_regular_12.ttf";
        private const string FontAssetPath = FontFolder + "/font_grimpack-pixel_regular_12_tmp.asset";
        private const string UiFolder = "Assets/_Project/Art/UI/";
        private const string BackdropPath = "Assets/_Project/Art/Environment/env_title_bg_640x360.png";
        private const string EvidenceFolder = "production/qa/evidence";

        // Master palette (tools/art/palette.py): INK-0 camera clear, BONE label colour.
        private static readonly Color Ink0 = new Color32(0x0B, 0x0F, 0x1C, 0xFF);
        private static readonly Color Bone = new Color32(0xE4, 0xDA, 0xC0, 0xFF);

        // TEMPORARY: user-facing strings must come from the localization system (System 7), which does
        // not exist in code yet. Replace these three with keyed lookups when it does.
        private const string LabelPlay = "Chơi";
        private const string LabelSettings = "Cài đặt";
        private const string LabelQuit = "Thoát";

        /// <summary>Imports the TMP essential resources (shaders and settings) if the project lacks them.</summary>
        public static void ImportDependencies()
        {
            AssetDatabase.Refresh();
            if (Resources.Load("TMP Settings") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.Log("[TitleSceneBuilder] Imported TMP Essential Resources.");
            }
            else
            {
                Debug.Log("[TitleSceneBuilder] TMP Essential Resources already present.");
            }

            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        /// <summary>Builds the scene and captures evidence screenshots at zoom 1 and 2, then exits.</summary>
        public static void BuildAndCapture()
        {
            int code = 0;
            try
            {
                Build();
                Capture(1);
                Capture(2);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[TitleSceneBuilder] " + e);
                code = 1;
            }

            EditorApplication.Exit(code);
        }

        /// <summary>
        /// Menu entry: capture evidence screenshots of the saved Title scene at zoom 1 and 2, then reload
        /// the scene from disk so the capture's temporary changes (forced zoom, focus sprite) never persist.
        /// Refuses to run if the open scene has unsaved changes.
        /// </summary>
        [MenuItem("Project05/Title/Capture Evidence")]
        public static void CaptureEvidence()
        {
            Scene open = SceneManager.GetActiveScene();
            if (open.isDirty)
            {
                Debug.LogWarning("[TitleSceneBuilder] The open scene has unsaved changes; save or discard them, then capture again.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                Capture(1);
                Capture(2);
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>Menu entry: rebuild the Title scene.</summary>
        [MenuItem("Project05/Title/Build Title Scene")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigurePanelBorder();
            TMP_FontAsset font = BuildFontAsset();
            BuildScene(font);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[TitleSceneBuilder] Built " + ScenePath);
        }

        private static void ConfigurePanelBorder()
        {
            string path = UiFolder + "ui_panel_default_9s.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.spriteBorder == new Vector4(4, 4, 4, 4)) return;
            importer.spriteBorder = new Vector4(4, 4, 4, 4);
            importer.SaveAndReimport();
        }

        private static TMP_FontAsset BuildFontAsset()
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (ttf == null) throw new FileNotFoundException("Missing font", FontTtfPath);

            // Raster (no anti-aliasing) at the design size: 1 design pixel = 1 atlas pixel (art bible 8.B.8).
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                ttf, 12, 1, GlyphRenderMode.RASTER, 256, 256, AtlasPopulationMode.Dynamic, false);
            if (asset == null) throw new System.Exception("TMP_FontAsset.CreateFontAsset returned null");

            var chars = new System.Text.StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) chars.Append((char)c);
            for (int c = 0xA0; c <= 0xBF; c++) if (ttf.HasCharacter((char)c)) chars.Append((char)c); // includes the middle dot
            for (int c = 0xC0; c <= 0x17F; c++) if (ttf.HasCharacter((char)c)) chars.Append((char)c);
            for (int c = 0x1A0; c <= 0x1B0; c++) if (ttf.HasCharacter((char)c)) chars.Append((char)c); // Ơ ơ Ư ư
            for (int c = 0x1EA0; c <= 0x1EF9; c++) if (ttf.HasCharacter((char)c)) chars.Append((char)c);
            asset.TryAddCharacters(chars.ToString(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("[TitleSceneBuilder] Not in atlas: " + missing);

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.name = "font_grimpack-pixel_regular_12_tmp";

            if (File.Exists(FontAssetPath)) AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            foreach (Texture2D atlas in asset.atlasTextures)
            {
                atlas.filterMode = FilterMode.Point;
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }

            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TitleSceneBuilder] Font asset: {asset.characterTable.Count} characters, " +
                      $"glyphs {asset.glyphTable.Count}, line height {asset.faceInfo.lineHeight}.");
            return asset;
        }

        private static string PoolFramePath(int index)
        {
            return "Assets/_Project/Art/Environment/env_title-pool-" + index + "_prop_210x160.png";
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new FileNotFoundException("Missing sprite (not imported?)", path);
            return sprite;
        }

        private static void BuildScene(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera: INK-0 clear, never black (art bible 4.3, 8.1).
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ink0;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            // Input: the project uses the Input System package only.
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var module = esGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            // Canvas at a whole-number zoom of the 640 x 360 safe frame.
            var canvasGo = new GameObject("TitleCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.pixelPerfect = true;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f; // matches the sprite import PPU, so 1 sprite px = 1 canvas px
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<IntegerCanvasScale>();

            RectTransform safe = NewRect("SafeFrame", canvasGo.transform);
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(IntegerCanvasScale.NativeWidth, IntegerCanvasScale.NativeHeight);
            safe.anchoredPosition = Vector2.zero;

            NewImage("Backdrop", safe, LoadSprite(BackdropPath), 0, 0);
            // The lantern pool breathes: a 210 x 160 overlay of palette-exact frames over the backdrop.
            Image pool = NewImage("LanternPool", safe, LoadSprite(PoolFramePath(1)), 280, 200);
            var breathing = pool.gameObject.AddComponent<LanternPoolBreathing>();
            var poolSo = new SerializedObject(breathing);
            SerializedProperty poolFrames = poolSo.FindProperty("frames");
            poolFrames.arraySize = 4;
            for (int i = 0; i < 4; i++) poolFrames.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(PoolFramePath(i));
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            NewImage("Wordmark", safe, LoadSprite(UiFolder + "ui_title-wordmark_default_190x26.png"), 44, 40);

            Sprite normal = LoadSprite(UiFolder + "ui_btn_default_120x28.png");
            var spriteState = new SpriteState
            {
                highlightedSprite = LoadSprite(UiFolder + "ui_btn_hover_120x28.png"),
                pressedSprite = LoadSprite(UiFolder + "ui_btn_pressed_120x28.png"),
                selectedSprite = LoadSprite(UiFolder + "ui_btn_focus_120x28.png"),
                disabledSprite = LoadSprite(UiFolder + "ui_btn_disabled_120x28.png"),
            };

            // Visible button rect starts at (52, y); the sprite has a 4 px margin for the focus ring.
            Button play = NewButton("PlayButton", safe, normal, spriteState, font, LabelPlay, 52, 150);
            Button settings = NewButton("SettingsButton", safe, normal, spriteState, font, LabelSettings, 52, 182);
            Button quit = NewButton("QuitButton", safe, normal, spriteState, font, LabelQuit, 52, 214);
            LinkVertical(play, settings, quit);

            // Buttons share a group so the settings panel can take the input; the panel sits above them.
            var buttonGroup = NewRect("MenuButtons", safe);
            foreach (Button b in new[] { play, settings, quit }) b.transform.SetParent(buttonGroup, false);
            buttonGroup.anchorMin = buttonGroup.anchorMax = buttonGroup.pivot = new Vector2(0f, 1f);
            buttonGroup.anchoredPosition = Vector2.zero;
            CanvasGroup menuGroup = buttonGroup.gameObject.AddComponent<CanvasGroup>();
            ModalKit.ConfigureBorders();
            SettingsPanelView settingsPanel = ModalKit.Settings(safe, font, 320, 180);

            var menuGo = new GameObject("TitleMenu");
            ModalKit.AddCursor(menuGo);
            var controller = menuGo.AddComponent<TitleMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("menuGroup").objectReferenceValue = menuGroup;
            so.FindProperty("playSceneName").stringValue = "Combat";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Image placed by its top-left corner inside the safe frame, at its native pixel size.</summary>
        private static Image NewImage(string name, RectTransform parent, Sprite sprite, int x, int y)
        {
            RectTransform rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Button NewButton(string name, RectTransform parent, Sprite normal, SpriteState states,
            TMP_FontAsset font, string label, int x, int y)
        {
            Image image = NewImage(name, parent, normal, x - 4, y - 4);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = states;

            RectTransform text = NewRect("Label", image.rectTransform);
            text.anchorMin = text.anchorMax = text.pivot = new Vector2(0f, 1f);
            text.anchoredPosition = new Vector2(14, -8); // 4 px margin + 10 px padding, 4 px above the 12 px line box
            text.sizeDelta = new Vector2(96, 12);
            TextMeshProUGUI tmp = text.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 12;
            tmp.color = Bone;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = Vector4.zero;
            tmp.raycastTarget = false;
            tmp.text = label;
            return button;
        }

        private static void LinkVertical(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length],
                };
                buttons[i].navigation = nav;
            }
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void Capture(int zoom)
        {
            Camera cam = Object.FindAnyObjectByType<Camera>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            canvas.GetComponent<IntegerCanvasScale>().enabled = false;
            scaler.scaleFactor = zoom;

            int w = IntegerCanvasScale.NativeWidth * zoom;
            int h = IntegerCanvasScale.NativeHeight * zoom;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            rt.Create();
            // The camera must target the texture before the canvas lays out, otherwise a Screen Space - Camera
            // canvas is sized from the (different) default game-view resolution and the capture is stretched.
            cam.targetTexture = rt;

            // Edit mode has no EventSystem selection, so show the focus state Play has at runtime.
            Button play = GameObject.Find("PlayButton").GetComponent<Button>();
            play.GetComponent<Image>().sprite = play.spriteState.selectedSprite;

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
            string file = Path.Combine(EvidenceFolder, $"title_scene_z{zoom}_2026-11-15.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Debug.Log("[TitleSceneBuilder] Captured " + file);
        }
    }
}
