using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [Header("Legacy Single References (kept for backward compatibility)")]
    public PlayerStats player;
    public EnemyStats enemy;

    [Header("Team Collections")]
    public List<PlayerStats> players = new List<PlayerStats>();
    public List<EnemyStats> enemies = new List<EnemyStats>();

    [Header("UI Elements")]
    public TMP_Text playerHPText;
    public TMP_Text enemyHPText;
    public TMP_Text battleLogText;
    public Button attackButton;

    [Header("Selection UI (wired these in inspector)")]
    public GameObject selectionPanel;
    public TMP_Text selectionPromptText;
    public Button selectAttackButton;
    public Button selectDefendButton;
    public Button selectRetreatButton;
    public Button selectBackButton;
    public Button selectConfirmButton;

    private bool playerSideTurn = true;
    private bool battleEnded = false;

    private int playerTurnIndex = 0;
    private int enemyTurnIndex = 0;

    // New: queued actions chosen by players before execution
    enum PlayerAction { None, Attack, Defend, Retreat }
    private List<PlayerAction> queuedPlayerActions = new List<PlayerAction>();
    private List<bool> playerDefending = new List<bool>();
    private int selectionIndex = 0; // which player we're choosing for

    void Start()
    {
        Debug.Log("Battle started!");

        // Collect players if list not manually populated in inspector
        if (players == null || players.Count == 0)
        {
            players = new List<PlayerStats>(FindObjectsByType<PlayerStats>(FindObjectsSortMode.None));
        }

        // Collect enemies if list not manually populated in inspector
        if (enemies == null || enemies.Count == 0)
        {
            enemies = new List<EnemyStats>(FindObjectsByType<EnemyStats>(FindObjectsSortMode.None));
        }

        // Maintain legacy references
        if (player == null && players.Count > 0)
            player = players[0];
        if (enemy == null && enemies.Count > 0)
            enemy = enemies[0];

        if (players.Count == 0)
        {
            Debug.LogError("No PlayerStats found in scene for battle!");
            battleEnded = true;
        }
        if (enemies.Count == 0)
        {
            Debug.LogError("No EnemyStats found in scene for battle!");
            battleEnded = true;
        }

        UpdateUI();

        if (attackButton != null)
            attackButton.onClick.AddListener(StartSelectionPhase);

        // Hook selection UI buttons if assigned
        if (selectAttackButton != null) selectAttackButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Attack));
        if (selectDefendButton != null) selectDefendButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Defend));
        if (selectRetreatButton != null) selectRetreatButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Retreat));
        if (selectBackButton != null) selectBackButton.onClick.AddListener(OnSelectBack);
        if (selectConfirmButton != null) selectConfirmButton.onClick.AddListener(OnSelectConfirm);

        // Initialize queued actions/defend list to match players
        queuedPlayerActions = new List<PlayerAction>(new PlayerAction[players.Count]);
        for (int i = 0; i < players.Count; i++) queuedPlayerActions[i] = PlayerAction.None;
        playerDefending = new List<bool>(new bool[players.Count]);
    }

    // =========================
    // New selection-phase system
    // =========================
    void StartSelectionPhase()
    {
        if (!playerSideTurn || battleEnded) return;

        // Fallback: if selection UI not wired, keep legacy single attack
        if (selectionPanel == null || selectAttackButton == null || selectDefendButton == null || selectRetreatButton == null)
        {
            OnAttackButtonPressed();
            return;
        }

        // Reset any lingering defend flags from previous round
        for (int i = 0; i < playerDefending.Count; i++) playerDefending[i] = false;

        // Ensure queue size matches current players
        EnsureQueuesMatchParty();

        // Start from first alive player
        selectionIndex = GetFirstAlivePlayerIndex();
        if (selectionIndex < 0)
        {
            EvaluateVictory();
            return;
        }

        if (attackButton != null) attackButton.interactable = false;
        ShowSelectionUI(true);
        UpdateSelectionPrompt();
        UpdateSelectionButtons();
    }

    void EnsureQueuesMatchParty()
    {
        if (queuedPlayerActions.Count != players.Count)
        {
            queuedPlayerActions = new List<PlayerAction>(new PlayerAction[players.Count]);
        }
        if (playerDefending.Count != players.Count)
        {
            playerDefending = new List<bool>(new bool[players.Count]);
        }
    }

    int GetFirstAlivePlayerIndex()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].currentHealth > 0) return i;
        }
        return -1;
    }

    int GetNextAlivePlayerIndex(int startExclusive)
    {
        for (int i = startExclusive + 1; i < players.Count; i++)
        {
            if (players[i] != null && players[i].currentHealth > 0) return i;
        }
        return -1;
    }

    int GetPrevAlivePlayerIndex(int startExclusive)
    {
        for (int i = startExclusive - 1; i >= 0; i--)
        {
            if (players[i] != null && players[i].currentHealth > 0) return i;
        }
        return -1;
    }

    void OnSelectAction(PlayerAction action)
    {
        if (battleEnded) return;
        if (selectionIndex < 0 || selectionIndex >= players.Count) return;
        if (players[selectionIndex] == null || players[selectionIndex].currentHealth <= 0) return;
        queuedPlayerActions[selectionIndex] = action;
        UpdateSelectionPrompt();
        UpdateSelectionButtons();
    }

    void OnSelectConfirm()
    {
        if (battleEnded) return;
        if (selectionIndex < 0) return;
        if (queuedPlayerActions[selectionIndex] == PlayerAction.None) return;

        // Advance to next alive player
        int next = GetNextAlivePlayerIndex(selectionIndex);
        if (next >= 0)
        {
            selectionIndex = next;
            UpdateSelectionPrompt();
            UpdateSelectionButtons();
        }
        else
        {
            // All chosen -> execute the round
            ShowSelectionUI(false);
            StartCoroutine(ExecuteRound());
        }
    }

    void OnSelectBack()
    {
        if (battleEnded) return;
        if (selectionIndex < 0) return;

        int prev = GetPrevAlivePlayerIndex(selectionIndex);
        if (prev >= 0)
        {
            selectionIndex = prev;
            UpdateSelectionPrompt();
            UpdateSelectionButtons();
        }
        else
        {
            // At first player; stay here. Could optionally exit selection.
            UpdateSelectionPrompt();
            UpdateSelectionButtons();
        }
    }

    void ShowSelectionUI(bool show)
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(show);
    }

    void UpdateSelectionPrompt()
    {
        if (selectionPromptText == null) return;
        int aliveCountBefore = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].currentHealth > 0)
            {
                if (i == selectionIndex) break;
                aliveCountBefore++;
            }
        }
        int aliveTotal = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].currentHealth > 0) aliveTotal++;
        }
        var p = players[selectionIndex];
        string current = queuedPlayerActions[selectionIndex].ToString();
        selectionPromptText.text = $"Choose action for {p.name} ({aliveCountBefore + 1}/{aliveTotal}). Current: {current}";
    }

    void UpdateSelectionButtons()
    {
        if (selectConfirmButton != null)
            selectConfirmButton.interactable = (selectionIndex >= 0 && selectionIndex < queuedPlayerActions.Count && queuedPlayerActions[selectionIndex] != PlayerAction.None);
        if (selectBackButton != null)
            selectBackButton.interactable = (GetPrevAlivePlayerIndex(selectionIndex) >= 0);
    }

    IEnumerator ExecuteRound()
    {
        playerSideTurn = false; // execution in progress
        if (attackButton != null) attackButton.interactable = false;

        // Player phase: execute chosen actions in order
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null || p.currentHealth <= 0) continue;
            var act = (i < queuedPlayerActions.Count) ? queuedPlayerActions[i] : PlayerAction.None;

            switch (act)
            {
                case PlayerAction.Attack:
                {
                    var target = GetFirstAliveEnemy();
                    if (target != null)
                    {
                        int dmg = p.attackPower;
                        target.TakeDamage(dmg);
                        LogMessage($"{p.name} attacked {target.name} for {dmg} damage!");
                        UpdateUI();
                        if (AreAllEnemiesDefeated()) { EndBattle(true); yield break; }
                    }
                    else
                    {
                        EvaluateVictory();
                        yield break;
                    }
                    break;
                }
                case PlayerAction.Defend:
                {
                    if (i < playerDefending.Count) playerDefending[i] = true;
                    LogMessage($"{p.name} is defending!");
                    break;
                }
                case PlayerAction.Retreat:
                {
                    LogMessage($"{p.name} initiated a retreat!");
                    EndBattle(false);
                    yield break;
                }
                case PlayerAction.None:
                default:
                {
                    // If no choice was made (shouldn't happen), default to defend
                    if (i < playerDefending.Count) playerDefending[i] = true;
                    LogMessage($"{p.name} braces for impact.");
                    break;
                }
            }
            yield return new WaitForSeconds(0.6f);
        }

        // After players acted, check victory
        if (AreAllEnemiesDefeated()) { EndBattle(true); yield break; }

        // Enemy phase: each alive enemy acts
        for (int j = 0; j < enemies.Count; j++)
        {
            var e = enemies[j];
            if (e == null || e.currentHealth <= 0) continue;

            var target = GetFirstAlivePlayer();
            if (target == null)
            {
                EvaluateVictory();
                yield break;
            }

            int targetIndex = players.IndexOf(target);
            int damage = e.attackPower;
            if (targetIndex >= 0 && targetIndex < playerDefending.Count && playerDefending[targetIndex])
            {
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.5f));
            }

            target.TakeDamage(damage);
            LogMessage($"{e.name} attacked {target.name} for {damage} damage!");
            UpdateUI();
            if (AreAllPlayersDefeated()) { EndBattle(false); yield break; }
            yield return new WaitForSeconds(0.6f);
        }

        // Clear defend after enemy phase
        for (int i = 0; i < playerDefending.Count; i++) playerDefending[i] = false;

        // Prepare next round
        for (int i = 0; i < queuedPlayerActions.Count; i++) queuedPlayerActions[i] = PlayerAction.None;
        selectionIndex = 0;
        playerSideTurn = true;
        if (!battleEnded && attackButton != null) attackButton.interactable = true;
        LogMessage("Choose your next actions.");
    }

    EnemyStats GetFirstAliveEnemy()
    {
        foreach (var e in enemies)
        {
            if (e != null && e.currentHealth > 0) return e;
        }
        return null;
    }

    PlayerStats GetFirstAlivePlayer()
    {
        foreach (var p in players)
        {
            if (p != null && p.currentHealth > 0) return p;
        }
        return null;
    }

    void OnAttackButtonPressed()
    {
        if (!playerSideTurn || battleEnded) return;

        PlayerStats actingPlayer = GetNextAlivePlayer();
        EnemyStats targetEnemy = GetNextAliveEnemy();

        if (actingPlayer == null || targetEnemy == null)
        {
            EvaluateVictory();
            return;
        }

        int damage = actingPlayer.attackPower;
        targetEnemy.TakeDamage(damage);
        LogMessage($"{actingPlayer.name} attacked {targetEnemy.name} for {damage} damage!");

        AdvancePlayerIndex();
        UpdateUI();

        if (AreAllEnemiesDefeated())
        {
            EndBattle(true);
            return;
        }

        playerSideTurn = false;
        if (attackButton != null)
            attackButton.interactable = false;

        Invoke(nameof(EnemyTurn), 1.0f);
    }

    void EnemyTurn()
    {
        if (battleEnded) return;

        EnemyStats actingEnemy = GetNextAliveEnemy();
        PlayerStats targetPlayer = GetNextAlivePlayer();

        if (actingEnemy == null || targetPlayer == null)
        {
            EvaluateVictory();
            return;
        }

        int damage = actingEnemy.attackPower;
        targetPlayer.TakeDamage(damage);
        LogMessage($"{actingEnemy.name} attacked {targetPlayer.name} for {damage} damage!");

        AdvanceEnemyIndex();
        UpdateUI();

        if (AreAllPlayersDefeated())
        {
            EndBattle(false);
            return;
        }

        playerSideTurn = true;
        if (attackButton != null)
            attackButton.interactable = true;
    }

    void UpdateUI()
    {
        if (playerHPText != null)
        {
            if (players.Count <= 1 && player != null)
            {
                playerHPText.text = $"Player HP: {player.currentHealth}/{player.maxHealth}";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("Players:");
                foreach (var p in players)
                {
                    if (p == null) continue;
                    sb.AppendLine($"{p.name}: {p.currentHealth}/{p.maxHealth}");
                }
                playerHPText.text = sb.ToString().TrimEnd();
            }
        }

        if (enemyHPText != null)
        {
            if (enemies.Count <= 1 && enemy != null)
            {
                enemyHPText.text = $"Enemy HP: {enemy.currentHealth}/{enemy.maxHealth}";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("Enemies:");
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    sb.AppendLine($"{e.name}: {e.currentHealth}/{e.maxHealth}");
                }
                enemyHPText.text = sb.ToString().TrimEnd();
            }
        }
    }

    void EndBattle(bool playerWon)
    {
        battleEnded = true;
        LogMessage(playerWon ? "Player side won!" : "Player side lost!");

        if (attackButton != null)
            attackButton.interactable = false;

        // Hide selection UI if visible
        ShowSelectionUI(false);

        if (player != null)
        {
            DataTransfer.playerCurrentHealth = player.currentHealth;
            DataTransfer.playerMaxHealth = player.maxHealth;
        }
        if (enemy != null)
        {
            DataTransfer.enemyCurrentHealth = enemy.currentHealth;
            DataTransfer.enemyMaxHealth = enemy.maxHealth;
        }

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

    public void OnRetreatButtonPressed()
    {
        if (battleEnded) return;

        LogMessage("You retreated from battle!");
        EndBattle(false);
    }

    PlayerStats GetNextAlivePlayer()
    {
        if (players == null || players.Count == 0) return player;
        int attempts = players.Count;
        int idx = playerTurnIndex;
        while (attempts-- > 0)
        {
            var p = players[idx];
            if (p != null && p.currentHealth > 0)
            {
                playerTurnIndex = idx;
                return p;
            }
            idx = (idx + 1) % players.Count;
        }
        return null;
    }

    EnemyStats GetNextAliveEnemy()
    {
        if (enemies == null || enemies.Count == 0) return enemy;
        int attempts = enemies.Count;
        int idx = enemyTurnIndex;
        while (attempts-- > 0)
        {
            var e = enemies[idx];
            if (e != null && e.currentHealth > 0)
            {
                enemyTurnIndex = idx;
                return e;
            }
            idx = (idx + 1) % enemies.Count;
        }
        return null;
    }

    void AdvancePlayerIndex()
    {
        if (players == null || players.Count == 0) return;
        playerTurnIndex = (playerTurnIndex + 1) % players.Count;
    }

    void AdvanceEnemyIndex()
    {
        if (enemies == null || enemies.Count == 0) return;
        enemyTurnIndex = (enemyTurnIndex + 1) % enemies.Count;
    }

    bool AreAllEnemiesDefeated()
    {
        foreach (var e in enemies)
        {
            if (e != null && e.currentHealth > 0) return false;
        }
        return true;
    }

    bool AreAllPlayersDefeated()
    {
        foreach (var p in players)
        {
            if (p != null && p.currentHealth > 0) return false;
        }
        return true;
    }

    void EvaluateVictory()
    {
        if (AreAllEnemiesDefeated())
        {
            EndBattle(true);
        }
        else if (AreAllPlayersDefeated())
        {
            EndBattle(false);
        }
    }
}