using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using StarterAssets;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GuardState
{
    Idle,
    Patrol,
    Investigate,
    Alert
}

public class GuardEnemy : MonoBehaviour
{
    [Header("Main Properties")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float walkSpeed = 2f;
    public GuardState currentState = GuardState.Idle;

    [Header("Sensor Properties")]
    [SerializeField] private float tickRate = 0.1f;
    [SerializeField] private float lookToPlayerDistance = 3f;
    [SerializeField] private float playerLookBlendSpeed = 3f;
    [SerializeField, Range(0f, 180f)] private float fieldOfViewAngle = 90f;
    [SerializeField] private float proximityDistance = 2f;
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private float awarenessRate = 0.5f;

    [Header("Idle Properties")]
    [SerializeField] private float minIdleDuration = 4f;
    [SerializeField] private float maxIdleDuration = 10f;
    private float idleDuration;

    [Header("Patrol Properties")]
    [SerializeField] private bool canPatrol = true;
    [SerializeField] private bool loopPatrol = true;
    public List<PatrolPoint> patrolPoints = new List<PatrolPoint>();

    [Header("References")]
    [SerializeField] private ThirdPersonController player;
    [SerializeField] private LayerMask playerLayer, obstructionLayer;
    [SerializeField] private Rig playerLookRig;

    [Header("UI References")]
    [SerializeField] private GameObject sensorHUD;
    [SerializeField] private CanvasGroup sightGroup;
    [SerializeField] private Image sightFill;
    [SerializeField] private CanvasGroup alertGroup;
    [SerializeField] private Image alertFill;
    [SerializeField] private CanvasGroup alarmGroup;

    [Header("Debug")]
    public bool isDummy = false;
    [SerializeField] private bool showProximity = true;
    [SerializeField] private bool showLookProximity = true;
    [SerializeField] private bool showFieldOfView = true;
    [SerializeField] private bool showDebugText = true;

    // Base components
    private NavMeshAgent _agent;
    private Animator _animator;

    // Head look rig
    private float _minDotThreshold;
    private float _currentLookWeight;

    // Player Awareness
    private float _playerDistance;
    private float _normalizedPlayerDistance;
    private float _playerThreatLevel; // from 0-100
    private PlayerState _playerState;
    private AwarenessState _awarenessState = AwarenessState.Idle;

    // Ragdoll
    private Rigidbody[] _rbs;
    private Collider[] _cols;

    private int _currentPatrolIndex = 0;
    private int _patrolDirection = 1;
    private float _currentIdleIndex = 0;
    private float _stateTimer = 0f;
    private float _suspicion = 0f;
    private float _alertProgress = 0f;
    private float _totalTravelDistance;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _rbs = GetComponentsInChildren<Rigidbody>();
        _cols = GetComponentsInChildren<Collider>();

        _minDotThreshold = Mathf.Cos((fieldOfViewAngle / 2f) * Mathf.Deg2Rad);
        playerLookRig.weight = 0f;

        idleDuration = Random.Range(minIdleDuration, maxIdleDuration);

        DisableRagdoll();
        EnterState(currentState);
    }

    private void Update()
    {
        if (isDummy) return;

        HandleAwareness(); // awareness HUD

        HandlePlayerLook(); // head aiming
        HandleState();
    }

    private void HandleAwareness()
    {
        // Calculate distance
        _playerDistance = Vector3.Distance(transform.position, player.transform.position);
        _normalizedPlayerDistance = 1f - Mathf.InverseLerp(proximityDistance, viewDistance, _playerDistance);

        // Calculate overall player threat level
        float baseThreatLevel = 0f;
        _playerState = player.GetPlayerState();
        if (_playerState.Sprinting) baseThreatLevel += 0.35f;
        if (_playerState.Crouching || _playerState.Covering) baseThreatLevel += 0.125f;
        if (_playerState.WeaponDrawn) baseThreatLevel += 0.8f;
        if (!_playerState.Crouching && !_playerState.Covering && !_playerState.WeaponDrawn) baseThreatLevel += 0.05f;
        if (!CanSeePlayer()) baseThreatLevel -= 0.04f;

        float totalThreat = baseThreatLevel * _normalizedPlayerDistance;
        totalThreat = Mathf.Clamp01(totalThreat);
        _playerThreatLevel = totalThreat * 100f;

        // HUD
        alertFill.fillAmount = _alertProgress;
        sightFill.fillAmount = _suspicion;

        switch (_awarenessState)
        {
            case AwarenessState.Idle:
                SetHUDGroup(null);
                _suspicion = 0f;
                _alertProgress = 0f;

                if (CanSeePlayer() && _playerThreatLevel > 36f)
                {
                    _awarenessState = AwarenessState.Suspicious;
                } else if (CanSeePlayer())
                {
                    SetHUDGroup(sightGroup);
                }
                break;
            case AwarenessState.Suspicious:
                SetHUDGroup(sightGroup);
                _alertProgress = 0f;

                if (CanSeePlayer())
                {
                    _suspicion += Time.deltaTime * (awarenessRate * totalThreat);
                } else
                {
                    _suspicion -= Time.deltaTime * awarenessRate;
                }

                if (_suspicion >= 1f)
                {
                    _awarenessState = AwarenessState.Investigating;
                    EnterState(GuardState.Investigate);
                } else if (_suspicion <= 0f)
                {
                    _awarenessState = AwarenessState.Idle;
                }
                break;
            case AwarenessState.Investigating:
                SetHUDGroup(alertGroup);

                if (_agent.pathPending || _agent.remainingDistance > 0.1f)
                {
                    _alertProgress = GetDestinationProgress();
                } else
                {
                    _alertProgress -= Time.deltaTime * awarenessRate;
                }

                if (_alertProgress >= 1f)
                {
                    if (CanSeePlayer() && _playerThreatLevel >= 0.75f) // fixme: this needs more conditions
                    {
                        _awarenessState = AwarenessState.Alarmed;
                    } else
                    {
                        _awarenessState = AwarenessState.Idle;
                        EnterState(GuardState.Idle);
                    }
                }

                if (_alertProgress <= 0f)
                {
                    _awarenessState = AwarenessState.Idle;
                    EnterState(GuardState.Idle);
                }
                break;
            case AwarenessState.Alarmed:
                SetHUDGroup(alarmGroup);
                break;
        }
    }

    private void SetHUDGroup(CanvasGroup activeGroup)
    {
        sightGroup.alpha = 0f;
        alertGroup.alpha = 0f;
        alarmGroup.alpha = 0f;

        if (activeGroup != null)
        {
            activeGroup.alpha = 1f;
        }
    }

    public void EnterState(GuardState newState, float idleTime = -1f)
    {
        currentState = newState;
        _stateTimer = 0f;
        float newIdleTime = Random.Range(minIdleDuration, maxIdleDuration);

        switch (newState)
        {
            // Idle State
            case GuardState.Idle:
                _agent.isStopped = true;
                if (idleTime <= 0f)
                {
                    idleDuration = newIdleTime;
                } else
                {
                    Debug.Log("Using custom idle time");
                    idleDuration = idleTime;
                }
                _currentIdleIndex = Mathf.Round(Random.Range(0f, 1f));
                break;

            // Patrol State
            case GuardState.Patrol:
                _agent.isStopped = false;
                if (patrolPoints.Count > 0)
                {
                    Vector3 pos = patrolPoints[_currentPatrolIndex].Position;
                    _agent.SetDestination(pos);
                }
                break;

            // Investigating State
            case GuardState.Investigate:
                _agent.isStopped = false;
                MoveToWithProgress(player.transform.position);
                break;
        }
    }

    private void HandleState()
    {
        _animator.SetFloat("Speed", _agent.velocity.magnitude);
        _animator.SetFloat("IdleBlend", _currentIdleIndex, 0.5f, Time.deltaTime);

        switch (currentState)
        {
            case GuardState.Idle:
                IdleState();
                break;
            case GuardState.Patrol:
                PatrolState();
                break;
            case GuardState.Investigate:
                InvestigateState();
                break;
        }
    }

    private void IdleState()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= idleDuration)
        {
            if (canPatrol)
            {
                EnterState(GuardState.Patrol);
            } else
            {
                EnterState(GuardState.Idle);
            }
        }
    }

    private void PatrolState()
    {
        // Check if player is blocking path
        if (PlayerInFront() && Vector3.Distance(transform.position, player.transform.position) <= 2f)
        {
            EnterState(GuardState.Idle);
            return;
        }

        if (_agent.pathPending || _agent.remainingDistance > 0.5f) return;

        // Skip idling at patrol point
        if (patrolPoints[_currentPatrolIndex].SkipIdle)
        {
            UpdatePatrolIndex();
            EnterState(GuardState.Patrol);
            return;
        }
        else
        {
            // Custom idle time at each patrol point
            if (patrolPoints[_currentPatrolIndex].CustomWaitTime > 0f)
            {
                EnterState(GuardState.Idle, patrolPoints[_currentPatrolIndex].CustomWaitTime);
                UpdatePatrolIndex();
                return;
            }
            else
            {
                // Use standard idle time if custom is null
                UpdatePatrolIndex();
                EnterState(GuardState.Idle);
            }
        }
    }

    private void InvestigateState()
    {
        //fixme
    }

    private bool PlayerInFront()
    {
        Vector3 dirToPlayer = player.transform.position - transform.position;
        dirToPlayer.y = 0f;

        float dot = Vector3.Dot(transform.forward, dirToPlayer.normalized);
        return dot > _minDotThreshold;
    }

    private void UpdatePatrolIndex()
    {
        if (loopPatrol)
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Count;
        }
        else
        {
            _currentPatrolIndex += _patrolDirection;
            if (_currentPatrolIndex >= patrolPoints.Count)
            {
                _currentPatrolIndex = patrolPoints.Count - 2;
                _patrolDirection = -1;
            }
            else if (_patrolDirection < 0)
            {
                _currentPatrolIndex = 0;
                _patrolDirection = 1;
            }
        }
    }

    private void HandlePlayerLook()
    {
        if (Vector3.Distance(transform.position, player.transform.position) <= lookToPlayerDistance && CanSeePlayer())
        {
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);

            // Only look at the player if they are in front of us
            float targetWeight = dot > _minDotThreshold ? 1f : 0f;
            _currentLookWeight = Mathf.Lerp(_currentLookWeight, targetWeight, playerLookBlendSpeed * Time.deltaTime);
            playerLookRig.weight = _currentLookWeight;
        } else
        {
            playerLookRig.weight = Mathf.Lerp(playerLookRig.weight, 0f, playerLookBlendSpeed * Time.deltaTime);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eyeOffset = transform.position + new Vector3(0f, 1f, 0f);
        Collider[] rangeCheck = Physics.OverlapSphere(transform.position, viewDistance, playerLayer);

        if (rangeCheck.Length > 0)
        {
            Transform target = rangeCheck[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < fieldOfViewAngle / 2f)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(eyeOffset, directionToTarget, distanceToTarget, obstructionLayer))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return false;
    }

    private void MoveToWithProgress(Vector3 position)
    {
        _totalTravelDistance = Vector3.Distance(transform.position, position);
        _agent.SetDestination(position);
    }

    private float GetDestinationProgress()
    {
        float traveled = _totalTravelDistance - _agent.remainingDistance;
        float progress = traveled / _totalTravelDistance;

        return Mathf.Clamp01(progress);
    }

    public void EnableRagdoll()
    {
        foreach (Rigidbody rb in _rbs)
        {
            rb.isKinematic = false;
        }

        _animator.enabled = false;
    }

    public void DisableRagdoll()
    {
        foreach (Rigidbody rb in _rbs)
        {
            rb.isKinematic = true;
        }

        _animator.enabled = true;
    }

    private void EnableRootMotion()
    {
        _animator.applyRootMotion = true;
    }

    private void DisableRootMotion()
    {
        _animator.applyRootMotion = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Disable unnecessary gizmos
        GizmoUtility.SetGizmoEnabled(typeof(BoxCollider), false);
        GizmoUtility.SetGizmoEnabled(typeof(CapsuleCollider), false);

        // Player Look Rig Distance
        if (showLookProximity)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, lookToPlayerDistance);
        }

        // Sight and Proximity Distance
        if (showFieldOfView)
        {
            Handles.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0f, -fieldOfViewAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0f, fieldOfViewAngle * 0.5f, 0f) * transform.forward;
            Handles.DrawWireArc(transform.position, Vector3.up, leftDir, fieldOfViewAngle, viewDistance, 2f);
            Handles.DrawLine(transform.position, transform.position + leftDir * viewDistance, 2f);
            Handles.DrawLine(transform.position, transform.position + rightDir * viewDistance, 2f);
        }

        if (showProximity)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityDistance);
        }

        // Patrol waypoints
        Gizmos.color = Color.green;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            Vector3 worldPos = patrolPoints[i].Position;
            Gizmos.DrawSphere(worldPos, 0.25f);

            if (i < patrolPoints.Count - 1)
            {
                Vector3 nextPos = patrolPoints[i + 1].Position;
                Gizmos.DrawLine(worldPos, nextPos);
            }
        }

        if (loopPatrol)
        {
            Gizmos.DrawLine(patrolPoints[patrolPoints.Count - 1].Position, patrolPoints[0].Position);
        }

        // Draw debug text
        if (showDebugText)
        {
            Handles.Label(transform.position + (Vector3.up * 2f) + (Vector3.right * 0.75f), "N_Distance: " + _normalizedPlayerDistance.ToString());
            string spl = CanSeePlayer() ? "Yes" : "No";
            Handles.Label(transform.position + (Vector3.up * 2f) + (Vector3.right * 0.75f), "\n\nSee Player: " + spl);
            Handles.Label(transform.position + (Vector3.up * 1.5f) + (Vector3.right * 0.75f),
                "Player Threat: " + _playerThreatLevel + "\nSuspicion: " + _suspicion);
        }
    }
}
