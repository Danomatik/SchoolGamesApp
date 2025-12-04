using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    // Die zu ladende Szene kann im Inspector festgelegt werden
    public string sceneToLoad;

    [Header("Save Game Before Switching")]
    [Tooltip("Wenn aktiviert, wird das Spiel gespeichert bevor die Scene gewechselt wird")]
    public bool saveGameBeforeSwitch = false;

    // Diese öffentliche Funktion wird vom Button aufgerufen
    public void LoadTargetScene()
    {
        // Wenn saveGameBeforeSwitch aktiviert ist, speichere zuerst
        if (saveGameBeforeSwitch)
        {
            SaveGameBeforeSwitch();
        }

        // Prüfen, ob der Szenenname gültig ist (gute UX-Praxis)
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is not set on the " + gameObject.name + " button!");
        }
    }

    /// <summary>
    /// Speichert das Spiel bevor die Scene gewechselt wird
    /// </summary>
    private void SaveGameBeforeSwitch()
    {
        // Finde GameManager und GameInitiator
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.gameInitiator != null)
        {
            var saveManager = FindFirstObjectByType<GameSaveManager>();
            if (saveManager == null)
            {
                GameObject saveManagerObj = new GameObject("GameSaveManager");
                saveManager = saveManagerObj.AddComponent<GameSaveManager>();
            }

            bool saved = saveManager.SaveGame(gameManager.gameInitiator);
            if (saved)
            {
                Debug.Log("✅ Spiel gespeichert vor Scene-Wechsel!");
            }
            else
            {
                Debug.LogWarning("⚠️ Spiel konnte nicht gespeichert werden, wechsle trotzdem zur Scene.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager nicht gefunden. Kann Spiel nicht speichern.");
        }
    }
}
