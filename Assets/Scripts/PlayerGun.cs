using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction gunInput;

    [Header("Gun Properties")]
    [SerializeField] private bool isAutomatic = true;
    [SerializeField] private float recoilAmount = 2f;
    [SerializeField] private float recoilRecovery = 5f;
    [SerializeField] private float rateOfFire = 0.1f;
    [SerializeField] private float reloadTime = 3.15f;
    [SerializeField] private float aimingAccuracy = 0.1f;
    [SerializeField] private float hipFireAccuracy = 0.25f;

    [Header("References")]
    [SerializeField] private Transform barrelTip;
    [SerializeField] private Transform crosshairTarget;
    [SerializeField] private Rig handRig;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleParticles;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private GameObject bulletImpactParticles;
    [SerializeField] private AudioClip[] rifleSounds;

    private PlayerWeaponController _weaponController;
    private Animator _animator;
    private float _cooldown;
    private bool _reloading = false;

    private void Start()
    {
        _weaponController = GetComponentInParent<PlayerWeaponController>();
        _animator = GetComponentInParent<Animator>();
        _cooldown = rateOfFire;
    }

    private void Update()
    {
        if (isAutomatic)
        {
            _cooldown -= Time.deltaTime;

            if (Input.GetMouseButton(0))
            {
                if (_cooldown <= 0f)
                {
                    Fire();
                    _cooldown = rateOfFire;
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Fire();
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && !_reloading)
        {
            _reloading = true;
            _animator.SetBool("Reload", true);
            handRig.weight = 0f;

            Invoke(nameof(ResetReload), reloadTime);
        }

        Debug.DrawRay(barrelTip.position, (crosshairTarget.position - barrelTip.position) * 50f, Color.blue);
    }

    private void ResetReload()
    {
        _animator.SetBool("Reload", false);
        handRig.weight = 1f;

        _reloading = false;
    }

    private void Fire()
    {
        if (!_weaponController.GetAiming())
        {
            _weaponController.SetAimRig(1f);
        }

        CameraController.Instance.ShakeCamera(0.6f, 0.2f);
        CameraController.Instance.ApplyRecoil(recoilAmount, recoilRecovery);
        //_animator.SetTrigger("Shoot");
        muzzleParticles.Play();

        AudioClip randomClip = rifleSounds[Random.Range(0, rifleSounds.Length)];
        AudioSource.PlayClipAtPoint(randomClip, barrelTip.position);

        RaycastHit hit;
        Vector3 dir = (crosshairTarget.position - barrelTip.position) + GetAccuracyOffset();
        if (Physics.Raycast(barrelTip.position, dir, out hit, 50f))
        {
            if (hit.transform.gameObject.GetComponent<ExplodingBarrel>() != null)
            {
                hit.transform.gameObject.GetComponent<ExplodingBarrel>().Explode();
            }

            TrailRenderer trail = Instantiate(bulletTrail, barrelTip.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, hit));
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float t = 0f;
        Vector3 startPos = trail.transform.position;

        while (t < 1f)
        {
            trail.transform.position = Vector3.Lerp(startPos, hit.point, t);
            t += Time.deltaTime / trail.time;

            yield return null;
        }

        trail.transform.position = hit.point;
        Instantiate(bulletImpactParticles, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(trail.gameObject, trail.time);
    }

    private Vector3 GetAccuracyOffset()
    {
        if (_weaponController.GetAiming())
        {
            return new Vector3(Random.Range(-aimingAccuracy, aimingAccuracy), 
                Random.Range(-aimingAccuracy, aimingAccuracy), 
                Random.Range(-aimingAccuracy, aimingAccuracy));
        } else
        {
            return new Vector3(Random.Range(-hipFireAccuracy, hipFireAccuracy), 
                Random.Range(-hipFireAccuracy, hipFireAccuracy), 
                Random.Range(-hipFireAccuracy, hipFireAccuracy));
        }
    }
}
