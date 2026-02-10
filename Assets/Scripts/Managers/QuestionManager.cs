using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections;

public class QuestionManager : MonoBehaviour
{
    public enum QuestionLanguage { English, German }
    public enum QuestionDifficulty { Junior, Senior }

    [Header("Question Set Selector")]
    public QuestionLanguage language = QuestionLanguage.English;
    public QuestionDifficulty difficulty = QuestionDifficulty.Junior;
    [System.Serializable]
    public class QuestionCategory
    {
        public List<QuestionData> gruendung;
        public List<QuestionData> investition;
        public List<QuestionData> ag;
    }

    [System.Serializable]
    public class QuestionDatabase
    {
        public QuestionCategory junior_en;
        public QuestionCategory junior_de;
        public QuestionCategory senior_en;
        public QuestionCategory senior_de;
    }

    private QuestionDatabase questionDatabase;
    private List<QuestionData> allQuestions = new List<QuestionData>();
    private QuizField[] quizFields;

    [Header("UI Elements")] // NEUER ABSCHNITT
    public GameObject quizPanel; // Ihr UI-Panel
    public TextMeshProUGUI questionText; // Das TMP-Feld für die Frage
    public TextMeshProUGUI questionID; // Array von TMP-Feldern für die Antworten (z.B. 4 Stück)
    // Optional: Buttons für die Antworten
    public Button[] optionButtons; // NEU: Array für die Antwort-Buttons
    public TextMeshProUGUI[] optionButtonTexts; // NEU: Array für die Texte AUF den Buttons

    [SerializeField]
    private GameObject moveButton;
    private int currentCorrectIndex = -1; // NEU: Merkt sich den korrekten Index
    private bool answerLocked = false;

        // --- Series quiz state ---
    private bool seriesActive = false;
    private int seriesTotal = 0;
    private int seriesRequired = 0;
    private int seriesIndex = 0;
    private int seriesCorrect = 0;
    private System.Action<bool> seriesOnDone = null;
    private HashSet<int> seriesUsedQuestionIds = new HashSet<int>();





    private UIManager uiManager;

    void Start()
    {
        uiManager = GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
        
        if (uiManager == null)
        {
            Debug.LogError("[QuestionManager] CRITICAL: UIManager not found! Quiz Panel will not work correctly in Overlay.");
        }
        else
        {
            Debug.Log("[QuestionManager] UIManager reference found.");
        }

        Debug.Log($"[QuestionManager] Startup Language: {language}, Difficulty: {difficulty}");
        LoadQuestions();
        FindQuizFields();
    }

    private void FindQuizFields()
    {
        quizFields = FindObjectsByType<QuizField>(FindObjectsSortMode.None);
    }

    private void LoadQuestions()
    {
        Debug.Log($"[QuestionManager] Attempting to load: lang={language}, diff={difficulty}");
        string lang = (language == QuestionLanguage.English) ? "EN" : "DE";
        string diff = (difficulty == QuestionDifficulty.Junior) ? "Junior" : "Senior";
        string fileName = $"Data/Schoolgames_Fragen_{diff}_{lang}";
        Debug.Log($"[QuestionManager] Loading file: {fileName}");

        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not load {fileName}.json from Resources/Data/");
            return;
        }
        try
        {
            questionDatabase = JsonUtility.FromJson<QuestionDatabase>(jsonFile.text);
            if (questionDatabase == null)
            {
                Debug.LogError("QuestionManager: questionDatabase is null after parsing!");
                return;
            }

            // Dynamic: check for correct QuestionCategory
            string key = $"{difficulty.ToString().ToLower()}_{(language == QuestionLanguage.English ? "en" : "de")}";
            var field = typeof(QuestionDatabase).GetField(key);
            var pickedCategory = field?.GetValue(questionDatabase) as QuestionCategory;
            if (pickedCategory == null)
            {
                Debug.LogError($"QuestionManager: key '{key}' is null! (Check your JSON file structure and field names)");
                return;
            }
            CompileAllQuestions();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
        }
    }

    private void CompileAllQuestions()
    {
        allQuestions.Clear();
        globalUsedQuestionIds.Clear(); // Reset used history on reload/change
        // Build key: "junior_en", "senior_en", etc.
        string key = $"{difficulty.ToString().ToLower()}_{(language == QuestionLanguage.English ? "en" : "de")}";
        QuestionCategory pickedCategory = null;

        // Use reflection to access public fields dynamically:
        var field = typeof(QuestionDatabase).GetField(key);
        if (field != null)
        {
            pickedCategory = field.GetValue(questionDatabase) as QuestionCategory;
        }

        if (pickedCategory != null)
        {
            if (pickedCategory.gruendung != null)
                allQuestions.AddRange(pickedCategory.gruendung);
            if (pickedCategory.investition != null)
                allQuestions.AddRange(pickedCategory.investition);
            if (pickedCategory.ag != null)
                allQuestions.AddRange(pickedCategory.ag);
        }
        else
        {
            Debug.LogError($"No questions found for {key}! Check your JSON structure & language/difficulty setting.");
        }
    }

    // Track used questions globally to avoid repeats until all are shown
    private HashSet<int> globalUsedQuestionIds = new HashSet<int>();

    public QuestionData GetRandomQuestion()
    {
        if (allQuestions.Count == 0)
        {
            Debug.LogWarning("No questions available!");
            return null;
        }

        // Filter out already used questions
        List<QuestionData> availableQuestions = new List<QuestionData>();
        foreach (var q in allQuestions)
        {
            if (!globalUsedQuestionIds.Contains(q.id))
            {
                availableQuestions.Add(q);
            }
        }

        // If all questions have been shown, reset the "deck"
        if (availableQuestions.Count == 0)
        {
            Debug.Log("[QuestionManager] All questions shown! Reshuffling deck.");
            globalUsedQuestionIds.Clear();
            availableQuestions.AddRange(allQuestions);
        }

        int randomIndex = Random.Range(0, availableQuestions.Count);
        QuestionData selected = availableQuestions[randomIndex];
        
        // Mark as used
        globalUsedQuestionIds.Add(selected.id);
        
        return selected;
    }

    public void PrintRandomQuestion()
    {
        QuestionData question = GetRandomQuestion();
        if (question != null)
        {
            string optionsText = "";
            for (int i = 0; i < question.options.Length; i++)
            {
                string marker = (i == question.correctIndex) ? "✓" : " ";
                optionsText += $"\n  {marker} {i + 1}. {question.options[i]}";
            }

            Debug.Log($"Question #{question.id}: {question.text}{optionsText}\nCorrect Answer: {question.correctIndex + 1}");

        }
    }
    public void SetLanguage(QuestionLanguage l)
    {
        language = l;
        LoadQuestions();
    }
    public void SetDifficulty(QuestionDifficulty d)
    {
        difficulty = d;
        LoadQuestions();
    }

    // Called by GameManager when player lands on a field
    public void CheckForQuizField(int fieldPosition)
    {
        if (quizFields == null)
        {
            FindQuizFields();
        }

        if (quizFields == null || quizFields.Length == 0)
        {
            return;
        }

        foreach (QuizField quizField in quizFields)
        {
            if (quizField.fieldIndex == fieldPosition)
            {
                quizField.TriggerQuestion();
                return;
            }
        }
    }

    // In QuestionManager.cs

    public void ShowQuestionInUI()
    {
        // Hole eine Frage wie bisher (dein Code)
        QuestionData q = GetRandomQuestion();
        if (q == null) return;

        answerLocked = false;
        currentCorrectIndex = q.correctIndex;

        // ✅ USE UIManager
        if (uiManager != null)
        {
            uiManager.ShowQuiz();
        }
        else if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        if (moveButton != null) moveButton.SetActive(false); // Würfeln blockieren solange Quiz offen

        if (questionText != null)
        {
            questionText.text = $"Frage {q.id}\n{q.text}";
        }
        if (questionID != null) questionID.text = "";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
            {
                optionButtonTexts[i].text = q.options[i];
            }

            // Click-Handler
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => HandleAnswer(idx));

            // Reset Outline
            var outline = optionButtons[i].GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }


    public void HandleAnswer(int selectedIndex)
    {
        if (answerLocked) return;
        answerLocked = true;

        foreach (var btn in optionButtons) btn.interactable = false;

        bool isCorrect = (selectedIndex == currentCorrectIndex);

        // Visual Feedback
        if (selectedIndex >= 0 && selectedIndex < optionButtons.Length)
        {
            var outline = optionButtons[selectedIndex].GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = isCorrect ? Color.green : Color.red;
            }
        }

        // Helper: Always show the correct answer if wrong one was picked
        if (!isCorrect && currentCorrectIndex >= 0 && currentCorrectIndex < optionButtons.Length)
        {
             var correctOutline = optionButtons[currentCorrectIndex].GetComponent<Outline>();
             if (correctOutline != null)
             {
                 correctOutline.enabled = true;
                 correctOutline.effectColor = Color.green;
             }
        }

        // Unterscheide zwischen Series und einzelnen Fragen
        if (seriesActive)
        {
            StartCoroutine(ContinueSeriesAfterDelay(isCorrect, 1.5f)); // Increased delay to see feedback
        }
        else
        {
            StartCoroutine(FinishQuizAfterDelay(isCorrect, 1.5f)); // Increased delay to see feedback
        }
    }


    private IEnumerator FinishQuizAfterDelay(bool isCorrect, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        // Alle Manager hängen am gleichen GameObject -> direkter Zugriff
        var gm = GetComponent<GameManager>();
        if (gm == null) gm = Object.FindFirstObjectByType<GameManager>(); // Fallback

        // GameManager führt Kauf/Upgrade aus und ruft IMMER EndTurn() (auch bei falscher Antwort)
        gm?.OnQuizResult(isCorrect);

        // ✅ USE UIManager
        if (uiManager != null)
        {
            uiManager.ClosePopup();
        }
        else if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        // 👉 WICHTIG: Move-Button wieder aktivieren, damit der NÄCHSTE Spieler würfeln kann
        // (EndTurn() hat isTurnInProgress bereits auf false gesetzt)
        if (moveButton != null)
            moveButton.SetActive(true);
    }
    // Starts a multi-question quiz series (e.g., 3 questions for AG upgrade)
    public void StartQuizSeries(int totalQuestions, int requiredCorrect, System.Action<bool> onDone)
    {
        // Guard: need questions and UI
        if (allQuestions == null || allQuestions.Count == 0)
        {
            Debug.LogWarning("StartQuizSeries: No questions available.");
            onDone?.Invoke(false);
            return;
        }

        seriesActive = true;
        seriesTotal = Mathf.Max(1, totalQuestions);
        seriesRequired = Mathf.Clamp(requiredCorrect, 1, seriesTotal);
        seriesIndex = 0;
        seriesCorrect = 0;
        seriesOnDone = onDone;
        seriesUsedQuestionIds.Clear();

        // Show panel + block rolling during the series
        // ✅ USE UIManager
        if (uiManager != null)
        {
            uiManager.ShowQuiz();
        }
        else if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        if (moveButton != null) moveButton.SetActive(false);

        ShowNextSeriesQuestion();
    }

    private void ShowNextSeriesQuestion()
    {
        // Fetch a random question not used in this series
        QuestionData q = GetRandomQuestion();
        if (q == null)
        {
            // Not enough unique questions -> fallback: finish with whatever we have
            Debug.LogWarning("Not enough unique questions for series. Finishing early.");
            FinishSeries();
            return;
        }

        // Keep track to avoid duplicates
        seriesUsedQuestionIds.Add(q.id);

        // Reuse your existing single-question UI setup
        answerLocked = false;
        currentCorrectIndex = q.correctIndex;

        if (questionText != null)
        {
            questionText.text = $"Frage {seriesIndex + 1}/{seriesTotal}\n{q.text}";
        }
        if (questionID != null) questionID.text = "";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
            {
                optionButtonTexts[i].text = q.options[i];
            }

            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => HandleAnswer(idx));

            // Reset Outline
            var outline = optionButtons[i].GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }


    private IEnumerator ContinueSeriesAfterDelay(bool isCorrect, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (isCorrect) seriesCorrect++;
        seriesIndex++;

        if (seriesIndex >= seriesTotal)
        {
            // series finished
            FinishSeries();
            yield break;
        }

        // Next question
        foreach (var btn in optionButtons) btn.interactable = true; // reset interactable for next Q
        ShowNextSeriesQuestion();
    }

    private void FinishSeries()
    {
        bool passed = (seriesCorrect >= seriesRequired);

        // Clean up UI
        // ✅ USE UIManager
        if (uiManager != null)
        {
            uiManager.ClosePopup();
        }
        else if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        if (moveButton != null) moveButton.SetActive(true);

        // Reset series state
        seriesActive = false;
        seriesTotal = seriesRequired = seriesIndex = seriesCorrect = 0;
        seriesUsedQuestionIds.Clear();

        // Notify GameManager
        var callback = seriesOnDone;
        seriesOnDone = null;
        callback?.Invoke(passed);
    }

}