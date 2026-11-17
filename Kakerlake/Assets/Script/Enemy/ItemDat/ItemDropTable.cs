using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropTable : MonoBehaviour
{
    // =========================================================
    // Drop Entry
    // =========================================================

    [Serializable]
    public class DropEntry
    {
        [Tooltip("떨어뜨릴 ItemPickup Prefab")]
        public ItemPickup pickupPrefab;


        [Range(0f, 100f)]
        [Tooltip("드랍 확률 (%)")]
        public float dropChance = 25f;


        [Min(1)]
        public int minAmount = 1;


        [Min(1)]
        public int maxAmount = 1;
    }


    // =========================================================
    // Drop Table
    // =========================================================

    [Header("Drop Table")]

    [SerializeField]
    private List<DropEntry> drops =
        new List<DropEntry>();


    [Header("Spawn")]

    [SerializeField]
    private float randomDropRadius = 0.5f;


    // =========================================================
    // Drop
    // =========================================================

    public void TryDrop()
    {
        foreach (
            DropEntry entry in drops)
        {
            if (entry == null ||
                entry.pickupPrefab == null)
            {
                continue;
            }


            float roll =
                UnityEngine.Random.Range(
                    0f,
                    100f
                );


            if (roll >
                entry.dropChance)
            {
                continue;
            }


            int min =
                Mathf.Max(
                    1,
                    entry.minAmount
                );


            int max =
                Mathf.Max(
                    min,
                    entry.maxAmount
                );


            int amount =
                UnityEngine.Random.Range(
                    min,
                    max + 1
                );


            Vector2 randomCircle =
                UnityEngine.Random.insideUnitCircle *
                randomDropRadius;


            Vector3 spawnPosition =
                transform.position +
                new Vector3(
                    randomCircle.x,
                    0.2f,
                    randomCircle.y
                );


            ItemPickup pickup =
                Instantiate(
                    entry.pickupPrefab,
                    spawnPosition,
                    Quaternion.identity
                );


            pickup.SetAmount(
                amount
            );


            Debug.Log(
                $"[ItemDrop] Drop 성공 : " +
                $"{pickup.name} x{amount}"
            );
        }
    }
}