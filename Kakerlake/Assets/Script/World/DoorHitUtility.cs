using System.Collections.Generic;
using UnityEngine;

public static class DoorHitUtility
{
    // =========================================================
    // Door 탐색
    // =========================================================

    private static Component FindDoor(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;


        SlidingDoorController slidingDoor =
            hitCollider.GetComponentInParent<SlidingDoorController>();


        if (slidingDoor != null)
        {
            return slidingDoor;
        }


        return hitCollider.GetComponentInParent<DoorController>();
    }


    public static bool IsDoor(Collider hitCollider)
    {
        return FindDoor(hitCollider) != null;
    }


    // =========================================================
    // 일반 총알 / 투사체 충돌
    // =========================================================

    public static bool HitProjectile(
        Collider hitCollider,
        Vector3 projectileVelocity,
        Vector3 hitPoint
    )
    {
        // 상호작용 Trigger는 공격 대상에서 제외
        if (hitCollider == null ||
            hitCollider.isTrigger)
        {
            return false;
        }


        Component door =
            FindDoor(hitCollider);


        if (door is SlidingDoorController slidingDoor)
        {
            slidingDoor.HitByProjectile(
                projectileVelocity,
                hitPoint
            );

            return true;
        }


        if (door is DoorController normalDoor)
        {
            normalDoor.HitByProjectile(
                projectileVelocity,
                hitPoint
            );

            return true;
        }


        return false;
    }


    // =========================================================
    // Raycast 공격
    //
    // Minigun / Shotgun 전용
    // =========================================================

    public static bool HitRaycast(
        RaycastHit hit,
        Vector3 direction,
        float impactSpeed
    )
    {
        Vector3 projectileVelocity =
            direction.normalized *
            Mathf.Max(0f, impactSpeed);


        return HitProjectile(
            hit.collider,
            projectileVelocity,
            hit.point
        );
    }


    // =========================================================
    // 폭발 충격
    //
    // Grenade / Rocket / Cluster / Explosive Minigun
    // =========================================================

    public static void HitExplosion(
        Collider[] hitColliders,
        Vector3 explosionCenter,
        float explosionRadius,
        GameObject owner = null
    )
    {
        if (hitColliders == null ||
            explosionRadius <= 0f)
        {
            return;
        }


        // 문 하나에 Collider가 여러 개 있어도
        // 폭발 충격은 한 번만 적용
        Dictionary<Component, Collider> affectedDoors =
            new Dictionary<Component, Collider>();


        foreach (Collider hit in hitColliders)
        {
            if (hit == null ||
                hit.isTrigger)
            {
                continue;
            }


            // 발사자 제외
            if (owner != null &&
                (hit.gameObject == owner ||
                 hit.transform.IsChildOf(owner.transform)))
            {
                continue;
            }


            Component door =
                FindDoor(hit);


            if (door == null)
                continue;


            // 여러 Collider 중 폭발 중심과
            // 가장 가까운 Collider를 선택
            if (affectedDoors.TryGetValue(
                    door,
                    out Collider previousCollider))
            {
                float previousDistance =
                    (previousCollider.ClosestPoint(explosionCenter)
                    - explosionCenter).sqrMagnitude;


                float newDistance =
                    (hit.ClosestPoint(explosionCenter)
                    - explosionCenter).sqrMagnitude;


                if (newDistance < previousDistance)
                {
                    affectedDoors[door] = hit;
                }
            }
            else
            {
                affectedDoors.Add(
                    door,
                    hit
                );
            }
        }


        // =====================================================
        // 실제 폭발 충격 적용
        // =====================================================

        foreach (var entry in affectedDoors)
        {
            Collider hit =
                entry.Value;


            Vector3 hitPoint =
                hit.ClosestPoint(explosionCenter);


            float distance =
                Vector3.Distance(
                    explosionCenter,
                    hitPoint
                );


            float strength =
                1f - Mathf.Clamp01(
                    distance / explosionRadius
                );


            // 폭발 중심이 Collider 내부인 경우
            if ((hitPoint - explosionCenter).sqrMagnitude
                < 0.0001f)
            {
                hitPoint =
                    hit.bounds.center;
            }


            if (entry.Key is SlidingDoorController slidingDoor)
            {
                slidingDoor.HitByExplosion(
                    explosionCenter,
                    hitPoint,
                    strength
                );
            }
            else if (entry.Key is DoorController normalDoor)
            {
                normalDoor.HitByExplosion(
                    explosionCenter,
                    hitPoint,
                    strength
                );
            }
        }
    }
}