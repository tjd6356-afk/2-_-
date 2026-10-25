using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class SpecialNPCPanelRouter : MonoBehaviour
{
    // =========================================================
    // Binding
    // =========================================================

    [Serializable]
    public class PanelBinding
    {
        [Tooltip("NewNPCData에서 선택할 Panel 종류")]
        public SpecialNPCPanelType panelType;


        [Tooltip("해당 Panel을 열 때 실행할 함수")]
        public UnityEvent openEvent;
    }


    // =========================================================
    // Panel Bindings
    // =========================================================

    [Header("Panel Bindings")]

    [SerializeField]
    private List<PanelBinding> bindings =
        new List<PanelBinding>();


    // =========================================================
    // Open
    // =========================================================

    public void OpenPanel(
        SpecialNPCPanelType panelType
    )
    {
        // 아무 UI도 열지 않음
        if (panelType ==
            SpecialNPCPanelType.None)
        {
            return;
        }


        foreach (
            PanelBinding binding in bindings)
        {
            if (binding == null)
                continue;


            if (binding.panelType !=
                panelType)
            {
                continue;
            }


            Debug.Log(
                $"[SpecialNPC] Panel Open : {panelType}"
            );


            binding.openEvent?.Invoke();

            return;
        }


        Debug.LogWarning(
            $"[SpecialNPC] {panelType}에 연결된 UI가 없습니다."
        );
    }
}