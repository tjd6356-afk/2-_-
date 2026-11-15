using UnityEngine;

public class MonsterSpawnLandingDetector : MonoBehaviour
{
    private LayerMask groundMask;


    public bool HasLanded
    {
        get;
        private set;
    }


    public void Initialize(
        LayerMask mask
    )
    {
        groundMask =
            mask;


        HasLanded =
            false;
    }


    private void OnCollisionEnter(
        Collision collision
    )
    {
        if (HasLanded)
            return;


        if (collision == null ||
            collision.collider == null)
        {
            return;
        }


        // =====================================================
        // Ground Layer인지 검사
        // =====================================================

        int layer =
            collision.collider.gameObject.layer;


        bool isGroundLayer =
            (
                groundMask.value &
                (1 << layer)
            ) != 0;


        if (!isGroundLayer)
            return;


        // =====================================================
        // 벽 옆면에 닿은 것을 착지로 오인하지 않게
        // 위쪽을 향하는 충돌면인지 검사
        // =====================================================

        for (int i = 0;
             i < collision.contactCount;
             i++)
        {
            ContactPoint contact =
                collision.GetContact(i);


            if (contact.normal.y >
                0.4f)
            {
                HasLanded =
                    true;


                Debug.Log(
                    $"[MonsterSpawn] 착지 : {name}"
                );


                return;
            }
        }
    }
}