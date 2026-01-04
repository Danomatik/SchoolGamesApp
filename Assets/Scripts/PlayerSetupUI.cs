using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Verbindet die UI-Elemente (Slider, InputFields) mit dem PlayerSetupManager
/// Füge dieses Script zu einem GameObject in Demo 3 Scene hinzu
/// </summary>
public class PlayerSetupUI : MonoBehaviour
{
    [Header("Player Count Slider")]
    [Tooltip("Der Slider für die Spieleranzahl")]
    public Slider playerCountSlider;
    
    [Header("Game Duration Slider")]
    [Tooltip("Der Slider für die Spiel Dauer (in Minuten)")]
    public Slider gameDurationSlider;
    
    [Header("Game Duration Display")]
    [Tooltip("Optional: Text um die Spiel Dauer anzuzeigen")]
    public TextMeshProUGUI gameDurationText;
    
    [Header("Player Name Input Fields")]
    [Tooltip("Die InputFields für Spielernamen (in Reihenfolge: Spieler 1, 2, 3, ...)")]
    public List<TMP_InputField> playerNameInputs = new List<TMP_InputField>();

    private PlayerSetupManager setupManager;
    void Update()
    {
        // Temporärer Debug-Code für Player Count Slider
        if (Input.GetKeyDown(KeyCode.P) && playerCountSlider != null)
        {
            Debug.Log($"=== PLAYER COUNT SLIDER DEBUG ===");
            Debug.Log($"Value: {playerCountSlider.value}");
            Debug.Log($"Min: {playerCountSlider.minValue}, Max: {playerCountSlider.maxValue}");
            Debug.Log($"Interactable: {playerCountSlider.interactable}");
            Debug.Log($"Whole Numbers: {playerCountSlider.wholeNumbers}");
            Debug.Log($"Handle assigned: {playerCountSlider.handleRect != null}");
            Debug.Log($"GameObject active: {playerCountSlider.gameObject.activeInHierarchy}");
            Debug.Log($"Component enabled: {playerCountSlider.enabled}");
        }
    }
    void Start()
    {
        // Finde PlayerSetupManager
        setupManager = FindFirstObjectByType<PlayerSetupManager>();
        if (setupManager == null)
        {
            Debug.LogError("[PlayerSetupUI] PlayerSetupManager nicht gefunden! Erstelle einen...");
            GameObject managerObj = new GameObject("PlayerSetupManager");
            setupManager = managerObj.AddComponent<PlayerSetupManager>();
        }

        // Initialisiere UI
        InitializeUI();
    }

    /// <summary>
    /// Initialisiert die UI-Elemente mit gespeicherten Werten
    /// </summary>
        private void InitializeUI()
        {
            // Lade gespeicherte Spieleranzahl
            if (playerCountSlider != null)
            {
                // Stelle sicher, dass Slider richtig konfiguriert ist
                if (playerCountSlider.minValue < 1) playerCountSlider.minValue = 1;
                if (playerCountSlider.maxValue < 6) playerCountSlider.maxValue = 6;
                playerCountSlider.wholeNumbers = true; // Nur ganze Zahlen
                
                int savedCount = setupManager.GetPlayerCount();
                // Stelle sicher, dass Wert im gültigen Bereich ist
                savedCount = Mathf.Clamp(savedCount, (int)playerCountSlider.minValue, (int)playerCountSlider.maxValue);
                playerCountSlider.value = savedCount;
                
                // Verbinde Slider mit Manager (WICHTIG: Nach Setzen des Wertes!)
                playerCountSlider.onValueChanged.RemoveAllListeners();
                playerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
                
                Debug.Log($"[PlayerSetupUI] Spieleranzahl-Slider initialisiert: {savedCount} (Min: {playerCountSlider.minValue}, Max: {playerCountSlider.maxValue})");
            }
            else
            {
                Debug.LogWarning("[PlayerSetupUI] Player Count Slider nicht zugewiesen!");
            }

            // Lade gespeicherte Spiel Dauer
            if (gameDurationSlider != null)
            {
                // Stelle sicher, dass Slider richtig konfiguriert ist (Max: 10 Minuten)
                if (gameDurationSlider.minValue < 0) gameDurationSlider.minValue = 0;
                if (gameDurationSlider.maxValue > 10) gameDurationSlider.maxValue = 10; // Max: 10 Minuten
                
                float savedDuration = setupManager.GetGameDuration();
                // Stelle sicher, dass Wert im gültigen Bereich ist (0-10 Minuten)
                savedDuration = Mathf.Clamp(savedDuration, 0f, 10f);
                gameDurationSlider.value = savedDuration;
                
                // Verbinde Slider mit Manager (WICHTIG: Nach Setzen des Wertes!)
                gameDurationSlider.onValueChanged.RemoveAllListeners();
                gameDurationSlider.onValueChanged.AddListener(OnGameDurationChanged);
                
                // Initialisiere Text-Anzeige
                UpdateGameDurationText(savedDuration);
                
                Debug.Log($"[PlayerSetupUI] Spiel Dauer-Slider initialisiert: {savedDuration} Minuten (Min: {gameDurationSlider.minValue}, Max: {gameDurationSlider.maxValue})");
            }
            else
            {
                Debug.LogWarning("[PlayerSetupUI] Game Duration Slider nicht zugewiesen!");
            }

            // Lade gespeicherte Spielernamen
            for (int i = 0; i < playerNameInputs.Count; i++)
            {
                if (playerNameInputs[i] != null)
                {
                    int playerID = i + 1;
                    string savedName = setupManager.GetPlayerName(playerID);
                    playerNameInputs[i].text = savedName;
                    
                    // Verbinde InputField mit Manager
                    playerNameInputs[i].onEndEdit.RemoveAllListeners();
                    playerNameInputs[i].onEndEdit.AddListener((string value) => OnPlayerNameChanged(playerID, value));
                    
                    Debug.Log($"[PlayerSetupUI] Spieler {playerID} InputField initialisiert: {savedName}");
                }
            }
        }

    /// <summary>
    /// Wird aufgerufen wenn der Spieleranzahl-Slider geändert wird
    /// </summary>
    private void OnPlayerCountChanged(float value)
    {
        int count = Mathf.RoundToInt(value);
        setupManager.SetPlayerCount(count);
        Debug.Log($"[PlayerSetupUI] Spieleranzahl geändert: {count}");
        
        // Optional: Aktiviere/Deaktiviere InputFields basierend auf Anzahl
        UpdateInputFieldVisibility(count);
    }

    /// <summary>
    /// Wird aufgerufen wenn ein Spielername geändert wird
    /// </summary>
    private void OnPlayerNameChanged(int playerID, string name)
    {
        setupManager.SetPlayerName(playerID, name);
        Debug.Log($"[PlayerSetupUI] Spieler {playerID} Name geändert: {name}");
    }

    /// <summary>
    /// Wird aufgerufen wenn der Spiel Dauer-Slider geändert wird
    /// </summary>
    private void OnGameDurationChanged(float value)
    {
        setupManager.SetGameDuration(value);
        UpdateGameDurationText(value);
        Debug.Log($"[PlayerSetupUI] Spiel Dauer geändert: {value} Minuten");
    }

    /// <summary>
    /// Aktualisiert die Text-Anzeige für Spiel Dauer
    /// </summary>
    private void UpdateGameDurationText(float durationInMinutes)
    {
        if (gameDurationText != null)
        {
            // Formatiere die Anzeige (z.B. "60 Minuten" oder "1 Stunde")
            if (durationInMinutes >= 60)
            {
                int hours = Mathf.FloorToInt(durationInMinutes / 60);
                int minutes = Mathf.RoundToInt(durationInMinutes % 60);
                if (minutes > 0)
                {
                    gameDurationText.text = $"{hours}h {minutes}min";
                }
                else
                {
                    gameDurationText.text = $"{hours}h";
                }
            }
            else
            {
                gameDurationText.text = $"{Mathf.RoundToInt(durationInMinutes)} Minuten";
            }
        }
    }

    /// <summary>
    /// Aktiviert/Deaktiviert InputFields basierend auf Spieleranzahl
    /// </summary>
    private void UpdateInputFieldVisibility(int activeCount)
    {
        for (int i = 0; i < playerNameInputs.Count; i++)
        {
            if (playerNameInputs[i] != null)
            {
                // Aktiviere nur die InputFields für aktive Spieler
                playerNameInputs[i].gameObject.SetActive(i < activeCount);
            }
        }
    }
}

