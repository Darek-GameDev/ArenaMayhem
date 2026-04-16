using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Quản lý tất cả khía cạnh camera cho third-person game
/// - Orbital follow với collision detection
/// - Smooth tracking
/// - Distance management
/// </summary>
[DisallowMultipleComponent]
public class ThirdPersonCameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineFollow followComponent;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private Transform playerTransform;

    [Header("Orbital Follow")]
    [SerializeField] private float normalRadius = 4f;
    [SerializeField] private Vector3 normalTargetOffset = new Vector3(0f, 1.13f, 0f);
    [SerializeField] private float aimRadius = 2.4f;
    [SerializeField] private Vector3 aimTargetOffset = new Vector3(0.2f, 1.45f, 0f);
    [SerializeField] private float knightBlockRadius = 1.95f;
    [SerializeField] private Vector3 knightBlockTargetOffset = new Vector3(0f, 1.55f, 0.08f);
    [SerializeField] private float blendSpeed = 8f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionCheckDistance = 0.5f;
    [SerializeField] private float minDistanceWhenColliding = 1f;

    [Header("Camera Smoothing")]
    [SerializeField] private bool enablePositionSmoothing = false;
    [SerializeField] private float positionSmoothingDamping = 0.1f;

    [Header("Look Sensitivity")]
    [SerializeField] private float normalLookSensitivity = 1f;
    [SerializeField] private float aimLookSensitivity = 0.55f;

    private float targetDistance;
    private float currentDistance;
    private Vector3 velocity = Vector3.zero;
    private bool isAiming;
    private bool isBlocking;
    private bool isKnight;

    private void Awake()
    {
        ResolveReferences();
        InitializeValues();
    }

    private void Update()
    {
        if (!IsLocalAuthority())
            return;

        HandleAimStateChanges();
        ApplyLookSensitivity();
    }

    private void LateUpdate()
    {
        if (!IsLocalAuthority() || !IsValid())
            return;

        HandleCollisionDetection();
        ApplyRadiusTransition();
        ApplySmoothing();
    }

    #region COLLISION DETECTION
    private void HandleCollisionDetection()
    {
        // Ray từ player đến camera
        Vector3 cameraDirection = (cinemachineCamera.transform.position - playerTransform.position).normalized;
        float baseDistance = GetCurrentBaseDistance();
        float desiredDistance = Mathf.Max(0.1f, baseDistance);

        // Raycast để phát hiện tường/vật cản
        if (Physics.Raycast(playerTransform.position, cameraDirection, out RaycastHit hit, desiredDistance, collisionLayers))
        {
            // Camera chạm vào vật, rút lại
            targetDistance = Mathf.Max(minDistanceWhenColliding, hit.distance - collisionCheckDistance);
        }
        else
        {
            // Không có vật cản, trở lại khoảng cách bình thường
            targetDistance = desiredDistance;
        }
    }

    private void ApplyRadiusTransition()
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, blendSpeed) * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, t);

        if (orbitalFollow != null)
        {
            orbitalFollow.Radius = currentDistance;
        }
        else if (followComponent != null)
        {
            Vector3 offset = followComponent.FollowOffset;
            float signedZ = offset.z <= 0f ? -currentDistance : currentDistance;
            offset.z = signedZ;
            followComponent.FollowOffset = offset;
        }

        // Also blend target offset
        Vector3 targetOffset = GetCurrentTargetOffset();
        if (rotationComposer != null)
        {
            rotationComposer.TargetOffset = Vector3.Lerp(rotationComposer.TargetOffset, targetOffset, t);
        }
    }

    private void ApplySmoothing()
    {
        if (!enablePositionSmoothing)
            return;

        // Follow mode already has its own damping. Extra transform smoothing can cause jitter.
        if (followComponent != null)
            return;

        Vector3 targetPosition = playerTransform.position;
        cinemachineCamera.transform.position = Vector3.SmoothDamp(
            cinemachineCamera.transform.position,
            targetPosition,
            ref velocity,
            positionSmoothingDamping
        );
    }
    #endregion

    #region AIM HANDLING
    private void HandleAimStateChanges()
    {
        bool wasAiming = isAiming;

        SharedModePlayerController sharedController = GetComponent<SharedModePlayerController>();
        if (sharedController != null)
        {
            isAiming = sharedController.IsAiming;
            isBlocking = sharedController.IsBlocking;
            isKnight = sharedController.WeaponType == SharedModePlayerController.PlayerWeaponType.Sword;
        }
        else if (GetComponent<PlayerAttack>() != null)
        {
            isAiming = GetComponent<PlayerAttack>().IsAiming;
            isBlocking = false;
            isKnight = false;
        }

        // Aim state changed
        if (wasAiming != isAiming)
        {
            OnAimStateChanged(isAiming);
        }
    }

    private void OnAimStateChanged(bool nowAiming)
    {
        // Can trigger effects here if needed
    }

    private void ApplyLookSensitivity()
    {
        float targetSensitivity = isAiming ? aimLookSensitivity : normalLookSensitivity;
        // Cinemachine 3.x handles input through InputVector automatically
        // Sensitivity is managed through the camera's input properties
    }
    #endregion

    #region HELPER METHODS
    private void ResolveReferences()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        if (orbitalFollow == null && cinemachineCamera != null)
        {
            orbitalFollow = cinemachineCamera.GetComponentInChildren<CinemachineOrbitalFollow>();
        }

        if (followComponent == null && cinemachineCamera != null)
        {
            followComponent = cinemachineCamera.GetComponentInChildren<CinemachineFollow>();
        }

        if (rotationComposer == null && cinemachineCamera != null)
        {
            rotationComposer = cinemachineCamera.GetComponentInChildren<CinemachineRotationComposer>();
        }

        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (collisionLayers == 0)
        {
            collisionLayers = LayerMask.GetMask("Default");
        }
    }

    private void InitializeValues()
    {
        currentDistance = GetCurrentBaseDistance();
        targetDistance = currentDistance;
    }

    private bool IsValid()
    {
        bool hasPositionControl = orbitalFollow != null || followComponent != null;
        return cinemachineCamera != null && playerTransform != null && hasPositionControl;
    }

    private float GetCurrentBaseDistance()
    {
        if (isKnight && isBlocking)
        {
            return knightBlockRadius;
        }

        if (orbitalFollow != null)
        {
            return isAiming ? aimRadius : normalRadius;
        }

        if (followComponent != null)
        {
            return Mathf.Abs(followComponent.FollowOffset.z);
        }

        return isAiming ? aimRadius : normalRadius;
    }

    private Vector3 GetCurrentTargetOffset()
    {
        if (isKnight && isBlocking)
        {
            return knightBlockTargetOffset;
        }

        return isAiming ? aimTargetOffset : normalTargetOffset;
    }

    private bool IsLocalAuthority()
    {
        var sharedMode = GetComponent<SharedModePlayerController>();
        if (sharedMode == null)
            return true;

        return sharedMode.Object != null && sharedMode.Object.HasInputAuthority;
    }
    #endregion

    #region PUBLIC API
    public void SetCameraDistance(float distance)
    {
        normalRadius = distance;
    }

    public void SetAimDistance(float distance)
    {
        aimRadius = distance;
    }

    public float GetCurrentDistance()
    {
        return currentDistance;
    }

    public bool IsAiming()
    {
        return isAiming;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null || cinemachineCamera == null)
            return;

        Gizmos.color = Color.red;
        Vector3 cameraDir = (cinemachineCamera.transform.position - playerTransform.position).normalized;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + cameraDir * (isAiming ? aimRadius : normalRadius));

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(playerTransform.position + cameraDir * currentDistance, 0.2f);
    }
}
