using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponType weaponType;

    [SerializeField] private GameObject weaponModel;


    public WeaponType Type => weaponType;


    public virtual void Equip()
    {
        enabled = true;

        if (weaponModel != null)
        {
            weaponModel.SetActive(true);
        }
    }


    public virtual void Unequip()
    {
        if (weaponModel != null)
        {
            weaponModel.SetActive(false);
        }

        enabled = false;
    }
}