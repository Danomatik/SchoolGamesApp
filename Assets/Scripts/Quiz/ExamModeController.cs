using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ExamModeController : MonoBehaviour
{
    [Header("Data/Provider")]
    public SimpleJsonQuestionProvider provider;
    public QuizLang language = QuizLang.DE;
    private LearnLevel level = LearnLevel.Junior;

    [Header("UI - Panels")]
    public GameObject levelSelectPanel;
    public Button btnJunior;
    public Button btnSenior;
    public Button levelBackButton;
    public GameObject examPanel;
    public GameObject resultPanel;

    [Header("UI - Exam")]
    public TMP_Text timerText;
    public TMP_Text questionCounterText;
    public TMP_Text questionText;
    public Button[] answerButtons = new Button[3];
    public Button prevButton;
    public Button nextButton;
    public Button finishButton;
    public Button backToMenuButton;

    [Header("UI - Result")]
    public TMP_Text resultScoreText;
    public TMP_Text resultPercentageText;
    public TMP_Text resultPassedText;
    public TMP_Text resultTimeText;
    public Button reviewQuestionsButton;       // Button zum Anschauen der Fragen
    public Button restartButton;
    public Button resultBackButton;

    [Header("UI - History")]
    public GameObject examHistoryPanel;
    public Button historyButton;
    public GameObject historyScrollView;       // Die ScrollView selbst
    public Transform historyContainer;
    public Button historyBackButton;
    public Button historyJuniorButton;
    public Button historySeniorButton;

    [Header("Exam Settings")]
    public int examQuestionCount = 20;
    public float examTotalTime = 1200f;  // 20 Minuten
    public float passingPercentage = 60f;

    [Header("Colors")]
    public Color neutralColor = new Color32(0x2A, 0x7C, 0xA6, 0xFF);
    public Color selectedColor = new Color32(0x4A, 0x9C, 0xC6, 0xFF); // Hellblau für Auswahl
    public Color correctColor = new Color(0.35f, 0.85f, 0.45f);
    public Color wrongColor = new Color(0.85f, 0.30f, 0.30f);

    // Intern
    private List<Question> _examQuestions = new();
    private List<int> _userAnswers = new();  // -1 = nicht beantwortet
    private int _currentQuestionIndex = 0;
    private float _timeRemaining = 0f;
    private float _timeUsed = 0f;
    private bool _examRunning = false;
    private bool _examFinished = false;
    private bool _reviewMode = false;          // Review-Modus nach Prüfung

    private void Awake()
    {
        // Panels initial
        if (examPanel) examPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
        if (resultPanel) resultPanel.SetActive(false);
        if (examHistoryPanel) examHistoryPanel.SetActive(false);
        if (historyScrollView) historyScrollView.SetActive(false);

        // Level-Auswahl
        if (btnJunior) btnJunior.onClick.AddListener(() => StartExam(LearnLevel.Junior));
        if (btnSenior) btnSenior.onClick.AddListener(() => StartExam(LearnLevel.Senior));

        // History Button
        if (historyButton) historyButton.onClick.AddListener(ShowHistory);

        // Zurück-Buttons
        if (levelBackButton) levelBackButton.onClick.AddListener(BackToMenu);
        if (backToMenuButton) backToMenuButton.onClick.AddListener(BackToMenu);

        // Answer-Button Listener
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int captured = i;
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(captured));
            }
        }

        // Navigation Buttons
        if (prevButton) prevButton.onClick.AddListener(PrevQuestion);
        if (nextButton) nextButton.onClick.AddListener(NextQuestion);
        if (finishButton) finishButton.onClick.AddListener(FinishExam);

        // Result Buttons
        if (reviewQuestionsButton) reviewQuestionsButton.onClick.AddListener(StartReviewMode);
        if (restartButton) restartButton.onClick.AddListener(RestartExam);
        if (resultBackButton) resultBackButton.onClick.AddListener(BackToMenu);

        // History Buttons
        if (historyBackButton) historyBackButton.onClick.AddListener(HideHistory);
        if (historyJuniorButton) historyJuniorButton.onClick.AddListener(() => LoadHistoryForLevel(LearnLevel.Junior));
        if (historySeniorButton) historySeniorButton.onClick.AddListener(() => LoadHistoryForLevel(LearnLevel.Senior));
    }

    private void Update()
    {
        if (_examRunning && !_examFinished)
        {
            _timeRemaining -= Time.deltaTime;
            _timeUsed += Time.deltaTime;

            UpdateTimerUI();

            // Zeit abgelaufen
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                FinishExam();
            }
        }
    }

    private void StartExam(LearnLevel selected)
    {
        level = selected;

        // Alle Fragen laden
        var allQuestions = provider != null ? provider.LoadQuestionsFlat(language, level) : new List<Question>();
        if (allQuestions == null || allQuestions.Count == 0)
        {
            Debug.LogError($"ExamModeController: Keine Fragen gefunden für {level}-{language}");
            return;
        }

        // Zufällige Auswahl von examQuestionCount Fragen
        _examQuestions = SelectRandomQuestions(allQuestions, examQuestionCount);

        // User-Antworten initialisieren (-1 = nicht beantwortet)
        _userAnswers = new List<int>();
        for (int i = 0; i < _examQuestions.Count; i++)
        {
            _userAnswers.Add(-1);
        }

        // UI umschalten
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (examPanel) examPanel.SetActive(true);
        if (resultPanel) resultPanel.SetActive(false);

        // Sicherstellen, dass der Controller aktiv ist (für Update())
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        // Exam initialisieren
        _currentQuestionIndex = 0;
        _timeRemaining = examTotalTime;
        _timeUsed = 0f;
        _examRunning = true;
        _examFinished = false;

        // Timer initial anzeigen
        UpdateTimerUI();

        ShowQuestion(_currentQuestionIndex);
        UpdateNavigationButtons();
    }

    private List<Question> SelectRandomQuestions(List<Question> pool, int count)
    {
        // Fisher-Yates Shuffle auf Kopie
        var shuffled = new List<Question>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Question temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        // Ersten 'count' Fragen nehmen
        int actualCount = Mathf.Min(count, shuffled.Count);
        return shuffled.GetRange(0, actualCount);
    }

    private void ShowQuestion(int index)
    {
        if (index < 0 || index >= _examQuestions.Count) return;

        _currentQuestionIndex = index;
        Question q = _examQuestions[index];

        if (questionText) questionText.text = q.text ?? "";
        if (questionCounterText) questionCounterText.text = $"{index + 1} / {_examQuestions.Count}";

        // Buttons zurücksetzen
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn) continue;
            btn.gameObject.SetActive(false);
            btn.interactable = true;

            var img = btn.GetComponent<Image>();
            if (img) img.color = neutralColor;

            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = "";
        }

        // Optionen eintragen
        var opts = q.options ?? new List<string>();
        for (int i = 0; i < opts.Count && i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn) continue;
            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = opts[i];
            btn.gameObject.SetActive(true);
        }

        // Im Review-Modus: Richtige/Falsche Antworten färben
        if (_reviewMode)
        {
            int userAnswer = _userAnswers[index];
            
            for (int i = 0; i < answerButtons.Length; i++)
            {
                var btn = answerButtons[i];
                if (!btn || !btn.gameObject.activeSelf) continue;

                btn.interactable = false;  // Buttons deaktivieren
                var img = btn.GetComponent<Image>();
                if (img == null) continue;

                if (i == q.correctIndex)
                {
                    // Richtige Antwort grün
                    img.color = correctColor;
                }
                else if (i == userAnswer)
                {
                    // Falsche Auswahl rot
                    img.color = wrongColor;
                }
                else
                {
                    // Andere Antworten neutral
                    img.color = neutralColor;
                }
            }
        }
        // Normal-Modus: Bereits gewählte Antwort markieren
        else
        {
            int userAnswer = _userAnswers[index];
            if (userAnswer >= 0 && userAnswer < answerButtons.Length)
            {
                var selectedBtn = answerButtons[userAnswer];
                if (selectedBtn)
                {
                    var img = selectedBtn.GetComponent<Image>();
                    if (img) img.color = selectedColor;
                }
            }
        }

        UpdateNavigationButtons();
    }

    private void OnAnswerSelected(int answerIndex)
    {
        if (_examFinished) return;

        // Antwort speichern
        _userAnswers[_currentQuestionIndex] = answerIndex;

        // Alle Buttons zurücksetzen
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;

            var img = btn.GetComponent<Image>();
            if (img) img.color = neutralColor;
        }

        // Gewählten Button markieren (KEIN grün/rot!)
        var selectedBtn = answerButtons[answerIndex];
        if (selectedBtn)
        {
            var img = selectedBtn.GetComponent<Image>();
            if (img) img.color = selectedColor;
        }
    }

    private void NextQuestion()
    {
        if (_currentQuestionIndex < _examQuestions.Count - 1)
        {
            ShowQuestion(_currentQuestionIndex + 1);
        }
    }

    private void PrevQuestion()
    {
        if (_currentQuestionIndex > 0)
        {
            ShowQuestion(_currentQuestionIndex - 1);
        }
    }

    private void UpdateNavigationButtons()
    {
        // Prev Button
        if (prevButton)
            prevButton.interactable = (_currentQuestionIndex > 0);

        // Im Review-Modus: Next immer anzeigen, Finish ausblenden
        if (_reviewMode)
        {
            if (nextButton)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = (_currentQuestionIndex < _examQuestions.Count - 1);
            }
            if (finishButton)
            {
                finishButton.gameObject.SetActive(false);
            }
        }
        // Normal-Modus: Next oder Finish je nach Position
        else
        {
            if (nextButton)
            {
                bool isLastQuestion = (_currentQuestionIndex >= _examQuestions.Count - 1);
                nextButton.gameObject.SetActive(!isLastQuestion);
            }
            if (finishButton)
            {
                bool isLastQuestion = (_currentQuestionIndex >= _examQuestions.Count - 1);
                finishButton.gameObject.SetActive(isLastQuestion);
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText)
        {
            int minutes = Mathf.Max(0, (int)(_timeRemaining / 60));
            int seconds = Mathf.Max(0, (int)(_timeRemaining % 60));
            timerText.text = $"{minutes:D2}:{seconds:D2}";
        }
    }

    private void FinishExam()
    {
        _examRunning = false;
        _examFinished = true;

        // Antworten auswerten
        ExamResult result = EvaluateAnswers();

        // Ergebnis speichern
        ExamProgressStore.SaveResult(result);

        // Ergebnisse anzeigen
        ShowResults(result);
    }

    private ExamResult EvaluateAnswers()
    {
        int correctCount = 0;

        for (int i = 0; i < _examQuestions.Count; i++)
        {
            Question q = _examQuestions[i];
            int userAnswer = _userAnswers[i];

            if (userAnswer >= 0 && userAnswer == q.correctIndex)
            {
                correctCount++;
            }
        }

        float percentage = (_examQuestions.Count > 0) ? (correctCount * 100f / _examQuestions.Count) : 0f;
        bool passed = percentage >= passingPercentage;

        ExamResult result = new ExamResult
        {
            language = language,
            level = level,
            totalQuestions = _examQuestions.Count,
            correctAnswers = correctCount,
            percentageScore = percentage,
            timeUsedSeconds = _timeUsed,
            passed = passed
        };

        return result;
    }

    private void ShowResults(ExamResult result)
    {
        if (examPanel) examPanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(true);

        if (resultScoreText)
            resultScoreText.text = $"{result.correctAnswers} / {result.totalQuestions} richtig";

        if (resultPercentageText)
            resultPercentageText.text = $"{result.percentageScore:F1}%";

        if (resultPassedText)
        {
            resultPassedText.text = result.passed ? "Bestanden!" : "Nicht bestanden";
            resultPassedText.color = result.passed ? correctColor : wrongColor;
        }

        if (resultTimeText)
            resultTimeText.text = $"Zeit: {result.GetFormattedTime()}";
    }

    private void StartReviewMode()
    {
        _reviewMode = true;
        _currentQuestionIndex = 0;

        // Panels umschalten
        if (resultPanel) resultPanel.SetActive(false);
        if (examPanel) examPanel.SetActive(true);

        // Timer ausblenden im Review-Modus
        if (timerText) timerText.gameObject.SetActive(false);

        // Erste Frage anzeigen
        ShowQuestion(_currentQuestionIndex);
    }

    private void RestartExam()
    {
        _reviewMode = false;
        if (resultPanel) resultPanel.SetActive(false);
        StartExam(level);
    }

    private void ShowHistory()
    {
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (examHistoryPanel) examHistoryPanel.SetActive(true);
        
        // ScrollView initial ausblenden
        if (historyScrollView) historyScrollView.SetActive(false);
    }

    private void HideHistory()
    {
        if (examHistoryPanel) examHistoryPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
    }

    private void LoadHistoryForLevel(LearnLevel selectedLevel)
    {
        level = selectedLevel;
        
        // ScrollView jetzt anzeigen
        if (historyScrollView) historyScrollView.SetActive(true);
        
        var results = ExamProgressStore.GetAllResults(language, level);
        PopulateHistory(results);
    }

    private void PopulateHistory(List<ExamResult> results)
    {
        if (historyContainer == null) return;

        // Alte Items löschen
        foreach (Transform child in historyContainer)
        {
            Destroy(child.gameObject);
        }

        if (results == null || results.Count == 0)
        {
            // Keine Ergebnisse vorhanden
            CreateHistoryText(historyContainer, "Keine Prüfungsergebnisse vorhanden", Color.gray, 100);
            return;
        }

        // Sortiert nach neuestem zuerst
        results = results.OrderByDescending(r => r.timestamp).ToList();

        // Für jedes Ergebnis ein Item erstellen
        foreach (var result in results)
        {
            GameObject itemObj = new GameObject("HistoryItem");
            itemObj.transform.SetParent(historyContainer, false);

            var layout = itemObj.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.spacing = 3;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Datum
            CreateHistoryText(itemObj.transform, result.timestamp, Color.white, 100, FontStyles.Bold);

            // Score
            CreateHistoryText(itemObj.transform, 
                $"{result.correctAnswers}/{result.totalQuestions} richtig ({result.percentageScore:F1}%)", 
                Color.white, 
                100);

            // Status
            CreateHistoryText(itemObj.transform, 
                result.passed ? "Bestanden" : "Nicht bestanden", 
                result.passed ? correctColor : wrongColor, 
                100, 
                FontStyles.Bold);

            // Zeit
            CreateHistoryText(itemObj.transform, 
                $"Zeit: {result.GetFormattedTime()}", 
                Color.white, 
                100);

            // Separator
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(itemObj.transform, false);
            var sepImage = separator.AddComponent<UnityEngine.UI.Image>();
            sepImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            var sepRect = separator.GetComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(0, 2);
        }
    }

    private void CreateHistoryText(Transform parent, string text, Color color, int fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.Center;  // Mittig
        
        var rect = textObj.GetComponent<RectTransform>();
        // Stretch horizontal (volle Breite)
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, tmp.preferredHeight);
        
        var layoutElement = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.minHeight = 20;
        layoutElement.preferredHeight = -1;
        layoutElement.flexibleWidth = 1;
    }

    private void BackToMenu()
    {
        // Im Review-Modus: Zurück zum ResultPanel
        if (_reviewMode)
        {
            _reviewMode = false;
            if (examPanel) examPanel.SetActive(false);
            if (resultPanel) resultPanel.SetActive(true);
            if (timerText) timerText.gameObject.SetActive(true);  // Timer wieder einblenden
        }
        // Sonst: Zurück zur Quiz Scene
        else
        {
            SceneManager.LoadScene("Quiz");
        }
    }
}


