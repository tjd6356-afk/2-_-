using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Prefab")]

    [Tooltip("Player가 실제로 장착할 무기 Prefab")]
    [SerializeField]
    private WeaponBase weaponPrefab;


    [Tooltip("장착 후 맵의 Pickup을 없앨 것인가")]
    [SerializeField]
    private bool destroyAfterEquip = false;


    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerWeaponManager manager =
            other.GetComponentInParent<PlayerWeaponManager>();


        if (manager == null)
            return;


        manager.RegisterPickup(
            this
        );
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerWeaponManager manager =
            other.GetComponentInParent<PlayerWeaponManager>();


        if (manager == null)
            return;


        manager.UnregisterPickup(
            this
        );
    }


    public void Use(
        PlayerWeaponManager manager
    )
    {
        if (weaponPrefab == null)
        {
            Debug.LogError(
                $"{gameObject.name} : Weapon Prefab이 없습니다."
            );

            return;
        }


        manager.EquipWeapon(
            weaponPrefab
        );


        if (destroyAfterEquip)
        {
            manager.UnregisterPickup(
                this
            );


            Destroy(
                gameObject
            );
        }
    }
}