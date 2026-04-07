using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class PlayerAimCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack localAttack;
    [SerializeField] private SharedModePlayerController sharedModeController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;

    [Header("Blend")]
    [SerializeField] private float blendSpeed = 8f;

    [Header("Normal Camera")]
    [SerializeField] private float normalRadius = 4f;
    [SerializeField] private Vector3 normalTargetOffset = new Vector3(0f, 1.13f, 0f);

    [Header("Aim Camera")]
    [SerializeField] private float aimRadius = 2.4f;
    [SerializeField] private Vector3 aimTargetOffset = new Vector3(0.45f, 1.45f, 0f);

    private void Awake()
    {
        if (localAttack == null)
        {
            localAttack = GetComponent<PlayerAttack>();
        }

        if (sharedModeController == null)
        {
            sharedModeController = GetComponent<SharedModePlayerController>();
        }

        if (cinemachineCamera == null || orbitalFollow == null || rotationComposer == null)
        {
            ResolveCameraComponents();
        }
    }

    private void Update()
    {
        if (!IsLocalAuthority())
        {
            return;
        }

        if (orbitalFollow == null || rotationComposer == null)
        {
            return;
        }

        bool isAiming = IsAimingNow();
        float targetRadius = isAiming ? aimRadius : normalRadius;
        Vector3 targetOffset = isAiming ? aimTargetOffset : normalTargetOffset;
        float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, blendSpeed) * Time.deltaTime);

        orbitalFollow.Radius = Mathf.Lerp(orbitalFollow.Radius, targetRadius, t);
        rotationComposer.TargetOffset = Vector3.Lerp(rotationComposer.TargetOffset, targetOffset, t);
    }

    private bool IsAimingNow()
    {
        if (sharedModeController != null)
        {
            return sharedModeController.IsAiming;
        }

        return localAttack != null && localAttack.IsAiming;
    }

    private bool IsLocalAuthority()
    {
        if (sharedModeController == null)
        {
            return true;
        }

        if (sharedModeController.Object == null)
        {
            return false;
        }

        return sharedModeController.Object.HasInputAuthority;
    }

    private void ResolveCameraComponents()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        if (orbitalFollow == null && cinemachineCamera != null)
        {
            orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        }

        if (rotationComposer == null && cinemachineCamera != null)
        {
            rotationComposer = cinemachineCamera.GetComponent<CinemachineRotationComposer>();
        }
    }
}
