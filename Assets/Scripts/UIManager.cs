using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [Header("Speed Display")]
    [SerializeField] private Text speedText;

    [Header("Modifier Indicators")]
    [SerializeField] private GameObject boostPanel;
    [SerializeField] private Text boostTimerText;
    [SerializeField] private GameObject slowPanel;
    [SerializeField] private Text slowTimerText;

    private void Update()
    {
        if (player == null) return;

        UpdateSpeedDisplay();
        UpdateModifierDisplay();
    }

    private void UpdateSpeedDisplay()
    {
        if (speedText == null) return;
        speedText.text = $"Brzina: {player.CurrentSpeed:F1} m/s";
    }

    private void UpdateModifierDisplay()
    {
        bool activeBoost = player.HasModifier && player.IsBoost;
        bool activeSlow  = player.HasModifier && !player.IsBoost;

        if (boostPanel != null)
            boostPanel.SetActive(activeBoost);

        if (slowPanel != null)
            slowPanel.SetActive(activeSlow);

        // Tajmer prikazuje vreme do isteka aktivnog modifikatora
        string timerStr = player.HasModifier ? $"{player.ModifierTimeLeft:F1}s" : "";

        if (boostTimerText != null)
            boostTimerText.text = activeBoost ? timerStr : "";

        if (slowTimerText != null)
            slowTimerText.text = activeSlow ? timerStr : "";
    }
}
