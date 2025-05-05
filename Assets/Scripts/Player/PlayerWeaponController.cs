using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public enum PrimaryWeapon
{
    AK47,
    M4A1,
    Shotgun,
    None
}

public enum SecondaryWeapon
{
    Pistol,
    None
}

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction weaponInput;

    [Header("Weapons")]
    [SerializeField] private PrimaryWeapon primaryWeapon;
    [SerializeField] private SecondaryWeapon secondaryWeapon;

    [Header("Properties")]
    [SerializeField] private float aimSpeed = 2f;
    [SerializeField] private float aimCameraDistance = 0.5f;
    [SerializeField] private Vector3 aimCameraOffset = Vector3.zero;
    [SerializeField] private Vector3 crouchAimOffset = Vector3.zero;

    [Header("References")]
    [SerializeField] private Rig aimRig;
    [SerializeField] private Rig secondaryRig;
    [SerializeField] private GameObject AK47gun, AK47holstered;
    [SerializeField] private GameObject M4A1gun, M4A1holstered;
    [SerializeField] private GameObject shotgun, shotgunHolstered;
    [SerializeField] private GameObject pistol, pistolHolstered;

    private Animator _animator;
    private bool _isAiming = false;
    private bool _primaryActive = false;
    private bool _secondaryActive = false;

    public bool PrimaryActive => _primaryActive;
    public bool SecondaryActive => _secondaryActive;

    private void OnEnable()
    {
        //Enable weapon input and assign bindings
        weaponInput.Enable();
        weaponInput.performed += ctx => StartAiming();
        weaponInput.canceled += ctx => StopAiming();
    }

    private void Start()
    {
        //Default to no aiming
        aimRig.weight = 0f;
        secondaryRig.weight = 0f;

        _animator = GetComponent<Animator>();
        _animator.SetBool("WeaponActive", false);
        _animator.SetBool("SecondaryActive", false);
    }

    private void Update()
    {
        //Handle Aiming
        if (_isAiming)
        {
            aimRig.weight = Mathf.Lerp(aimRig.weight, 1f, Time.deltaTime * aimSpeed);
            if (_secondaryActive) secondaryRig.weight = 1f;
        } else
        {
            aimRig.weight = Mathf.Lerp(aimRig.weight, 0f, Time.deltaTime * aimSpeed);
            secondaryRig.weight = 0f;
        }

        //Handle Holstering
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwapHolsterPrimary();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwapHolsterSecondary();
        }
    }

    public void SwapHolsterPrimary()
    {
        if (_primaryActive)
        {
            _animator.SetTrigger("HolsterRifle");
            _primaryActive = false;
        }
        else
        {
            _animator.SetTrigger("UnholsterRifle");
            _primaryActive = true;
        }
    }

    public void SwapHolsterSecondary()
    {
        if (_secondaryActive)
        {
            _animator.SetTrigger("HolsterPistol");
            _secondaryActive = false;
        } else
        {
            _animator.SetTrigger("UnholsterPistol");
            _secondaryActive = true;
        }
    }

    private void StartAiming()
    {
        //if (!_primaryActive || !_secondaryActive) return;

        _isAiming = true;
        _animator.SetBool("Aiming", true);
        CameraController.Instance.StartAiming(aimSpeed, aimCameraDistance, aimCameraOffset, _animator.GetBool("CoverCrouch"), crouchAimOffset);
    }

    private void StopAiming()
    {
        _isAiming = false;
        _animator.SetBool("Aiming", false);
        CameraController.Instance.StopAiming();
    }

    public void SetAimRig(float value)
    {
        aimRig.weight = value;
    }

    public bool GetAiming()
    {
        return _isAiming;
    }

    public void OnAnimationHolster()
    {
        switch (primaryWeapon)
        {
            case PrimaryWeapon.AK47:
                AK47gun.SetActive(false);
                AK47holstered.SetActive(true);
                break;
            case PrimaryWeapon.M4A1:
                M4A1gun.SetActive(false);
                M4A1holstered.SetActive(true);
                break;
            case PrimaryWeapon.Shotgun:
                shotgun.SetActive(false);
                shotgunHolstered.SetActive(true);
                break;
        }

        _animator.SetBool("WeaponActive", false);
    }

    public void OnAnimationHolsterSecondary()
    {
        switch (secondaryWeapon)
        {
            case SecondaryWeapon.Pistol:
                pistol.SetActive(false);
                pistolHolstered.SetActive(true);
                break;
        }

        _animator.SetBool("SecondaryActive", false);
    }

    public void OnAnimationUnholster()
    {
        switch (primaryWeapon)
        {
            case PrimaryWeapon.AK47:
                AK47gun.SetActive(true);
                AK47holstered.SetActive(false);
                break;
            case PrimaryWeapon.M4A1:
                M4A1gun.SetActive(true);
                M4A1holstered.SetActive(false);
                break;
            case PrimaryWeapon.Shotgun:
                shotgun.SetActive(true);
                shotgunHolstered.SetActive(false);
                break;
        }

        _animator.SetBool("WeaponActive", true);
    }

    public void OnAnimationUnholsterSecondary()
    {
        switch (secondaryWeapon)
        {
            case SecondaryWeapon.Pistol:
                pistol.SetActive(true);
                pistolHolstered.SetActive(false);
                break;
        }

        _animator.SetBool("SecondaryActive", true);
    }

    private void OnDisable()
    {
        //Unsubscribe from bindings
        weaponInput.performed -= ctx => StartAiming();
        weaponInput.canceled -= ctx => StopAiming();
        weaponInput.Disable();
    }
}
