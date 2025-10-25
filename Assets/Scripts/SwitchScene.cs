using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
        // Die zu ladende Szene kann im Inspector festgelegt werden
    public string sceneToLoad;

    // Diese öffentliche Funktion wird vom Button aufgerufen
    public void LoadTargetScene()
    {
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
}
