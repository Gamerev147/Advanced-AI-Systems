using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public enum EnemyState
{
    Idle,
    Patrol,
    CloseIn,
    Attack,
    Dead
}

public class SwatEnemy : MonoBehaviour
{
    [Header("General Properties")]
    [SerializeField] private EnemyState defaultState = EnemyState.Patrol;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float idleTime = 3f;

    [Header("Sight Properties")]
    [SerializeField, Range(0f, 180f)] private float fieldOfViewAngle = 90f;
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private Vector3 eyeOffset = Vector3.zero;

    [Header("Patrol Properties")]
    [SerializeField] private float patrolDistance = 6f;
    [SerializeField] private float patrolTime = 5f;

    [Header("Cover Properties"), Tooltip("These will be used when the enemy is closing in on the player.")]
    [SerializeField] private float coverWaitTime = 4f;
    [SerializeField, Tooltip("How far the player moves before swapping cover")] private float coverSwapThreshold = 2.5f;

    [Header("Attack Properties")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackTime = 4f;

    [Header("References")]
    [SerializeField] private LayerMask playerLayer, obstructionLayer;
    [SerializeField] private Rig aimRig;

    private EnemyState _currentState;
    private Transform _playerTransform;

    private NavMeshAgent _agent;
    private Animator _animator;

    private float _idleIndex = 0f;
    private float _idleTimer;

    private Vector3 _startPosition;
    private Vector3 _patrolPosition;
    private bool _movingToPatrolPoint = true;
    private bool _pausingPatrol = false;
    private float _patrolPauseTimer = 0f;

    private Transform _currentCover;
    private bool _waitingAtCover = false;
    private bool _playerMovedSinceCover;
    private Vector3 _lastPlayerPosition;
    private float _coverWaitTimer = 0f;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _currentState = defaultState;
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        _startPosition = transform.position;
        _patrolPosition = transform.position + transform.forward * (patrolDistance + _agent.stoppingDistance);
    }

    private void Update()
    {
        _animator.SetFloat("Speed", _agent.velocity.magnitude);

        switch (_currentState)
        {
            case EnemyState.Idle:
                IdleState();
                break;
            case EnemyState.Patrol:
                PatrolState();
                break;
            case EnemyState.CloseIn:
                CloseInState();
                break;
            case EnemyState.Attack:
                AttackState();
                break;
        }
    }

    private void IdleState()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleTime)
        {
            if (_idleIndex < 2)
            {
                _idleIndex += 1f;
            } else
            {
                _idleIndex = 0f;
            }

            _idleTimer = 0f;
        }

        aimRig.weight = Mathf.Lerp(aimRig.weight, 0f, Time.time * 4f);
        _animator.SetFloat("IdleIndex", _idleIndex, 1f, Time.deltaTime);
    }

    private void PatrolState()
    {
        aimRig.weight = Mathf.Lerp(aimRig.weight, 0f, Time.time * 4f);

        if (_pausingPatrol)
        {
            _patrolPauseTimer += Time.deltaTime;
            if (_patrolPauseTimer >= patrolTime)
            {
                _pausingPatrol = false;
                _patrolPauseTimer = 0f;

                _movingToPatrolPoint = !_movingToPatrolPoint;
                _agent.SetDestination(_movingToPatrolPoint ? _patrolPosition : _startPosition);
            }
        } else
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _pausingPatrol = true;
                _idleIndex = 2f;
                _agent.isStopped = true;
            } else
            {
                _agent.isStopped = false;
                _agent.speed = walkSpeed;
            }
        }

        if (CanSeePlayer())
        {
            _agent.isStopped = false;
            _currentState = EnemyState.CloseIn;
        }
    }

    private void CloseInState()
    {
        _agent.speed = runSpeed;
        aimRig.weight = Mathf.Lerp(aimRig.weight, 0f, Time.time * 4f);

        if (_waitingAtCover)
        {
            _coverWaitTimer += Time.deltaTime;
            if (_coverWaitTimer >= coverWaitTime)
            {
                _waitingAtCover = false;
                _coverWaitTimer = 0f;
            }
            return;
        }

        if (Vector3.Distance(_playerTransform.position, _lastPlayerPosition) > coverSwapThreshold)
        {
            _playerMovedSinceCover = true;
            _lastPlayerPosition = _playerTransform.position;
        }

        if (_currentCover == null || (_playerMovedSinceCover && _agent.remainingDistance <= _agent.stoppingDistance))
        {
            _currentCover = FindNearestCover();
            if (_currentCover != null)
            {
                Vector3 safeP = GetSafeCoverPosition(_currentCover);
                _agent.SetDestination(safeP);
                _playerMovedSinceCover = false;
                _lastPlayerPosition = _playerTransform.position;
            }
        }

        if (_agent.remainingDistance <= _agent.stoppingDistance && _currentCover != null)
        {
            _waitingAtCover = true;
        }
    }

    private Transform FindNearestCover()
    {
        GameObject[] coverObjects = GameObject.FindGameObjectsWithTag("Cover");
        Transform bestCover = null;
        float nearestDistance = Mathf.Infinity;
        float highestScore = float.NegativeInfinity;

        foreach (GameObject co in coverObjects)
        {
            float disToCover = Vector3.Distance(transform.position, co.transform.position);
            float disToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            float coverScore = EvaluateCover(co.transform);

            if (disToCover < nearestDistance && disToPlayer < disToCover && coverScore > highestScore)
            {
                nearestDistance = disToCover;
                highestScore = coverScore;
                bestCover = co.transform;
            }
        }

        return bestCover;
    }

    private float EvaluateCover(Transform cover)
    {
        Vector3 dirToPlayer = (_playerTransform.position - cover.position).normalized;

        if (Physics.Raycast(cover.position, dirToPlayer, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player"))
            {
                //Player has a direct line of sight, bad cover
                return -100f;
            }
        }

        Vector3 safePos = GetSafeCoverPosition(cover);

        if (Physics.Raycast(_playerTransform.position, (safePos - _playerTransform.position).normalized, out RaycastHit safeHit))
        {
            if (safeHit.transform == cover)
            {
                //Player sees the cover object
                return 100f;
            } else
            {
                //Player sees past the cover, calculate a score based on distance
                float dis = Vector3.Distance(cover.position, _playerTransform.position);
                return Mathf.Clamp(100f - dis, 0f, 100f);
            }
        }

        return -100f;
    }

    private Vector3 GetSafeCoverPosition(Transform cover)
    {
        Vector3 dirToPlayer = (_playerTransform.position - cover.position).normalized;
        Vector3 coverForward = cover.transform.forward;
        float dot = Vector3.Dot(coverForward, dirToPlayer);
        Vector3 safePos;

        if (dot < 0) //player in front of cover forward vector
        {
            safePos = cover.position + (coverForward * 1f);
        } else
        {
            safePos = cover.position - (coverForward * 1f);
        }

        return safePos;
    }

    private void AttackState()
    {
        _agent.isStopped = true;
        aimRig.weight = Mathf.Lerp(aimRig.weight, 1f, Time.time * 4f);

        //Look at player
        Vector3 dir = (_playerTransform.position - transform.position).normalized;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = targetRot;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        //Draw Patrol Gizmos
        if (defaultState == EnemyState.Patrol)
        {
            Gizmos.DrawRay(transform.position, transform.forward * patrolDistance);
            Gizmos.DrawWireSphere(transform.position + transform.forward * patrolDistance, 0.1f);
        }
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;

        //Draw Sight Gizmos
        Gizmos.DrawWireSphere(transform.position + eyeOffset, viewDistance);
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, transform.up) * transform.forward * viewDistance;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, transform.up) * transform.forward * viewDistance;
        Gizmos.DrawRay(transform.position + eyeOffset, fovLine1);
        Gizmos.DrawRay(transform.position + eyeOffset, fovLine2);
    }
}
