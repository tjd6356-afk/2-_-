using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class MerchantNPCInteraction : MonoBehaviour
{
    // =========================================================
    // Data
    // =========================================================

    [Header("Merchant")]

    [SerializeField]
    private MerchantNPCData merchantData;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private MerchantDialogueUI dialogueUI;


    // =========================================================
    // Runtime
    // =========================================================

    private bool playerInside;

    private PlayerGameplayControl playerControl;

    private MerchantShopViewPoint shopViewPoint;

    private void Awake()
    {
        if (dialogueUI == null)
        {
            dialogueUI =
                FindFirstObjectByType<MerchantDialogueUI>(
                    FindObjectsInactive.Include
                );
        }

        if (shopViewPoint == null)
        {
            shopViewPoint =
            GetComponent<MerchantShopViewPoint>();
        }
    }


    private void Update()
    {
        if (!playerInside)
            return;


        if (dialogueUI == null)
            return;


        if (dialogueUI.IsOpen)
            return;


        if (Keyboard.current == null)
            return;


        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            dialogueUI.BeginConversation(
                merchantData,
                playerControl,
                shopViewPoint
            );
        }
    }


    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerGameplayControl control =
            other.GetComponentInParent
            <PlayerGameplayControl>();


        if (control == null)
            return;


        playerInside =
            true;


        playerControl =
            control;
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerGameplayControl control =
            other.GetComponentInParent
            <PlayerGameplayControl>();


        if (control == null)
            return;


        if (control != playerControl)
            return;


        playerInside =
            false;


        playerControl =
            null;
    }
}