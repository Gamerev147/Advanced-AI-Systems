using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

public enum AwarenessState
{
    Idle,
    Suspicious,
    Investigating,
    Alarmed
}

[System.Serializable]
public class PatrolPoint
{
    public Vector3 Position;
    public bool SkipIdle = false; // skip idling on the patrol point
    public float CustomWaitTime = -1f; // use default wait time
}

public class AI_Agent : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField] private StateID initialState;
    [HideInInspector] public AI_StateMachine StateMachine;

    [Header("Sensor Properties")]
    public float TickRate = 0.1f;
    public float LookToPlayerDistance = 3f;
    public float PlayerLookBlendSpeed = 3f;
    [Range(0f, 180f)] public float FieldOfViewAngle = 90f;
    public float ProximityDistance = 2f;
    public float ViewDistance = 12f;
    public float AwarenessRate = 0.5f;

    [Header("Idle Properties")]
    public float MinIdleDuration = 4f;
    public float MaxIdleDuration = 10f;

    [Header("Patrol Properties")]
    public bool CanPatrol = true;
    public bool LoopPatrol = true;
    public List<PatrolPoint> PatrolPoints = new List<PatrolPoint>();

    [Header("References")]
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

    // These components are needed only by the state machine
    [HideInInspector] public NavMeshAgent NavAgent;
    [HideInInspector] public ThirdPersonController Player;
    [HideInInspector] public float StateTimer = 0f;
    [HideInInspector] public int CurrentPatrolIndex = 0;
    [HideInInspector] public int PatrolDirection = 1;
    [HideInInspector] public float PlayerThreatLevel; // from 0-100

    // Head look rig
    private float _minDotThreshold;
    private float _currentLookWeight;

    // Player Awareness
    private float _playerDistance;
    private float _normalizedPlayerDistance;
    private float _suspicion = 0f;
    private float _alertProgress = 0f;
    private PlayerState _playerState;
    private AwarenessState _awarenessState = AwarenessState.Idle;

    // Travel Distance
    private float _totalTravelDistance;

    private void Start()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();

        // State Machine Registration
        StateMachine = new AI_StateMachine(this);
        StateMachine.RegisterState(new AI_StateIdle());
        StateMachine.RegisterState(new AI_StatePatrol());
        StateMachine.RegisterState(new AI_StateInvestigate());
        StateMachine.RegisterState(new AI_StateAlert());
        StateMachine.RegisterState(new AI_StateAttack());
        StateMachine.RegisterState(new AI_StateDead());

        // Set Initial State
        StateMachine.ChangeState(initialState);
    }

    private void Update()
    {
        // Update the state machine
        StateMachine.Update();

        // Update head rig
        HandlePlayerLook();

        // Handle awareness calculations
        HandleAwareness();
    }

    private void HandleAwareness()
    {
        // Calculate distance
        _playerDistance = Vector3.Distance(transform.position, Player.transform.position);
        _normalizedPlayerDistance = 1f - Mathf.InverseLerp(ProximityDistance, ViewDistance, _playerDistance);

        // Calculate overall player threat level
        float baseThreatLevel = 0f;
        _playerState = Player.GetPlayerState();
        if (_playerState.Sprinting) baseThreatLevel += 0.35f;
        if (_playerState.Crouching || _playerState.Covering) baseThreatLevel += 0.125f;
        if (_playerState.WeaponDrawn) baseThreatLevel += 0.8f;
        if (!_playerState.Crouching && !_playerState.Covering && !_playerState.WeaponDrawn) baseThreatLevel += 0.05f;
        if (!CanSeePlayer()) baseThreatLevel -= 0.04f;

        float totalThreat = baseThreatLevel * _normalizedPlayerDistance;
        totalThreat = Mathf.Clamp01(totalThreat);
        PlayerThreatLevel = totalThreat * 100f;

        // HUD
        alertFill.fillAmount = _alertProgress;
        sightFill.fillAmount = _suspicion;

        switch (_awarenessState)
        {
            case AwarenessState.Idle:
                SetHUDGroup(null);
                _suspicion = 0f;
                _alertProgress = 0f;

                if (CanSeePlayer() && PlayerThreatLevel > 36f)
                {
                    _awarenessState = AwarenessState.Suspicious;
                }
                else if (CanSeePlayer())
                {
                    SetHUDGroup(sightGroup);
                }
                break;
            case AwarenessState.Suspicious:
                SetHUDGroup(sightGroup);
                _alertProgress = 0f;

                if (CanSeePlayer())
                {
                    _suspicion += Time.deltaTime * (AwarenessRate * totalThreat);
                }
                else
                {
                    _suspicion -= Time.deltaTime * AwarenessRate;
                }

                if (_suspicion >= 1f)
                {
                    _awarenessState = AwarenessState.Investigating;
                    StateMachine.ChangeState(StateID.Investigate);
                }
                else if (_suspicion <= 0f)
                {
                    _awarenessState = AwarenessState.Idle;
                }
                break;
            case AwarenessState.Investigating:
                SetHUDGroup(alertGroup);

                if (NavAgent.pathPending || NavAgent.remainingDistance > 0.1f)
                {
                    _alertProgress = GetDestinationProgress();
                }
                else
                {
                    _alertProgress -= Time.deltaTime * AwarenessRate;
                }

                if (_alertProgress >= 1f)
                {
                    if (CanSeePlayer() && PlayerThreatLevel >= 0.75f) // fixme: this needs more conditions
                    {
                        _awarenessState = AwarenessState.Alarmed;
                    }
                    else
                    {
                        _awarenessState = AwarenessState.Idle;
                        StateMachine.ChangeState(StateID.Idle);
                    }
                }

                if (_alertProgress <= 0f)
                {
                    _awarenessState = AwarenessState.Idle;
                    StateMachine.ChangeState(StateID.Idle);
                }
                break;
            case AwarenessState.Alarmed:
                SetHUDGroup(alarmGroup);
                break;
        }
    }

    public bool CanSeePlayer()
    {
        Vector3 eyeOffset = transform.position + new Vector3(0f, 1f, 0f);
        Collider[] rangeCheck = Physics.OverlapSphere(transform.position, ViewDistance, playerLayer);

        if (rangeCheck.Length > 0)
        {
            Transform target = rangeCheck[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < FieldOfViewAngle / 2f)
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

    public bool PlayerInFront()
    {
        Vector3 dirToPlayer = Player.transform.position - transform.position;
        dirToPlayer.y = 0f;

        float dot = Vector3.Dot(transform.forward, dirToPlayer.normalized);
        return dot > _minDotThreshold;
    }

    private void HandlePlayerLook()
    {
        if (Vector3.Distance(transform.position, Player.transform.position) <= LookToPlayerDistance && CanSeePlayer())
        {
            Vector3 dirToPlayer = (Player.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);

            // Only look at the player if they are in front of us
            float targetWeight = dot > _minDotThreshold ? 1f : 0f;
            _currentLookWeight = Mathf.Lerp(_currentLookWeight, targetWeight, PlayerLookBlendSpeed * Time.deltaTime);
            playerLookRig.weight = _currentLookWeight;
        }
        else
        {
            playerLookRig.weight = Mathf.Lerp(playerLookRig.weight, 0f, PlayerLookBlendSpeed * Time.deltaTime);
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

    public void MoveToWithProgress(Vector3 position)
    {
        _totalTravelDistance = Vector3.Distance(transform.position, position);
        NavAgent.SetDestination(position);
    }

    private float GetDestinationProgress()
    {
        float traveled = _totalTravelDistance - NavAgent.remainingDistance;
        float progress = traveled / _totalTravelDistance;

        return Mathf.Clamp01(progress);
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
            Gizmos.DrawWireSphere(transform.position, LookToPlayerDistance);
        }

        // Sight and Proximity Distance
        if (showFieldOfView)
        {
            Handles.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0f, -FieldOfViewAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0f, FieldOfViewAngle * 0.5f, 0f) * transform.forward;
            Handles.DrawWireArc(transform.position, Vector3.up, leftDir, FieldOfViewAngle, ViewDistance, 2f);
            Handles.DrawLine(transform.position, transform.position + leftDir * ViewDistance, 2f);
            Handles.DrawLine(transform.position, transform.position + rightDir * ViewDistance, 2f);
        }

        if (showProximity)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, ProximityDistance);
        }

        // Patrol waypoints
        Gizmos.color = Color.green;
        for (int i = 0; i < PatrolPoints.Count; i++)
        {
            Vector3 worldPos = PatrolPoints[i].Position;
            Gizmos.DrawSphere(worldPos, 0.25f);

            if (i < PatrolPoints.Count - 1)
            {
                Vector3 nextPos = PatrolPoints[i + 1].Position;
                Gizmos.DrawLine(worldPos, nextPos);
            }
        }

        if (LoopPatrol)
        {
            Gizmos.DrawLine(PatrolPoints[PatrolPoints.Count - 1].Position, PatrolPoints[0].Position);
        }

        // Draw debug text
        /*
        if (showDebugText)
        {
            Handles.Label(transform.position + (Vector3.up * 2f) + (Vector3.right * 0.75f), "N_Distance: " + _normalizedPlayerDistance.ToString());
            string spl = CanSeePlayer() ? "Yes" : "No";
            Handles.Label(transform.position + (Vector3.up * 2f) + (Vector3.right * 0.75f), "\n\nSee Player: " + spl);
            Handles.Label(transform.position + (Vector3.up * 1.5f) + (Vector3.right * 0.75f),
                "Player Threat: " + _playerThreatLevel + "\nSuspicion: " + _suspicion);
        }
        */
    }
}
