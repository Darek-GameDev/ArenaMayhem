using System;
using PlayModeChangesSaver.Editor.ChangesTracker;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Popup window for comparing original and current component/transform states side-by-side.
    ///     Delegates functionality to specialized helper classes.
    /// </summary>
    internal class OcpContent : PopupWindowContent
    {
        // Layout constants
        private const float MinWidth = 350f;
        private const float HeaderHeight = 24f;
        private const float BaseFooterHeight = 40f;
        private const float MaxWindowHeight = 400f;
        private const float MinWindowHeight = 250f;
        private readonly Component liveComponent;
        private readonly Action onRefreshRequest;
        private readonly bool openedFromBrowser;
        private float footerHeight = BaseFooterHeight;
        private OcpInteraction interactionHelper;
        private UnityEditor.Editor leftEditor;
        private float leftMaxScroll;
        private UnityEditor.Editor rightEditor;
        private float rightMaxScroll;

        // Scroll state
        private float scrollNormalized;

        private OcpSnapshot snapshotHelper;

        private float targetWindowHeight = -1f;

        public OcpContent(Component component, bool openedFromBrowser = false, Action onRefreshRequest = null)
        {
            liveComponent = component;
            this.openedFromBrowser = openedFromBrowser;
            this.onRefreshRequest = onRefreshRequest;
            InitializePopup();
        }

        private void InitializePopup()
        {
            snapshotHelper = new OcpSnapshot(liveComponent);
            interactionHelper = new OcpInteraction(
                liveComponent,
                snapshotHelper.SnapshotComponent
            );

            CreateEditors();

            bool showMaterialToggle = ShouldShowMaterialToggle();
            if (showMaterialToggle)
            {
                footerHeight += 18f;
            }
        }

        private void CreateEditors()
        {
            if (snapshotHelper.SnapshotComponent)
            {
                leftEditor = UnityEditor.Editor.CreateEditor(snapshotHelper.SnapshotComponent);
                rightEditor = UnityEditor.Editor.CreateEditor(liveComponent);
            }
            else
            {
            }
        }

        public override Vector2 GetWindowSize()
        {
            float h = targetWindowHeight < 0 ? MinWindowHeight : targetWindowHeight;
            return new Vector2(MinWidth * 2 + 6, h);
        }

        public override void OnGUI(Rect rect)
        {
            if (!leftEditor || !rightEditor)
            {
                return;
            }

            interactionHelper.HandleDragAndDrop(rect, editorWindow);

            // Dynamic size adjustment
            float extraSpaceNeeded = Mathf.Max(leftMaxScroll, rightMaxScroll);

            if (Event.current.type == EventType.Layout)
            {
                float desiredHeight = Mathf.Clamp(rect.height + extraSpaceNeeded, MinWindowHeight, MaxWindowHeight);

                if (Mathf.Abs(targetWindowHeight - desiredHeight) > 1f)
                {
                    targetWindowHeight = desiredHeight;
                    editorWindow.ShowAsDropDown(new Rect(editorWindow.position.position, Vector2.zero),
                        GetWindowSize());
                }
            }

            // Scroll handling
            bool needsScrolling = rect.height >= MaxWindowHeight - 1f && extraSpaceNeeded > 0.5f;
            HandleMouseWheel(rect, needsScrolling);

            // Layout setup
            float scrollbarWidth = needsScrolling ? 15f : 0f;
            float columnWidth = (rect.width - scrollbarWidth - 6) * 0.5f;
            float contentHeight = rect.height - footerHeight - HeaderHeight;

            OcpUI.DrawColumnHeader(new Rect(rect.x, rect.y, columnWidth, HeaderHeight), leftEditor.target, "Original");
            OcpUI.DrawColumnHeader(new Rect(rect.x + columnWidth + 6, rect.y, columnWidth, HeaderHeight),
                rightEditor.target, "Play Mode");

            Rect contentRect = new(rect.x, rect.y + HeaderHeight, rect.width, contentHeight);
            GUILayout.BeginArea(contentRect);
            GUILayout.BeginHorizontal();

            var leftContext =
                new OcpUI.ColumnRenderContext(columnWidth, contentHeight, scrollNormalized, leftMaxScroll);
            OcpUI.DrawSynchronizedColumn(leftEditor, ref leftContext, false);
            leftMaxScroll = leftContext.MaxScroll;

            OcpUI.DrawSeparator(new Rect(columnWidth, 0, 2, contentHeight));

            var rightContext =
                new OcpUI.ColumnRenderContext(columnWidth, contentHeight, scrollNormalized, rightMaxScroll);
            OcpUI.DrawSynchronizedColumn(rightEditor, ref rightContext, true);
            rightMaxScroll = rightContext.MaxScroll;

            if (needsScrolling)
            {
                Rect scrollbarRect = new(rect.width - 15, 0, 15, contentHeight);
                scrollNormalized = GUI.VerticalScrollbar(scrollbarRect, scrollNormalized, 0.1f, 0f, 1.0f);
            }
            else
            {
                scrollNormalized = 0f;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            DrawFooter(new Rect(rect.x, rect.y + rect.height - footerHeight, rect.width, footerHeight));
        }

        private void HandleMouseWheel(Rect rect, bool needsScrolling)
        {
            bool isScrollWheelOverRect = needsScrolling && rect.Contains(Event.current.mousePosition) &&
                                         Event.current.type == EventType.ScrollWheel;
            if (isScrollWheelOverRect)
            {
                scrollNormalized = Mathf.Clamp01(scrollNormalized + Event.current.delta.y * 0.05f);
                Event.current.Use();
            }
        }

        private void DrawFooter(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();

            bool hasUnsavedChanges = Application.isPlaying && interactionHelper.HasUnsavedChanges();
            bool hasSavedEntry = interactionHelper.HasSavedEntry();
            bool showMaterialToggle = ShouldShowMaterialToggle();

            DrawFooterLeftPanel(rect, hasUnsavedChanges, showMaterialToggle);
            GUILayout.FlexibleSpace();
            DrawFooterActionButtons(hasUnsavedChanges, hasSavedEntry);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawFooterLeftPanel(Rect rect, bool hasUnsavedChanges, bool showMaterialToggle)
        {
            GUILayout.BeginVertical();
            OcpUI.DrawFooter(rect, hasUnsavedChanges);
            GUILayout.Space(2);
            DrawMaterialToggle(showMaterialToggle);
            GUILayout.EndVertical();
        }

        private void DrawFooterActionButtons(bool hasUnsavedChanges, bool hasSavedEntry)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            DrawRevertToOriginalButton();
            GUILayout.Space(4);
            DrawRevertToSavedButton(hasSavedEntry);
            GUILayout.Space(8);
            DrawApplyButton(hasUnsavedChanges);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawMaterialToggle(bool showMaterialToggle)
        {
            if (showMaterialToggle)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Toggle(true, "Persist Material changes", GUILayout.Width(180f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRevertToOriginalButton()
        {
            DrawButtonWithAction("Revert to Original", 130f, false,
                () => interactionHelper.RevertToOriginal(openedFromBrowser));
        }

        private void DrawRevertToSavedButton(bool hasSavedEntry)
        {
            DrawButtonWithAction("Revert to Saved", 130f, !hasSavedEntry,
                () => interactionHelper.RevertToSaved(openedFromBrowser));
        }

        private void DrawApplyButton(bool hasUnsavedChanges)
        {
            DrawButtonWithAction("Apply", 120f, !hasUnsavedChanges,
                () => interactionHelper.ApplyChanges(openedFromBrowser));
        }

        private void DrawButtonWithAction(string label, float width, bool isDisabled, Action action)
        {
            EditorGUI.BeginDisabledGroup(isDisabled);
            if (GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(28f)))
            {
                action?.Invoke();
                onRefreshRequest?.Invoke();
                if (!openedFromBrowser)
                {
                    editorWindow.Close();
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        public override void OnClose()
        {
            if (leftEditor)
            {
                Object.DestroyImmediate(leftEditor);
            }

            if (rightEditor)
            {
                Object.DestroyImmediate(rightEditor);
            }

            snapshotHelper?.Cleanup();
        }

        private bool ShouldShowMaterialToggle()
        {
            if (liveComponent is Renderer renderer)
            {
                return ChangesTrackerCore.HasMaterialDelta(renderer);
            }

            return false;
        }
    }
}