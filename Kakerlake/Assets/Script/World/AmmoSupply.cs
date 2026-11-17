using UnityEngine;

public class AmmoSupply : MonoBehaviour
{
    [Header("Supply")]

    [Tooltip("플레이어가 획득할 예비 탄창 개수")]
    [SerializeField] private int magazineSupplyAmount = 3;

    [Tooltip("현재 탄약이 0이면 즉시 재장전을 시작할지")]
    [SerializeField] private bool autoReloadIfEmpty = true;

    [Tooltip("한 번 사용하면 보급 오브젝트를 없앨지")]
    [SerializeField] private bool destroyAfterSupply = true;


    private bool used;


    private void OnTriggerEnter(Collider other)
    {
        // 이미 사용한 1회용 보급소라면 무시
        if (used && destroyAfterSupply)
            return;


        // 충돌한 대상 또는 부모에서 PlayerShooter 검색
        PlayerShooter playerShooter =
            other.GetComponentInParent<PlayerShooter>();


        // Player가 아님
        if (playerShooter == null)
            return;


        // ==============================================
        // 탄창 지급
        // ==============================================

        playerShooter.AddReserveMagazines(
            magazineSupplyAmount,
            autoReloadIfEmpty
        );


        Debug.Log(
            $"Ammo Supply 사용 : 탄창 +{magazineSupplyAmount}"
        );


        // ==============================================
        // 1회용 보급 아이템
        // ==============================================

        if (destroyAfterSupply)
        {
            used = true;

            Destroy(gameObject);
        }
    }


    private void OnValidate()
    {
        magazineSupplyAmount =
            Mathf.Max(
                1,
                magazineSupplyAmount
            );
    }
}