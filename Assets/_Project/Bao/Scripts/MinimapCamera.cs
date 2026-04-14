using Fusion;
using UnityEngine;

/// <summary>
/// Makes the minimap camera follow the local player and rotate with them.
/// Assumes camera is already configured to render to RenderTexture.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private SharedModePlayerController controller;
    [SerializeField] private float resolveRetryInterval = 0.5f;
    [SerializeField] private float cameraHeight = 30f;

    private Camera _camera;
    private SharedModePlayerController _ownerController;
    private Transform _playerTransform;
    private float nextResolveTime;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _ownerController = GetComponentInParent<SharedModePlayerController>();
    }

    private void LateUpdate()
    {
        if (!CanRenderForThisInstance())
        {
            _playerTransform = null;
            return;
        }

        ResolveReferences();

        if (_playerTransform == null)
        {
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation();
    }

    private void ResolveReferences()
    {
        if (IsLocalController(controller))
        {
            _playerTransform = controller.transform;
            return;
        }

        if (Time.unscaledTime < nextResolveTime)
        {
            return;
        }

        nextResolveTime = Time.unscaledTime + Mathf.Max(0.1f, resolveRetryInterval);

        controller = FindLocalController();
        _playerTransform = controller != null ? controller.transform : null;
    }

    private bool IsLocalController(SharedModePlayerController candidate)
    {
        return candidate != null && candidate.Object != null && candidate.Object.HasInputAuthority;
    }

    private bool CanRenderForThisInstance()
    {
        if (_ownerController != null)
        {
            bool isLocalOwner = IsLocalController(_ownerController);
            if (_camera != null)
            {
                _camera.enabled = isLocalOwner;
            }

            return isLocalOwner;
        }

        if (_camera != null && !_camera.enabled)
        {
            _camera.enabled = true;
        }

        return true;
    }

    private SharedModePlayerController FindLocalController()
    {
        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        if (sessionManager != null && sessionManager.Runner != null && sessionManager.Runner.LocalPlayer.IsRealPlayer)
        {
            NetworkObject playerObject = sessionManager.Runner.GetPlayerObject(sessionManager.Runner.LocalPlayer);
            if (playerObject != null)
            {
                SharedModePlayerController localController = playerObject.GetComponent<SharedModePlayerController>();
                if (localController == null)
                {
                    localController = playerObject.GetComponentInChildren<SharedModePlayerController>(true);
                }

                if (IsLocalController(localController))
                {
                    return localController;
                }
            }
        }

        SharedModePlayerController[] players = Object.FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            SharedModePlayerController candidate = players[i];
            if (IsLocalController(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void UpdateCameraPosition()
    {
        Vector3 playerPos = _playerTransform.position;
        Vector3 newPos = new Vector3(playerPos.x, playerPos.y + cameraHeight, playerPos.z);
        transform.position = newPos;
    }

    private void UpdateCameraRotation()
    {
        // Rotate with player, but keep looking down (90 degrees pitch)
        float playerYRotation = _playerTransform.eulerAngles.y;
        transform.eulerAngles = new Vector3(90f, playerYRotation, 0f);
    }
}
