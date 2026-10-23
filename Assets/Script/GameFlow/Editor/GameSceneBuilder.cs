using System;
using System.Linq;
using Smoking.Audio;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Smoking.GameFlow.Editor
{
    /// <summary>One-time authoring tool. Runtime never generates or replaces the editable Canvas UI.</summary>
    public static class GameSceneBuilder
    {
        private const string Root = "Assets/Script/GameFlow";
        private static readonly Color Ink = new Color32(24, 29, 30, 255);
        private static readonly Color Panel = new Color32(35, 42, 43, 255);
        private static readonly Color Paper = new Color32(241, 233, 215, 255);
        private static readonly Color Muted = new Color32(159, 177, 171, 255);
        private static readonly Color Accent = new Color32(220, 159, 104, 255);
        private static TMP_FontAsset font;
        private static GameSettingsConfig config;
        private static AudioCue bgm, click;

        [MenuItem("Tools/Smoking/Create Missing Game Scenes")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scenes before authoring new scenes.");
            EnsureFolder(Root + "/Setup");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import TMP Essentials before building scenes.");
            config = AssetDatabase.LoadAssetAtPath<GameSettingsConfig>(Root + "/Setup/GameSettings.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameSettingsConfig>();
                AssetDatabase.CreateAsset(config, Root + "/Setup/GameSettings.asset");
            }
            const string audio = "Assets/ReadyAssets/Audio/Casual Puzzle World Sounds - Free Package/";
            bgm = Cue("TestBackgroundMusic", audio + "Soundtracks/WAV/track_stepbystep_loop.wav", true, 0.55f);
            click = Cue("MenuClick", audio + "Menu/WAV/sfx_button1_click1.wav", false, 0.65f);
            CreateScene("MainMenu", 0);
            CreateScene("Gameplay", 1);
            CreateScene("Credits", 2);
            string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Gameplay.unity", "Assets/Scenes/Credits.unity" };
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(paths[0]);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static AudioCue Cue(string name, string clipPath, bool loop, float volume)
        {
            string path = Root + "/Setup/" + name + ".asset";
            var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(path);
            if (cue != null) return cue;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) throw new InvalidOperationException("Missing audio clip: " + clipPath);
            cue = ScriptableObject.CreateInstance<AudioCue>();
            var serialized = new SerializedObject(cue);
            var clips = serialized.FindProperty("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            serialized.FindProperty("loop").boolValue = loop;
            serialized.FindProperty("volume").floatValue = volume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(cue, path);
            return cue;
        }

        private static void CreateScene(string name, int kind)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            var cam = camera.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ink;
            cam.orthographic = true;
            var audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Script/Audio/Setup/AudioBootstrap.prefab");
            var audio = (GameObject)PrefabUtility.InstantiatePrefab(audioPrefab);
            var service = new GameObject("GameSettingsService").AddComponent<GameSettingsService>();
            Assign(service, "config", config); Assign(service, "audioRoot", audio.GetComponent<AudioBootstrap>());
            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var flow = new GameObject("SceneFlow").AddComponent<SceneFlow>();
            Assign(flow, "backgroundMusic", bgm); Assign(flow, "clickSound", click);
            var canvas = CanvasRoot("Canvas", 0);
            var background = Image("Background", canvas, Ink); Stretch(background.rectTransform);
            ImageLine(canvas, "HeaderRule", new Vector2(0.07f, 0.89f), new Vector2(0.93f, 0.892f), Accent);
            var brand = Text("Brand", canvas, "SMOKING  /  JYU GAME JAM", 21, Muted);
            Area(brand.rectTransform, .07f, .91f, .7f, .96f);
            var footer = Text("Footer", canvas, "JYU  /  2026", 18, Muted);
            Area(footer.rectTransform, .07f, .035f, .45f, .08f);
            if (kind == 0) MainMenu(canvas, flow);
            else if (kind == 1) Gameplay(canvas, flow);
            else Credits(canvas, flow);
            var brightnessCanvas = CanvasRoot("BrightnessCanvas", 100);
            // This overlay dims the complete image but never intercepts pointer input.
            var shade = Image("BrightnessShade", brightnessCanvas, Color.clear);
            Stretch(shade.rectTransform);
            var brightness = brightnessCanvas.gameObject.AddComponent<BrightnessOverlay>();
            Assign(brightness, "shade", shade);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static RectTransform CanvasRoot(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            return (RectTransform)go.transform;
        }

        private static void MainMenu(RectTransform canvas, SceneFlow flow)
        {
            var home = Rect("MainMenuPanel", canvas); Stretch(home);
            var eyebrow = Text("Eyebrow", home, "A MOMENT TO PAUSE.", 23, Accent); Area(eyebrow.rectTransform, .07f, .71f, .56f, .77f);
            var title = Text("Title", home, "SMOKING", 116, Paper); title.fontStyle = FontStyles.Bold; Area(title.rectTransform, .065f, .55f, .61f, .72f);
            var subtitle = Text("Subtitle", home, "Take a breath.\nThen take the next step.", 30, Muted); Area(subtitle.rectTransform, .07f, .38f, .55f, .53f);
            var stamp = Text("Chapter", home, "01\nTHE BEGINNING", 25, Accent); Area(stamp.rectTransform, .07f, .17f, .50f, .29f);
            var list = Stack("MenuButtons", home, .65f, .23f, .91f, .73f, 18);
            var start = Button("StartButton", list, "START GAME", flow.StartGame, flow, true);
            var settings = Button("SettingsButton", list, "SETTINGS", null, flow);
            Button("RestartButton", list, "RESTART", flow.RestartGame, flow);
            Button("QuitButton", list, "QUIT", flow.Quit, flow);
            var controller = canvas.gameObject.AddComponent<MenuPanels>();
            var settingsPanel = BuildSettings(canvas, controller, flow, out var first);
            Assign(controller, "homePanel", home.gameObject); Assign(controller, "settingsPanel", settingsPanel.gameObject);
            Assign(controller, "homeSelection", start.gameObject); Assign(controller, "settingsSelection", first);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            settingsPanel.gameObject.SetActive(false);
        }

        private static void Gameplay(RectTransform canvas, SceneFlow flow)
        {
            var hud = Rect("GameplayHUD", canvas); Stretch(hud);
            var title = Text("Title", hud, "THE QUIET ROOM", 66, Paper); Area(title.rectTransform, .07f, .65f, .88f, .79f);
            var subtitle = Text("Hint", hud, "An empty scene, ready for your game.\nPress ESC to pause.", 28, Muted); Area(subtitle.rectTransform, .07f, .48f, .9f, .64f);
            var clockText = Text("PlayTime", hud, "PLAY TIME  000.0 s", 22, Accent); Area(clockText.rectTransform, .07f, .37f, .8f, .44f);
            var clock = new GameObject("GameplayClock").AddComponent<GameplayClock>(); Assign(clock, "label", clockText);
            var list = Stack("TestControls", hud, .07f, .15f, .34f, .33f, 12);
            Button("TestSfxButton", list, "PLAY TEST SFX", flow.PlayClick, null);
            Button("CompleteButton", list, "COMPLETE / CREDITS", flow.CompleteGame, flow);
            var controller = canvas.gameObject.AddComponent<MenuPanels>(); SetBool(controller, "gameplay", true);
            var pause = Rect("PausePanel", canvas); Stretch(pause);
            var backdrop = Image("Backdrop", pause, new Color(0.04f, .06f, .06f, .97f)); Stretch(backdrop.rectTransform); backdrop.raycastTarget = true;
            var label = Text("Title", pause, "TAKE A BREATH", 66, Paper); label.alignment = TextAlignmentOptions.Center; Area(label.rectTransform, .2f, .70f, .8f, .82f);
            var caption = Text("Subtitle", pause, "PAUSED", 22, Accent); caption.alignment = TextAlignmentOptions.Center; Area(caption.rectTransform, .2f, .63f, .8f, .69f);
            var buttons = Stack("Buttons", pause, .35f, .23f, .65f, .57f, 18);
            var resume = Button("ResumeButton", buttons, "RESUME", controller.Resume, flow, true);
            Button("SettingsButton", buttons, "SETTINGS", controller.OpenSettings, flow);
            Button("MainMenuButton", buttons, "MAIN MENU", flow.MainMenu, flow);
            var settingsPanel = BuildSettings(canvas, controller, flow, out var first);
            Assign(controller, "homePanel", hud.gameObject); Assign(controller, "pausePanel", pause.gameObject);
            Assign(controller, "settingsPanel", settingsPanel.gameObject); Assign(controller, "pauseSelection", resume.gameObject);
            Assign(controller, "settingsSelection", first);
            pause.gameObject.SetActive(false); settingsPanel.gameObject.SetActive(false);
        }

        private static RectTransform BuildSettings(RectTransform canvas, MenuPanels controller, SceneFlow flow, out GameObject first)
        {
            var panel = Rect("SettingsPanel", canvas); Stretch(panel);
            var background = Image("Backdrop", panel, Ink); Stretch(background.rectTransform); background.raycastTarget = true;
            var card = Image("SettingsCard", panel, Panel); Area(card.rectTransform, .22f, .13f, .78f, .88f);
            var title = Text("Title", panel, "MAKE YOURSELF COMFORTABLE", 35, Paper); Area(title.rectTransform, .27f, .72f, .73f, .83f);
            var subtitle = Text("Subtitle", panel, "SETTINGS  /  SAVED ACROSS ALL SCENES", 18, Muted); Area(subtitle.rectTransform, .27f, .67f, .73f, .73f);
            var view = panel.gameObject.AddComponent<SettingsPanel>();
            var music = SliderRow(panel, "Music", "BGM VOLUME", .55f, out var musicValue);
            var sfx = SliderRow(panel, "Sfx", "SFX VOLUME", .42f, out var sfxValue);
            var brightness = SliderRow(panel, "Brightness", "BRIGHTNESS", .29f, out var brightnessValue);
            Assign(view, "musicSlider", music); Assign(view, "sfxSlider", sfx); Assign(view, "brightnessSlider", brightness);
            Assign(view, "musicValue", musicValue); Assign(view, "sfxValue", sfxValue); Assign(view, "brightnessValue", brightnessValue);
            Assign(view, "previewSound", click);
            UnityEventTools.AddPersistentListener(music.onValueChanged, view.SetMusic);
            UnityEventTools.AddPersistentListener(sfx.onValueChanged, view.SetSfx);
            UnityEventTools.AddPersistentListener(brightness.onValueChanged, view.SetBrightness);
            var back = Button("BackButton", panel, "BACK", controller.CloseSettings, flow, true); Area((RectTransform)back.transform, .27f, .16f, .46f, .23f);
            var preview = Button("PreviewSfxButton", panel, "TEST SOUND", view.PreviewSfx, null); Area((RectTransform)preview.transform, .51f, .16f, .73f, .23f);
            first = music.gameObject;
            return panel;
        }

        private static UnityEngine.UI.Slider SliderRow(RectTransform parent, string name, string label, float y, out TMP_Text value)
        {
            var row = Rect(name + "Row", parent); Area(row, .27f, y, .73f, y + .10f);
            var caption = Text("Label", row, label, 23, Paper); Area(caption.rectTransform, 0, .5f, .8f, 1);
            value = Text("Value", row, "100%", 23, Accent); value.alignment = TextAlignmentOptions.Right; Area(value.rectTransform, .8f, .5f, 1, 1);
            var rect = Rect(name + "Slider", row); Area(rect, 0, 0, 1, .45f);
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            var hit = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var track = Image("Track", rect, new Color32(70, 83, 81, 255)); Area(track.rectTransform, 0, .4f, 1, .6f);
            var fillArea = Rect("FillArea", rect); Area(fillArea, 0, .4f, 1, .6f); fillArea.offsetMin = new Vector2(12, 0); fillArea.offsetMax = new Vector2(-12, 0);
            var fill = Image("Fill", fillArea, Accent); Area(fill.rectTransform, 0, .4f, 1, .6f);
            var handleArea = Rect("HandleArea", rect); Area(handleArea, 0, .1f, 1, .9f); handleArea.offsetMin = new Vector2(12, 0); handleArea.offsetMax = new Vector2(-12, 0);
            var handle = Image("Handle", handleArea, Paper); handle.rectTransform.sizeDelta = new Vector2(24, 0); handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        private static void Credits(RectTransform canvas, SceneFlow flow)
        {
            var title = Text("Title", canvas, "A LITTLE BREATH. A BIG THANK YOU.", 40, Paper); title.alignment = TextAlignmentOptions.Center; Area(title.rectTransform, .1f, .76f, .9f, .87f);
            var viewport = Rect("CreditsViewport", canvas); Area(viewport, .18f, .22f, .82f, .73f); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var credits = Text("CreditsText", viewport,
                "<size=28><color=#DC9F68>YOU MADE IT</color></size>\n\n<size=62>Thank you for playing.</size>\n\n\n<size=26><color=#DC9F68>CREATED BY</color></size>\n\nYour team name\n\n\n<size=26><color=#DC9F68>DESIGN & DEVELOPMENT</color></size>\n\nAdd names here\n\n\n<size=26><color=#DC9F68>ART & AUDIO</color></size>\n\nAdd names here\n\n\n<size=26><color=#DC9F68>SPECIAL THANKS</color></size>\n\nFriends, mentors, and playtesters\n\n\n<size=26><color=#DC9F68>JYU GAME JAM / 2026</color></size>\n\nUntil next time.", 34, Paper);
            credits.alignment = TextAlignmentOptions.Center;
            credits.rectTransform.anchorMin = credits.rectTransform.anchorMax = new Vector2(.5f, .5f);
            credits.rectTransform.sizeDelta = new Vector2(1100, 1700);
            var scroller = viewport.gameObject.AddComponent<CreditsScroller>(); Assign(scroller, "viewport", viewport); Assign(scroller, "creditsText", credits);
            var back = Button("MainMenuButton", canvas, "MAIN MENU", flow.MainMenu, flow, true); Area((RectTransform)back.transform, .36f, .10f, .64f, .18f);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.Left; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private static void Stretch(RectTransform rect) => Area(rect, 0, 0, 1, 1);
        private static void Area(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void ImageLine(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        { var image = Image(name, parent, color); Area(image.rectTransform, min.x, min.y, max.x, max.y); }
        private static RectTransform Stack(string name, Transform parent, float left, float bottom, float right, float top, float spacing)
        {
            var rect = Rect(name, parent); Area(rect, left, bottom, right, top);
            var layout = rect.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = spacing; layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = true;
            return rect;
        }
        private static UnityEngine.UI.Button Button(string name, Transform parent, string label, UnityAction action, SceneFlow flow, bool primary = false)
        {
            var image = Image(name, parent, primary ? Accent : Panel); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f); colors.selectedColor = new Color(.85f, .95f, .95f); colors.pressedColor = new Color(.7f, .7f, .7f); button.colors = colors;
            var text = Text("Label", image.transform, label, 26, primary ? Ink : Paper); text.alignment = TextAlignmentOptions.Center; Stretch(text.rectTransform);
            var element = image.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); element.minHeight = 58; element.flexibleHeight = 1;
            if (flow != null) UnityEventTools.AddPersistentListener(button.onClick, flow.PlayClick);
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
        private static void Assign(UnityEngine.Object owner, string field, UnityEngine.Object value)
        { var serialized = new SerializedObject(owner); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetBool(UnityEngine.Object owner, string field, bool value)
        { var serialized = new SerializedObject(owner); serialized.FindProperty(field).boolValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
