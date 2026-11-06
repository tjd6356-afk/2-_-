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
// 판매 상품
// =============================================================

[Serializable]
public class MerchantShopEntry
{
    // =========================================================
    // 판매되는 Item
    // =========================================================

    [Header("Product")]

    [Tooltip("플레이어가 구입하게 될 ItemData")]
    [SerializeField]
    private ItemData itemData;


    [Tooltip("한 번 거래할 때 받는 수량")]
    [SerializeField]
    private int amount = 1;


    // =========================================================
    // 필요한 물물교환 재료
    // =========================================================

    [Header("Trade Requirements")]

    [Tooltip(
        "이 상품을 구입하기 위해 필요한 ItemData와 수량"
    )]
    [SerializeField]
    private List<MerchantTradeRequirement>
        requirements =
            new List<MerchantTradeRequirement>();


    // =========================================================
    // Properties
    // =========================================================

    public ItemData ItemData =>
        itemData;


    public int Amount =>
        Mathf.Max(
            1,
            amount
        );


    public IReadOnlyList<MerchantTradeRequirement>
        Requirements =>
            requirements;
}


// =============================================================
// 물물교환 조건 하나
// =============================================================

[Serializable]
public class MerchantTradeRequirement
{
    [Tooltip("거래에 필요한 ItemData")]
    [SerializeField]
    private ItemData requiredItem;


    [Tooltip("필요한 수량")]
    [SerializeField]
    private int requiredAmount = 1;


    public ItemData RequiredItem =>
        requiredItem;


    public int RequiredAmount =>
        Mathf.Max(
            1,
            requiredAmount
        );
}