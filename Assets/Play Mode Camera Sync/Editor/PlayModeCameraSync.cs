#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PlayModeCameraSync
{
    [InitializeOnLoad]
    public class PlayModeCameraSync
    {
        private static bool m_IsEnabled = true;
        private static Camera m_TrackedCamera;
        private static GameObject m_FollowTarget = null;
        private static FollowMode m_FollowMode = FollowMode.Camera;
        private static ViewPreset m_CurrentViewPreset = ViewPreset.Free;
        private static float m_CameraViewDistance = 1f;
        private static float m_TransformViewDistance = 10f;
        private static bool m_UserIsRotating = false;
        private static bool m_EnableClickToFollow = true;
        private static bool m_ShowFollowIndicator = true;
        private static bool m_AutoRestoreTarget = true;

        private static string m_FollowTargetPath = "";
        private static int m_FollowTargetInstanceID = 0;

        private const string ENABLED_PREF = "PlayModeCameraSync_Enabled";
        private const string FOLLOW_MODE_PREF = "PlayModeCameraSync_FollowMode";
        private const string CAMERA_VIEW_DISTANCE_PREF = "PlayModeCameraSync_CameraViewDistance";
        private const string TRANSFORM_VIEW_DISTANCE_PREF = "PlayModeCameraSync_TransformViewDistance";
        private const string FOLLOW_TARGET_PATH_PREF = "PlayModeCameraSync_FollowTargetPath";
        private const string CLICK_TO_FOLLOW_PREF = "PlayModeCameraSync_ClickToFollow";
        private const string SHOW_INDICATOR_PREF = "PlayModeCameraSync_ShowIndicator";
        private const string AUTO_RESTORE_PREF = "PlayModeCameraSync_AutoRestore";

        public enum FollowMode
        {
            Camera,
            Transform
        }

        public enum ViewPreset
        {
            Free,
            Front,
            Back,
            Left,
            Right,
            Top,
            Bottom
        }

        static PlayModeCameraSync()
        {
            m_IsEnabled = EditorPrefs.GetBool(ENABLED_PREF, true);
            m_FollowMode = (FollowMode)EditorPrefs.GetInt(FOLLOW_MODE_PREF, 0);
            m_CameraViewDistance = EditorPrefs.GetFloat(CAMERA_VIEW_DISTANCE_PREF, 1f);
            m_TransformViewDistance = EditorPrefs.GetFloat(TRANSFORM_VIEW_DISTANCE_PREF, 10f);
            m_FollowTargetPath = EditorPrefs.GetString(FOLLOW_TARGET_PATH_PREF, "");
            m_EnableClickToFollow = EditorPrefs.GetBool(CLICK_TO_FOLLOW_PREF, true);
            m_ShowFollowIndicator = EditorPrefs.GetBool(SHOW_INDICATOR_PREF, true);
            m_AutoRestoreTarget = EditorPrefs.GetBool(AUTO_RESTORE_PREF, true);

            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (m_AutoRestoreTarget && !string.IsNullOrEmpty(m_FollowTargetPath))
                {
                    EditorApplication.delayCall += () => {
                        RestoreFollowTarget();
                    };
                }
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (m_FollowTarget != null)
                {
                    m_FollowTargetPath = GetGameObjectPath(m_FollowTarget);
                    m_FollowTargetInstanceID = m_FollowTarget.GetInstanceID();
                    EditorPrefs.SetString(FOLLOW_TARGET_PATH_PREF, m_FollowTargetPath);
                }
            }
        }

        private static void RestoreFollowTarget()
        {
            if (string.IsNullOrEmpty(m_FollowTargetPath))
                return;

            GameObject target = GameObject.Find(m_FollowTargetPath);
            if (target == null)
            {
                target = FindGameObjectByPath(m_FollowTargetPath);
            }

            if (target != null)
            {
                m_FollowTarget = target;
            }
            else
            {
                m_FollowTargetPath = "";
            }
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            string[] pathComponents = path.Split('/');
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            GameObject current = null;
            foreach (GameObject root in rootObjects)
            {
                if (root.name == pathComponents[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null)
                return null;

            for (int i = 1; i < pathComponents.Length; i++)
            {
                Transform child = current.transform.Find(pathComponents[i]);
                if (child == null)
                    return null;
                current = child.gameObject;
            }

            return current;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                m_UserIsRotating = true;
            }
            else if (e.type == EventType.MouseDrag && e.button == 1 && m_UserIsRotating)
            {
                if (m_CurrentViewPreset != ViewPreset.Free && m_FollowMode == FollowMode.Transform)
                {
                    m_CurrentViewPreset = ViewPreset.Free;
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 1)
            {
                m_UserIsRotating = false;
            }

            if (e.type == EventType.ScrollWheel && m_IsEnabled && EditorApplication.isPlaying)
            {
                float scrollDelta = e.delta.y;

                if (m_FollowMode == FollowMode.Camera)
                {
                    m_CameraViewDistance += scrollDelta * 0.1f;
                    m_CameraViewDistance = Mathf.Clamp(m_CameraViewDistance, 0.1f, 10f);
                    EditorPrefs.SetFloat(CAMERA_VIEW_DISTANCE_PREF, m_CameraViewDistance);
                }
                else
                {
                    m_TransformViewDistance += scrollDelta * 0.5f;
                    m_TransformViewDistance = Mathf.Clamp(m_TransformViewDistance, 1f, 50f);
                    EditorPrefs.SetFloat(TRANSFORM_VIEW_DISTANCE_PREF, m_TransformViewDistance);
                }

                sceneView.Repaint();
            }

            if (m_EnableClickToFollow &&
                e.type == EventType.MouseDown &&
                e.button == 0 &&
                e.control && e.shift)
            {
                GameObject pickedObject = HandleUtility.PickGameObject(e.mousePosition, false);

                if (pickedObject != null)
                {
                    SetFollowTarget(pickedObject);
                    e.Use();
                    sceneView.Repaint();
                }
            }

            if (m_EnableClickToFollow && e.control && e.shift && e.type == EventType.Repaint)
            {
                GameObject hoverObject = HandleUtility.PickGameObject(e.mousePosition, false);
                if (hoverObject != null)
                {
                    Handles.BeginGUI();
                    Vector2 mousePos = e.mousePosition;

                    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                    boxStyle.normal.background = Texture2D.whiteTexture;
                    boxStyle.normal.textColor = Color.white;

                    GUIContent content = new GUIContent($"  Click to follow: {hoverObject.name}  ");
                    Vector2 textSize = EditorStyles.boldLabel.CalcSize(content);
                    Rect boxRect = new Rect(mousePos.x + 15, mousePos.y, textSize.x + 10, textSize.y + 4);

                    Color bgColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                    EditorGUI.DrawRect(boxRect, bgColor);

                    GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = Color.cyan;
                    style.padding = new RectOffset(5, 5, 2, 2);
                    GUI.Label(boxRect, content, style);

                    Handles.EndGUI();

                    sceneView.Repaint();
                }
            }

            if (m_ShowFollowIndicator && m_FollowTarget != null && m_FollowMode == FollowMode.Transform && m_IsEnabled && EditorApplication.isPlaying)
            {
                Handles.color = new Color(0.3f, 0.7f, 1f, 0.8f);
                Handles.DrawWireCube(m_FollowTarget.transform.position, Vector3.one * 0.5f);

                Handles.BeginGUI();
                Vector3 screenPos = sceneView.camera.WorldToScreenPoint(m_FollowTarget.transform.position);
                if (screenPos.z > 0)
                {
                    Vector2 guiPos = HandleUtility.WorldToGUIPoint(m_FollowTarget.transform.position);
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    labelStyle.normal.textColor = new Color(0.3f, 0.7f, 1f, 1f);

                    string viewMode = m_CurrentViewPreset == ViewPreset.Free ? "Free" : m_CurrentViewPreset.ToString();
                    GUI.Label(new Rect(guiPos.x + 10, guiPos.y - 10, 200, 20), $"Following: {m_FollowTarget.name} ({viewMode})", labelStyle);
                }
                Handles.EndGUI();
            }
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying || !m_IsEnabled)
            {
                m_TrackedCamera = null;
                return;
            }

            if (m_FollowMode == FollowMode.Transform && m_FollowTarget != null)
            {
                SyncToTransform(m_FollowTarget.transform);
            }
            else if (m_FollowMode == FollowMode.Camera)
            {
                if (m_TrackedCamera == null || !m_TrackedCamera.gameObject.activeInHierarchy)
                {
                    m_TrackedCamera = Camera.main;
                    if (m_TrackedCamera == null)
                    {
                        m_TrackedCamera = Object.FindFirstObjectByType<Camera>();
                    }
                    if (m_TrackedCamera == null)
                    {
                        return;
                    }
                }

                Camera currentMain = Camera.main;
                if (currentMain != null && currentMain != m_TrackedCamera && currentMain.gameObject.activeInHierarchy)
                {
                    m_TrackedCamera = currentMain;
                }

                SyncSceneViewCamera();
            }
        }

        private static void SyncSceneViewCamera()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (sceneView != null && m_TrackedCamera != null && m_FollowMode == FollowMode.Camera)
            {
                sceneView.pivot = m_TrackedCamera.transform.position;
                sceneView.rotation = m_TrackedCamera.transform.rotation;

                if (m_TrackedCamera.orthographic)
                {
                    sceneView.orthographic = true;
                    sceneView.size = m_TrackedCamera.orthographicSize;
                }
                else
                {
                    sceneView.orthographic = false;
                    float fov = m_TrackedCamera.fieldOfView;
                    float size = m_CameraViewDistance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                    sceneView.size = size;
                }

                sceneView.Repaint();
            }
        }


        private static void SyncToTransform(Transform target)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && target != null)
            {
                sceneView.pivot = target.position;


                    sceneView.size = m_TransformViewDistance;
                    sceneView.orthographic = false;

                if (m_CurrentViewPreset != ViewPreset.Free)
                {
                    sceneView.LookAt(target.position, GetViewPresetRotation(m_CurrentViewPreset), m_TransformViewDistance);
                }

                sceneView.Repaint();
            }
        }

        private static Quaternion GetViewPresetRotation(ViewPreset preset)
        {
            switch (preset)
            {
                case ViewPreset.Front: return Quaternion.Euler(0, 180, 0);
                case ViewPreset.Back: return Quaternion.Euler(0, 0, 0);
                case ViewPreset.Left: return Quaternion.Euler(0, 90, 0);
                case ViewPreset.Right: return Quaternion.Euler(0, -90, 0);
                case ViewPreset.Top: return Quaternion.Euler(90, 0, 0);
                case ViewPreset.Bottom: return Quaternion.Euler(-90, 0, 0);
                default: return Quaternion.identity;
            }
        }

        public static bool IsEnabled
        {
            get { return m_IsEnabled; }
        }

        public static FollowMode CurrentFollowMode
        {
            get { return m_FollowMode; }
        }

        public static ViewPreset CurrentViewPreset
        {
            get { return m_CurrentViewPreset; }
        }

        public static GameObject FollowTarget
        {
            get { return m_FollowTarget; }
            set { m_FollowTarget = value; }
        }

        public static float CameraViewDistance
        {
            get { return m_CameraViewDistance; }
        }

        public static float TransformViewDistance
        {
            get { return m_TransformViewDistance; }
        }

        public static bool EnableClickToFollow
        {
            get { return m_EnableClickToFollow; }
            set
            {
                m_EnableClickToFollow = value;
                EditorPrefs.SetBool(CLICK_TO_FOLLOW_PREF, value);
            }
        }

        public static bool ShowFollowIndicator
        {
            get { return m_ShowFollowIndicator; }
            set
            {
                m_ShowFollowIndicator = value;
                EditorPrefs.SetBool(SHOW_INDICATOR_PREF, value);
            }
        }

        public static bool AutoRestoreTarget
        {
            get { return m_AutoRestoreTarget; }
            set
            {
                m_AutoRestoreTarget = value;
                EditorPrefs.SetBool(AUTO_RESTORE_PREF, value);
            }
        }

        public static void ToggleEnabledFromButton()
        {
            m_IsEnabled = !m_IsEnabled;
            EditorPrefs.SetBool(ENABLED_PREF, m_IsEnabled);

            if (!m_IsEnabled)
            {
                m_TrackedCamera = null;
            }
        }

        public static void SetFollowMode(FollowMode mode)
        {
            m_FollowMode = mode;
            EditorPrefs.SetInt(FOLLOW_MODE_PREF, (int)mode);

            if (mode == FollowMode.Camera)
            {
                m_TrackedCamera = null;
            }
        }

        public static void SetViewPreset(ViewPreset preset)
        {
            m_CurrentViewPreset = preset;
        }

        public static void SetFollowTarget(GameObject target)
        {
            m_FollowTarget = target;
            m_FollowMode = FollowMode.Transform;
            EditorPrefs.SetInt(FOLLOW_MODE_PREF, (int)FollowMode.Transform);
            if (target != null)
            {
                m_FollowTargetPath = GetGameObjectPath(target);
                m_FollowTargetInstanceID = target.GetInstanceID();
                EditorPrefs.SetString(FOLLOW_TARGET_PATH_PREF, m_FollowTargetPath);
            }
            var windows = Resources.FindObjectsOfTypeAll<PlayModeCameraSyncButton>();
            foreach (var win in windows)
            {
                win.Repaint();
            }
        }

        public static void ClearFollowTarget()
        {
            m_FollowTarget = null;
            m_FollowTargetPath = "";
            m_FollowTargetInstanceID = 0;
            m_FollowMode = FollowMode.Camera;
            m_CurrentViewPreset = ViewPreset.Free;
            EditorPrefs.SetInt(FOLLOW_MODE_PREF, (int)FollowMode.Camera);
            EditorPrefs.SetString(FOLLOW_TARGET_PATH_PREF, "");
        }
    }

    public class PlayModeCameraSyncButton : EditorWindow
    {
        private int m_dragControlId;
        private bool m_isDragging;
        private Vector2 m_dragStartScreenPos;
        private Rect m_dragStartRect;

        private const float BUTTON_WIDTH = 36f;
        private const float BUTTON_HEIGHT = 24f;
        private const float BUTTON_SPACING = 4f;
        private const float WINDOW_PADDING = 6f;
        private const float DRAG_HANDLE_HEIGHT = 12f;
        private const float TARGET_FIELD_WIDTH = 150f;
        private const float DROPDOWN_WIDTH = 80f;

        private const string WAS_OPEN_PREF = "PlayModeCameraSync.WasOpen";

        private static string[] s_ViewPresetNames = new string[]
        {
            "Free", "Front", "Back", "Left", "Right", "Top", "Bottom"
        };

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            bool wasOpen = EditorPrefs.GetBool(WAS_OPEN_PREF, false);

            if (wasOpen)
            {
                EditorApplication.delayCall += () => {
                    ShowWindow();
                };
            }
        }

        [MenuItem("Tools/Play Mode Camera Sync/Toggle Overlay", false, 1)]
        public static void ShowWindow()
        {
            var existingWindows = Resources.FindObjectsOfTypeAll<PlayModeCameraSyncButton>();
            if (existingWindows.Length > 0)
            {
                foreach (var w in existingWindows)
                {
                    w.Close();
                }
                return;
            }
            var win = CreateInstance<PlayModeCameraSyncButton>();
            win.titleContent = new GUIContent("Camera Sync");

            float initialWidth = (BUTTON_WIDTH * 2) + BUTTON_SPACING + (WINDOW_PADDING * 2);
            float initialHeight = DRAG_HANDLE_HEIGHT + (WINDOW_PADDING * 2) + BUTTON_HEIGHT;

            float posX;
            float posY;
            if (EditorPrefs.HasKey("PlayModeCameraSync.Button.X") &&
                EditorPrefs.HasKey("PlayModeCameraSync.Button.Y"))
            {
                posX = EditorPrefs.GetFloat("PlayModeCameraSync.Button.X");
                posY = EditorPrefs.GetFloat("PlayModeCameraSync.Button.Y");
                if (posX <= 0 || posY <= 0)
                {
                    posX = -1;
                    posY = -1;
                }
            }
            else
            {
                posX = -1;
                posY = -1;
            }
            if (posX < 0)
            {
                posX = (Screen.currentResolution.width - initialWidth) * 0.5f;
                posY = (Screen.currentResolution.height - initialHeight) * 0.5f;
            }

            var pos = new Rect(posX, posY, initialWidth, initialHeight);

            win.minSize = new Vector2(50, initialHeight);
            win.maxSize = new Vector2(500, initialHeight);
            win.ShowPopup();
            win.position = pos;
        }

        [MenuItem("Tools/Play Mode Camera Sync/Settings", false, 2)]
        public static void ShowSettingsMenuItem()
        {
            PlayModeCameraSyncSettings.ShowWindow();
        }

        private void OnEnable()
        {
            EditorPrefs.SetBool(WAS_OPEN_PREF, true);
        }

        private void OnDisable()
        {
            EditorPrefs.SetFloat("PlayModeCameraSync.Button.X", position.x);
            EditorPrefs.SetFloat("PlayModeCameraSync.Button.Y", position.y);
            EditorPrefs.SetBool(WAS_OPEN_PREF, false);
        }

        private void OnGUI()
        {
            m_dragControlId = GUIUtility.GetControlID(FocusType.Passive);

            HandleCloseButton();
            HandleDragging();

            float windowWidth = CalculateWindowWidth();
            float windowHeight = DRAG_HANDLE_HEIGHT + (WINDOW_PADDING * 2) + BUTTON_HEIGHT;

            minSize = new Vector2(50, windowHeight);
            maxSize = new Vector2(500, windowHeight);

            if (!m_isDragging)
            {
                var correctedPos = position;
                correctedPos.width = windowWidth;
                correctedPos.height = windowHeight;
                if (Mathf.Abs(position.width - correctedPos.width) > 0.1f ||
                    Mathf.Abs(position.height - correctedPos.height) > 0.1f)
                {
                    position = correctedPos;
                }
            }

            DrawBackground();
            DrawDragHandle();
            DrawButtons();
            if (Event.current.type == EventType.Repaint)
            {
                EditorPrefs.SetFloat("PlayModeCameraSync.Button.X", position.x);
                EditorPrefs.SetFloat("PlayModeCameraSync.Button.Y", position.y);
            }

            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private float CalculateWindowWidth()
        {
            var followMode = PlayModeCameraSync.CurrentFollowMode;
            var followTarget = PlayModeCameraSync.FollowTarget;

            float baseWidth = (BUTTON_WIDTH * 2) + BUTTON_SPACING + (WINDOW_PADDING * 2);

            if (followMode == PlayModeCameraSync.FollowMode.Transform)
            {
                baseWidth += TARGET_FIELD_WIDTH + BUTTON_SPACING;
                if (followTarget != null)
                {
                    baseWidth += DROPDOWN_WIDTH + BUTTON_SPACING;
                }
            }

            return baseWidth;
        }

        private void HandleCloseButton()
        {
            var e = Event.current;
            var closeRect = new Rect(position.width - 14f, 1f, 12f, 10f);

            if (e.type == EventType.MouseDown && e.button == 0 && closeRect.Contains(e.mousePosition))
            {
                Close();
                e.Use();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawDragHandle()
        {
            var handleRect = new Rect(0, 0, position.width, DRAG_HANDLE_HEIGHT);

            var handleColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.68f, 0.68f, 0.68f, 1f);
            EditorGUI.DrawRect(handleRect, handleColor);

            var gripColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.3f)
                : new Color(0f, 0f, 0f, 0.3f);

            float centerX = position.width / 2f;
            float centerY = DRAG_HANDLE_HEIGHT / 2f;

            for (int i = -2; i <= 2; i++)
            {
                EditorGUI.DrawRect(new Rect(centerX + (i * 4f) - 1f, centerY - 1f, 2f, 2f), gripColor);
            }

            var closeRect = new Rect(position.width - 14f, 1f, 12f, 10f);
            var closeColor = closeRect.Contains(Event.current.mousePosition)
                ? (EditorGUIUtility.isProSkin ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.8f, 0.2f, 0.2f, 1f))
                : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.5f));

            var style = new GUIStyle(EditorStyles.miniLabel);
            style.fontSize = 9;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = closeColor;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(closeRect, "✕", style);
        }

        private void DrawBackground()
        {
            var bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
            EditorGUI.DrawRect(new Rect(0, 0, position.width, DRAG_HANDLE_HEIGHT), new Color(0, 0, 0, 0));
            float buttonAreaTop = DRAG_HANDLE_HEIGHT;
            float buttonAreaHeight = WINDOW_PADDING * 2 + BUTTON_HEIGHT;
            EditorGUI.DrawRect(new Rect(0, buttonAreaTop, position.width, buttonAreaHeight), bgColor);
            var borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.1f, 0.1f, 0.1f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(0, position.height - 1, position.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(0, 0, 1, position.height), borderColor);
            EditorGUI.DrawRect(new Rect(position.width - 1, 0, 1, position.height), borderColor);
        }

        private void HandleDragging()
        {
            var e = Event.current;
            var dragRect = new Rect(0, 0, position.width, DRAG_HANDLE_HEIGHT);

            switch (e.GetTypeForControl(m_dragControlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && dragRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = m_dragControlId;
                        m_isDragging = true;
                        m_dragStartScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                        m_dragStartRect = position;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == m_dragControlId && m_isDragging)
                    {
                        Vector2 currentScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                        Vector2 delta = currentScreenPos - m_dragStartScreenPos;

                        var newPos = m_dragStartRect;
                        newPos.position += delta;
                        position = newPos;

                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == m_dragControlId)
                    {
                        GUIUtility.hotControl = 0;
                        m_isDragging = false;
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawButtons()
        {
            bool isEnabled = PlayModeCameraSync.IsEnabled;
            bool isInPlayMode = EditorApplication.isPlaying;
            var followMode = PlayModeCameraSync.CurrentFollowMode;
            var currentPreset = PlayModeCameraSync.CurrentViewPreset;
            var followTarget = PlayModeCameraSync.FollowTarget;

            float startX = WINDOW_PADDING;
            float buttonY = DRAG_HANDLE_HEIGHT + WINDOW_PADDING;

            Rect lockButtonRect = new Rect(startX, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
            DrawIconButton(lockButtonRect, isEnabled, isInPlayMode, followMode, ButtonType.Lock);

            startX += BUTTON_WIDTH + BUTTON_SPACING;
            Rect modeButtonRect = new Rect(startX, buttonY, BUTTON_WIDTH, BUTTON_HEIGHT);
            DrawIconButton(modeButtonRect, isEnabled, isInPlayMode, followMode, ButtonType.Mode);

            startX += BUTTON_WIDTH + BUTTON_SPACING;

            if (followMode == PlayModeCameraSync.FollowMode.Transform)
            {
                startX += BUTTON_SPACING;
                Rect targetFieldRect = new Rect(startX, buttonY, TARGET_FIELD_WIDTH, BUTTON_HEIGHT);
                Color fieldBg = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
                EditorGUI.DrawRect(targetFieldRect, fieldBg);
                DrawButtonBorder(targetFieldRect);

                EditorGUI.BeginChangeCheck();
                GameObject newTarget = (GameObject)EditorGUI.ObjectField(
                    targetFieldRect,
                    (UnityEngine.Object)followTarget,
                    typeof(GameObject),
                    true
                );

                if (EditorGUI.EndChangeCheck())
                {
                    if (newTarget != null)
                    {
                        PlayModeCameraSync.SetFollowTarget(newTarget);
                    }
                    else
                    {
                        PlayModeCameraSync.ClearFollowTarget();
                    }
                }

                startX += TARGET_FIELD_WIDTH;

                if (followTarget != null)
                {
                    startX += BUTTON_SPACING;
                    Rect dropdownRect = new Rect(startX, buttonY, DROPDOWN_WIDTH, BUTTON_HEIGHT);
                    Color dropdownBg = EditorGUIUtility.isProSkin ? new Color(0.28f, 0.28f, 0.28f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);
                    EditorGUI.DrawRect(dropdownRect, dropdownBg);
                    DrawButtonBorder(dropdownRect);

                    int currentIndex = (int)currentPreset;

                    GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
                    popupStyle.fontSize = 9;
                    popupStyle.padding = new RectOffset(4, 18, 4, 4);
                    popupStyle.alignment = TextAnchor.MiddleLeft;
                    popupStyle.fixedHeight = BUTTON_HEIGHT;

                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUI.Popup(dropdownRect, currentIndex, s_ViewPresetNames, popupStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayModeCameraSync.SetViewPreset((PlayModeCameraSync.ViewPreset)newIndex);
                    }
                }
            }
        }

        private enum ButtonType
        {
            Lock,
            Mode
        }

        private void DrawIconButton(Rect buttonRect, bool isEnabled, bool isInPlayMode, PlayModeCameraSync.FollowMode followMode, ButtonType buttonType)
        {
            Color buttonColor;

            switch (buttonType)
            {
                case ButtonType.Lock:
                    if (isEnabled)
                    {
                        buttonColor = new Color(0.2f, 0.6f, 0.2f, 1f);
                    }
                    else
                    {
                        buttonColor = new Color(0.6f, 0.2f, 0.2f, 1f);
                    }
                    break;

                case ButtonType.Mode:
                    buttonColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f);
                    break;

                default:
                    buttonColor = Color.gray;
                    break;
            }

            if (buttonRect.Contains(Event.current.mousePosition))
            {
                buttonColor = new Color(buttonColor.r + 0.15f, buttonColor.g + 0.15f, buttonColor.b + 0.15f, 1f);
            }
            EditorGUI.DrawRect(buttonRect, buttonColor);
            GUIContent icon = null;

            switch (buttonType)
            {
                case ButtonType.Lock:
                    icon = isEnabled
                        ? EditorGUIUtility.IconContent("IN LockButton on")
                        : EditorGUIUtility.IconContent("IN LockButton");
                    break;

                case ButtonType.Mode:
                    icon = followMode == PlayModeCameraSync.FollowMode.Camera
                        ? EditorGUIUtility.IconContent("Camera Icon")
                        : EditorGUIUtility.IconContent("Transform Icon");
                    break;
            }

            if (icon?.image != null)
            {
                float iconSize = 16f;
                float iconX = buttonRect.x + (buttonRect.width - iconSize) * 0.5f;
                float iconY = buttonRect.y + (buttonRect.height - iconSize) * 0.5f;

                GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), icon.image);
            }
            DrawButtonBorder(buttonRect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && buttonRect.Contains(Event.current.mousePosition))
            {
                switch (buttonType)
                {
                    case ButtonType.Lock:
                        PlayModeCameraSync.ToggleEnabledFromButton();
                        break;

                    case ButtonType.Mode:
                        if (followMode == PlayModeCameraSync.FollowMode.Camera)
                        {
                            PlayModeCameraSync.SetFollowMode(PlayModeCameraSync.FollowMode.Transform);
                        }
                        else
                        {
                            PlayModeCameraSync.SetFollowMode(PlayModeCameraSync.FollowMode.Camera);
                        }
                        break;
                }

                Event.current.Use();
            }

            if (buttonRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
            }
        }

        private void DrawButtonBorder(Rect rect)
        {
            var borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.1f, 0.1f, 0.1f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), borderColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), borderColor);
        }
    }

    public class PlayModeCameraSyncSettings : EditorWindow
    {
        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<PlayModeCameraSyncSettings>("Camera Sync Settings");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Play Mode Camera Sync Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            bool clickToFollow = EditorGUILayout.Toggle(new GUIContent("Enable Ctrl+Shift+Click",
                "Hold Ctrl+Shift and click an object in Scene view to follow it"), PlayModeCameraSync.EnableClickToFollow);
            if (EditorGUI.EndChangeCheck())
            {
                PlayModeCameraSync.EnableClickToFollow = clickToFollow;
            }

            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            bool showIndicator = EditorGUILayout.Toggle(new GUIContent("Show Follow Indicator",
                "Display wireframe and label around followed object"), PlayModeCameraSync.ShowFollowIndicator);
            if (EditorGUI.EndChangeCheck())
            {
                PlayModeCameraSync.ShowFollowIndicator = showIndicator;
            }

            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            bool autoRestore = EditorGUILayout.Toggle(new GUIContent("Auto-Restore Target",
                "Automatically restore follow target when entering play mode"), PlayModeCameraSync.AutoRestoreTarget);
            if (EditorGUI.EndChangeCheck())
            {
                PlayModeCameraSync.AutoRestoreTarget = autoRestore;
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sync Enabled:", PlayModeCameraSync.IsEnabled ? "Yes" : "No");
            EditorGUILayout.LabelField("Follow Mode:", PlayModeCameraSync.CurrentFollowMode.ToString());
            if (PlayModeCameraSync.FollowTarget != null)
            {
                EditorGUILayout.LabelField("Following:", PlayModeCameraSync.FollowTarget.name);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);

            GUIStyle centeredStyle = new GUIStyle(EditorStyles.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            centeredStyle.wordWrap = true;

            EditorGUILayout.LabelField("Play Mode Camera Sync", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Version 1.0.0", centeredStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("© 2026 Cheeky Chops Labs", centeredStyle);
            GUILayout.Space(3);

            GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.3f, 0.6f, 1f);
            linkStyle.alignment = TextAnchor.MiddleCenter;

            if (GUILayout.Button("cheekychopslabs@gmail.com", linkStyle))
            {
                Application.OpenURL("mailto:cheekychopslabs@gmail.com");
            }

            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif