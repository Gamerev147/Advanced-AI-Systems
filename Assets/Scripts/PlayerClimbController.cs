using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbController : MonoBehaviour
{
    [Header("Actions")]
    public List<ClimbActionSO> climbAction;

    private EnvironmentChecker _envChecker;
    public bool PlayerInAction { get; set; }
    private Animator _animator;
    private ThirdPersonController _playerController;
    private StarterAssetsInputs _input;
    private PlayerWeaponController _weaponController;

    private void Start()
    {
        _envChecker = GetComponent<EnvironmentChecker>();
        _animator = GetComponent<Animator>();
        _playerController = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _weaponController = GetComponent<PlayerWeaponController>();
    }

    private void Update()
    {
        // Perform the climb action if one is not already being performed
        if (Input.GetKeyDown(KeyCode.Space) && !_weaponController.PrimaryActive)
        {
            if (!PlayerInAction && _input.move.y > 0.05f && !_playerController.PlayerHanging)
            {
                CheckClimbAction();
            }
        } else if (Input.GetKeyDown(KeyCode.Space) && _weaponController.PrimaryActive)
        {
            StartCoroutine(HolsterThenClimb());
        }
    }

    private void CheckClimbAction()
    {
        var hitData = _envChecker.CheckObstacle();
        if (hitData.hitFound) // An obstacle was found to perform climbing on
        {
            foreach (var action in climbAction)
            {
                if (action.CheckAvailable(hitData, transform))
                {
                    // Perform the climb action
                    StartCoroutine(PerformClimbAction(action));
                    break;
                }
            }
        }
    }

    private IEnumerator HolsterThenClimb()
    {
        _weaponController.SwapHolsterPrimary();
        yield return new WaitForSeconds(2.1f);

        if (!PlayerInAction && _input.move.y > 0.05f && !_playerController.PlayerHanging)
        {
            CheckClimbAction();
        }

        yield return null;
    }

    public IEnumerator PerformAction(string AnimationName, CompareTargetParams targetParams, Quaternion RequiredRotation, bool LookAtObstacle = false)
    {
        PlayerInAction = true;
        _animator.applyRootMotion = true;
        _playerController.SetControl(false);
        _animator.CrossFadeInFixedTime(AnimationName, 0.2f);

        yield return null;

        var animState = _animator.GetNextAnimatorStateInfo(0);

        float elapsed = 0f;
        while (elapsed < animState.length)
        {
            elapsed += Time.deltaTime;

            // Rotate towards the obstacle if required
            if (LookAtObstacle)
            {
                Quaternion targetRot = RequiredRotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // Target Matching
            if (targetParams != null)
            {
                CompareTarget(targetParams);
            }

            yield return null;
        }

        _animator.applyRootMotion = false;
        _playerController.SetControl(true);
        PlayerInAction = false;
    }

    private void CompareTarget(CompareTargetParams parameters)
    {
        _animator.MatchTarget(parameters.position, transform.rotation, parameters.bodyTarget, 
            new MatchTargetWeightMask(parameters.positionWeight, 0f), parameters.startTime, parameters.endTime);
    }

    public IEnumerator PerformClimbAction(ClimbActionSO action)
    {
        PlayerInAction = true;
        _animator.applyRootMotion = true;
        _playerController.SetControl(false);
        _animator.CrossFade(action.AnimationName, 0.2f);

        yield return null;

        var animState = _animator.GetNextAnimatorStateInfo(0);

        float elapsed = 0f;
        while (elapsed < animState.length)
        {
            elapsed += Time.deltaTime;

            // Rotate towards the obstacle if required
            if (action.LookAtObstacle)
            {
                Quaternion targetRot = action.RequiredRotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // Target Matching
            if (action.UseTargetMatching)
            {
                CompareTarget(action);
            }

            yield return null;
        }

        _animator.applyRootMotion = false;
        _playerController.SetControl(true);
        PlayerInAction = false;
    }

    private void CompareTarget(ClimbActionSO action)
    {
        _animator.MatchTarget(action.ComparePosition, transform.rotation, action.CompareBodyPart,
            new MatchTargetWeightMask(action.PositionWeight, 0f), action.CompareStartTime, action.CompareEndTime);
    }
}

public class CompareTargetParams
{
    public Vector3 position;
    public AvatarTarget bodyTarget;
    public Vector3 positionWeight;
    public float startTime;
    public float endTime;
}
