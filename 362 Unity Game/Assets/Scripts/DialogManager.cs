using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] Text dialogText;

    public event Action OnShowDialog;
    public event Action OnHideDialog;

    public static DialogManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    Dialog dialog;
    int currentLine = 0;
    bool isTyping;
    public void HandleUpdate()
    {
        // Handle dialog updates if needed
        if (Input.GetKeyDown(KeyCode.Z) && !isTyping)
        {
            ++currentLine;
            if (currentLine < dialog.Lines.Count)
            {
                StartCoroutine(TypeDialog(dialog));
            }
            else
            {
                dialogBox.SetActive(false);
                OnHideDialog?.Invoke();
                currentLine = 0;
            }
        }

    }

    public IEnumerator ShowDialog(Dialog dialog)
    {
        yield return new WaitForEndOfFrame();
        OnShowDialog?.Invoke();

        this.dialog = dialog;
        dialogBox.SetActive(true);
        StartCoroutine(TypeDialog(dialog));
    }

    public IEnumerator TypeDialog(Dialog dialog)
{
    isTyping = true;
    dialogText.text = "";

    var line = dialog.Lines[currentLine];
    foreach (var letter in line.ToCharArray())
    {
        dialogText.text += letter;
        yield return new WaitForSeconds(0.02f);
    }

    isTyping = false;
}

}
