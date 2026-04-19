using UnityEngine;

/// <summary>
/// Press F11 to toggle fullscreen at runtime.
/// Auto-created on load so no manual scene wiring is required.
/// </summary>
public class FullscreenToggleHotkey : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F11;

    private int windowedWidth;
    private int windowedHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<FullscreenToggleHotkey>() != null)
        {
            return;
        }

        GameObject host = new GameObject(nameof(FullscreenToggleHotkey));
        DontDestroyOnLoad(host);
        host.AddComponent<FullscreenToggleHotkey>();
    }

    private void Awake()
    {
        CacheWindowedSize();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        ToggleFullscreen();
    }

    private void ToggleFullscreen()
    {
        bool isFullscreen = Screen.fullScreen;

        if (!isFullscreen)
        {
            CacheWindowedSize();
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
            return;
        }

        if (windowedWidth <= 0 || windowedHeight <= 0)
        {
            windowedWidth = Mathf.Max(1280, Screen.currentResolution.width);
            windowedHeight = Mathf.Max(720, Screen.currentResolution.height);
        }

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(windowedWidth, windowedHeight, FullScreenMode.Windowed);
        Screen.fullScreen = false;
    }

    private void CacheWindowedSize()
    {
        if (Screen.fullScreen)
        {
            return;
        }

        windowedWidth = Mathf.Max(640, Screen.width);
        windowedHeight = Mathf.Max(360, Screen.height);
    }
}
