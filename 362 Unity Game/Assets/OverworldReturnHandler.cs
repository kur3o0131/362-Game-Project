using UnityEngine;
using UnityEngine.SceneManagement;

public class OverworldReturnHandler : MonoBehaviour
{
    void Start()
    {
        if (SceneManager.GetActiveScene().name == DataTransfer.lastSceneName)
        {
            // Destroy the old overworld enemy
            if (DataTransfer.overworldEnemy != null)
            {
                Destroy(DataTransfer.overworldEnemy);
                DataTransfer.overworldEnemy = null;
            }
        }
    }
}