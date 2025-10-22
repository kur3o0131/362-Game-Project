using UnityEngine;

public class BattleSetup : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    void Start()
    {
        Debug.Log("Setting up battle scene...");

        // Spawn player
        PlayerStats player = Instantiate(playerPrefab, new Vector3(-4, 0, 0), Quaternion.identity)
                               .GetComponent<PlayerStats>();
        player.maxHealth = DataTransfer.playerMaxHealth;
        player.currentHealth = DataTransfer.playerCurrentHealth;
        player.attackPower = DataTransfer.playerAttackPower;

        // Spawn enemy
        EnemyStats enemy = Instantiate(enemyPrefab, new Vector3(4, 0, 0), Quaternion.identity)
                               .GetComponent<EnemyStats>();
        enemy.maxHealth = DataTransfer.enemyMaxHealth;
        enemy.currentHealth = DataTransfer.enemyCurrentHealth;
        enemy.attackPower = DataTransfer.enemyAttackPower;

        Debug.Log("Battle setup complete!");
    }
}
