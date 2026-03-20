#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.IO;

#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
#else
using UnityEngine.UIElements;
#endif


[InitializeOnLoad]
public static class SceneSwitcherToolbar
{
    private static string[] sceneNames = new string[0];
    private static int selectedIndex = 0;
    private static string lastActiveScene = "";

    private static bool fetchAllScenes
    {
        get => EditorPrefs.GetBool("SceneSwitcher_FetchAllScenes", false);
        set => EditorPrefs.SetBool("SceneSwitcher_FetchAllScenes", value);
    }

#if !UNITY_6000_3_OR_NEWER
    private static VisualElement toolbarUI;

    private static bool needsSceneListRefresh = false;
    private static float dropdownBoxHeight = 20f;
#else
    private const string k_ElementPath = "Scene Switcher Pro";
#endif

    static SceneSwitcherToolbar()
    {
        RefreshSceneList();
        SelectCurrentScene();

        EditorBuildSettings.sceneListChanged += RefreshSceneList;
        EditorApplication.projectChanged += RefreshSceneList;

        EditorSceneManager.activeSceneChangedInEditMode += (prev, current) => {
            UpdateSceneSelection();
#if UNITY_6000_3_OR_NEWER
            RefreshMainToolbar();
#endif
        };
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

#if !UNITY_6000_3_OR_NEWER
        needsSceneListRefresh = true;
        EditorApplication.delayCall += AddToolbarUI;
#endif
    }

#if UNITY_6000_3_OR_NEWER
    [InitializeOnLoadMethod]
    private static void ShowWelcomePopup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool("SceneSwitcherToolbar_HasShownWelcomePopup_63", false))
            {
                EditorPrefs.SetBool("SceneSwitcherToolbar_HasShownWelcomePopup_63", true);
                ToolbarWelcomeWindow.ShowWindow();
            }
        };
    }

    public class ToolbarWelcomeWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<ToolbarWelcomeWindow>(true, "Scene Switcher Pro", true);
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(20);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUILayout.Label("Welcome to Scene Switcher Pro!", headerStyle);
            GUILayout.Label("(Unity 6.3+ Integration)", new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });

            EditorGUILayout.Space(10);

            GUIStyle bodyStyle = new GUIStyle(EditorStyles.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
            GUILayout.Label("In Unity 6.3 and newer, the main toolbar has been revamped.\n\nTo find the Scene Switcher, click the <b>Three Dots (⋮)</b> near the Play buttons on the top toolbar and select the <b>Scene Switcher Pro</b> to open or pin it.", bodyStyle);

            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Got It!", GUILayout.Width(120), GUILayout.Height(30)))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    [MainToolbarElement(k_ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateSceneSelectorDropdown()
    {
        var activeSceneName = EditorSceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(activeSceneName))
            activeSceneName = "Untitled";

        var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
        var content = new MainToolbarContent(activeSceneName, icon, "Scene Switcher Pro");
        return new MainToolbarDropdown(content, ShowDropdownMenu);
    }

    private static void ShowDropdownMenu(Rect dropDownRect)
    {
        PopupWindow.Show(dropDownRect, new SceneSwitcherToolbarPopup());
    }

    internal static void RefreshMainToolbar()
    {
        MainToolbar.Refresh(k_ElementPath);
    }

#else
    // --- Legacy Toolbar Implementation (Unity < 6.3) ---

    static void AddToolbarUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null) return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0) return;

        var toolbar = toolbars[0];
        var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null) return;

        var root = rootField.GetValue(toolbar) as VisualElement;
        if (root == null) return;

        var playModeContainer = root.Q("ToolbarZonePlayMode");
        if (playModeContainer == null) return;

        if (toolbarUI != null)
        {
            playModeContainer.Remove(toolbarUI);
        }

        toolbarUI = new IMGUIContainer(OnGUI);

        playModeContainer.Add(toolbarUI);
    }

    static void OnGUI()
    {
        if (needsSceneListRefresh)
        {
            RefreshSceneList();
            SelectCurrentScene();
            needsSceneListRefresh = false;
        }

        bool isPlaying = EditorApplication.isPlaying; 
        GUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(isPlaying);

        GUIStyle popupStyle = new GUIStyle(EditorStyles.popup)
        {
            fixedHeight = dropdownBoxHeight
        };

        Rect buttonRect = GUILayoutUtility.GetRect(150, dropdownBoxHeight, popupStyle);

        string fullName = EditorSceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(fullName)) fullName = "Untitled";

        string truncName = (fullName.Length > 15) ? (fullName.Substring(0, 12) + "...") : fullName;
        GUIContent buttonContent = new GUIContent(truncName, fullName);

        if (GUI.Button(buttonRect, buttonContent, popupStyle))
        {
            UnityEditor.PopupWindow.Show(buttonRect, new SceneSwitcherToolbarPopup());
        }
        
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }

    internal static void RepaintToolbar()
    {
        if (toolbarUI != null)
            toolbarUI.MarkDirtyRepaint();
    }
#endif

    // --- Core Application Logic ---

    static void RefreshSceneList()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (fetchAllScenes)
        {
            sceneNames = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }
        else
        {
            var validScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            sceneNames = validScenes;
        }

        SelectCurrentScene();

#if !UNITY_6000_3_OR_NEWER
        needsSceneListRefresh = true;
        RepaintToolbar();
#else
        RefreshMainToolbar();
#endif
    }

    static void CheckAndRefreshScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string[] currentScenes = fetchAllScenes
            ? Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray()
            : EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

        string currentHash = string.Join(",", currentScenes);
        string lastHash = string.Join(",", sceneNames);

        if (currentHash != lastHash)
        {
            sceneNames = currentScenes;
            SelectCurrentScene();
        }
    }

    static void SelectCurrentScene()
    {
        string currentScene = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);

        // Remove any previous "(not in build index)" label to avoid duplicates
        sceneNames = sceneNames.Where(name => !name.EndsWith(" (not in build index)")).ToArray();

        int index = System.Array.IndexOf(sceneNames, currentScene);

        if (index != -1)
        {
            selectedIndex = index;
            lastActiveScene = currentScene;
        }
        else
        {
            string notInBuildName = currentScene + " (not in build index)";
            sceneNames = new[] { notInBuildName }.Concat(sceneNames).ToArray();
            selectedIndex = 0;
            lastActiveScene = currentScene;
        }
    }

    static void UpdateSceneSelection()
    {
        string currentScene = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);
        if (currentScene != lastActiveScene)
        {
            lastActiveScene = currentScene;
            sceneNames = sceneNames.Where(name => !name.EndsWith(" (not in build index)")).ToArray();
            SelectCurrentScene();
        }
    }

    static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName.Contains("(not in build index)"))
            return;

        string scenePath = null;

        if (fetchAllScenes)
        {
            scenePath = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
                .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == sceneName);
        }
        else
        {
            var buildScene = EditorBuildSettings.scenes
                .FirstOrDefault(scene => scene.enabled && Path.GetFileNameWithoutExtension(scene.path) == sceneName);

            if (buildScene != null && buildScene.path != null && File.Exists(buildScene.path))
                scenePath = buildScene.path;
        }

        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        {
            Debug.LogWarning(
                $"<color=orange>Scene Switcher:</color> Scene \"{sceneName}\" could not be found or has been deleted.\n" +
                $"Please remove it from Build Settings or re-add the file."
            );
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
        {
#if !UNITY_6000_3_OR_NEWER
            EditorApplication.delayCall += () => AddToolbarUI();
#endif
        }
    }

    // --- Unified UI Popup Component used by BOTH Legacy Toolbar and Unity 6.3+ Toolbar ---

    private class SceneSwitcherToolbarPopup : PopupWindowContent
    {
        private Vector2 _scroll;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(240, 300);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginVertical();

            DrawModeButtons();

            EditorGUILayout.Space(4);

            DrawSelectedScene();

            EditorGUILayout.Space(4);

            DrawSceneList();

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedScene()
        {
            string activeSceneName = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(activeSceneName))
                activeSceneName = "Untitled";

            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(4, 4, 4, 4)
            };

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageLeft
            };

            EditorGUILayout.BeginVertical(boxStyle);

            var icon = EditorGUIUtility.IconContent("SceneAsset Icon");
            GUIContent content = new GUIContent(activeSceneName, icon.image, "Currently active scene");
            GUILayout.Label(content, labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22));

            EditorGUILayout.EndVertical();
        }

        private void DrawModeButtons()
        {
            bool isAll = fetchAllScenes;

            EditorGUILayout.BeginHorizontal();
            bool newAll = GUILayout.Toggle(isAll, "All Scenes", "Button", GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();

            if (newAll != isAll)
            {
                fetchAllScenes = newAll;
                RefreshSceneList();
                SelectCurrentScene();
#if UNITY_6000_3_OR_NEWER
                RefreshMainToolbar();
#else
                RepaintToolbar();
#endif
            }
        }

        private void DrawSceneList()
        {
            EditorGUILayout.Space(4);

            string listName = fetchAllScenes ? "All Scenes" : "Build-in Scenes";
            EditorGUILayout.LabelField(listName, EditorStyles.boldLabel);

            if (sceneNames == null || sceneNames.Length == 0)
            {
                EditorGUILayout.LabelField("No scenes available.");
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var sceneName in sceneNames)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;

                EditorGUILayout.BeginHorizontal();

                GUIStyle sceneBtnStyle = new GUIStyle(GUI.skin.button);
                sceneBtnStyle.fontSize = 13;

                string displayname = sceneName;
                if (displayname.Length > 15)
                {
                    displayname = displayname.Substring(0, 12) + "...";
                }

                if (GUILayout.Button(new GUIContent(displayname, sceneName), sceneBtnStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24)))
                {
                    LoadScene(sceneName);
#if UNITY_6000_3_OR_NEWER
                    RefreshMainToolbar();
#else
                    RepaintToolbar();
#endif
                    editorWindow.Close();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
