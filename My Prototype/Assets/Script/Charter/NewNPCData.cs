using System.Collections.Generic;
using UnityEngine;


// =============================================================
// 대화 종료 후 어떤 UI 시스템으로 연결할지
// =============================================================

public enum SpecialNPCPanelType
{
    None,

    WeaponUpgrade,

    Shop
}


[CreateAssetMenu(
    fileName = "New Special NPC Data",
    menuName = "NPC/Special NPC Data"
)]
public class NewNPCData : ScriptableObject
{
    // =========================================================
    // 기본 정보
    // =========================================================

    [Header("NPC Information")]

    [SerializeField]
    private string npcName = "NPC";


    [SerializeField]
    private Sprite npcPortrait;


    // =========================================================
    // Dialogue
    // =========================================================

    [Header("First Dialogue")]

    [TextArea(2, 5)]
    [SerializeField]
    private List<string> firstDialogue =
        new List<string>();


    [Header("Repeat Dialogue")]

    [TextArea(2, 5)]
    [SerializeField]
    private List<string> repeatDialogue =
        new List<string>();


    // =========================================================
    // 대화 종료 후 열 UI
    // =========================================================

    [Header("After Dialogue")]

    [Tooltip("대화가 끝난 뒤 실행할 특수 UI")]
    [SerializeField]
    private SpecialNPCPanelType panelType =
        SpecialNPCPanelType.None;


    // =========================================================
    // Properties
    // =========================================================

    public string NPCName =>
        npcName;


    public Sprite NPCPortrait =>
        npcPortrait;


    public IReadOnlyList<string> FirstDialogue =>
        firstDialogue;


    public IReadOnlyList<string> RepeatDialogue =>
        repeatDialogue;


    public SpecialNPCPanelType PanelType =>
        panelType;
}