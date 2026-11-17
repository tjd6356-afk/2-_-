using System.Collections.Generic;
using UnityEngine;

public class WeaponCatalog : MonoBehaviour
{
    public static WeaponCatalog Instance
    {
        get;
        private set;
    }


    [Header("Weapon Prefabs")]

    [SerializeField]
    private List<WeaponBase> weaponPrefabs =
        new List<WeaponBase>();


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    public WeaponBase GetWeapon(
        string weaponId
    )
    {
        if (string.IsNullOrEmpty(
                weaponId))
        {
            return null;
        }


        foreach (WeaponBase weapon in weaponPrefabs)
        {
            if (weapon == null)
                continue;


            if (weapon.WeaponId ==
                weaponId)
            {
                return weapon;
            }
        }


        return null;
    }
}