using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerTakedownController : MonoBehaviour
{
    [Header("Takedown Properties")]
    [SerializeField] private float takedownOffset = 0.1f;
    [SerializeField] private float takedownCharacterOffset = 1f;
    [SerializeField] private float takedownDistance = 2f;
    [SerializeField] private float searchRadius = 8f;

    private Animator _animator;
    private PlayerWeaponController _weaponController;

    private List<Transform> enemiesInRange = new List<Transform>();

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _weaponController = GetComponent<PlayerWeaponController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && _animator.GetBool("WeaponActive") == false)
        {
            // Perform a standard takedown
            PerformMeleeAttack();
        } else if (Input.GetKeyDown(KeyCode.F) && _animator.GetBool("WeaponActive") == true)
        {
            // Otherwise perform weapon bash
            PerformWeaponBash();
        }
    }

    private void PerformMeleeAttack()
    {
        FindEnemyInRange();
        Transform enemy = GetNearestEnemy();

        if (enemy != null)
        {
            Vector3 dirToEnemy = (transform.position - enemy.position).normalized;
            float dot = Vector3.Dot(enemy.forward, dirToEnemy);
            float dis = Vector3.Distance(transform.position, enemy.position);

            if (dot < -1f + takedownOffset && dis <= takedownDistance)
            {
                Vector3 ed = (enemy.position - transform.position).normalized;
                transform.forward = ed;
                enemy.forward = ed;
                enemy.position = transform.position + ed * takedownCharacterOffset;

                _animator.SetTrigger("Takedown");
                CameraController.Instance.ShakeCameraWobble(0.8f, 0.6f);

                enemy.GetComponent<AI_Agent>().isDummy = true;
                enemy.GetComponent<NavMeshAgent>().isStopped = true;
                enemy.GetComponent<Animator>().SetTrigger("Takedown");
            }
        }
    }

    private void PerformWeaponBash()
    {
        FindEnemyInRange();
        Transform enemy = GetNearestEnemy();

        if (enemy != null)
        {
            _animator.SetTrigger("Melee");
            CameraController.Instance.ShakeCameraWobble(0.8f, 0.6f);
        }
    }

    private void FindEnemyInRange()
    {
        enemiesInRange.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);
        foreach (Collider c in colliders)
        {
            if (c.CompareTag("Enemy"))
            {
                enemiesInRange.Add(c.transform.root.transform);
            }
        }
    }

    private Transform GetNearestEnemy()
    {
        Transform nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Transform e in enemiesInRange)
        {
            float dis = Vector3.Distance(transform.position, e.position);
            if (dis < shortestDistance)
            {
                shortestDistance = dis;
                nearestEnemy = e;
            }
        }

        return nearestEnemy;
    }
}
