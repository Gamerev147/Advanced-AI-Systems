using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    //[Header("General Camera Properties")]
    //[SerializeField] private Transform cameraTarget;
    //[SerializeField] private float rotationSpeed = 10f;
    //[SerializeField] private float smoothingSpeed = 2f;
    //[SerializeField] private float topClamp = 70f;
    //[SerializeField] private float bottomClamp = -40f;

    [Header("Camera Shake Properties")]
    [SerializeField] private float shakeFalloff = 1f;
    [SerializeField] private NoiseSettings wobbleNoiseProfile;
    [SerializeField] private NoiseSettings shakeNoiseProfile;

    private float _cinemachineTargetPitch, _cinemachineTargetYaw;
    private CinemachineVirtualCamera _cinemachineVirtualCamera;
    private Cinemachine3rdPersonFollow _virtualFollowComponent;
    private CinemachineBasicMultiChannelPerlin _perlinNoise;

    private float _currentIntensity;
    private float _currentFrequency;

    private float _recoilRecoverySpeed = 5f;
    private float _recoilOffset = 0f;

    private float _defaultFOV;
    private float _targetFOV;

    private float _defaultCameraDistance;
    private float _defaultCameraSide;
    private Vector3 _defaultCameraOffset;
    private bool _aiming = false;
    private bool _crouching = false;
    private float _aimSpeed;
    private float _aimDistance;
    private Vector3 _aimOffset;
    private Vector3 _crouchAimOffset;

    private float _side = 0.65f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else { Destroy(gameObject); }
    }

    private void Start()
    {
        _cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        _virtualFollowComponent = _cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        _perlinNoise = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        _defaultFOV = _cinemachineVirtualCamera.m_Lens.FieldOfView;
        _defaultCameraDistance = _virtualFollowComponent.CameraDistance;
        _defaultCameraSide = _virtualFollowComponent.CameraSide;
        _defaultCameraOffset = _virtualFollowComponent.ShoulderOffset;
        Cursor.lockState = CursorLockMode.Locked;

        _targetFOV = _defaultFOV;
    }

    private void Update()
    {
        // Camera Shake
        CheckCameraNoise();

        // Camera FOV
        _cinemachineVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cinemachineVirtualCamera.m_Lens.FieldOfView, _targetFOV, Time.deltaTime);

        // Camera Aiming
        if (_aiming)
        {
            _virtualFollowComponent.CameraDistance = Mathf.Lerp(_virtualFollowComponent.CameraDistance, _aimDistance, Time.deltaTime * _aimSpeed);

            if (!_crouching)
            {
                _virtualFollowComponent.ShoulderOffset = Vector3.Lerp(_virtualFollowComponent.ShoulderOffset, _aimOffset, Time.deltaTime * _aimSpeed);
            } else
            {
                _virtualFollowComponent.ShoulderOffset = Vector3.Lerp(_virtualFollowComponent.ShoulderOffset, _crouchAimOffset, Time.deltaTime * _aimSpeed);
            }
            
            _virtualFollowComponent.CameraSide = Mathf.Lerp(_virtualFollowComponent.CameraSide, _side, Time.deltaTime * _aimSpeed);
        } else
        {
            _virtualFollowComponent.CameraDistance = Mathf.Lerp(_virtualFollowComponent.CameraDistance, _defaultCameraDistance, Time.deltaTime * _aimSpeed);
            _virtualFollowComponent.ShoulderOffset = Vector3.Lerp(_virtualFollowComponent.ShoulderOffset, _defaultCameraOffset, Time.deltaTime * _aimSpeed);
            _virtualFollowComponent.CameraSide = Mathf.Lerp(_virtualFollowComponent.CameraSide, _defaultCameraSide, Time.deltaTime * _aimSpeed);
        }
    }

    /*
    private void LateUpdate()
    {
        //Read input values
        float mouseX = cameraControls.ReadValue<Vector2>().x * rotationSpeed * Time.deltaTime;
        float mouseY = cameraControls.ReadValue<Vector2>().y * rotationSpeed * Time.deltaTime;

        //Recoil decay
        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, Time.deltaTime * _recoilRecoverySpeed);

        //Update camera rotation
        _cinemachineTargetPitch = UpdateCameraRotation(_cinemachineTargetPitch + _recoilOffset, mouseY, bottomClamp, topClamp, true);
        _cinemachineTargetYaw = UpdateCameraRotation(_cinemachineTargetYaw, mouseX, float.MinValue, float.MaxValue, false);

        //Assign updated rotation
        //cameraTarget.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, cameraTarget.eulerAngles.z);
        Quaternion targetRot = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
        cameraTarget.rotation = Quaternion.Slerp(cameraTarget.rotation, targetRot, Time.deltaTime * smoothingSpeed);
    }
    */

    private float UpdateCameraRotation(float currentRot, float input, float min, float max, bool xAxis)
    {
        currentRot += input;
        return Mathf.Clamp(currentRot, min, max);
    }

    public void StartAiming(float speed, float distance, Vector3 offset, bool crouching, Vector3 crouchOffset)
    {
        _aimSpeed = speed;
        _aimDistance = distance;
        _aimOffset = offset;
        _crouchAimOffset = crouchOffset;
        _crouching = crouching;
        _aiming = true;
    }

    public void StopAiming()
    {
        _aiming = false;
    }

    public void ChangeSide(float side)
    {
        _side = side;
    }

    public float GetDefaultSide()
    {
        return _defaultCameraSide;
    }

    private void ShakeWithProfile(NoiseSettings noiseProfile, float intensity, float frequency)
    {
        if (_perlinNoise == null) return;

        _perlinNoise.m_NoiseProfile = noiseProfile;
        _perlinNoise.m_AmplitudeGain = intensity;
        _perlinNoise.m_FrequencyGain = frequency;
    }

    public void ShakeCamera(float _intensity, float _frequency)
    {
        ShakeWithProfile(shakeNoiseProfile, _intensity, _frequency);
    }

    public void ShakeCameraWobble(float _intensity, float _frequency)
    {
        ShakeWithProfile(wobbleNoiseProfile, _intensity, _frequency);
    }

    private void CheckCameraNoise()
    {
        _currentIntensity = _perlinNoise.m_AmplitudeGain;
        _currentFrequency = _perlinNoise.m_FrequencyGain;

        if (_perlinNoise.m_AmplitudeGain > 0)
        {
            _perlinNoise.m_AmplitudeGain -= Time.deltaTime * shakeFalloff;
        }

        if (_perlinNoise.m_FrequencyGain > 0)
        {
            _perlinNoise.m_FrequencyGain -= Time.deltaTime * shakeFalloff;
        }
    }

    public void ApplyRecoil(float amount, float recovery)
    {
        _recoilRecoverySpeed = recovery;
        _recoilOffset += Random.Range(amount * 0.8f, amount * 1.2f);
    }

    public void SetFOV(float fov = -1f)
    {
        if (fov <= 0f)
        {
            _targetFOV = _defaultFOV;
        }
        else
        {
            _targetFOV = fov;
        }
    }
}
