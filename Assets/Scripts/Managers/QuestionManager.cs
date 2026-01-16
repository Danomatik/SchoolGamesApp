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





    void Start()
    {
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

    public QuestionData GetRandomQuestion()
    {
        if (allQuestions.Count == 0)
        {
            Debug.LogWarning("No questions available!");
            return null;
        }

        int randomIndex = Random.Range(0, allQuestions.Count);
        return allQuestions[randomIndex];
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

        if (quizPanel != null) quizPanel.SetActive(true);
        if (moveButton != null) moveButton.SetActive(false); // Würfeln blockieren solange Quiz offen

        // ✅ Modernes Formatting für Frage-Text mit Farbschema
        if (questionText != null)
        {
            questionText.enableWordWrapping = true;
            questionText.overflowMode = TextOverflowModes.Page;
            questionText.text = $"<b><color=#3EBCD5><size=+1>Frage {q.id}</size></color></b>\n<color=#FFFFFF>{q.text}</color>";
        }
        if (questionID != null) questionID.text = ""; // Nicht mehr benötigt, da ID im Titel steht

        // ✅ Optionen setzen + Formatting mit Farbschema
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
            {
                // ✅ Modernes Button-Text-Formatting
                optionButtonTexts[i].text = $"<size=+1><b><color=#FFFFFF>{q.options[i]}</color></b></size>";
                optionButtonTexts[i].fontStyle = FontStyles.Bold;
                optionButtonTexts[i].alignment = TextAlignmentOptions.Center;
                optionButtonTexts[i].outlineWidth = 0.2f;
                optionButtonTexts[i].outlineColor = new Color(0, 0, 0, 0.4f);
            }

            // ✅ Button-Hintergrund mit Farbschema: SKY BLUE für neutrale Buttons
            var img = optionButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = new Color(0.243f, 0.737f, 0.835f, 1f); // #3EBCD5 SKY BLUE

            // Click-Handler
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => HandleAnswer(idx));
        }
    }


    public void HandleAnswer(int selectedIndex)
    {
        if (answerLocked) return;
        answerLocked = true;

        foreach (var btn in optionButtons) btn.interactable = false;

        bool isCorrect = (selectedIndex == currentCorrectIndex);

        // ✅ Verwende Farbschema: MINT für richtig, BUSINESS für falsch
        var selectedImg = optionButtons[selectedIndex].GetComponent<UnityEngine.UI.Image>();
        if (selectedImg)
        {
            if (isCorrect)
            {
                // MINT (#96C23D) für richtige Antwort
                selectedImg.color = new Color(0.588f, 0.761f, 0.239f, 1f);
            }
            else
            {
                // BUSINESS (#D79244) für falsche Antwort
                selectedImg.color = new Color(0.843f, 0.573f, 0.267f, 1f);
            }
        }

        // Zeige korrekte Antwort in MINT, wenn falsch geantwortet wurde
        if (!isCorrect && currentCorrectIndex >= 0 && currentCorrectIndex < optionButtons.Length)
        {
            var correctImg = optionButtons[currentCorrectIndex].GetComponent<UnityEngine.UI.Image>();
            if (correctImg) correctImg.color = new Color(0.588f, 0.761f, 0.239f, 1f); // MINT
        }

        // ✅ Unterscheide zwischen Series und einzelnen Fragen
        if (seriesActive)
        {
            StartCoroutine(ContinueSeriesAfterDelay(isCorrect, 0.9f));
        }
        else
        {
            StartCoroutine(FinishQuizAfterDelay(isCorrect, 0.9f));
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

        // Panel schließen
        if (quizPanel != null)
            quizPanel.SetActive(false);

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
        if (quizPanel != null) quizPanel.SetActive(true);
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

        // ✅ Modernes Formatting für Frage-Text mit Farbschema (Series)
        if (questionText != null)
        {
            questionText.enableWordWrapping = true;
            questionText.overflowMode = TextOverflowModes.Page;
            questionText.text = $"<b><color=#3EBCD5><size=+1>Frage {seriesIndex + 1}/{seriesTotal}</size></color></b>\n<color=#FFFFFF>{q.text}</color>";
        }
        if (questionID != null) questionID.text = ""; // Nicht mehr benötigt, da ID im Titel steht

        // ✅ Optionen setzen + Formatting mit Farbschema (Series)
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
            {
                // ✅ Modernes Button-Text-Formatting
                optionButtonTexts[i].text = $"<size=+1><b><color=#FFFFFF>{q.options[i]}</color></b></size>";
                optionButtonTexts[i].fontStyle = FontStyles.Bold;
                optionButtonTexts[i].alignment = TextAlignmentOptions.Center;
                optionButtonTexts[i].outlineWidth = 0.2f;
                optionButtonTexts[i].outlineColor = new Color(0, 0, 0, 0.4f);
            }

            // ✅ Button-Hintergrund mit Farbschema: SKY BLUE für neutrale Buttons
            var img = optionButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = new Color(0.243f, 0.737f, 0.835f, 1f); // #3EBCD5 SKY BLUE

            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => HandleAnswer(idx));
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
        if (quizPanel != null) quizPanel.SetActive(false);
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