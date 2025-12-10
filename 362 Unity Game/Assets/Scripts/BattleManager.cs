using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{

    [Header("Spawn Positions")]
    public Vector3 playerBattlePosition = new Vector3(-4f, 0f, 0f);
    public Vector3 companionBattleOffset = new Vector3(1.75f, 0f, 0f);
    public Vector3 enemyBattlePosition = new Vector3(4f, 0f, 0f);

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject companionPrefab;

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

    // Player actions
    enum PlayerAction { None, Attack, Defend, Retreat }
    private List<PlayerAction> queuedPlayerActions = new List<PlayerAction>();
    private List<bool> playerDefending = new List<bool>();
    private int selectionIndex = 0;

    void Start()
    {
        Debug.Log("Battle started!");

        players = new List<PlayerStats>();
        enemies = new List<EnemyStats>();

        // ==========================
        // SPAWN PLAYER
        // ==========================
        if (playerPrefab == null)
        {
            Debug.LogError("BattleManager: playerPrefab is NOT assigned in the inspector!");
            return;
        }

        GameObject playerObj = Instantiate(playerPrefab, playerBattlePosition, Quaternion.identity);
        PlayerStats pStats = playerObj.GetComponent<PlayerStats>();
        if (pStats == null)
        {
            Debug.LogError("BattleManager: playerPrefab has NO PlayerStats component!");
            return;
        }

        pStats.name = "Player";
        pStats.maxHealth = DataTransfer.playerMaxHealth > 0 ? DataTransfer.playerMaxHealth : pStats.maxHealth;
        pStats.currentHealth = DataTransfer.playerCurrentHealth > 0 ? DataTransfer.playerCurrentHealth : pStats.maxHealth;
        pStats.attackPower = DataTransfer.playerAttackPower > 0 ? DataTransfer.playerAttackPower : pStats.attackPower;
        players.Add(pStats);

        // ==========================
        // SPAWN COMPANION (ALWAYS if prefab assigned)
        // ==========================
        if (companionPrefab != null)
        {
            Vector3 compPos = playerBattlePosition + companionBattleOffset;
            GameObject companionObj = Instantiate(companionPrefab, compPos, Quaternion.identity);
            PlayerStats cStats = companionObj.GetComponent<PlayerStats>();
            if (cStats == null)
            {
                Debug.LogError("BattleManager: companionPrefab has NO PlayerStats component!");
            }
            else
            {
                cStats.name = "Companion";
                cStats.maxHealth = DataTransfer.companionMaxHealth > 0 ? DataTransfer.companionMaxHealth : cStats.maxHealth;
                cStats.currentHealth = DataTransfer.companionCurrentHealth > 0 ? DataTransfer.companionCurrentHealth : cStats.maxHealth;
                cStats.attackPower = DataTransfer.companionAttackPower > 0 ? DataTransfer.companionAttackPower : cStats.attackPower;
                players.Add(cStats);
            }
        }

        // ==========================
        // SPAWN ENEMY
        // ==========================
        if (DataTransfer.enemyPrefab == null)
        {
            Debug.LogError("BattleManager: DataTransfer.enemyPrefab is NULL. " +
                           "Did the player collide with an enemy, and is EnemyStats.prefabReference assigned?");
            return;
        }

        GameObject enemyObj = Instantiate(DataTransfer.enemyPrefab, enemyBattlePosition, Quaternion.identity);
        if (enemyObj == null)
        {
            Debug.LogError("BattleManager: Instantiate returned NULL for enemyPrefab!");
            return;
        }

        EnemyStats eStats = enemyObj.GetComponent<EnemyStats>();
        if (eStats == null)
        {
            Debug.LogError("BattleManager: enemy prefab has NO EnemyStats on the root object!");
            return;
        }

        eStats.name = "Enemy";
        eStats.maxHealth = DataTransfer.enemyMaxHealth > 0 ? DataTransfer.enemyMaxHealth : eStats.maxHealth;
        eStats.currentHealth = DataTransfer.enemyCurrentHealth > 0 ? DataTransfer.enemyCurrentHealth : eStats.maxHealth;
        eStats.attackPower = DataTransfer.enemyAttackPower > 0 ? DataTransfer.enemyAttackPower : eStats.attackPower;
        enemies.Add(eStats);

        // Backwards compatibility single refs
        if (players.Count > 0) player = players[0];
        if (enemies.Count > 0) enemy = enemies[0];

        UpdateUI();

        if (attackButton != null)
            attackButton.onClick.AddListener(StartSelectionPhase);

        if (selectAttackButton != null) selectAttackButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Attack));
        if (selectDefendButton != null) selectDefendButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Defend));
        if (selectRetreatButton != null) selectRetreatButton.onClick.AddListener(() => OnSelectAction(PlayerAction.Retreat));
        if (selectBackButton != null) selectBackButton.onClick.AddListener(OnSelectBack);
        if (selectConfirmButton != null) selectConfirmButton.onClick.AddListener(OnSelectConfirm);

        queuedPlayerActions = new List<PlayerAction>(new PlayerAction[players.Count]);
        playerDefending = new List<bool>(new bool[players.Count]);
    }

    // ==========================
    // START SELECTION PHASE
    // ==========================
    void StartSelectionPhase()
    {
        if (!playerSideTurn || battleEnded) return;

        // Reset defend flags
        for (int i = 0; i < playerDefending.Count; i++)
            playerDefending[i] = false;

        // Ensure queues match party
        EnsureQueuesMatchParty();

        // Start with first alive player
        selectionIndex = GetFirstAlivePlayerIndex();
        if (selectionIndex < 0)
        {
            EvaluateVictory();
            return;
        }

        // Show selection UI
        ShowSelectionUI(true);
        UpdateSelectionPrompt();
        UpdateSelectionButtons();

        if (attackButton != null)
            attackButton.interactable = false;
    }

    void EnsureQueuesMatchParty()
    {
        if (queuedPlayerActions.Count != players.Count)
            queuedPlayerActions = new List<PlayerAction>(new PlayerAction[players.Count]);

        if (playerDefending.Count != players.Count)
            playerDefending = new List<bool>(new bool[players.Count]);
    }

    int GetFirstAlivePlayerIndex()
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i] != null && players[i].currentHealth > 0) return i;
        return -1;
    }

    int GetNextAlivePlayerIndex(int startExclusive)
    {
        for (int i = startExclusive + 1; i < players.Count; i++)
            if (players[i] != null && players[i].currentHealth > 0) return i;
        return -1;
    }

    int GetPrevAlivePlayerIndex(int startExclusive)
    {
        for (int i = startExclusive - 1; i >= 0; i--)
            if (players[i] != null && players[i].currentHealth > 0) return i;
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

        int next = GetNextAlivePlayerIndex(selectionIndex);
        if (next >= 0)
        {
            selectionIndex = next;
            UpdateSelectionPrompt();
            UpdateSelectionButtons();
        }
        else
        {
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
            if (players[i] != null && players[i].currentHealth > 0) aliveTotal++;

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
        playerSideTurn = false;
        if (attackButton != null) attackButton.interactable = false;

        // Player phase
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null || p.currentHealth <= 0) continue;
            var act = (i < queuedPlayerActions.Count) ? queuedPlayerActions[i] : PlayerAction.None;

            switch (act)
            {
                case PlayerAction.Attack:
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

                case PlayerAction.Defend:
                    if (i < playerDefending.Count) playerDefending[i] = true;
                    LogMessage($"{p.name} is defending!");
                    break;

                case PlayerAction.Retreat:
                    LogMessage($"{p.name} initiated a retreat!");
                    EndBattle(false);
                    yield break;

                case PlayerAction.None:
                default:
                    if (i < playerDefending.Count) playerDefending[i] = true;
                    LogMessage($"{p.name} braces for impact.");
                    break;
            }
            yield return new WaitForSeconds(0.6f);
        }

        // Enemy phase
        for (int j = 0; j < enemies.Count; j++)
        {
            var e = enemies[j];
            if (e == null || e.currentHealth <= 0) continue;

            var target = GetFirstAlivePlayer();
            if (target == null) { EvaluateVictory(); yield break; }

            int targetIndex = players.IndexOf(target);
            int damage = e.attackPower;
            if (targetIndex >= 0 && targetIndex < playerDefending.Count && playerDefending[targetIndex])
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.5f));

            target.TakeDamage(damage);
            LogMessage($"{e.name} attacked {target.name} for {damage} damage!");
            UpdateUI();
            if (AreAllPlayersDefeated()) { EndBattle(false); yield break; }
            yield return new WaitForSeconds(0.6f);
        }

        for (int i = 0; i < playerDefending.Count; i++) playerDefending[i] = false;

        for (int i = 0; i < queuedPlayerActions.Count; i++) queuedPlayerActions[i] = PlayerAction.None;
        selectionIndex = 0;
        playerSideTurn = true;
        if (!battleEnded && attackButton != null) attackButton.interactable = true;
        LogMessage("Choose your next actions.");
    }

    EnemyStats GetFirstAliveEnemy()
    {
        foreach (var e in enemies)
            if (e != null && e.currentHealth > 0) return e;
        return null;
    }

    PlayerStats GetFirstAlivePlayer()
    {
        foreach (var p in players)
            if (p != null && p.currentHealth > 0) return p;
        return null;
    }

    void UpdateUI()
    {
        if (playerHPText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Players:");
            foreach (var p in players)
                if (p != null) sb.AppendLine($"{p.name}: {p.currentHealth}/{p.maxHealth}");
            playerHPText.text = sb.ToString().TrimEnd();
        }

        if (enemyHPText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Enemies:");
            foreach (var e in enemies)
                if (e != null) sb.AppendLine($"{e.name}: {e.currentHealth}/{e.maxHealth}");
            enemyHPText.text = sb.ToString().TrimEnd();
        }
    }

    void EndBattle(bool playerWon)
    {
        battleEnded = true;
        LogMessage(playerWon ? "Player side won!" : "Player side lost!");

        if (attackButton != null)
            attackButton.interactable = false;

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
        if (playerWon && DataTransfer.overworldEnemy != null)
        {
            Destroy(DataTransfer.overworldEnemy);
            DataTransfer.overworldEnemy = null;
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

    void EvaluateVictory()
    {
        if (AreAllEnemiesDefeated()) EndBattle(true);
        else if (AreAllPlayersDefeated()) EndBattle(false);
    }

    bool AreAllEnemiesDefeated()
    {
        foreach (var e in enemies) if (e != null && e.currentHealth > 0) return false;
        return true;
    }

    bool AreAllPlayersDefeated()
    {
        foreach (var p in players) if (p != null && p.currentHealth > 0) return false;
        return true;
    }
}
