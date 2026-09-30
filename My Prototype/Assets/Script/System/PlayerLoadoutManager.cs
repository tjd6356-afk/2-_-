using UnityEngine;

public class PlayerLoadoutManager : MonoBehaviour
{
    public static PlayerLoadoutManager Instance
    {
        get;
        private set;
    }


    [Header("Current Weapon")]

    [SerializeField]
    private string equippedWeaponId;


    public string EquippedWeaponId =>
        equippedWeaponId;


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


    public void SaveWeapon(
        string weaponId
    )
    {
        equippedWeaponId =
            weaponId;


        Debug.Log(
            $"Weapon Saved : {weaponId}"
        );
    }
}