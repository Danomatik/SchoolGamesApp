using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Wichtig für Coroutines

public class LayoutRefresher : MonoBehaviour
{
    void Start()
    {
        // Starte die Coroutine, um das Layout im nächsten Frame zu aktualisieren
        StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        // Warte einen Frame (yield return null)
        // Dies gibt dem Canvas und allen Rect Transforms Zeit zur Initialisierung
        yield return null; 

        // 1. Das Layout des Containers aktualisieren
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        // Optional: Wenn das Panel in einem anderen Layout-Container liegt, 
        // musst du eventuell auch das Eltern-Layout neu berechnen lassen.
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }

        // UX-Cleanup: Entferne dieses Skript, da es nur einmal benötigt wird
        Destroy(this); 
    }
}