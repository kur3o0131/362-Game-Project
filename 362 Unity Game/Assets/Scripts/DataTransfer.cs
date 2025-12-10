using UnityEngine;

public static class DataTransfer
{
    // Player stats
    public static int playerMaxHealth;
    public static int playerCurrentHealth;
    public static int playerAttackPower;

    // Enemy stats
    public static GameObject enemyPrefab; 
    public static int enemyMaxHealth;
    public static int enemyCurrentHealth;
    public static int enemyAttackPower;

    // Companion stats
    public static int companionMaxHealth;
    public static int companionCurrentHealth;
    public static int companionAttackPower;

    // Scene + position tracking
    public static Vector3 lastPlayerPosition;
    public static string lastSceneName;

    public static GameObject overworldEnemy;
}
