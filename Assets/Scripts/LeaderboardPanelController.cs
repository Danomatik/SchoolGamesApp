using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bestenliste / Leaderboard-Popup.
/// Inspector-Setup:
///   - panel:          Root des Leaderboard-Panels (wird aktiviert/deaktiviert)
///   - templateRow:    ResultRow1 unter ResultsList (dient als Template, wird kopiert)
///   - rowsParent:     ResultsList (Eltern-Transform der Zeilen)
///   - backButton:     Back-Button in ButtonsRow (schließt das Popup)
///   - background:     optional, Background-Button (Klick schließt ebenfalls)
///
/// Erwartete Row-Hierarchie pro ResultRow:
///   ResultRow
///     ├── RankCircle  (Image, wird in Spielerfarbe gefärbt)
///     │     └── RankText  (TMP_Text, zeigt die Platzierung)
///     └── Info
///           ├── NameText   (TMP_Text)
///           ├── StatsText  (TMP_Text)
///           └── MoneyText  (TMP_Text)
/// </summary>
public class LeaderboardPanelController : MonoBehaviour
{
    [Header("Panel Referenzen")]
    public GameObject popupOverlay;   // PopupOverlay (gemeinsamer dim/colored Hintergrund aller Popups)
    public GameObject panel;          // Das Leaderboard-Panel selbst
    public GameObject templateRow;
    public Transform rowsParent;
    public Button backButton;
    public Button background;

    [Header("Open-Button (Quick Actions)")]
    [Tooltip("Der Leaderboard-Button unter QuickActions, der das Popup öffnet.")]
    public Button openButton;
    [Tooltip("Wird automatisch gefunden, falls leer.")]
    public DiceManager diceManager;
    [Tooltip("GameManager (für Zugriff auf alle PlayerCTRLs). Wird automatisch gefunden, falls leer.")]
    public GameManager gameManager;

    [Header("Data Source")]
    public GameInitiator gameInitiator;

    // Spielerfarben — gleiche Palette wie MainMenuController
    private static readonly Color[] PLAYER_COLORS = new Color[]
    {
        new Color(0.30f, 0.69f, 0.31f),  // Green
        new Color(0.13f, 0.59f, 0.95f),  // Blue
        new Color(0.98f, 0.74f, 0.02f),  // Yellow/Gold
        new Color(0.90f, 0.30f, 0.24f),  // Red
        new Color(0.61f, 0.15f, 0.69f),  // Purple
        new Color(1.00f, 0.60f, 0.00f),  // Orange
    };

    private readonly List<GameObject> _spawnedRows = new();

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(ClosePanel);
        if (background != null) background.onClick.AddListener(ClosePanel);
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
        }
        if (gameInitiator == null) gameInitiator = FindObjectOfType<GameInitiator>();
        if (diceManager   == null) diceManager   = FindObjectOfType<DiceManager>();
        if (gameManager   == null) gameManager   = FindObjectOfType<GameManager>();

        // Template darf nicht sichtbar sein
        if (templateRow != null) templateRow.SetActive(false);

        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (openButton == null) return;
        bool canOpen = !IsBusy() && (panel == null || !panel.activeSelf);
        if (openButton.interactable != canOpen)
            openButton.interactable = canOpen;
    }

    /// <summary>
    /// Busy = Würfel rollt oder irgendein Spieler bewegt sich gerade.
    /// </summary>
    private bool IsBusy()
    {
        if (diceManager != null && diceManager.IsRolling) return true;
        if (gameManager != null && gameManager.players != null)
        {
            foreach (var p in gameManager.players)
                if (p != null && p.IsMoving) return true;
        }
        return false;
    }

    public void OpenPanel()
    {
        // Sicherheitsnetz: nicht öffnen während gewürfelt wird oder ein Spieler läuft.
        if (IsBusy()) return;

        if (panel == null) panel = gameObject;
        Refresh();
        if (popupOverlay != null) popupOverlay.SetActive(true);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
        if (popupOverlay != null) popupOverlay.SetActive(false);
    }

    private void Refresh()
    {
        if (templateRow == null || rowsParent == null)
        {
            Debug.LogError("[LeaderboardPanelController] templateRow oder rowsParent nicht zugewiesen.");
            return;
        }

        // Alte Klone entfernen
        foreach (var go in _spawnedRows) if (go) Destroy(go);
        _spawnedRows.Clear();

        var players = (gameInitiator != null && gameInitiator.CurrentGame != null)
            ? gameInitiator.CurrentGame.AllPlayers
            : new List<PlayerData>();

        var fields = (gameInitiator != null) ? gameInitiator.GetCompanyFields() : null;

        var sorted = players
            .Where(p => p != null)
            .OrderByDescending(p => p.Money)
            .ThenByDescending(p => CountOwnedFields(p, fields))
            .ToList();

        for (int rank = 1; rank <= sorted.Count; rank++)
        {
            var p = sorted[rank - 1];
            int owned = CountOwnedFields(p, fields);
            int ags   = CountAGs(p, fields);

            GameObject row = Instantiate(templateRow, rowsParent);
            row.name = $"ResultRow_{rank}_{p.PlayerName}";
            row.SetActive(true);
            _spawnedRows.Add(row);

            // Rank-Circle einfärben + Nummer setzen
            var rankCircle = FindChildContaining(row.transform, "RankCircle");
            if (rankCircle != null)
            {
                var img = rankCircle.GetComponent<Image>();
                if (img != null) img.color = GetColorForPlayer(p.PlayerID);

                var rankText = FindTMPContaining(rankCircle, "RankText");
                if (rankText != null) rankText.text = rank.ToString();
            }

            // Info-Texte
            var info = FindChildContaining(row.transform, "Info");
            if (info != null)
            {
                var nameText  = FindTMPContaining(info, "NameText");
                var statsText = FindTMPContaining(info, "StatsText");
                var moneyText = FindTMPContaining(info, "MoneyText");

                if (nameText)  nameText.text  = string.IsNullOrEmpty(p.PlayerName) ? $"Spieler {p.PlayerID}" : p.PlayerName;
                if (statsText) statsText.text = $"{owned} Unternehmen • {ags} AGs";
                if (moneyText) moneyText.text = $"€{p.Money:N0}".Replace(",", ".");
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static int CountOwnedFields(PlayerData p, List<CompanyField> fields)
    {
        if (fields == null || p == null) return p?.companies?.Count ?? 0;
        int c = 0;
        foreach (var f in fields) if (f != null && f.ownerID == p.PlayerID) c++;
        return c;
    }

    private static int CountAGs(PlayerData p, List<CompanyField> fields)
    {
        if (fields == null || p == null) return 0;
        int c = 0;
        foreach (var f in fields)
            if (f != null && f.ownerID == p.PlayerID && f.level == CompanyLevel.AG) c++;
        return c;
    }

    private static Color GetColorForPlayer(int playerId)
    {
        int idx = Mathf.Clamp(playerId - 1, 0, PLAYER_COLORS.Length - 1);
        return PLAYER_COLORS[idx];
    }

    /// <summary>Findet das erste Kind, dessen Name den Substring enthält (case-insensitive, rekursiv).</summary>
    private static Transform FindChildContaining(Transform root, string namePart)
    {
        if (root == null) return null;
        foreach (Transform child in root)
        {
            if (child.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
            var deeper = FindChildContaining(child, namePart);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private static TMP_Text FindTMPContaining(Transform root, string namePart)
    {
        var t = FindChildContaining(root, namePart);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }
}
