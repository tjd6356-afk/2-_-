using UnityEngine;


// =========================================================
// Item Type
// =========================================================

public enum ItemType
{
    Food,
    Danger,
    Parts,
    History
}


[CreateAssetMenu(
    fileName = "NewItemData",
    menuName = "Game/Item/Item Data"
)]
public class ItemData : ScriptableObject
{
    // =========================================================
    // Basic
    // =========================================================

    [Header("Basic Information")]

    [SerializeField]
    private string itemId;


    [SerializeField]
    private string itemTitle;


    [TextArea(3, 10)]
    [SerializeField]
    private string itemDescription;


    [SerializeField]
    private Sprite itemIcon;


    // =========================================================
    // Type
    // =========================================================

    [Header("Item Type")]

    [SerializeField]
    private ItemType itemType;


    // =========================================================
    // Use Effect
    // =========================================================

    [Header("Use Effect")]
    [Tooltip("Food 타입 사용 시 회복하는 HP")]
    [Min(0f)]
    [SerializeField]
    private float healAmount;


    [Tooltip("Danger 타입 사용 시 받는 피해")]
    [Min(0f)]
    [SerializeField]
    private float damageAmount;


    [TextArea(3, 10)]
    [Tooltip("사용하기를 눌렀을 때 출력되는 대사")]
    [SerializeField]
    private string useDialogueText;


    [Tooltip("사용 후 인벤토리에서 1개 제거할지")]
    [SerializeField]
    private bool consumeOnUse = true;


    // =========================================================
    // World
    // =========================================================

    [Header("World Prefabs")]

    [Tooltip("버리거나 적에게서 드랍될 때 사용할 프리팹")]
    [SerializeField]
    private ItemPickup worldPickupPrefab;


    [Tooltip("플레이어가 손에 들 때 보일 프리팹. 비워두면 World Pickup 사용")]
    [SerializeField]
    private GameObject heldVisualPrefab;


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

    public ItemType ItemType =>
        itemType;

    public float HealAmount =>
    healAmount;


    public float DamageAmount =>
        damageAmount;

    public string UseDialogueText =>
        useDialogueText;

    public bool ConsumeOnUse =>
        consumeOnUse;

    public ItemPickup WorldPickupPrefab =>
        worldPickupPrefab;

    public GameObject HeldVisualPrefab =>
        heldVisualPrefab;

    public int MaxStack =>
        maxStack;
}