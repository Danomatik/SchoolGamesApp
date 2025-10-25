using UnityEngine;
using UnityEngine.UI; // Für LayoutElement und LayoutGroup

public class LayoutRefresher : MonoBehaviour
{
    // Diese Funktion wird nach dem Start einmal aufgerufen.
    void Start()
    {
        // Ruft die Funktion zur Aktualisierung des Layouts im nächsten Frame auf.
        // Dies gibt dem Canvas Zeit, seine finale Größe zu berechnen.
        Invoke("RefreshLayout", 0.01f);
    }

    void RefreshLayout()
    {
        // 1. Zuerst das Layout des Kind-Objekts (LayoutGroup) aktualisieren
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        // 2. Danach das Layout des Eltern-Objekts aktualisieren (um sicherzugehen)
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}