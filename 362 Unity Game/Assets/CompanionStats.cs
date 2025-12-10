using UnityEngine;

public class CompanionStats : MonoBehaviour
{
    public int maxHealth = 80;
    public int currentHealth = 80;
    public int attackPower = 15;

    void Awake()
    {
        if (currentHealth <= 0)
            currentHealth = maxHealth;
    }
}
