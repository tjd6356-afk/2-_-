using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Merchant NPC Data",
    menuName = "NPC/Merchant NPC Data"
)]
public class MerchantNPCData : ScriptableObject
{
    // =========================================================
    // Merchant Information
    // =========================================================

    [Header("Merchant Information")]

    [SerializeField]
    private string merchantName = "Merchant";


    [SerializeField]
    private Sprite merchantPortrait;


    // =========================================================
    // Start Dialogue
    // =========================================================

    [Header("Start Dialogue")]

    [Tooltip("처음 상인에게 말을 걸었을 때 나오는 대사")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> startDialogue =
        new List<string>();


    // =========================================================
    // About Dialogue
    // =========================================================

    [Header("About Dialogue")]

    [Tooltip("'너에 대해서' 선택 시 나오는 대사")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> aboutDialogue =
        new List<string>();


    // =========================================================
    // Purchase Dialogue
    // =========================================================

    [Header("Purchase Complete Dialogue")]

    [Tooltip("아이템을 하나 이상 구입하고 상점을 닫았을 때")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> purchaseDialogue =
        new List<string>();


    // =========================================================
    // No Purchase Dialogue
    // =========================================================

    [Header("No Purchase Dialogue")]

    [Tooltip("아무것도 구입하지 않고 거래를 끝냈을 때")]
    [TextArea(2, 5)]
    [SerializeField]
    private List<string> noPurchaseDialogue =
        new List<string>();


    // =========================================================
    // Shop
    // =========================================================

    [Header("Shop Items")]

    [SerializeField]
    private List<MerchantShopEntry> shopItems =
        new List<MerchantShopEntry>();


    // =========================================================
    // Properties
    // =========================================================

    public string MerchantName =>
        merchantName;


    public Sprite MerchantPortrait =>
        merchantPortrait;


    public IReadOnlyList<string> StartDialogue =>
        startDialogue;


    public IReadOnlyList<string> AboutDialogue =>
        aboutDialogue;


    public IReadOnlyList<string> PurchaseDialogue =>
        purchaseDialogue;


    public IReadOnlyList<string> NoPurchaseDialogue =>
        noPurchaseDialogue;


    public IReadOnlyList<MerchantShopEntry> ShopItems =>
        shopItems;
}


// =============================================================
// 판매 상품 하나
// =============================================================

[Serializable]
public class MerchantShopEntry
{
    [SerializeField]
    private ItemData itemData;


    [Tooltip("한 번 구매할 때 들어오는 수량")]
    [SerializeField]
    private int amount = 1;


    public ItemData ItemData =>
        itemData;


    public int Amount =>
        Mathf.Max(
            1,
            amount
        );
}