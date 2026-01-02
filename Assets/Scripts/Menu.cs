using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene die geladen wird wenn 'Spiel starten' geklickt wird")]
    public string gameSetupScene = "Demo 3";
  
    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Wird vom "Spiel starten" Button aufgerufen
    /// Lädt die Spieler-Einstellungs-Scene (Demo 3)
    /// </summary>
    public void StartGame()
    {
        if (!string.IsNullOrEmpty(gameSetupScene))
        {
            Debug.Log($"[Menu] Starte Spiel - Lade Scene: {gameSetupScene}");
            SceneManager.LoadScene(gameSetupScene);
        }
        else
        {
            Debug.LogError("[Menu] gameSetupScene ist nicht gesetzt!");
        }
    }
}
