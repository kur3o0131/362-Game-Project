using UnityEngine;

public class DamageBuffItem : MonoBehaviour
{
    public int damageIncrease = 20;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        // Apply buff
        stats.attackPower += damageIncrease;

        // Save new value so BattleSetup loads it correctly
        DataTransfer.playerAttackPower = stats.attackPower;
        DataTransfer.playerMaxHealth = stats.maxHealth;
        DataTransfer.playerCurrentHealth = stats.currentHealth;

        Destroy(gameObject);
    }
}
