using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New NPC Data",
    menuName = "NPC/NPC Data"
)]
public class NPCData : ScriptableObject
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
    // NPC Stats
    // =========================================================

    [Header("NPC Stats")]

    [SerializeField]
    private List<NPCStat> stats =
        new List<NPCStat>();


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
    // 외부 접근
    // =========================================================

    public string NPCName =>
        npcName;

    public Sprite NPCPortrait =>
        npcPortrait;

    public IReadOnlyList<NPCStat> Stats =>
        stats;

    public IReadOnlyList<string> FirstDialogue =>
        firstDialogue;

    public IReadOnlyList<string> RepeatDialogue =>
        repeatDialogue;
}


// =============================================================
// NPC가 원하는 만큼 직접 만들 수 있는 스탯
// =============================================================

[Serializable]
public class NPCStat
{
    [SerializeField]
    private string statName;

    [SerializeField]
    private float value;


    public string StatName =>
        statName;

    public float Value =>
        value;
}