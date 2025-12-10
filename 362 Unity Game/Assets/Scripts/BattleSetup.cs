using UnityEngine;

public class BattleSetup : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject npcPrefab;        // optional companion
    public GameObject enemyPrefab;      // set by overworld collision

    [Header("Positions")]
    public Vector3 playerPosition = new Vector3(-4f, 0f, 0f);
    public Vector3 npcOffset = new Vector3(1.75f, 0f, 0f);
    public Vector3 enemyPosition = new Vector3(4f, 0f, 0f);

    void Start()
    {
        Debug.Log("Setting up battle scene...");

        // ===== Spawn main player =====
        PlayerStats player = Instantiate(playerPrefab, playerPosition, Quaternion.identity)
                                .GetComponent<PlayerStats>();
        player.name = "Player";
        player.maxHealth = DataTransfer.playerMaxHealth > 0 ? DataTransfer.playerMaxHealth : player.maxHealth;
        player.currentHealth = DataTransfer.playerCurrentHealth > 0 ? DataTransfer.playerCurrentHealth : player.maxHealth;
        player.attackPower = DataTransfer.playerAttackPower > 0 ? DataTransfer.playerAttackPower : player.attackPower;

        // ===== Spawn NPC companion if assigned =====
        if (npcPrefab != null)
        {
            Vector3 npcPosition = playerPosition + npcOffset;
            PlayerStats npc = Instantiate(npcPrefab, npcPosition, Quaternion.identity)
                                .GetComponent<PlayerStats>();
            npc.name = "Companion";

            if (DataTransfer.companionMaxHealth > 0) npc.maxHealth = DataTransfer.companionMaxHealth;
            if (DataTransfer.companionCurrentHealth > 0) npc.currentHealth = DataTransfer.companionCurrentHealth;
            if (DataTransfer.companionAttackPower > 0) npc.attackPower = DataTransfer.companionAttackPower;
        }

        // ===== Spawn enemy from overworld trigger =====
        if (DataTransfer.enemyPrefab != null)
        {
            EnemyStats enemy = Instantiate(DataTransfer.enemyPrefab, enemyPosition, Quaternion.identity)
                                .GetComponent<EnemyStats>();
            enemy.name = "Enemy";

            enemy.maxHealth = DataTransfer.enemyMaxHealth > 0 ? DataTransfer.enemyMaxHealth : enemy.maxHealth;
            enemy.currentHealth = DataTransfer.enemyCurrentHealth > 0 ? DataTransfer.enemyCurrentHealth : enemy.maxHealth;
            enemy.attackPower = DataTransfer.enemyAttackPower > 0 ? DataTransfer.enemyAttackPower : enemy.attackPower;
        }

        Debug.Log("Battle setup complete!");
    }
}
