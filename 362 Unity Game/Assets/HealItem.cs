using UnityEngine;

public class HealItem : MonoBehaviour
{
    public int healAmount = 20;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        // Heal and clamp
        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);

        // Save new value so BattleSetup loads it correctly
        DataTransfer.playerAttackPower = stats.attackPower;
        DataTransfer.playerMaxHealth = stats.maxHealth;
        DataTransfer.playerCurrentHealth = stats.currentHealth;

        Destroy(gameObject);
    }
}
