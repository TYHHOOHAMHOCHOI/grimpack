using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Editor
{
    /// <summary>
    /// Builds the modal pieces shared by the Combat and Title scenes (design/ux/combat-screen-buttons.md): the primary button,
    /// the pause menu, the abandon confirmation, the settings panel and the dim mask. All positions are native pixels measured
    /// from the top-left of the parent, which is the 640 x 360 safe frame.
    /// </summary>
    public static class ModalKit
    {
        private const string Art = "Assets/_Project/Art/UI/";

        // TEMPORARY: user-facing strings must come from the localization system (System 7), which does not exist in code yet.
        private const string TextPause = "Tạm dừng";
        private const string TextResume = "Tiếp tục";
        private const string TextSettings = "Cài đặt";
        private const string TextAbandonRun = "Bỏ cuộc chạy";
        private const string TextConfirmTitle = "Bỏ cuộc chạy này?";
        private const string TextConfirmBody = "Trận hiện tại sẽ không được lưu.";
        private const string TextKeep = "Tiếp tục chơi";
        private const string TextAbandon = "Bỏ cuộc";
        private const string TextBack = "Quay lại";
        private const string TextFlashes = "Giảm nhấp nháy";
        private const string TextMotion = "Giảm chuyển động";
        private const string TextSkipEnd = "Skip đến cuối";
        private const string TextOn = "Bật";
        private const string TextOff = "Tắt";

        private static readonly Color Bone = new Color32(0xE4, 0xDA, 0xC0, 0xFF);
        private static readonly Color Fog = new Color32(0x87, 0x95, 0xB3, 0xFF);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        /// <summary>Sets the 9-slice borders of the sprites this kit uses. Call before building.</summary>
        public static void ConfigureBorders()
        {
            foreach (string s in new[] { "default", "hover", "pressed", "focus", "disabled" })
            {
                Border(Art + "ui_primary_" + s + "_9s.png", new Vector4(8, 8, 8, 8));
                Border(Art + "ui_chipbtn_" + s + "_9s.png", new Vector4(3, 3, 3, 3));
                Border(Art + "ui_start_" + s + "_9s.png", new Vector4(8, 8, 8, 8));
            }

            Border(Art + "ui_panel_default_9s.png", new Vector4(4, 4, 4, 4));
        }

        private static void Border(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        /// <summary>Loads a sprite from the UI art folder.</summary>
        public static Sprite Sprite(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
            if (sprite == null) throw new FileNotFoundException("Missing sprite (not imported?)", Art + file);
            return sprite;
        }

        /// <summary>Makes a child rect anchored to the parent top-left.</summary>
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = TopLeft;
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Makes a Pixel-12 text line.</summary>
        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string text, float x, float y, float w,
            Color colour, TextAlignmentOptions alignment)
        {
            RectTransform rt = Rect(name, parent, x, y, w, 12);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
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

        /// <summary>
        /// A primary button: a 24 px tall rect whose body is 16 px, with 4 px of room for the hover and focus rings.
        /// (x, y, w) describe the rect, so the body is 8 px narrower.
        /// </summary>
        public static Button Primary(string name, Transform parent, TMP_FontAsset font, string label, float x, float y, float w, out TMP_Text text)
        {
            return Primary(name, parent, font, label, x, y, w, "primary", Bone, out text);
        }

        /// <summary>A 24 px tall button in a given sprite style ("primary" outlined, "start" filled) with a label colour.</summary>
        public static Button Primary(string name, Transform parent, TMP_FontAsset font, string label, float x, float y, float w,
            string style, Color labelColour, out TMP_Text text)
        {
            RectTransform rt = Rect(name, parent, x, y, w, 24);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Sprite("ui_" + style + "_default_9s.png");
            image.type = Image.Type.Sliced;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Sprite("ui_" + style + "_hover_9s.png"),
                pressedSprite = Sprite("ui_" + style + "_pressed_9s.png"),
                selectedSprite = Sprite("ui_" + style + "_focus_9s.png"),
                disabledSprite = Sprite("ui_" + style + "_disabled_9s.png"),
            };
            text = Text("Label", rt, font, label, 0, 6, w, labelColour, TextAlignmentOptions.Top);
            return button;
        }

        /// <summary>
        /// Adds the pixel cursor to a scene object: imports the pre-scaled cursor textures as cursors (readable, point filtered,
        /// uncompressed) and assigns them with their hot spots.
        /// </summary>
        public static PixelCursor AddCursor(GameObject host)
        {
            var cursor = host.AddComponent<PixelCursor>();
            var so = new SerializedObject(cursor);
            Shape(so.FindProperty("arrow"), "arrow", 11, 16, Vector2.zero);
            Shape(so.FindProperty("grab"), "grab", 14, 11, new Vector2(7, 5));
            so.ApplyModifiedPropertiesWithoutUndo();
            return cursor;
        }

        private static void Shape(SerializedProperty set, string name, int w, int h, Vector2 hotSpot)
        {
            SerializedProperty list = set.FindPropertyRelative("byZoom");
            list.arraySize = 4;
            for (int z = 1; z <= 4; z++)
            {
                string path = Art + "Cursors/ui_cursor-" + name + "_x" + z + "_" + (w * z) + "x" + (h * z) + ".png";
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Cursor)
                {
                    importer.textureType = TextureImporterType.Cursor;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) throw new FileNotFoundException("Missing cursor texture", path);
                list.GetArrayElementAtIndex(z - 1).objectReferenceValue = texture;
            }

            set.FindPropertyRelative("hotSpot").vector2Value = hotSpot;
        }

        /// <summary>The checker mask that dims the stage one step (art bible 2.3.1).</summary>
        public static GameObject DimMask(Transform parent, float x, float y, float w, float h)
        {
            RectTransform rt = Rect("StageDim", parent, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Sprite("ui_dim-mask_default_4x4.png");
            image.type = Image.Type.Tiled;
            image.raycastTarget = false;
            rt.gameObject.SetActive(false);
            return rt.gameObject;
        }

        private static RectTransform Panel(string name, Transform parent, float cx, float cy, float w, float h)
        {
            RectTransform rt = Rect(name, parent, Mathf.Round(cx - w / 2f), Mathf.Round(cy - h / 2f), w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Sprite("ui_panel_default_9s.png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;          // the panel itself catches clicks so nothing behind it is hit
            rt.gameObject.SetActive(false);
            return rt;
        }

        private static void Link(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length],
                    selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnRight = buttons[(i + 1) % buttons.Length],
                };
            }
        }

        /// <summary>Builds the pause menu centred on (cx, cy). Hidden until the controller shows it.</summary>
        public static PauseMenuView Pause(Transform parent, TMP_FontAsset font, float cx, float cy)
        {
            RectTransform panel = Panel("PauseMenu", parent, cx, cy, 152, 112);
            Text("Title", panel, font, TextPause, 0, 8, 152, Fog, TextAlignmentOptions.Top);
            Button resume = Primary("Resume", panel, font, TextResume, 4, 24, 144, out _);
            Button settings = Primary("Settings", panel, font, TextSettings, 4, 52, 144, out _);
            Button abandon = Primary("Abandon", panel, font, TextAbandonRun, 4, 80, 144, out _);
            Link(resume, settings, abandon);

            var view = panel.gameObject.AddComponent<PauseMenuView>();
            var so = new SerializedObject(view);
            so.FindProperty("resume").objectReferenceValue = resume;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("abandon").objectReferenceValue = abandon;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>Builds the abandon confirmation centred on (cx, cy). Hidden until the controller shows it.</summary>
        public static ConfirmDialogView Confirm(Transform parent, TMP_FontAsset font, float cx, float cy)
        {
            RectTransform panel = Panel("ConfirmDialog", parent, cx, cy, 224, 92);
            Text("Title", panel, font, TextConfirmTitle, 0, 8, 224, Bone, TextAlignmentOptions.Top);
            Text("Body", panel, font, TextConfirmBody, 0, 24, 224, Fog, TextAlignmentOptions.Top);
            Button keep = Primary("Keep", panel, font, TextKeep, 4, 52, 108, out _);
            Button abandon = Primary("Abandon", panel, font, TextAbandon, 112, 52, 108, out _);
            Link(keep, abandon);

            var view = panel.gameObject.AddComponent<ConfirmDialogView>();
            var so = new SerializedObject(view);
            so.FindProperty("keepPlaying").objectReferenceValue = keep;
            so.FindProperty("abandon").objectReferenceValue = abandon;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>Builds the settings panel centred on (cx, cy). Hidden until shown.</summary>
        public static SettingsPanelView Settings(Transform parent, TMP_FontAsset font, float cx, float cy)
        {
            RectTransform panel = Panel("SettingsPanel", parent, cx, cy, 252, 148);
            Text("Title", panel, font, TextSettings, 0, 8, 252, Fog, TextAlignmentOptions.Top);
            Button flashes = Primary("Flashes", panel, font, string.Empty, 4, 24, 244, out TMP_Text flashesLabel);
            Button motion = Primary("Motion", panel, font, string.Empty, 4, 52, 244, out TMP_Text motionLabel);
            Button skipEnd = Primary("SkipEnd", panel, font, string.Empty, 4, 80, 244, out TMP_Text skipEndLabel);
            Button back = Primary("Back", panel, font, TextBack, 4, 112, 244, out _);
            Link(flashes, motion, skipEnd, back);

            var view = panel.gameObject.AddComponent<SettingsPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("flashesButton").objectReferenceValue = flashes;
            so.FindProperty("motionButton").objectReferenceValue = motion;
            so.FindProperty("skipEndButton").objectReferenceValue = skipEnd;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("flashesLabel").objectReferenceValue = flashesLabel;
            so.FindProperty("motionLabel").objectReferenceValue = motionLabel;
            so.FindProperty("skipEndLabel").objectReferenceValue = skipEndLabel;
            so.FindProperty("flashesName").stringValue = TextFlashes;
            so.FindProperty("motionName").stringValue = TextMotion;
            so.FindProperty("skipEndName").stringValue = TextSkipEnd;
            so.FindProperty("onText").stringValue = TextOn;
            so.FindProperty("offText").stringValue = TextOff;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
