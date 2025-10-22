using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats player;
    public Image healthImage;

    [Header("Health Sprites")]
    public Sprite hpFull;
    public Sprite hpHigh;
    public Sprite hpMid;
    public Sprite hpLow;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

        UpdateHealthUI();
    }

    void Update()
    {
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (player == null || healthImage == null) return;

        float healthPercent = (float)player.currentHealth / player.maxHealth * 100f;

        if (healthPercent >= 100f)
            healthImage.sprite = hpFull;
        else if (healthPercent >= 51f)
            healthImage.sprite = hpHigh;
        else if (healthPercent >= 26f)
            healthImage.sprite = hpMid;
        else
            healthImage.sprite = hpLow;
    }
}
