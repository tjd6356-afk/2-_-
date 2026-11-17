using UnityEngine;

public static class ItemWorldSpawner
{
    public static ItemPickup Spawn(
        ItemData itemData,
        Vector3 position,
        int amount = 1
    )
    {
        if (itemData == null)
            return null;


        if (itemData.WorldPickupPrefab == null)
        {
            Debug.LogError(
                $"[ItemWorldSpawner] " +
                $"{itemData.ItemTitle}의 WorldPickupPrefab이 없습니다."
            );

            return null;
        }


        ItemPickup pickup =
            Object.Instantiate(
                itemData.WorldPickupPrefab,
                position,
                Quaternion.identity
            );


        pickup.Initialize(
            itemData,
            amount
        );


        return pickup;
    }
}