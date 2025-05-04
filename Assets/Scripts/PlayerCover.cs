using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCover : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float verticalCheckOffset = 0f;
    [SerializeField] private float checkDistance = 2f;
    [SerializeField] private float sideCheckDistance = 0.5f;
    [SerializeField] private float aimSideCheckDistance = 0.64f;
    [SerializeField] private Vector3 rearCheckOffset = Vector3.zero;
    [SerializeField] private float animatorDamping = 0.5f;

    [Header("References")]
    [SerializeField] private Transform cameraTarget; //cinemachine camera target root

    private bool _inCover = false;
    private Animator _animator;
    private CharacterController _controller;
    private ThirdPersonController _player;
    private PlayerWeaponController _weaponController;
    private RaycastHit hit;

    private float _diveRollTimeout = 1.5f;
    private float _diveRollTimer = 0f;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _player = GetComponent<ThirdPersonController>();
        _weaponController = GetComponent<PlayerWeaponController>();
    }

    private void Update()
    {
        //Dive roll / slide
        _diveRollTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            PerformDiveRoll();
        }

        //Take Cover
        if (Input.GetKeyUp(KeyCode.Q) && !_inCover)
        {
            TakeCover();
        }

        //Exit cover
        if (Input.GetKeyDown(KeyCode.Q) && _inCover)
        {
            ExitCover();
        }

        //Handle cover state
        if (_inCover)
        {
            CoverAiming();

            //Move the player parallel to the cover
            float horInput = Input.GetAxis("Horizontal");
            Vector3 moveDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
            bool canMoveLeft = CheckForCover("left");
            bool canMoveRight = CheckForCover("right");

            //Clamp movement to cover
            if ((horInput > 0f && canMoveRight) || (horInput < 0f && canMoveLeft))
            {
                //Move player
                _controller.Move(moveDir * horInput * Time.deltaTime);
                _animator.SetFloat("CoverSpeed", -horInput, animatorDamping, Time.deltaTime);

                //Check for lower cover
                CheckForCrouchCover();
            } else
            {
                _animator.SetFloat("CoverSpeed", 0f, animatorDamping, Time.deltaTime);
            }
        }
    }

    private void PerformDiveRoll() //actually a slide now
    {
        if (_diveRollTimer <= 0f)
        {
            _animator.ResetTrigger("DiveRoll");
            EnableRootMotion();
            _animator.SetTrigger("DiveRoll");

            _diveRollTimer = _diveRollTimeout;
        }
    }

    private void TakeCover()
    {
        Vector3 pos = transform.position + new Vector3(0f, verticalCheckOffset / 2f, 0f);
        if (Physics.Raycast(pos, transform.forward, out hit, checkDistance))
        {
            if (hit.collider.CompareTag("Cover"))
            {
                _animator.SetBool("Cover", true);
                _inCover = true;

                //Set the player's position and rotation according to the wall
                Vector3 coverPos = hit.point - (transform.forward * checkDistance);
                transform.position = coverPos + new Vector3(0f, -0.5f, 0f);
                transform.rotation = Quaternion.LookRotation(hit.normal, transform.up);

                //dir = Vector3.Cross(hit.normal, Vector3.up)
                //Move(dir * horInput * deltaTime?)
            }
        }

        Debug.DrawRay(pos, transform.forward * checkDistance, Color.green);
    }

    private void CheckForCrouchCover()
    {
        Vector3 midPos = transform.position + new Vector3(0f, 1.5f, 0f); //for crouch cover
        
        RaycastHit midHit; //used to detect crouch cover
        Debug.DrawRay(midPos, -transform.forward * checkDistance, Color.magenta);

        bool mid = Physics.Raycast(midPos, -transform.forward, out midHit, checkDistance);
        if (mid)
        {
            _animator.SetBool("CoverCrouch", false);
        } else
        {
            _animator.SetBool("CoverCrouch", true);
        }
    }

    private bool CheckForCover(string side)
    {
        float horInput = Input.GetAxis("Horizontal");

        Vector3 sidePos;
        Vector3 endPos = transform.position + new Vector3(0f, 0.15f, 0f);

        if (horInput < 0f)
        {
            sidePos = endPos + transform.right * sideCheckDistance;
        } else
        {
            sidePos = endPos - transform.right * sideCheckDistance;
        }

        RaycastHit endHitSide;
        //RaycastHit endHitMid;
        //Debug.DrawRay(endPos, -transform.forward * checkDistance, Color.red);
        Debug.DrawRay(sidePos, -transform.forward * checkDistance, Color.green);

        bool rcr = Physics.Raycast(sidePos, -transform.forward, out endHitSide, checkDistance);
        //bool mid = Physics.Raycast(endPos, -transform.forward, out endHitMid, checkDistance);

        return rcr;
    }

    private void CoverAiming()
    {
        if (_weaponController.GetAiming())
        {
            Vector3 endPos = transform.position + new Vector3(0f, 0.2f, 0f);
            Vector3 leftPos = endPos + transform.right * (sideCheckDistance + 0.2f);
            Vector3 rightPos = endPos - transform.right * (sideCheckDistance + 0.2f);

            RaycastHit hitLeft;
            RaycastHit hitRight;
            Debug.DrawRay(leftPos, -transform.forward * (checkDistance + 0.5f), Color.red);
            Debug.DrawRay(rightPos, -transform.forward * (checkDistance + 0.5f), Color.red);

            bool left = Physics.Raycast(leftPos, -transform.forward, out hitLeft, checkDistance + 0.5f);
            bool right = Physics.Raycast(rightPos, -transform.forward, out hitRight, checkDistance + 0.5f);

            if (left && right)
            {
                CameraController.Instance.ChangeSide(CameraController.Instance.GetDefaultSide());
                if (!_animator.GetBool("CoverCrouch"))
                {
                    _animator.SetBool("CS_AimRight", false);
                    _animator.SetBool("CS_AimLeft", false);
                } else
                {
                    _animator.SetBool("CC_AimRight", false);
                    _animator.SetBool("CC_AimLeft", false);
                    _animator.SetBool("CS_AimRight", false);
                    _animator.SetBool("CS_AimLeft", false);
                }
            } else if (left)
            {
                CameraController.Instance.ChangeSide(1f);
                if (!_animator.GetBool("CoverCrouch"))
                {
                    _animator.SetBool("CS_AimRight", true);
                    _animator.SetBool("CS_AimLeft", false);
                } else
                {
                    _animator.SetBool("CC_AimRight", true);
                    _animator.SetBool("CC_AimLeft", false);
                    _animator.SetBool("CS_AimRight", false);
                    _animator.SetBool("CS_AimLeft", false);
                }
            } else if (right)
            {
                CameraController.Instance.ChangeSide(-1f);
                if (!_animator.GetBool("CoverCrouch"))
                {
                    _animator.SetBool("CS_AimLeft", true);
                    _animator.SetBool("CS_AimRight", false);
                } else
                {
                    _animator.SetBool("CC_AimRight", false);
                    _animator.SetBool("CC_AimLeft", true);
                    _animator.SetBool("CS_AimRight", false);
                    _animator.SetBool("CS_AimLeft", false);
                }
            }
        } else
        {
            CameraController.Instance.ChangeSide(CameraController.Instance.GetDefaultSide());
            _animator.SetBool("CS_AimRight", false);
            _animator.SetBool("CS_AimLeft", false);
            _animator.SetBool("CC_AimRight", false);
            _animator.SetBool("CC_AimLeft", false);
        }
    }

    private void ExitCover()
    {
        _animator.SetBool("Cover", false);
        _animator.SetBool("CoverCrouch", false);
        _inCover = false;
    }

    public bool GetCoverState()
    {
        return _inCover;
    }

    public void SlowTime()
    {
        Time.timeScale = 0.3f;
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void EnableRootMotion()
    {
        _animator.applyRootMotion = true;
    }

    public void DisableRootMotion()
    {
        _animator.applyRootMotion = false;
    }
}
