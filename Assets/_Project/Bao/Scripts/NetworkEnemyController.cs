using Fusion;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacterController))]
public class NetworkEnemyController : NetworkBehaviour
{
    public enum LocomotionState : byte
    {
        Idle = 0,
        Moving = 1,
    }

    public enum CombatState : byte
    {
        None = 0,
        Attacking = 1,
        Blocking = 2,
        Recovering = 3,
    }

    public enum EnemyCombatStyle : byte
    {
        Sword = 0,
        Bow = 1,
    }

    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyWeapon enemyWeapon;
    [SerializeField] private BowWeapon bowWeapon;

    [Header("Combat Style")]
    [SerializeField] private EnemyCombatStyle enemyCombatStyle = EnemyCombatStyle.Sword;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 3.8f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float attackRangeHysteresis = 0.45f;
    [SerializeField] private float disengageRange = 18f;
    [SerializeField] private float rotationSharpness = 14f;

    [Header("Patrol")]
    [SerializeField] private float patrolRadius = 7f;
    [SerializeField] private float patrolPauseSeconds = 1.2f;
    [SerializeField] private float patrolArriveDistance = 0.8f;

    [Header("Combat")]
    [SerializeField] private float attackCooldownSeconds = 1.1f;
    [SerializeField] private float recoverAfterAttackSeconds = 0.35f;
    [SerializeField] private int attackComboCount = 3;
    [SerializeField] private float rangedMinRetreatDistance = 4f;
    [SerializeField] private float rangedOptimalDistance = 6f;
    [SerializeField] private float rangedAttackRange = 8f;
    [SerializeField] private float rangedPreferredDistance = 6f;
    [SerializeField] private float rangedAttackCooldownSeconds = 1.35f;
    [SerializeField] private float rangedRecoverAfterAttackSeconds = 0.4f;
    [SerializeField] private float rangedAutoFireFallbackDelay = 0.3f;
    [SerializeField] private float blockDurationSeconds = 0.6f;
    [SerializeField] private float blockCooldownSeconds = 2f;
    [SerializeField, Range(0f, 1f)] private float blockChanceOnHit = 0.35f;
    [SerializeField] private float smartBlockThreatRange = 2.8f;
    [SerializeField, Range(-1f, 1f)] private float smartBlockFacingDot = 0.15f;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.3f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private float lineOfSightEyeHeight = 1.2f;
    [SerializeField] private float lineOfSightTargetHeight = 1.1f;
    [SerializeField] private float noLineOfSightToleranceSeconds = 1.2f;

    [Networked] public NetworkBool IsBlocking { get; set; }
    [Networked] public int AttackSequence { get; set; }
    [Networked] public byte ComboStep { get; set; }
    [Networked] public LocomotionState NetLocomotionState { get; set; }
    [Networked] public CombatState NetCombatState { get; set; }
    [Networked] public NetworkBool IsAiming { get; set; }

    [Networked] private float NextAttackAllowedAt { get; set; }
    [Networked] private float RecoverUntil { get; set; }
    [Networked] private float BlockUntil { get; set; }
    [Networked] private float NextBlockAllowedAt { get; set; }
    [Networked] private Vector3 SpawnOrigin { get; set; }
    [Networked] private Vector3 PatrolDestination { get; set; }
    [Networked] private NetworkBool HasPatrolDestination { get; set; }
    [Networked] private float NextPatrolDecisionAt { get; set; }

    private NetworkCharacterController cc;
    private SharedModePlayerController currentTarget;
    private NavMeshPath navPath;
    private float nextRetargetAt;
    private float nextRepathAt;
    private int lastObservedHitSequence = -1;
    private bool pendingRangedShot;
    private Vector3 pendingRangedAimPoint;
    private PlayerRef pendingRangedAttackerRef;
    private float pendingRangedFallbackFireAt;
    private float lostLineOfSightSince = -1f;

    public bool UsesBow => enemyCombatStyle == EnemyCombatStyle.Bow;

    public override void Spawned()
    {
        cc = GetComponent<NetworkCharacterController>();
        cc.maxSpeed = moveSpeed;
        cc.rotationSpeed = 0f;

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (enemyWeapon == null)
        {
            enemyWeapon = GetComponentInChildren<EnemyWeapon>(true);
        }

        if (enemyWeapon != null)
        {
            enemyWeapon.SetOwner(transform.root);
        }

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>(true);
        }

        navPath = new NavMeshPath();
        pendingRangedShot = false;

        if (HasStateAuthority)
        {
            SpawnOrigin = transform.position;
            IsBlocking = false;
            AttackSequence = 0;
            ComboStep = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextAttackAllowedAt = 0f;
            RecoverUntil = 0f;
            BlockUntil = 0f;
            NextBlockAllowedAt = 0f;
            PatrolDestination = SpawnOrigin;
            HasPatrolDestination = false;
            NextPatrolDecisionAt = 0f;
        }

        if (enemyHealth != null)
        {
            lastObservedHitSequence = enemyHealth.HitSequence;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            IsBlocking = false;
            IsAiming = false;
            NetCombatState = CombatState.None;
            NetLocomotionState = LocomotionState.Idle;
            cc.Move(Vector3.zero);
            return;
        }

        float simTime = (float)Runner.SimulationTime;

        if (pendingRangedShot && simTime >= pendingRangedFallbackFireAt)
        {
            ReleaseQueuedRangedShotFromAnimationEvent();
        }

        ObserveHitReaction(simTime);

        if (IsBlocking)
        {
            if (simTime >= BlockUntil)
            {
                IsBlocking = false;
                NetCombatState = CombatState.None;
            }
            else
            {
                NetCombatState = CombatState.Blocking;
                NetLocomotionState = LocomotionState.Idle;
                cc.Move(Vector3.zero);
                return;
            }
        }

        if (simTime < RecoverUntil)
        {
            NetCombatState = CombatState.Recovering;
            NetLocomotionState = LocomotionState.Idle;
            cc.Move(Vector3.zero);
            return;
        }

        ResolveTarget(simTime);

        IsAiming = false;

        if (currentTarget != null && !currentTarget.IsDead)
        {
            Vector3 targetPosition = currentTarget.transform.position;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (!UsesBow)
            {
                TrySmartBlockAgainstTarget(simTime, currentTarget, distance);
                if (IsBlocking)
                {
                    FaceToward(targetPosition);
                    NetLocomotionState = LocomotionState.Idle;
                    cc.Move(Vector3.zero);
                    return;
                }
            }

            if (distance > disengageRange)
            {
                currentTarget = null;
                StartPatrolIdle(simTime);
                return;
            }

            if (UsesBow)
            {
                HandleRangedCombat(simTime, targetPosition, distance);
                return;
            }

            if (distance <= attackRange)
            {
                FaceToward(targetPosition);
                TryAttack(simTime);
                return;
            }

            if (distance <= attackRange + attackRangeHysteresis)
            {
                FaceToward(targetPosition);
            }

            MoveTowards(targetPosition, simTime, chaseSpeed);
            return;
        }

        RunPatrol(simTime);
    }

    public void SetComboStepFromAnimationEvent(int step)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        ComboStep = (byte)Mathf.Clamp(step, 0, byte.MaxValue);
    }

    public void ResetComboStepFromAnimationEvent()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        ComboStep = 0;
    }

    private void ObserveHitReaction(float simTime)
    {
        if (enemyHealth == null)
        {
            return;
        }

        int sequence = enemyHealth.HitSequence;
        if (lastObservedHitSequence < 0)
        {
            lastObservedHitSequence = sequence;
            return;
        }

        if (sequence == lastObservedHitSequence)
        {
            return;
        }

        lastObservedHitSequence = sequence;

        float adaptiveChance = blockChanceOnHit;
        if (enemyHealth != null)
        {
            float healthRatio = (float)enemyHealth.Health / Mathf.Max(1f, enemyHealth.MaxHealth);
            if (healthRatio <= 0.45f)
            {
                adaptiveChance += 0.2f;
            }
        }

        if (currentTarget != null && currentTarget.NetCombatState == SharedModePlayerController.CombatState.Attacking)
        {
            adaptiveChance += 0.2f;
        }

        if (simTime >= NextBlockAllowedAt && Random.value <= Mathf.Clamp01(adaptiveChance))
        {
            StartBlock(simTime, blockDurationSeconds);
        }
    }

    private void ResolveTarget(float simTime)
    {
        if (simTime < nextRetargetAt)
        {
            if (currentTarget != null && !currentTarget.IsDead)
            {
                float currentDistance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (currentDistance <= disengageRange)
                {
                    return;
                }
            }
        }

        nextRetargetAt = simTime + retargetInterval;

        SharedModePlayerController bestTarget = null;
        float bestDistance = float.MaxValue;

        SharedModePlayerController[] players = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            SharedModePlayerController candidate = players[i];
            if (candidate == null || candidate.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance > chaseRange)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }

        currentTarget = bestTarget;
    }

    private void TryAttack(float simTime)
    {
        NetLocomotionState = LocomotionState.Idle;
        cc.Move(Vector3.zero);

        if (simTime < NextAttackAllowedAt)
        {
            NetCombatState = CombatState.None;
            return;
        }

        NextAttackAllowedAt = simTime + attackCooldownSeconds;
        RecoverUntil = simTime + recoverAfterAttackSeconds;

        NetCombatState = CombatState.Attacking;
        AttackSequence++;

        int comboCount = Mathf.Max(1, attackComboCount);
        ComboStep = (byte)((ComboStep + 1) % comboCount);
    }

    private void HandleRangedCombat(float simTime, Vector3 targetPosition, float distance)
    {
        bool hasLineOfSight = HasLineOfSightToTarget(targetPosition);
        if (!hasLineOfSight)
        {
            IsAiming = false;
            if (lostLineOfSightSince < 0f)
            {
                lostLineOfSightSince = simTime;
            }

            MoveTowards(targetPosition, simTime, chaseSpeed);
            return;
        }

        lostLineOfSightSince = -1f;

        if (distance > rangedAttackRange)
        {
            IsAiming = false;
            MoveTowards(targetPosition, simTime, chaseSpeed);
            return;
        }

        float retreatDistance = Mathf.Max(0.5f, rangedMinRetreatDistance);
        if (distance < retreatDistance)
        {
            IsAiming = false;
            MoveAwayFrom(targetPosition, simTime, chaseSpeed);
            return;
        }

        float holdDistance = Mathf.Max(retreatDistance + 0.25f, rangedOptimalDistance);
        if (distance > holdDistance)
        {
            IsAiming = false;
            MoveTowards(targetPosition, simTime, moveSpeed);
            return;
        }

        IsAiming = true;
        NetLocomotionState = LocomotionState.Idle;
        cc.Move(Vector3.zero);
        FaceToward(targetPosition);

        if (lostLineOfSightSince > 0f && simTime - lostLineOfSightSince > noLineOfSightToleranceSeconds)
        {
            return;
        }

        TryRangedAttack(simTime);
    }

    private void TryRangedAttack(float simTime)
    {
        NetLocomotionState = LocomotionState.Idle;
        cc.Move(Vector3.zero);

        if (simTime < NextAttackAllowedAt)
        {
            NetCombatState = CombatState.None;
            return;
        }

        NextAttackAllowedAt = simTime + rangedAttackCooldownSeconds;
        RecoverUntil = simTime + rangedRecoverAfterAttackSeconds;

        NetCombatState = CombatState.Attacking;
        AttackSequence++;
        ComboStep = 0;

        pendingRangedShot = true;
        pendingRangedAimPoint = GetRangedAimPoint();
        pendingRangedAttackerRef = default;
        pendingRangedFallbackFireAt = simTime + Mathf.Max(0.05f, rangedAutoFireFallbackDelay);
    }

    private void MoveTowards(Vector3 destination, float simTime, float speed, bool faceMoveTarget = true, Vector3 explicitFaceTarget = default)
    {
        NetCombatState = CombatState.None;

        Vector3 moveTarget = destination;

        if (simTime >= nextRepathAt)
        {
            nextRepathAt = simTime + repathInterval;
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, navPath) && navPath.corners.Length > 1)
            {
                moveTarget = navPath.corners[1];
            }
        }
        else if (navPath != null && navPath.corners != null && navPath.corners.Length > 1)
        {
            moveTarget = navPath.corners[1];
        }

        Vector3 planarDirection = moveTarget - transform.position;
        planarDirection.y = 0f;

        if (planarDirection.sqrMagnitude <= 0.0004f)
        {
            NetLocomotionState = LocomotionState.Idle;
            cc.Move(Vector3.zero);
            return;
        }

        cc.maxSpeed = speed;
        cc.Move(planarDirection.normalized);
        NetLocomotionState = LocomotionState.Moving;

        if (faceMoveTarget)
        {
            FaceToward(moveTarget);
        }
        else
        {
            FaceToward(explicitFaceTarget);
        }
    }

    private void MoveAwayFrom(Vector3 targetPosition, float simTime, float speed)
    {
        Vector3 awayDirection = transform.position - targetPosition;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude <= 0.0001f)
        {
            NetLocomotionState = LocomotionState.Idle;
            cc.Move(Vector3.zero);
            return;
        }

        Vector3 retreatPoint = transform.position + awayDirection.normalized * Mathf.Max(rangedPreferredDistance, 1f);
        MoveTowards(retreatPoint, simTime, speed);
    }

    private void RunPatrol(float simTime)
    {
        NetCombatState = CombatState.None;

        if (!HasPatrolDestination)
        {
            if (simTime < NextPatrolDecisionAt)
            {
                NetLocomotionState = LocomotionState.Idle;
                cc.Move(Vector3.zero);
                return;
            }

            if (!TryPickPatrolPoint(out Vector3 patrolPoint))
            {
                NextPatrolDecisionAt = simTime + patrolPauseSeconds;
                NetLocomotionState = LocomotionState.Idle;
                cc.Move(Vector3.zero);
                return;
            }

            PatrolDestination = patrolPoint;
            HasPatrolDestination = true;
        }

        float distance = Vector3.Distance(transform.position, PatrolDestination);
        if (distance <= patrolArriveDistance)
        {
            HasPatrolDestination = false;
            NextPatrolDecisionAt = simTime + patrolPauseSeconds;
            NetLocomotionState = LocomotionState.Idle;
            cc.Move(Vector3.zero);
            return;
        }

        MoveTowards(PatrolDestination, simTime, moveSpeed);
    }

    private bool TryPickPatrolPoint(out Vector3 patrolPoint)
    {
        Vector2 offset2D = Random.insideUnitCircle * patrolRadius;
        Vector3 candidate = SpawnOrigin + new Vector3(offset2D.x, 0f, offset2D.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            return true;
        }

        patrolPoint = SpawnOrigin;
        return false;
    }

    private void StartPatrolIdle(float simTime)
    {
        NetCombatState = CombatState.None;
        NetLocomotionState = LocomotionState.Idle;
        HasPatrolDestination = false;
        NextPatrolDecisionAt = simTime + patrolPauseSeconds;
        cc.Move(Vector3.zero);
    }

    private void TrySmartBlockAgainstTarget(float simTime, SharedModePlayerController target, float distance)
    {
        if (target == null || IsBlocking)
        {
            return;
        }

        if (simTime < NextBlockAllowedAt)
        {
            return;
        }

        if (distance > smartBlockThreatRange)
        {
            return;
        }

        if (target.NetCombatState != SharedModePlayerController.CombatState.Attacking)
        {
            return;
        }

        if (!IsFacingTargetForBlock(target.transform.position))
        {
            return;
        }

        StartBlock(simTime, blockDurationSeconds);
    }

    private bool IsFacingTargetForBlock(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        float dot = Vector3.Dot(forward.normalized, toTarget.normalized);
        return dot >= smartBlockFacingDot;
    }

    private void StartBlock(float simTime, float duration)
    {
        IsBlocking = true;
        NetCombatState = CombatState.Blocking;
        ComboStep = 0;
        BlockUntil = simTime + Mathf.Max(0.05f, duration);
        NextBlockAllowedAt = simTime + blockCooldownSeconds;
        cc.Move(Vector3.zero);
    }

    private void FaceToward(Vector3 worldPosition)
    {
        Vector3 lookDirection = worldPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * Runner.DeltaTime);
    }

    private Vector3 GetRangedAimPoint()
    {
        if (currentTarget == null)
        {
            return transform.position + transform.forward * Mathf.Max(1f, rangedAttackRange);
        }

        Collider targetCollider = currentTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return currentTarget.transform.position + Vector3.up * 1.1f;
    }

    public void ReleaseQueuedRangedShotFromAnimationEvent()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (!pendingRangedShot)
        {
            return;
        }

        pendingRangedShot = false;

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>(true);
        }

        if (bowWeapon == null)
        {
            return;
        }

        RPC_SpawnBowProjectile(pendingRangedAimPoint, pendingRangedAttackerRef);
    }

    private bool HasLineOfSightToTarget(Vector3 targetPosition)
    {
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(0f, lineOfSightEyeHeight);
        Vector3 destination = targetPosition + Vector3.up * Mathf.Max(0f, lineOfSightTargetHeight);
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            return true;
        }

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return currentTarget != null && hit.collider != null && hit.collider.transform.root == currentTarget.transform.root;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnBowProjectile(Vector3 aimPoint, PlayerRef attackerRef)
    {
        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>(true);
        }

        if (bowWeapon == null)
        {
            return;
        }

        bowWeapon.SpawnProjectile(transform, attackerRef, aimPoint, HasStateAuthority);
    }
}
