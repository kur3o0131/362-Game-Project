using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    FreeRoam,
    Dialog,
    Battle
}
public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    GameState state;

    private void Start()
    {
        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };
        DialogManager.Instance.OnHideDialog += () =>
        {
            state = GameState.FreeRoam;
        };
    }
    private void Update()
    {
        switch (state)
        {
            case GameState.FreeRoam:
                playerController.HandleUpdate();
                break;
            case GameState.Dialog:
                DialogManager.Instance.HandleUpdate();
                break;
            case GameState.Battle:
                // Handle battle updates
                break;
        }
    }
}
