using UnityEngine;

public class BattleSetup : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    [Header("Extra Testing")]
    public bool spawnExtraPlayerForTest = true;
    public Vector3 firstPlayerPosition = new Vector3(-4f, 0f, 0f);
    public Vector3 extraPlayerOffset = new Vector3(1.75f, 0f, 0f);
    public Vector3 enemyPosition = new Vector3(4f, 0f, 0f);

    void Start()
    {
        Debug.Log("Setting up battle scene...");

        // Spawn main player (back-compatible with DataTransfer)
        PlayerStats player = Instantiate(playerPrefab, firstPlayerPosition, Quaternion.identity)
                                .GetComponent<PlayerStats>();
        player.name = "Player";
        player.maxHealth = DataTransfer.playerMaxHealth > 0 ? DataTransfer.playerMaxHealth : player.maxHealth;
        player.currentHealth = DataTransfer.playerCurrentHealth > 0 ? DataTransfer.playerCurrentHealth : player.maxHealth;
        player.attackPower = DataTransfer.playerAttackPower > 0 ? DataTransfer.playerAttackPower : player.attackPower;

        // Spawn extra test ally
        if (spawnExtraPlayerForTest)
        {
            Vector3 allyPos = firstPlayerPosition + extraPlayerOffset;
            PlayerStats ally = Instantiate(playerPrefab, allyPos, Quaternion.identity)
                                    .GetComponent<PlayerStats>();
            ally.name = "Ally 2";

            // Ensure health initialized (in case prefab doesn't set currentHealth yet)
            if (ally.currentHealth <= 0)
                ally.currentHealth = ally.maxHealth;
        }

        // Spawn enemy (back-compatible with DataTransfer)
        EnemyStats enemy = Instantiate(enemyPrefab, enemyPosition, Quaternion.identity)
                                .GetComponent<EnemyStats>();
        enemy.name = "Enemy";
        enemy.maxHealth = DataTransfer.enemyMaxHealth > 0 ? DataTransfer.enemyMaxHealth : enemy.maxHealth;
        enemy.currentHealth = DataTransfer.enemyCurrentHealth > 0 ? DataTransfer.enemyCurrentHealth : enemy.maxHealth;
        enemy.attackPower = DataTransfer.enemyAttackPower > 0 ? DataTransfer.enemyAttackPower : enemy.attackPower;

        Debug.Log("Battle setup complete!");
    }
}