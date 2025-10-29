using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections;

public class QuestionManager : MonoBehaviour
{
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
        public QuestionCategory junior_de;
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
        LoadQuestions();
        FindQuizFields();
    }

    private void FindQuizFields()
    {
        quizFields = FindObjectsByType<QuizField>(FindObjectsSortMode.None);
    }

    private void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Fragen_Junior_DE");

        if (jsonFile == null)
        {
            Debug.LogError("Could not load Schoolgames_Fragen_Junior_DE.json from Resources/Data/");
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

            if (questionDatabase.junior_de == null)
            {
                Debug.LogError("QuestionManager: junior_de is null!");
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

        if (questionDatabase?.junior_de != null)
        {
            if (questionDatabase.junior_de.gruendung != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.gruendung);
            }

            if (questionDatabase.junior_de.investition != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.investition);
            }

            if (questionDatabase.junior_de.ag != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.ag);
            }
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

        if (questionText != null) questionText.text = q.text;
        if (questionID != null) questionID.text = q.id.ToString();

        // Optionen setzen + Farben zurückstellen
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
                optionButtonTexts[i].text = q.options[i];

            // Farbe zurück auf weiß
            var img = optionButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = Color.white;

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

        var selectedImg = optionButtons[selectedIndex].GetComponent<UnityEngine.UI.Image>();
        if (selectedImg) selectedImg.color = isCorrect ? Color.green : Color.red;

        if (!isCorrect && currentCorrectIndex >= 0 && currentCorrectIndex < optionButtons.Length)
        {
            var correctImg = optionButtons[currentCorrectIndex].GetComponent<UnityEngine.UI.Image>();
            if (correctImg) correctImg.color = Color.green;
        }

        // ✅ If we are in series mode, continue the series; otherwise, keep your old 1-question flow
        if (seriesActive)
        {
            StartCoroutine(ContinueSeriesAfterDelay(isCorrect, 0.9f));
        }
        else
        {
            StartCoroutine(FinishQuizAfterDelay(isCorrect, 0.9f)); // your existing single-question path
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

        if (questionText != null) questionText.text = q.text;
        if (questionID != null) questionID.text = q.id.ToString();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool active = (i < q.options.Length);
            optionButtons[i].gameObject.SetActive(active);
            optionButtons[i].interactable = active;

            if (active && optionButtonTexts != null && i < optionButtonTexts.Length)
                optionButtonTexts[i].text = q.options[i];

            var img = optionButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = Color.white;

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
