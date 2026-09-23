using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField]
    private WeaponType weaponType;

    [Tooltip("획득 후 오브젝트를 없앨 것인가")]
    [SerializeField]
    private bool destroyAfterEquip = false;


    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerWeaponManager manager =
            other.GetComponentInParent
            <PlayerWeaponManager>();


        if (manager == null)
            return;


        manager.RegisterPickup(this);
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerWeaponManager manager =
            other.GetComponentInParent
            <PlayerWeaponManager>();


        if (manager == null)
            return;


        manager.UnregisterPickup(this);
    }


    public void Use(
        PlayerWeaponManager manager
    )
    {
        manager.EquipWeapon(
            weaponType
        );


        if (destroyAfterEquip)
        {
            manager.UnregisterPickup(this);

            Destroy(gameObject);
        }
    }
}