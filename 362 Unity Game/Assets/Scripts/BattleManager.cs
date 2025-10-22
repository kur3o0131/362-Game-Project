using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    public PlayerStats player;
    public EnemyStats enemy;

    [Header("UI Elements")]
    public TMP_Text playerHPText;
    public TMP_Text enemyHPText;
    public TMP_Text battleLogText;
    public Button attackButton;

    private bool playerTurn = true;
    private bool battleEnded = false;

    void Start()
    {
        Debug.Log("Battle started!");

        if (player == null) player = FindFirstObjectByType<PlayerStats>();
        if (enemy == null) enemy = FindFirstObjectByType<EnemyStats>();

        UpdateUI();

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonPressed);
    }

    void OnAttackButtonPressed()
    {
        if (!playerTurn || battleEnded) return;

        int damage = player.attackPower;
        enemy.TakeDamage(damage);
        LogMessage($"Player attacked Enemy for {damage} damage!");

        UpdateUI();

        if (enemy.currentHealth <= 0)
        {
            EndBattle(true);
            return;
        }

        playerTurn = false;
        attackButton.interactable = false;
        Invoke(nameof(EnemyTurn), 1.0f);
    }

    void EnemyTurn()
    {
        if (battleEnded) return;

        int damage = enemy.attackPower;
        player.TakeDamage(damage);
        LogMessage($"Enemy attacked Player for {damage} damage!");

        UpdateUI();

        if (player.currentHealth <= 0)
        {
            EndBattle(false);
            return;
        }

        playerTurn = true;
        attackButton.interactable = true;
    }

    void UpdateUI()
    {
        if (playerHPText != null)
            playerHPText.text = $"Player HP: {player.currentHealth}/{player.maxHealth}";
        if (enemyHPText != null)
            enemyHPText.text = $"Enemy HP: {enemy.currentHealth}/{enemy.maxHealth}";
    }

    void EndBattle(bool playerWon)
    {
        battleEnded = true;
        LogMessage(playerWon ? "Player won!" : "Player lost!");

        if (attackButton != null)
            attackButton.interactable = false;

        // Save updated player health for when we return
        DataTransfer.playerCurrentHealth = player.currentHealth;
        DataTransfer.playerMaxHealth = player.maxHealth;

        Invoke(nameof(ReturnToWorld), 2.5f);
    }

    void ReturnToWorld()
    {
        if (!string.IsNullOrEmpty(DataTransfer.lastSceneName))
            SceneManager.LoadScene(DataTransfer.lastSceneName);
        else
            SceneManager.LoadScene("Town");
    }

    void LogMessage(string msg)
    {
        Debug.Log(msg);
        if (battleLogText != null)
            battleLogText.text = msg;
    }
    
    // retreat
    public void OnRetreatButtonPressed()
    {
        if (battleEnded) return;

        LogMessage("You retreated from battle!");
        EndBattle(false);
    }

}
