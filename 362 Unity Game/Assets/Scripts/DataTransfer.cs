using UnityEngine;

public static class DataTransfer
{
    // Player stats
    public static int playerMaxHealth;
    public static int playerCurrentHealth;
    public static int playerAttackPower;

    // Enemy stats
    public static int enemyMaxHealth;
    public static int enemyCurrentHealth;
    public static int enemyAttackPower;

    // Scene + position tracking
    public static Vector3 lastPlayerPosition;
    public static string lastSceneName;
}
