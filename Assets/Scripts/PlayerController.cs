using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction playerInput;

    [Header("Camera")]
    [SerializeField] private GameObject CinemachineCameraTarget;
    [SerializeField] private float TopClamp = 70.0f;
    [SerializeField] private float BottomClamp = -30.0f;
    [SerializeField] private float Sensitivity = 1f;
    [SerializeField] private float Damping = 1f;

    [Header("Properties")]
    [SerializeField] private float RotationSpeed;


    private Vector2 _mouseInput;
    private Animator _animator;
    private CharacterController _controller;

    private float _rotationVelocity = 0f;
    private float _verticalSpeed = 0f;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private const float _threshold = 0.01f;

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Movement();
        CameraRotation();
    }

    private void Movement()
    {
        Vector3 moveDir = new Vector3(playerInput.ReadValue<Vector2>().x, 0f, playerInput.ReadValue<Vector2>().y);
        float inputMag = Mathf.Clamp01(moveDir.magnitude);

        if (!_controller.isGrounded) _verticalSpeed += Physics.gravity.y * Time.deltaTime;

        if (moveDir != Vector3.zero)
        {
            float _targetRotation = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg +
                                  Camera.main.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                0.11f);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        _animator.SetFloat("Speed", Input.GetKey(KeyCode.LeftShift) ? 2f : inputMag, 0.2f, Time.deltaTime);
    }

    private void CameraRotation()
    {
        _mouseInput = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));

        // if there is an input and camera position is not fixed
        if (_mouseInput.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += _mouseInput.x * Sensitivity;
            _cinemachineTargetPitch += _mouseInput.y * Sensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        Quaternion targetRot = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        CinemachineCameraTarget.transform.rotation = Quaternion.Slerp(CinemachineCameraTarget.transform.rotation, targetRot, Time.deltaTime * Damping);
        //CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnAnimatorMove()
    {
        Vector3 velocity = _animator.deltaPosition;
        velocity.y = _verticalSpeed;

        _controller.Move(velocity);
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
}
