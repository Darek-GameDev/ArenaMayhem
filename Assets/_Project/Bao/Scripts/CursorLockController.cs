using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CursorLockController : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;
    [SerializeField] private bool lockWhenFocused = true;
    [SerializeField] private bool unlockWithEscape = true;

    private bool isActiveForLocalPlayer;

    private void Start()
    {
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        if (!isActiveForLocalPlayer)
        {
            return;
        }

        if (unlockWithEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
            return;
        }

        if (Mouse.current != null)
        {
            bool clickedGame = Mouse.current.leftButton.wasPressedThisFrame ||
                               Mouse.current.rightButton.wasPressedThisFrame ||
                               Mouse.current.middleButton.wasPressedThisFrame;

            if (clickedGame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (isActiveForLocalPlayer && lockWhenFocused && hasFocus)
        {
            LockCursor();
        }
    }

    private void OnDisable()
    {
        if (isActiveForLocalPlayer)
        {
            UnlockCursor();
        }
    }

    public void SetActiveForLocalPlayer(bool isActive)
    {
        isActiveForLocalPlayer = isActive;

        if (isActiveForLocalPlayer)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }

    public void LockCursor()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}