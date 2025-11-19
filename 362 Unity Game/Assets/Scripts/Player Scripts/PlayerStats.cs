using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 20;

    void Awake()
    {
        if (maxHealth <= 0)
            maxHealth = 100;
        if (currentHealth <= 0)
            currentHealth = maxHealth;
    }

    void Start()
    {
        if (DataTransfer.playerCurrentHealth > 0)
        {
            maxHealth = DataTransfer.playerMaxHealth;
            currentHealth = DataTransfer.playerCurrentHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        if (DataTransfer.lastPlayerPosition != Vector3.zero &&
            SceneManager.GetActiveScene().name != "BattleScene")
        {
            transform.position = DataTransfer.lastPlayerPosition;
            DataTransfer.lastPlayerPosition = Vector3.zero;
        }

        StartCoroutine(RefreshUIAfterDelay());
    }

    IEnumerator RefreshUIAfterDelay()
    {
        yield return null;
        var healthUI = FindFirstObjectByType<PlayerHealthUI>();
        if (healthUI != null)
        {
            healthUI.player = this;
            healthUI.UpdateHealthUI();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            DataTransfer.playerMaxHealth = maxHealth;
            DataTransfer.playerCurrentHealth = currentHealth;
            DataTransfer.playerAttackPower = attackPower;

            EnemyStats enemy = other.GetComponent<EnemyStats>();
            DataTransfer.enemyMaxHealth = enemy.maxHealth;
            DataTransfer.enemyCurrentHealth = enemy.currentHealth;
            DataTransfer.enemyAttackPower = enemy.attackPower;

            DataTransfer.lastSceneName = SceneManager.GetActiveScene().name;
            Vector3 offset = -((Vector3)other.transform.position - transform.position).normalized * 0.5f;
            DataTransfer.lastPlayerPosition = transform.position + offset;

            SceneManager.LoadScene("BattleScene");
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        if (currentHealth <= 0)
            Die();

        // Update UI if it exists
        var healthUI = FindFirstObjectByType<PlayerHealthUI>();
        if (healthUI != null)
            healthUI.UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        var healthUI = FindFirstObjectByType<PlayerHealthUI>();
        if (healthUI != null)
            healthUI.UpdateHealthUI();

        Debug.Log($"{gameObject.name} healed by {amount}. Current HP: {currentHealth}");
    }

    public void IncreaseAttack(int amount)
    {
        attackPower += amount;
        Debug.Log($"{gameObject.name} attack buffed by {amount}. New ATK: {attackPower}");
    }

    private void Die()
    {
        Debug.Log("Player died!");
    }
}
