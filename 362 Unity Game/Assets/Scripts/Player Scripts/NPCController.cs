using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{
    [SerializeField] Dialog dialog;
    NPCFollower follower;

    void Start()
    {
        follower = GetComponent<NPCFollower>();

        DialogManager.Instance.OnHideDialog += OnDialogFinished;
    }

    public void Interact()
    {
        StartCoroutine(DialogManager.Instance.ShowDialog(dialog));
    }

    void OnDialogFinished()
    {
        follower.StartFollowing();
    }
}
