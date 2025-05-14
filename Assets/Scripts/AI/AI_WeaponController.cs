using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AI_Agent))]
public class AI_WeaponController : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] private GameObject holsteredWeapon;
    [SerializeField] private GameObject weaponObject;

    private void Start()
    {
        // Disable hand weapon
        holsteredWeapon.SetActive(true);
        weaponObject.SetActive(false);
    }

    private void OnAnimationActivateWeapon()
    {
        holsteredWeapon.SetActive(false);
        weaponObject.SetActive(true);
    }
}
