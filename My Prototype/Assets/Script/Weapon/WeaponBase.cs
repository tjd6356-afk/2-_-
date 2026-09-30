using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Information")]
    [SerializeField]
    private string weaponId = "weapon_001";

    [SerializeField]
    private string weaponName = "Weapon";

    [SerializeField]
    private WeaponType weaponType;


    // =========================================================
    // PlayerWeaponManager가 전달해주는 Player 정보
    // =========================================================

    protected PlayerWeaponManager WeaponManager
    {
        get;
        private set;
    }

    protected PlayerStats PlayerStats
    {
        get;
        private set;
    }

    protected Camera PlayerCamera
    {
        get;
        private set;
    }

    protected CharacterController PlayerController
    {
        get;
        private set;
    }


    // =========================================================
    // 외부 접근
    // =========================================================

    public string WeaponId =>
        weaponId;

    public string WeaponName =>
        weaponName;

    public WeaponType WeaponType =>
        weaponType;


    // 예전 코드와의 호환성을 위해 유지
    public WeaponType Type =>
        weaponType;


    // =========================================================
    // 무기 생성 직후 PlayerWeaponManager가 호출
    // =========================================================

    public void Initialize(
        PlayerWeaponManager weaponManager,
        PlayerStats playerStats,
        Camera playerCamera,
        CharacterController playerController
    )
    {
        WeaponManager =
            weaponManager;

        PlayerStats =
            playerStats;

        PlayerCamera =
            playerCamera;

        PlayerController =
            playerController;


        // Initialize가 끝나면 실제 장착 처리
        Equip();
    }


    // =========================================================
    // 무기 장착
    //
    // 자식 무기에서 override 가능
    // =========================================================

    public virtual void Equip()
    {
        gameObject.SetActive(true);
    }


    // =========================================================
    // 무기 해제
    //
    // 자식 무기에서 override 가능
    // =========================================================

    public virtual void Unequip()
    {
        // Prefab 방식에서는 보통 바로 Destroy하므로
        // 여기서 SetActive(false)를 강제로 할 필요는 없음.
    }
}