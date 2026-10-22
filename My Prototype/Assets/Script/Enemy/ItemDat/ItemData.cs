using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItemData",
    menuName = "Game/Item/Item Data"
)]
public class ItemData : ScriptableObject
{
    // =========================================================
    // Identification
    // =========================================================

    [Header("Identification")]

    [Tooltip("아이템 고유 ID. 서로 중복되면 안 됨.")]
    [SerializeField]
    private string itemId;


    // =========================================================
    // UI Information
    // =========================================================

    [Header("UI Information")]

    [SerializeField]
    private string itemTitle;


    [TextArea(3, 10)]
    [SerializeField]
    private string itemDescription;


    [SerializeField]
    private Sprite itemIcon;


    // =========================================================
    // Inventory
    // =========================================================

    [Header("Inventory")]

    [Min(1)]
    [SerializeField]
    private int maxStack = 99;


    // =========================================================
    // Properties
    // =========================================================

    public string ItemId =>
        itemId;


    public string ItemTitle =>
        itemTitle;


    public string ItemDescription =>
        itemDescription;


    public Sprite ItemIcon =>
        itemIcon;


    public int MaxStack =>
        maxStack;
}