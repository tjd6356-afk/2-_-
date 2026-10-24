using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemUseDialogueUI : MonoBehaviour
{
    [Header("Dialogue UI")]

    [SerializeField]
    private GameObject dialoguePanel;


    [SerializeField]
    private TMP_Text titleText;


    [SerializeField]
    private TMP_Text dialogueText;


    [SerializeField]
    private Image characterImage;


    private int openedFrame;


    public void Show(
        ItemData itemData
    )
    {
        if (itemData == null)
            return;


        ShowText(
            itemData.ItemTitle,
            itemData.UseDialogueText,
            itemData.ItemIcon
        );
    }


    public void ShowText(
        string title,
        string text,
        Sprite icon
    )
    {
        if (dialoguePanel == null)
            return;


        dialoguePanel.SetActive(
            true
        );


        dialoguePanel.transform.SetAsLastSibling();


        if (titleText != null)
        {
            titleText.text =
                title;
        }


        if (dialogueText != null)
        {
            dialogueText.text =
                text;
        }


        if (characterImage != null)
        {
            characterImage.sprite =
                icon;

            characterImage.enabled =
                icon != null;
        }


        openedFrame =
            Time.frameCount;
    }


    private void Update()
    {
        if (dialoguePanel == null ||
            !dialoguePanel.activeSelf)
        {
            return;
        }


        if (Time.frameCount == openedFrame)
            return;


        bool next =
            false;


        if (Keyboard.current != null)
        {
            next =
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame;
        }


        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            next =
                true;
        }


        if (next)
        {
            Close();
        }
    }


    public void Close()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(
                false
            );
        }
    }
}