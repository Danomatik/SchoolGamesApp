using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script to load a saved game when button is clicked
/// Add this to a button and set the target scene (usually MainScene)
/// </summary>
public class LoadSavedGame : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene to load (usually MainScene)")]
    public string sceneToLoad = "MainScene";

    [Header("Save File Settings")]
    [Tooltip("If true, will only load if save file exists. If false, will always load scene.")]
    public bool requireSaveFile = true;

    /// <summary>
    /// Called by button - loads the scene and marks that saved game should be loaded
    /// </summary>
    public void LoadGameScene()
    {
        // Check if save file exists
        var saveManager = FindFirstObjectByType<GameSaveManager>();
        if (saveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }

        if (requireSaveFile && !saveManager.HasSaveFile())
        {
            Debug.LogWarning("⚠️ Kein gespeichertes Spiel gefunden!");
            // Optionally show a UI message to the user
            return;
        }

        // Mark that we want to load saved game
        PlayerPrefs.SetInt("LoadSavedGame", 1);
        PlayerPrefs.Save();
        
        // Verify the flag was set
        int flagValue = PlayerPrefs.GetInt("LoadSavedGame", 0);
        Debug.Log($"[LoadSavedGame] ✅ Flag gesetzt: LoadSavedGame = {flagValue}");

        Debug.Log($"🔄 Lade Scene: {sceneToLoad} (mit gespeichertem Spiel)");
        
        // Load the scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is not set on " + gameObject.name + "!");
        }
    }
}

