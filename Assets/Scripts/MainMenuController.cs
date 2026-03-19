using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the single-scene main menu flow:
/// Main Menu → Player Setup → Game Settings → Load MainScene
/// Attach to the Panel GameObject in the Menu scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu")]
    [Tooltip("The ButtonsContainer (Spiel starten, Quizmodus, Spiel laden)")]
    public GameObject buttonsContainer;
    [Tooltip("The BottomIcons panel (Info, Settings)")]
    public GameObject bottomIcons;
    [Tooltip("The Logo GameObject in the MenuContainer")]
    public GameObject logo;

    [Header("Quiz Mode")]
    [Tooltip("Container with the 3 mode buttons (Lernmodus, Punktemodus, Prüfungsmodus)")]
    public GameObject quizModusButtonContainer;
    [Tooltip("The top-level QuizContainer GameObject")]
    public GameObject quizContainer;
    [Tooltip("The QuizController on the QuizContainer")]
    public QuizController quizController;
    [Tooltip("The container holding all EndScreens (Game Over, etc.)")]
    public GameObject endScreenContainer;

    [Header("Difficulty Selection")]
    [Tooltip("The DifficultyPanel GameObject")]
    public GameObject difficultyPanel;
    public TextMeshProUGUI modeNameText;
    public TextMeshProUGUI modeSubText;
    [Tooltip("The clickable cards (buttons)")]
    public Button juniorButton;
    public Button seniorButton;
    [Tooltip("The TagText (Tag1) inside the cards")]
    public TextMeshProUGUI juniorTagText;
    public TextMeshProUGUI seniorTagText;
    [Tooltip("The History button inside DifficultyPanel (only visible in Exam mode)")]
    public Button difficultyHistoryButton;
    [Tooltip("The History button prefab to instantiate inside CardsContainer")]
    public GameObject historyButtonPrefab;

    [Header("Exam History")]
    public GameObject examHistoryPanel;
    public Transform historyContent;
    public GameObject historyEntryWinPrefab;
    public GameObject historyEntryLossPrefab;
    public Button historyMainMenuButton;
    public Button historyNewExamButton;

    private QuizController.QuizMode _selectedQuizMode;

    [Header("Options")]
    [Tooltip("The OptionsPanel (Musik Lautstärke etc.)")]
    public GameObject optionsPanel;

    [Header("Anleitung")]
    [Tooltip("The AnleitungPanel (rules pages)")]
    public GameObject anleitungPanel;

    [Header("Player Setup")]
    [Tooltip("The PlayerSetupPanel")]
    public GameObject playerSetupPanel;
    [Tooltip("The PlayerRow prefab to instantiate")]
    public GameObject playerRowPrefab;
    [Tooltip("The Content transform inside PlayersScrollView")]
    public Transform playerRowContent;
    [Tooltip("The AddPlayerButton")]
    public Button addPlayerButton;
    [Tooltip("The player count text (e.g. '2 / 6 Spieler')")]
    public TextMeshProUGUI playerCountText;
    [Tooltip("The 'Weiter' button in PlayerSetupPanel")]
    public Button weiterButton;

    [Header("Game Settings")]
    [Tooltip("The GameSettingsPanel")]
    public GameObject gameSettingsPanel;
    [Tooltip("The TimeSlider for game duration")]
    public Slider timeSlider;
    [Tooltip("The TimeValue text that shows the current slider value")]
    public TextMeshProUGUI timeValueText;
    [Tooltip("The TimeUnit text (e.g. 'Minuten')")]
    public TextMeshProUGUI timeUnitText;
    [Tooltip("The 'Spiel starten' button in GameSettingsPanel")]
    public Button startGameButton;

    [Header("Scene")]
    [Tooltip("The scene to load when starting the game")]
    public string gameSceneName = "MainScene";

    [Header("Limits")]
    public int minPlayers = 2;
    public int maxPlayers = 6;

    // Player color palette matching the screenshot style
    private static readonly Color[] playerColors = new Color[]
    {
        new Color(0.30f, 0.69f, 0.31f),  // Green
        new Color(0.13f, 0.59f, 0.95f),  // Blue
        new Color(0.98f, 0.74f, 0.02f),  // Yellow/Gold
        new Color(0.90f, 0.30f, 0.24f),  // Red
        new Color(0.61f, 0.15f, 0.69f),  // Purple
        new Color(1.00f, 0.60f, 0.00f),  // Orange
    };

    // Track spawned player rows
    private List<GameObject> playerRows = new List<GameObject>();

    private void Start()
    {
        // Ensure correct initial state
        ShowMainMenu();
        SetActive(endScreenContainer, false);

        // Wire button listeners
        if (addPlayerButton != null)
            addPlayerButton.onClick.AddListener(AddPlayerRow);
        if (weiterButton != null)
            weiterButton.onClick.AddListener(OnWeiterClicked);
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        if (timeSlider != null)
        {
            timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            // Initialize the display with the current slider value
            OnTimeSliderChanged(timeSlider.value);
        }

        // Auto-find DifficultyPanel references if not set
        AutoFindDifficultyReferences();

        // Auto-find ExamHistoryPanel references if not set
        AutoFindHistoryReferences();

        if (juniorButton != null) juniorButton.onClick.AddListener(OnJuniorClicked);
        if (seniorButton != null) seniorButton.onClick.AddListener(OnSeniorClicked);
        
        if (difficultyHistoryButton != null) difficultyHistoryButton.onClick.AddListener(OnDifficultyHistoryClicked);
        if (historyMainMenuButton != null) historyMainMenuButton.onClick.AddListener(OnHistoryMainMenuClicked);
        if (historyNewExamButton != null) historyNewExamButton.onClick.AddListener(OnHistoryNewExamClicked);
    }

    private void AutoFindDifficultyReferences()
    {
        if (difficultyPanel == null) difficultyPanel = transform.Find("MenuContainer/DifficultyPanel")?.gameObject;
        if (difficultyPanel == null) return;

        if (modeNameText == null) modeNameText = difficultyPanel.transform.Find("ModeName")?.GetComponent<TextMeshProUGUI>();
        if (modeSubText == null)  modeSubText  = difficultyPanel.transform.Find("ModeSub")?.GetComponent<TextMeshProUGUI>();
        
        if (juniorButton == null) juniorButton = difficultyPanel.transform.Find("CardsContainer/JuniorCard")?.GetComponent<Button>();
        if (seniorButton == null) seniorButton = difficultyPanel.transform.Find("CardsContainer/SeniorCard")?.GetComponent<Button>();

        if (juniorTagText == null) juniorTagText = difficultyPanel.transform.Find("CardsContainer/JuniorCard/TagsRow/Tag1/TagText")?.GetComponent<TextMeshProUGUI>();
        if (seniorTagText == null) seniorTagText = difficultyPanel.transform.Find("CardsContainer/SeniorCard/TagsRow/Tag1/TagText")?.GetComponent<TextMeshProUGUI>();

        if (difficultyHistoryButton == null) difficultyHistoryButton = difficultyPanel.transform.Find("CardsContainer/History")?.GetComponent<Button>();
        if (historyButtonPrefab == null) historyButtonPrefab = Resources.Load<GameObject>("Prefabs/History");
    }

    private void AutoFindHistoryReferences()
    {
        if (examHistoryPanel == null) examHistoryPanel = transform.Find("MenuContainer/ExamHistoryPanel")?.gameObject;
        if (examHistoryPanel == null) return;

        if (historyContent == null) historyContent = examHistoryPanel.transform.Find("HistoryScrollView/Viewport/Content");
        
        if (historyMainMenuButton == null) historyMainMenuButton = examHistoryPanel.transform.Find("BottomBar/MainMenuButton")?.GetComponent<Button>();
        if (historyNewExamButton == null) historyNewExamButton = examHistoryPanel.transform.Find("BottomBar/NewExamButton")?.GetComponent<Button>();
        
        // Try to load prefabs from Resources if not assigned
        if (historyEntryWinPrefab == null) historyEntryWinPrefab = Resources.Load<GameObject>("Prefabs/HistoryEntryW");
        if (historyEntryLossPrefab == null) historyEntryLossPrefab = Resources.Load<GameObject>("Prefabs/HistoryEntryL");
    }

    /// <summary>
    /// Shows the main menu (ButtonsContainer + BottomIcons), hides other panels.
    /// </summary>
    public void ShowMainMenu()
    {
        SetActive(buttonsContainer, true);
        SetActive(bottomIcons, true);
        SetActive(logo, true);
        SetActive(playerSetupPanel, false);
        SetActive(gameSettingsPanel, false);
        SetActive(optionsPanel, false);
        SetActive(anleitungPanel, false);
        SetActive(quizModusButtonContainer, false);
        SetActive(difficultyPanel, false);
        SetActive(examHistoryPanel, false);
        SetActive(quizContainer, false);
    }

    // ─── Quiz Mode ────────────────────────────────────────────────────

    /// <summary>
    /// Called by the "Quizmodus" button in ButtonsContainer.
    /// Hides the main buttons, shows the 3 quiz mode buttons.
    /// </summary>
    public void OnQuizModusClicked()
    {
        Debug.Log("[MainMenuController] Quiz mode picker opened");
        SetActive(buttonsContainer, false);
        SetActive(quizModusButtonContainer, true);
    }

    /// <summary>Called by the Lernmodus button.</summary>
    public void OnLernmodusClicked()
    {
        OpenDifficultyPanel(QuizController.QuizMode.Learn);
    }

    /// <summary>Called by the Punktemodus button.</summary>
    public void OnPunktemodusClicked()
    {
        OpenDifficultyPanel(QuizController.QuizMode.Score);
    }

    /// <summary>Called by the Prüfungsmodus button.</summary>
    public void OnPrüfungsmodusClicked()
    {
        OpenDifficultyPanel(QuizController.QuizMode.Exam);
    }

    private void OpenDifficultyPanel(QuizController.QuizMode mode)
    {
        _selectedQuizMode = mode;
        
        // Hide all possible menu containers to ensure clean transition
        SetActive(buttonsContainer, false);
        SetActive(quizModusButtonContainer, false);
        SetActive(optionsPanel, false);
        SetActive(anleitungPanel, false);

        // Show difficulty panel
        SetActive(difficultyPanel, true);
        if (difficultyPanel != null) difficultyPanel.transform.SetAsLastSibling();

        // Update texts based on mode
        switch (mode)
        {
            case QuizController.QuizMode.Learn:
                if (modeNameText) modeNameText.text = "Lernmodus";
                if (modeSubText)  modeSubText.text  = "Übe in deinem eigenen Tempo";
                if (juniorTagText) juniorTagText.text = "360 Fragen";
                if (seniorTagText) seniorTagText.text = "360 Fragen";
                if (difficultyHistoryButton != null) difficultyHistoryButton.gameObject.SetActive(false);
                break;
            case QuizController.QuizMode.Score:
                if (modeNameText) modeNameText.text = "Punktemodus";
                if (modeSubText)  modeSubText.text  = "Sammle Punkte und knacke den Highscore";
                if (juniorTagText) juniorTagText.text = "Endlos";
                if (seniorTagText) seniorTagText.text = "Endlos";
                if (difficultyHistoryButton != null) difficultyHistoryButton.gameObject.SetActive(false);
                break;
            case QuizController.QuizMode.Exam:
                if (modeNameText) modeNameText.text = "Prüfungsmodus";
                if (modeSubText)  modeSubText.text  = "Bestehst du die Prüfung? (70%)";
                if (juniorTagText) juniorTagText.text = "20 Fragen";
                if (seniorTagText) seniorTagText.text = "20 Fragen";
                EnsureHistoryButtonExists();
                if (difficultyHistoryButton != null)
                {
                    difficultyHistoryButton.gameObject.SetActive(true);
                    // Move it to the bottom of the container
                    difficultyHistoryButton.transform.SetAsLastSibling();
                }
                break;
        }
    }

    private void EnsureHistoryButtonExists()
    {
        if (difficultyHistoryButton != null) return;
        if (difficultyPanel == null || historyButtonPrefab == null) return;

        Transform cardsContainer = difficultyPanel.transform.Find("CardsContainer");
        if (cardsContainer == null) return;

        GameObject historyObj = Instantiate(historyButtonPrefab, cardsContainer);
        historyObj.name = "History";
        difficultyHistoryButton = historyObj.GetComponent<Button>();
        
        if (difficultyHistoryButton != null)
        {
            difficultyHistoryButton.onClick.AddListener(OnDifficultyHistoryClicked);
        }
    }

    /// <summary>
    /// Returns from DifficultyPanel back to mode selection.
    /// </summary>
    public void OnBackFromDifficulty()
    {
        SetActive(difficultyPanel, false);
        SetActive(quizModusButtonContainer, true);
    }

    // ─── Exam History ───────────────────────────────────────────────

    private void OnDifficultyHistoryClicked()
    {
        Debug.Log("[MainMenuController] Opening Exam History");
        SetActive(difficultyPanel, false);
        SetActive(examHistoryPanel, true);
        if (examHistoryPanel != null) examHistoryPanel.transform.SetAsLastSibling();
        
        PopulateExamHistory();
    }

    private void PopulateExamHistory()
    {
        if (historyContent == null) return;

        // Clear existing entries
        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }

        if (historyEntryWinPrefab == null || historyEntryLossPrefab == null)
        {
            Debug.LogError("[MainMenuController] History prefabs not assigned!");
            return;
        }

        // Fetch DE results (assume DE as primary for now, could fetch EN as well)
        List<ExamResult> allResults = new List<ExamResult>();
        allResults.AddRange(ExamProgressStore.GetAllResults(QuizLang.DE, LearnLevel.Junior));
        allResults.AddRange(ExamProgressStore.GetAllResults(QuizLang.DE, LearnLevel.Senior));
        
        // Sort by timestamp descending (newest first)
        allResults = allResults.OrderByDescending(r => r.timestamp).ToList();

        foreach (var result in allResults)
        {
            GameObject prefabToUse = result.passed ? historyEntryWinPrefab : historyEntryLossPrefab;
            GameObject entry = Instantiate(prefabToUse, historyContent);

            // Fetch generic TextMeshPro components based on their common naming in screenshots
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();

            // E.g. Date: 15.03.2026, Perc: 87%, Level: JUNIOR, Time: 19:01, Status: Bestanden
            // We do basic string matching or order based if exact names aren't strictly guaranteed,
            // but usually we can match text components by name.
            foreach (var txt in texts)
            {
                string n = txt.gameObject.name.ToLower();
                
                // Usually "DateText" or "Date"
                if (n.Contains("date"))
                {
                    // "2026-03-15 19:01:23"
                    if (System.DateTime.TryParse(result.timestamp, out System.DateTime dt))
                        txt.text = dt.ToString("dd.MM.yyyy");
                    else
                        txt.text = result.timestamp;
                }
                else if (n.Contains("time"))
                {
                    if (System.DateTime.TryParse(result.timestamp, out System.DateTime dt))
                        txt.text = dt.ToString("HH:mm");
                }
                else if (n.Contains("percent") || txt.text.Contains("%"))
                {
                    txt.text = $"{Mathf.RoundToInt(result.percentageScore)}%";
                }
                else if (n.Contains("level") || txt.text.Contains("JUNIOR") || txt.text.Contains("SENIOR"))
                {
                    txt.text = result.level.ToString().ToUpper();
                }
                else if (n.Contains("status"))
                {
                    txt.text = result.passed ? "Bestanden" : "Nicht bestanden";
                }
            }
        }
    }

    private void OnHistoryMainMenuClicked()
    {
        SetActive(examHistoryPanel, false);
        ShowMainMenu();
    }

    private void OnHistoryNewExamClicked()
    {
        SetActive(examHistoryPanel, false);
        // Start new exam => goes back to Difficulty panel for Exam
        OpenDifficultyPanel(QuizController.QuizMode.Exam);
    }

    // ─── Junior / Senior Handlers ────────────────────────────────────

    private void OnJuniorClicked()
    {
        ShowQuizContainer(_selectedQuizMode, LearnLevel.Junior);
    }

    private void OnSeniorClicked()
    {
        ShowQuizContainer(_selectedQuizMode, LearnLevel.Senior);
    }

    /// <summary>
    /// Hides the menu UI and opens the QuizContainer for the specified mode.
    /// </summary>
    private void ShowQuizContainer(QuizController.QuizMode mode, LearnLevel level)
    {
        Debug.Log($"[MainMenuController] Starting quiz mode: {mode}");

        // Hide menu elements
        SetActive(buttonsContainer, false);
        SetActive(quizModusButtonContainer, false);
        SetActive(bottomIcons, false);
        SetActive(logo, false);

        // Show quiz
        SetActive(quizContainer, true);
        SetActive(difficultyPanel, false);

        if (quizController != null)
            quizController.StartMode(mode, level);
        else
            Debug.LogError("[MainMenuController] QuizController reference not set!");
    }

    /// <summary>
    /// Called by QuizController when the user presses the Menü button inside the quiz.
    /// Returns to the main menu.
    /// </summary>
    public void HideQuizContainer()
    {
        Debug.Log("[MainMenuController] Returning to main menu from quiz");
        SetActive(quizContainer, false);
        SetActive(endScreenContainer, false);
        ShowMainMenu();
    }

    /// <summary>
    /// Shows the EndScreenContainer (e.g. at the end of a quiz).
    /// </summary>
    public void ShowEndScreen()
    {
        Debug.Log("[MainMenuController] Showing Game Over Screen");
        SetActive(endScreenContainer, true);
    }

    public void ShowQuizEndScreen()
    {
        // Removed, using ShowEndScreen on MainMenuController instead
    }

    // ─── Anleitung Panel ─────────────────────────────────────────────

    /// <summary>
    /// Called by the InfoButton in BottomIcons.
    /// Opens the AnleitungPanel, hides the main menu.
    /// </summary>
    public void OnAnleitungClicked()
    {
        Debug.Log("[MainMenuController] Anleitung opened");
        SetActive(buttonsContainer, false);
        SetActive(bottomIcons, false);
        SetActive(anleitungPanel, true);
    }

    // ─── Options Panel ───────────────────────────────────────────────

    /// <summary>
    /// Called by the SettingsButton (gear icon) in BottomIcons.
    /// Opens the OptionsPanel, hides the main menu.
    /// </summary>
    public void OnOptionsClicked()
    {
        Debug.Log("[MainMenuController] Options opened");
        SetActive(buttonsContainer, false);
        SetActive(bottomIcons, false);
        SetActive(optionsPanel, true);
    }

    /// <summary>
    /// Called by the CloseButton (Schließen) in OptionsPanel.
    /// Closes OptionsPanel and returns to main menu.
    /// </summary>
    public void OnOptionsClose()
    {
        Debug.Log("[MainMenuController] Options closed");
        SetActive(optionsPanel, false);
        SetActive(buttonsContainer, true);
        SetActive(bottomIcons, true);
    }

    // ─── STEP 1: "Spiel starten" from Main Menu ─────────────────────

    /// <summary>
    /// Called by the "Spiel starten" (PlayButton) OnClick.
    /// Hides main menu, shows PlayerSetupPanel with 2 default rows.
    /// </summary>
    public void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Spiel starten → Player Setup");

        SetActive(buttonsContainer, false);
        SetActive(bottomIcons, false);
        SetActive(playerSetupPanel, true);
        SetActive(gameSettingsPanel, false);

        // Clear any existing rows
        ClearAllPlayerRows();

        // Spawn 2 default player rows
        AddPlayerRow();
        AddPlayerRow();
    }

    // ─── STEP 2: Player Setup ────────────────────────────────────────

    /// <summary>
    /// Adds a new PlayerRow to the scroll view.
    /// Called by the "Spieler hinzufügen" button.
    /// </summary>
    public void AddPlayerRow()
    {
        if (playerRows.Count >= maxPlayers)
        {
            Debug.LogWarning($"[MainMenuController] Max players ({maxPlayers}) reached!");
            return;
        }

        if (playerRowPrefab == null || playerRowContent == null)
        {
            Debug.LogError("[MainMenuController] PlayerRow prefab or content transform not assigned!");
            return;
        }

        GameObject newRow = Instantiate(playerRowPrefab, playerRowContent);
        playerRows.Add(newRow);

        int playerNumber = playerRows.Count;

        // Set number badge text and color
        SetPlayerRowNumber(newRow, playerNumber);

        // Wire the delete button (the X button)
        Button deleteBtn = FindDeleteButton(newRow);
        if (deleteBtn != null)
        {
            deleteBtn.onClick.RemoveAllListeners();
            deleteBtn.onClick.AddListener(() => RemovePlayerRow(newRow));
        }

        UpdatePlayerCountDisplay();
        UpdateAddButtonState();

        Debug.Log($"[MainMenuController] Added player row #{playerNumber}");
    }

    /// <summary>
    /// Removes a player row and renumbers remaining rows.
    /// </summary>
    public void RemovePlayerRow(GameObject row)
    {
        if (playerRows.Count <= minPlayers)
        {
            Debug.LogWarning($"[MainMenuController] Cannot remove — minimum {minPlayers} players required!");
            return;
        }

        playerRows.Remove(row);
        Destroy(row);

        // Renumber remaining rows
        for (int i = 0; i < playerRows.Count; i++)
        {
            SetPlayerRowNumber(playerRows[i], i + 1);
        }

        UpdatePlayerCountDisplay();
        UpdateAddButtonState();

        Debug.Log($"[MainMenuController] Removed player row. Remaining: {playerRows.Count}");
    }

    private void ClearAllPlayerRows()
    {
        foreach (var row in playerRows)
        {
            if (row != null) Destroy(row);
        }
        playerRows.Clear();
    }

    private void SetPlayerRowNumber(GameObject row, int number)
    {
        // The first child of PlayerRow is the number badge container
        // Find the TMP text in the first child (the badge)
        if (row.transform.childCount > 0)
        {
            Transform badge = row.transform.GetChild(0);
            TextMeshProUGUI badgeText = badge.GetComponentInChildren<TextMeshProUGUI>();
            if (badgeText != null)
            {
                badgeText.text = number.ToString();
            }

            // Set badge color
            Image badgeImage = badge.GetComponent<Image>();
            if (badgeImage != null && number - 1 < playerColors.Length)
            {
                badgeImage.color = playerColors[number - 1];
            }
        }
    }

    private Button FindDeleteButton(GameObject row)
    {
        // The delete button is the last child of the PlayerRow (the X button)
        if (row.transform.childCount >= 3)
        {
            Transform deleteTransform = row.transform.GetChild(row.transform.childCount - 1);
            return deleteTransform.GetComponent<Button>();
        }
        return null;
    }

    private void UpdatePlayerCountDisplay()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"{playerRows.Count} / {maxPlayers} Spieler";
        }
    }

    private void UpdateAddButtonState()
    {
        if (addPlayerButton != null)
        {
            addPlayerButton.interactable = playerRows.Count < maxPlayers;
        }
    }

    // ─── STEP 2 → 3: "Weiter" ───────────────────────────────────────

    /// <summary>
    /// Called by the "Weiter" button in PlayerSetupPanel.
    /// Saves player data and transitions to GameSettingsPanel.
    /// </summary>
    public void OnWeiterClicked()
    {
        if (playerRows.Count < minPlayers)
        {
            Debug.LogWarning($"[MainMenuController] Need at least {minPlayers} players!");
            return;
        }

        // Save player count and names to PlayerPrefs
        PlayerPrefs.SetInt("PlayerCount", playerRows.Count);

        for (int i = 0; i < playerRows.Count; i++)
        {
            string playerName = GetPlayerNameFromRow(playerRows[i], i + 1);
            PlayerPrefs.SetString($"PlayerName_{i + 1}", playerName);
        }
        PlayerPrefs.Save();

        Debug.Log($"[MainMenuController] Saved {playerRows.Count} players → Game Settings");

        SetActive(playerSetupPanel, false);
        SetActive(gameSettingsPanel, true);
    }

    private string GetPlayerNameFromRow(GameObject row, int fallbackNumber)
    {
        TMP_InputField inputField = row.GetComponentInChildren<TMP_InputField>();
        if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
        {
            return inputField.text;
        }
        return $"Spieler {fallbackNumber}";
    }

    // ─── STEP 3: Game Settings ───────────────────────────────────────

    /// <summary>
    /// Called when the time slider value changes.
    /// Updates the TimeValue display text.
    /// </summary>
    public void OnTimeSliderChanged(float value)
    {
        int minutes = Mathf.RoundToInt(value);

        if (timeValueText != null)
        {
            timeValueText.text = minutes.ToString();
        }

        if (timeUnitText != null)
        {
            timeUnitText.text = minutes == 1 ? "Minute" : "Minuten";
        }
    }

    /// <summary>
    /// Sets the time slider to a specific preset value (in minutes).
    /// Wire each preset button's OnClick to this with the desired value.
    /// </summary>
    public void SetTimePreset(float minutes)
    {
        if (timeSlider != null)
        {
            timeSlider.value = minutes; // This automatically triggers OnTimeSliderChanged
        }
    }

    /// <summary>
    /// Called by the "Zurück" button in GameSettingsPanel.
    /// Returns to PlayerSetupPanel without losing player data.
    /// </summary>
    public void OnBackToPlayerSetup()
    {
        Debug.Log("[MainMenuController] Zurück → Player Setup");
        SetActive(gameSettingsPanel, false);
        SetActive(playerSetupPanel, true);
    }

    /// <summary>
    /// Called by the "Spiel starten" button in GameSettingsPanel.
    /// Saves game duration and loads the game scene.
    /// </summary>
    public void OnStartGameClicked()
    {
        // Save game duration
        float duration = timeSlider != null ? timeSlider.value : 5f;
        PlayerPrefs.SetFloat("GameDuration", duration);

        // Mark as new game (not loading a save)
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        PlayerPrefs.Save();

        Debug.Log($"[MainMenuController] Starting game — Duration: {duration} min, Players: {PlayerPrefs.GetInt("PlayerCount")}");

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("[MainMenuController] Game scene name not set!");
        }
    }

    // ─── Utility ─────────────────────────────────────────────────────

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
