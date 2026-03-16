using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unified quiz controller for all three quiz modes (Learn, Score, Exam).
/// Attach to the QuizContainer GameObject in the main menu scene.
/// Call StartMode() from MainMenuController to launch a specific mode.
/// </summary>
public class QuizController : MonoBehaviour
{
    public enum QuizMode { Learn, Score, Exam }

    [Header("Navigation")]
    [Tooltip("Reference to the MainMenuController for returning to the menu.")]
    public MainMenuController mainMenuController;

    [Header("Data")]
    public SimpleJsonQuestionProvider provider;
    public QuizLang language = QuizLang.DE;

    [Header("UI - Header")]
    [Tooltip("Badge GameObject shown only in Learn mode")]
    public GameObject learnModeBadge;
    [Tooltip("Badge GameObject shown only in Score mode")]
    public GameObject scoreModeBadge;
    [Tooltip("Badge GameObject shown only in Exam mode")]
    public GameObject examModeBadge;
    public Button menuButton;

    [Header("UI - TopSection Areas")]
    [Tooltip("TimerArea: shown in Exam mode (countdown timer).")]
    public GameObject timerArea;
    [Tooltip("TimerText inside TimerArea – shows the exam mode countdown")]
    public TMP_Text examTimerText;

    [Tooltip("LivesArea: shown in Score mode.")]
    public GameObject livesArea;
    public Image[] heartImages = new Image[3];  // Heart1(1), Heart1(3), Heart1(2)
    public Color heartActiveColor = Color.white;
    public Color heartLostColor   = Color.black;
    public TMP_Text scoreText;
    public TMP_Text highscoreText;

    [Tooltip("ProgressArea: shown in Learn mode (question progress).")]
    public GameObject progressArea;
    public Slider learnProgressSlider;
    public TMP_Text learnProgressText;

    [Header("UI - QuestionCard")]
    [Tooltip("The QuestionCard root GameObject")]
    public GameObject questionCardRoot;
    public TMP_Text questionLabel;     // e.g. "FRAGE 7"
    public TMP_Text questionText;
    public TMP_Text scoreInCardText;   // Added: Score display on card
    public Slider questionTimerSlider; // Score mode: per-question countdown
    public TMP_Text questionProgress;  // e.g. "7 / 20"

    [Header("UI - Answers")]
    [Tooltip("The AnswersContainer root GameObject")]
    public GameObject answersContainerRoot;
    public Button[] answerButtons = new Button[3];

    [Header("UI - BottomButtons")]
    [Tooltip("The BottomButtons root GameObject")]
    public GameObject bottomButtonsRoot;
    public Button backButton;
    public Button nextButton;
    public Button finishButton;

    [Header("UI - Game Over Panel")]
    public GameObject scoreGameOverPanel;
    public TMP_Text gameOverScoreText;
    public TMP_Text gameOverHighscoreText;
    public TMP_Text gameOverAllTimeBestText;
    public TMP_Text gameOverQuestionsAnsweredText;
    public Button gameOverMenuButton;
    public Button gameOverRestartButton;

    [Header("Exam Settings")]
    public int examQuestionCount = 20;
    public float examTotalTime = 300f;  // 5 minutes
    public float passingPercentage = 70f;

    [Header("UI - Exam Result Panel")]
    public GameObject examResultPanel;
    public Slider resultProgressBar;
    public TMP_Text resultProgressText;      // "20 / 20 ✓"
    public TMP_Text resultTitleText;         // "BESTANDEN"
    public TMP_Text resultPercentageText;    // "87%"
    public TMP_Text resultCountSubText;      // "20 / 20"
    public TMP_Text resultThresholdText;     // "Mindestens 70% zum Bestehen"
    public Button resultMenuButton;
    public Button resultRestartButton;
    public Button resultViewQuestionsButton;

    [Header("Score Settings")]
    public float timePerQuestion = 20f;
    public int pointsPerCorrectAnswer = 100;
    public int startingLives = 3;

    [Header("Colors (button background feedback)")]
    public Color neutralColor = new Color32(0x2A, 0x7C, 0xA6, 0xFF);
    public Color correctColor  = new Color(0.35f, 0.85f, 0.45f);
    public Color wrongColor    = new Color(0.85f, 0.30f, 0.30f);
    public Color selectedColor = new Color32(0x4A, 0x9C, 0xC6, 0xFF);

    // ─── Internal State ───────────────────────────────────────────────

    private QuizMode _currentMode;
    private LearnLevel _currentLevel;
    private List<Question> _questions = new();

    // Shared
    private int _currentIndex = 0;
    private Question _current;
    private bool _answered = false;

    // Learn mode
    // (Timer removed)
    public int learnQuestionCount = 20;

    // Score mode
    private int _currentScore = 0;
    private int _livesRemaining = 3;
    private float _questionTime = 0f;
    private bool _questionTimerRunning = false;
    private bool _gameOver = false;

    // Exam mode
    private List<int> _userAnswers = new(); // -1 = unanswered
    private float _examTimeRemaining = 0f;
    private bool _examRunning = false;
    private bool _examFinished = false;
    private bool _isReviewMode = false;

    // ─── Unity Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        // ── Auto-find all references by hierarchy name (works even if Inspector not wired) ──
        AutoFindReferences();

        // ── Reset everything to a clean state regardless of Editor setup ──
        ResetAllUI();

        // Wire answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int captured = i;
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
            }
        }

        if (menuButton)   menuButton.onClick.AddListener(OnMenuButtonClicked);
        if (backButton)   backButton.onClick.AddListener(PrevQuestion);
        if (nextButton)   nextButton.onClick.AddListener(NextQuestion);
        if (finishButton) finishButton.onClick.AddListener(FinishExam);
        if (gameOverMenuButton) gameOverMenuButton.onClick.AddListener(OnMenuButtonClicked);
        if (gameOverRestartButton) gameOverRestartButton.onClick.AddListener(RestartCurrentMode);

        // --- Exam Result Panel Buttons ---
        if (resultMenuButton) resultMenuButton.onClick.AddListener(OnMenuButtonClicked);
        if (resultRestartButton) resultRestartButton.onClick.AddListener(RestartCurrentMode);
        if (resultViewQuestionsButton) resultViewQuestionsButton.onClick.AddListener(ShowReviewMode);

        // Sliders not user-interactable
        if (learnProgressSlider) learnProgressSlider.interactable = false;
        if (questionTimerSlider) questionTimerSlider.interactable = false;
    }

    /// <summary>
    /// Finds all UI references by their GameObject names in the QuizContainer hierarchy.
    /// Only fills in fields that are not already assigned in the Inspector.
    /// </summary>
    private void AutoFindReferences()
    {
        // ── Provider: auto-create if not assigned ──────────────────────
        if (!provider)
        {
            // Try to find one already in the scene
            provider = FindObjectOfType<SimpleJsonQuestionProvider>();

            if (!provider)
            {
                // Create it on this GameObject with the correct defaults
                provider = gameObject.AddComponent<SimpleJsonQuestionProvider>();
                provider.resourcesFolder = "Data";
                provider.filePattern     = "Schoolgames_Fragen_{LEVEL}_{LANG}";
                Debug.Log("[QuizController] Created SimpleJsonQuestionProvider (Data/Schoolgames_Fragen_{LEVEL}_{LANG})");
            }
        }

        // Determine search roots from Inspector assignments or fall back to named children
        Transform cardRoot    = questionCardRoot     ? questionCardRoot.transform     : transform.Find("QuestionCard");
        Transform answersRoot = answersContainerRoot ? answersContainerRoot.transform : transform.Find("AnswersContainer");
        Transform bottomRoot  = bottomButtonsRoot    ? bottomButtonsRoot.transform    : transform.Find("BottomButtons");


        T Find<T>(string path) where T : Component
        {
            var go = transform.Find(path);
            return go != null ? go.GetComponent<T>() : null;
        }
        T FindIn<T>(Transform root, string path) where T : Component
        {
            if (root == null) return null;
            var go = root.Find(path);
            return go != null ? go.GetComponent<T>() : null;
        }
        GameObject FindGO(string path)
        {
            var go = transform.Find(path);
            return go != null ? go.gameObject : null;
        }

        // Header badges
        if (!learnModeBadge)  learnModeBadge  = FindGO("Header/Lernmodus");
        if (!scoreModeBadge)  scoreModeBadge  = FindGO("Header/Punktemodus");
        if (!examModeBadge)   examModeBadge   = FindGO("Header/Prüfungsmodus");
        if (!menuButton)      menuButton       = Find<Button>("Header/MenuButton");

        // TopSection areas
        if (!timerArea)    timerArea    = FindGO("TopSection/TimerArea");
        if (!livesArea)    livesArea    = FindGO("TopSection/LivesArea");
        if (!progressArea) progressArea = FindGO("TopSection/ProgressArea");

        // TimerArea
        if (!examTimerText)  examTimerText  = Find<TMP_Text>("TopSection/TimerArea/TimerText");

        // ProgressArea
        if (!learnProgressSlider) learnProgressSlider = Find<Slider>("TopSection/ProgressArea/ProgressBar");
        if (!learnProgressText)   learnProgressText   = Find<TMP_Text>("TopSection/ProgressArea/ProgressText");

        // LivesArea
        if (!scoreText)     scoreText     = Find<TMP_Text>("TopSection/LivesArea/ScoreRow/ScoreItem");
        if (!highscoreText) highscoreText = Find<TMP_Text>("TopSection/LivesArea/ScoreRow/HighscoreItem");

        // Hearts – find by HeartsContainer children
        if (heartImages == null || heartImages.Length == 0 || heartImages[0] == null)
        {
            var heartsContainer = transform.Find("TopSection/LivesArea/HeartsContainer");
            if (heartsContainer != null)
            {
                var imgs = new System.Collections.Generic.List<Image>();
                foreach (Transform child in heartsContainer)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) imgs.Add(img);
                }
                heartImages = imgs.ToArray();
            }
        }

        // QuestionCard – use assigned root or auto-found
        if (!questionLabel)       questionLabel       = FindIn<TMP_Text>(cardRoot, "QuestionLabel");
        if (!questionText)        questionText        = FindIn<TMP_Text>(cardRoot, "QuestionText");
        if (!scoreInCardText)     scoreInCardText     = FindIn<TMP_Text>(cardRoot, "Current Score");
        if (!questionTimerSlider) questionTimerSlider = FindIn<Slider>(cardRoot, "QuestionTimer");
        if (!questionProgress)    questionProgress    = FindIn<TMP_Text>(cardRoot, "QuestionProgress");

        // Answer buttons – use all Button children of AnswersContainer root
        if (answerButtons == null || answerButtons.Length == 0 || answerButtons[0] == null)
        {
            if (answersRoot != null)
            {
                var btns = new System.Collections.Generic.List<Button>();
                foreach (Transform child in answersRoot)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null) btns.Add(btn);
                }
                answerButtons = btns.ToArray();
            }
        }

        // BottomButtons – use assigned root or auto-found
        if (!backButton)   backButton   = FindIn<Button>(bottomRoot, "NavigationRow/BackButton");
        if (!nextButton)   nextButton   = FindIn<Button>(bottomRoot, "NavigationRow/NextButton");
        if (!finishButton) finishButton = FindIn<Button>(bottomRoot, "FinishButton");

        // Game Over Panel - EndScreenContainer is now a sibling of QuizContainer, so search from parent
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            if (!scoreGameOverPanel) scoreGameOverPanel = FindIn<Transform>(parentTransform, "EndScreenContainer/GameOverPanel")?.gameObject;
            if (!gameOverScoreText) gameOverScoreText = FindIn<TMP_Text>(parentTransform, "EndScreenContainer/GameOverPanel/Content/ScoreCard/ScoreRow/Value");
            if (!gameOverHighscoreText) gameOverHighscoreText = FindIn<TMP_Text>(parentTransform, "EndScreenContainer/GameOverPanel/Content/ScoreCard/HighscoreRow/Value");
            if (!gameOverAllTimeBestText) gameOverAllTimeBestText = FindIn<TMP_Text>(parentTransform, "EndScreenContainer/GameOverPanel/Content/ScoreCard/AllTimeRow/Value");
            if (!gameOverQuestionsAnsweredText) gameOverQuestionsAnsweredText = FindIn<TMP_Text>(parentTransform, "EndScreenContainer/GameOverPanel/Content/AnsweredCount");
            if (!gameOverMenuButton) gameOverMenuButton = FindIn<Button>(parentTransform, "EndScreenContainer/GameOverPanel/Content/ButtonRow/MenuButton");
            if (!gameOverRestartButton) gameOverRestartButton = FindIn<Button>(parentTransform, "EndScreenContainer/GameOverPanel/Content/ButtonRow/MenuButton (1)");

            // --- Exam Result Panel ---
            if (!examResultPanel) examResultPanel = FindIn<Transform>(parentTransform, "EndScreenContainer/ExamResultPanel")?.gameObject;
            Transform resT = examResultPanel?.transform;
            if (resT != null)
            {
                if (!resultProgressBar)         resultProgressBar         = FindIn<Slider>(resT, "Content/ProgressBar");
                if (!resultProgressText)        resultProgressText        = FindIn<TMP_Text>(resT, "Content/ProgressText");
                if (!resultTitleText)           resultTitleText           = FindIn<TMP_Text>(resT, "Content/ResultCard/ResultTitle");
                if (!resultPercentageText)      resultPercentageText      = FindIn<TMP_Text>(resT, "Content/ResultCard/PercentValue");
                if (!resultCountSubText)        resultCountSubText        = FindIn<TMP_Text>(resT, "Content/ResultCard/PercentSub");
                if (!resultThresholdText)       resultThresholdText       = FindIn<TMP_Text>(resT, "Content/ResultCard/ThresholdText");
                if (!resultMenuButton)          resultMenuButton          = FindIn<Button>(resT, "Content/ButtonArea/ButtonRow/MenuButton");
                if (!resultRestartButton)       resultRestartButton       = FindIn<Button>(resT, "Content/ButtonArea/ButtonRow/MenuButton (1)");
                if (!resultViewQuestionsButton) resultViewQuestionsButton = FindIn<Button>(resT, "Content/ButtonArea/ViewQuestionsButton");
            }
        }

        Debug.Log($"[QuizController] AutoFind: " +
                  $"badges={learnModeBadge != null}/{scoreModeBadge != null}/{examModeBadge != null} " +
                  $"answers={answerButtons.Length} " +
                  $"card=({questionLabel != null},{questionText != null},{questionProgress != null}) " +
                  $"nav=back:{backButton != null} next:{nextButton != null} finish:{finishButton != null}");
    }



    /// <summary>
    /// Hides ALL mode-specific UI elements so the state is always clean
    /// when Play is pressed, regardless of what was visible in the Editor.
    /// </summary>
    private void ResetAllUI()
    {
        // Header badges – all off
        SetActive(learnModeBadge, false);
        SetActive(scoreModeBadge, false);
        SetActive(examModeBadge,  false);

        // TopSection areas – all off
        SetActive(timerArea,    false);
        SetActive(livesArea,    false);
        SetActive(progressArea, false);

        // QuestionCard extras – all off
        SetActive(questionTimerSlider?.gameObject, false);
        SetActive(questionProgress?.gameObject,    false);
        SetActive(scoreInCardText?.gameObject,     false);

        // BottomButtons – all off
        SetActive(backButton?.gameObject,   false);
        SetActive(nextButton?.gameObject,   false);
        SetActive(finishButton?.gameObject, false);
        
        // Game Over – off
        SetActive(scoreGameOverPanel, false);
        SetActive(examResultPanel, false);

        // Reset all answer buttons off
        foreach (var btn in answerButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        // Reset hearts to active color
        foreach (var h in heartImages)
            if (h != null) h.color = heartActiveColor;
    }


    private void Update()
    {
        // (Learn mode timer removed)

        if (_currentMode == QuizMode.Score && _questionTimerRunning && !_gameOver)
        {
            _questionTime -= Time.deltaTime;
            if (questionTimerSlider)
                questionTimerSlider.value = Mathf.Max(0, _questionTime);
            if (_questionTime <= 0)
            {
                _questionTimerRunning = false;
                OnScoreTimeout();
            }
        }

        if (_currentMode == QuizMode.Exam && _examRunning && !_examFinished)
        {
            _examTimeRemaining -= Time.deltaTime;
            UpdateExamTimerUI();
            if (_examTimeRemaining <= 0)
            {
                _examTimeRemaining = 0;
                FinishExam();
            }
        }
    }

    // ─── Public Entry Points ─────────────────────────────────────────

    /// <summary>
    /// Called by MainMenuController when the user selects a mode and level.
    /// </summary>
    public void StartMode(QuizMode mode, LearnLevel level)
    {
        _currentMode = mode;
        _currentLevel = level;
        _gameOver = false;
        _examFinished = false;
        _answered = false;

        // Load questions
        _questions = provider != null
            ? provider.LoadQuestionsFlat(language, level)
            : new List<Question>();

        if (_questions == null || _questions.Count == 0)
        {
            Debug.LogError($"[QuizController] No questions found for mode={mode}, level={level}");
            return;
        }

        // Mode-specific setup
        switch (mode)
        {
            case QuizMode.Learn:  SetupLearnMode();  break;
            case QuizMode.Score:  SetupScoreMode();  break;
            case QuizMode.Exam:   SetupExamMode();   break;
        }

        // Re-enable main sections if they were hidden by Game Over
        SetActive(questionCardRoot, true);
        SetActive(answersContainerRoot, true);
        SetActive(bottomButtonsRoot, true);
        SetActive(scoreGameOverPanel, false);
        SetActive(examResultPanel, false);
        _isReviewMode = false;
        
        // Re-enable header elements
        SetActive(menuButton?.gameObject, true);

        ShowQuestion(_currentIndex);
        UpdateNavigationButtons();
    }

    public void RestartCurrentMode()
    {
        StartMode(_currentMode, _currentLevel);
    }

    // ─── Mode Setup ───────────────────────────────────────────────────

    private void SetupLearnMode()
    {
        SetActiveBadge(QuizMode.Learn);
        // TopSection: only ProgressArea
        ShowTopSection(timerArea: false, livesArea: false, progressArea: true);
        // QuestionCard: QuestionProgress visible, QuestionTimer (per-question slider) hidden
        SetActive(questionTimerSlider?.gameObject, false);
        SetActive(questionProgress?.gameObject, true);

        // Resume from last index if possible, otherwise find first unsolved question
        int lastIndex = LearnProgressStore.GetLastIndex(language, _currentLevel);
        
        if (lastIndex >= 0 && lastIndex < _questions.Count)
        {
            _currentIndex = lastIndex;
        }
        else
        {
            _currentIndex = 0;
        }

        // Initialize progress slider and text
        if (learnProgressSlider)
        {
            learnProgressSlider.maxValue = _questions.Count;
            learnProgressSlider.value = _currentIndex + 1;
        }
        if (learnProgressText)
        {
            learnProgressText.text = $"{_currentIndex + 1} / {_questions.Count}";
        }
        
        // Initialize Score for Learn Mode
        _currentScore = 0;
        UpdateScoreUI();
        SetActive(scoreInCardText?.gameObject, true);
    }

    private void SetupScoreMode()
    {
        SetActiveBadge(QuizMode.Score);
        // TopSection: only LivesArea (hearts + score)
        ShowTopSection(timerArea: false, livesArea: true, progressArea: false);
        // QuestionCard: QuestionTimer (per-question countdown slider) visible, QuestionProgress hidden
        SetActive(questionTimerSlider?.gameObject, true);
        SetActive(questionProgress?.gameObject, false);
        SetActive(scoreInCardText?.gameObject, true); // Added: Show score on card

        // Shuffle questions
        ShuffleList(_questions);

        _currentScore = 0;
        _livesRemaining = startingLives;
        _currentIndex = 0;

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateHighscoreUI();
    }

    private void SetupExamMode()
    {
        SetActiveBadge(QuizMode.Exam);
        // TopSection: only TimerArea
        ShowTopSection(timerArea: true, livesArea: false, progressArea: false);
        // QuestionCard: QuestionProgress visible, QuestionTimer (per-question slider) hidden
        SetActive(questionTimerSlider?.gameObject, false);
        SetActive(questionProgress?.gameObject, true);
        SetActive(scoreInCardText?.gameObject, false);

        // Select random subset
        _questions = SelectRandom(_questions, examQuestionCount);

        // Init answers array
        _userAnswers = new List<int>();
        for (int i = 0; i < _questions.Count; i++) _userAnswers.Add(-1);

        _currentIndex = 0;
        _examTimeRemaining = 300f; // Force 5 minutes
        _examRunning = true;

        UpdateExamTimerUI();
    }

    // ─── Question Display ─────────────────────────────────────────────

    private void ShowQuestion(int index)
    {
        if (_questions == null || index < 0 || index >= _questions.Count) return;

        _currentIndex = index;
        _current = _questions[index];
        _answered = false;

        // Label e.g. "FRAGE 7"
        if (questionLabel) questionLabel.text = $"FRAGE {index + 1}";

        // Question text
        if (questionText) questionText.text = _current.text ?? "";

        // Progress e.g. "7 / 20"
        if (questionProgress) questionProgress.text = $"{index + 1} / {_questions.Count}";

        // Update Learn progress visuals
        if (_currentMode == QuizMode.Learn)
        {
            if (learnProgressSlider) learnProgressSlider.value = index + 1;
            if (learnProgressText)   learnProgressText.text   = $"{index + 1} / {_questions.Count}";
        }

        // Reset all answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn) continue;
            btn.gameObject.SetActive(false);
            btn.interactable = true;
            ResetButtonFeedback(btn);

            // Target the last TMP_Text (the answer label)
            var texts = btn.GetComponentsInChildren<TMP_Text>();
            if (texts != null && texts.Length > 0)
                texts[texts.Length - 1].text = "";
        }

        // Fill in options
        var opts = _current.options ?? new List<string>();
        for (int i = 0; i < opts.Count && i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn) continue;
            var texts = btn.GetComponentsInChildren<TMP_Text>();
            if (texts != null && texts.Length > 0)
                texts[texts.Length - 1].text = opts[i];
            btn.gameObject.SetActive(true);
        }

        // Mode-specific per-question state
        switch (_currentMode)
        {
            case QuizMode.Learn:
                // nothing extra
                break;

            case QuizMode.Score:
                // Restart per-question timer
                _questionTime = timePerQuestion;
                _questionTimerRunning = true;
                if (questionTimerSlider)
                {
                    questionTimerSlider.maxValue = timePerQuestion; // z.B. 30
                    questionTimerSlider.minValue = 0;
                    questionTimerSlider.value    = timePerQuestion;
                }
                break;

            case QuizMode.Exam:
                if (_isReviewMode)
                {
                    // In review mode, show full correct/wrong feedback
                    int userAnswer = _userAnswers[index];
                    for (int i = 0; i < answerButtons.Length; i++)
                    {
                        var btn = answerButtons[i];
                        if (!btn || !btn.gameObject.activeSelf) continue;
                        btn.interactable = false;
                        if (i == _current.correctIndex) SetButtonFeedback(btn, correctColor);
                        else if (i == userAnswer)       SetButtonFeedback(btn, wrongColor);
                        else                            ResetButtonFeedback(btn);
                    }
                }
                else
                {
                    // Normal mode: Re-apply previously selected answer (highlight only)
                    int prev = _userAnswers[index];
                    if (prev >= 0 && prev < answerButtons.Length)
                        SetButtonFeedback(answerButtons[prev], selectedColor);
                }
                break;
        }

        UpdateNavigationButtons();
    }

    // ─── Answer Handling ──────────────────────────────────────────────

    private void OnAnswerClicked(int index)
    {
        switch (_currentMode)
        {
            case QuizMode.Learn:  OnLearnAnswer(index);  break;
            case QuizMode.Score:  OnScoreAnswer(index);  break;
            case QuizMode.Exam:   OnExamAnswer(index);   break;
        }
    }

    // --- Learn Mode ---

    private void OnLearnAnswer(int index)
    {
        if (_answered || _current == null) return;
        _answered = true;

        bool correct = (index == _current.correctIndex);

        if (correct)
        {
            _currentScore += 100;
            UpdateScoreUI();
        }

        // Color buttons
        // Visual Feedback
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;

            btn.interactable = false;
            
            if (i == _current.correctIndex)
                SetButtonFeedback(btn, correctColor);
            else if (i == index)
                SetButtonFeedback(btn, wrongColor);
            else
                ResetButtonFeedback(btn);
        }

        if (correct)
        {
            LearnProgressStore.MarkSolved(language, _currentLevel, _current.storageKey);
        }

        UpdateNavigationButtons();
    }

    // --- Score Mode ---

    private void OnScoreAnswer(int index)
    {
        if (_answered || _current == null || _gameOver) return;
        _answered = true;
        _questionTimerRunning = false;

        bool correct = (index == _current.correctIndex);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;
            btn.interactable = false;
            if (i == _current.correctIndex)  SetButtonFeedback(btn, correctColor);
            else if (i == index)              SetButtonFeedback(btn, wrongColor);
            else                             ResetButtonFeedback(btn);
        }

        if (correct)
        {
            _currentScore += pointsPerCorrectAnswer;
            UpdateScoreUI();
        }
        else
        {
            _livesRemaining--;
            UpdateLivesUI();
        }

        if (_livesRemaining <= 0)
        {
            if (isActiveAndEnabled) StartCoroutine(ShowScoreGameOverDelayed());
            else ShowScoreGameOver();
        }
        else
        {
            UpdateNavigationButtons();
        }
    }

    private void OnScoreTimeout()
    {
        if (_answered || _gameOver) return;

        _livesRemaining--;
        UpdateLivesUI();

        // Show correct answer
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;
            btn.interactable = false;
            if (i == _current.correctIndex) SetButtonFeedback(btn, correctColor);
            else ResetButtonFeedback(btn);
        }
        _answered = true;

        if (_livesRemaining <= 0)
        {
            if (isActiveAndEnabled) StartCoroutine(ShowScoreGameOverDelayed());
            else ShowScoreGameOver();
        }
        else
        {
            UpdateNavigationButtons();
        }
    }

    private IEnumerator ShowScoreGameOverDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        ShowScoreGameOver();
    }

    private void ShowScoreGameOver()
    {
        _gameOver = true;
        _questionTimerRunning = false;

        // Save highscore
        bool isNewHighscore = ScoreProgressStore.IsNewHighscore(language, _currentLevel, _currentScore);
        if (isNewHighscore) ScoreProgressStore.SaveHighscore(language, _currentLevel, _currentScore);
        
        ScoreProgressStore.UpdateAllTimeBest(language, _currentScore);

        Debug.Log($"[QuizController] Score mode game over. Score={_currentScore}, NewHighscore={isNewHighscore}");

        int highscore = ScoreProgressStore.GetHighscore(language, _currentLevel);
        int allTimeBest = ScoreProgressStore.GetAllTimeBest(language);

        if (scoreGameOverPanel)
        {
            if (mainMenuController != null) mainMenuController.ShowEndScreen();
            
            scoreGameOverPanel.SetActive(true);
            
            var culture = new System.Globalization.CultureInfo("de-DE");
            if (gameOverScoreText) gameOverScoreText.text = _currentScore.ToString("N0", culture);
            if (gameOverHighscoreText) gameOverHighscoreText.text = highscore.ToString("N0", culture);
            if (gameOverAllTimeBestText) gameOverAllTimeBestText.text = allTimeBest.ToString("N0", culture);
            
            int questionsAnswered = _currentIndex + 1;
            if (gameOverQuestionsAnsweredText) gameOverQuestionsAnsweredText.text = $"{questionsAnswered} Fragen beantwortet";

            // Hide normal quiz UI
            SetActiveBadge((QuizMode)(-1)); // Hide all badges
            SetActive(menuButton?.gameObject, false);
            ShowTopSection(false, false, false);
            SetActive(questionCardRoot, false);
            SetActive(answersContainerRoot, false);
            SetActive(bottomButtonsRoot, false);
        }
        else
        {
            // For now just go back to menu (if game over UI is missing)
            OnMenuButtonClicked();
        }
    }

    // --- Exam Mode ---

    private void OnExamAnswer(int index)
    {
        if (_examFinished) return;

        _userAnswers[_currentIndex] = index;

        // Reset all to neutral, highlight selected
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;
            if (i == index) SetButtonFeedback(btn, selectedColor);
            else            ResetButtonFeedback(btn);
        }
    }

    private void FinishExam()
    {
        _examRunning = false;
        _examFinished = true;

        int correct = 0;
        for (int i = 0; i < _questions.Count; i++)
        {
            if (_userAnswers[i] == _questions[i].correctIndex) correct++;
        }

        ShowExamResult(correct, _questions.Count);
    }

    // ─── Navigation ───────────────────────────────────────────────────

    private void NextQuestion()
    {
        switch (_currentMode)
        {
            case QuizMode.Learn:
                int nextUnsolved = GetNextUnsolvedLearnIndex();
                if (nextUnsolved < 0)
                {
                    Debug.Log("[QuizController] Lernmodus abgeschlossen!");
                    int total = _questions.Count;
                    ShowExamResult(total, total);
                }
                else
                {
                    ShowQuestion(nextUnsolved);
                }
                break;

            case QuizMode.Score:
                _currentIndex++;
                if (_currentIndex >= _questions.Count)
                {
                    ShowScoreGameOver();
                }
                else
                {
                    ShowQuestion(_currentIndex);
                }
                break;

            case QuizMode.Exam:
                if (_currentIndex < _questions.Count - 1)
                    ShowQuestion(_currentIndex + 1);
                break;
        }
    }

    private void PrevQuestion()
    {
        if (_currentMode == QuizMode.Exam && _currentIndex > 0)
            ShowQuestion(_currentIndex - 1);
    }

    private int GetNextUnsolvedLearnIndex()
    {
        if (_questions.Count == 0) return -1;
        for (int step = 1; step <= _questions.Count; step++)
        {
            int idx = (_currentIndex + step) % _questions.Count;
            if (!LearnProgressStore.IsSolved(language, _currentLevel, _questions[idx].storageKey))
                return idx;
        }
        return -1;
    }

    private void UpdateNavigationButtons()
    {
        bool showBack   = false;
        bool showNext   = false;
        bool showFinish = false;

        if (_isReviewMode)
        {
            showBack = _currentIndex > 0;
            showNext = _currentIndex < _questions.Count - 1;
            showFinish = false;
        }
        else
        {
            switch (_currentMode)
            {
                case QuizMode.Learn:
                    showBack   = false;
                    showNext   = _answered;
                    showFinish = false;
                    break;

                case QuizMode.Score:
                    showBack   = false;
                    showNext   = _answered && _livesRemaining > 0;
                    showFinish = false;
                    break;

                case QuizMode.Exam:
                    bool isLast = (_currentIndex >= _questions.Count - 1);
                    showBack   = (_currentIndex > 0);
                    showNext   = !isLast;
                    showFinish = isLast;
                    break;
            }
        }

        SetActive(backButton?.gameObject,   showBack);
        SetActive(nextButton?.gameObject,   showNext);
        SetActive(finishButton?.gameObject, showFinish);
    }

    // ─── Menu Button ──────────────────────────────────────────────────

    public void OnMenuButtonClicked()
    {
        // Persistence: Save Learn progress
        if (_currentMode == QuizMode.Learn)
        {
            LearnProgressStore.SaveLastIndex(language, _currentLevel, _currentIndex);
        }

        // Stop all timers
        _questionTimerRunning = false;
        _examRunning  = false;

        if (mainMenuController != null)
            mainMenuController.HideQuizContainer();
        else
            Debug.LogError("[QuizController] MainMenuController reference not set!");
    }

    // ─── UI Helpers ───────────────────────────────────────────────────

    private void SetActiveBadge(QuizMode mode)
    {
        SetActive(learnModeBadge, mode == QuizMode.Learn);
        SetActive(scoreModeBadge, mode == QuizMode.Score);
        SetActive(examModeBadge,  mode == QuizMode.Exam);
    }

    private void ShowTopSection(bool timerArea, bool livesArea, bool progressArea)
    {
        SetActive(this.timerArea,    timerArea);
        SetActive(this.livesArea,    livesArea);
        SetActive(this.progressArea, progressArea);
    }

    private void UpdateExamTimerUI()
    {
        if (examTimerText)
        {
            int m = Mathf.Max(0, (int)(_examTimeRemaining / 60));
            int s = Mathf.Max(0, (int)(_examTimeRemaining % 60));
            examTimerText.text = $"{m:D2}:{s:D2}";
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {_currentScore}";
        if (scoreInCardText) scoreInCardText.text = $"Score: {_currentScore}";
    }

    private void UpdateLivesUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            // Hearts are shown from left to right; lost lives darken from the right
            bool alive = i < _livesRemaining;
            heartImages[i].color = alive ? heartActiveColor : heartLostColor;
        }
    }

    private void UpdateHighscoreUI()
    {
        if (highscoreText)
        {
            int hs = ScoreProgressStore.GetHighscore(language, _currentLevel);
            highscoreText.text = $"Best: {hs}";
        }
    }

    // Enables the Outline component on the button (or any child) with the given color.
    private void SetButtonFeedback(Button btn, Color color)
    {
        var img = btn.GetComponent<Image>();
        if (img) img.color = color;

        // Also enable outline if present for extra punch
        var outline = btn.GetComponentInChildren<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            outline.effectColor = color;
        }
    }

    private void ResetButtonFeedback(Button btn)
    {
        var img = btn.GetComponent<Image>();
        if (img) img.color = neutralColor;

        var outline = btn.GetComponentInChildren<UnityEngine.UI.Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    private void ShowExamResult(int correct, int total)
    {
        // Hide game UI
        SetActive(questionCardRoot, false);
        SetActive(answersContainerRoot, false);
        SetActive(bottomButtonsRoot, false);
        SetActive(scoreGameOverPanel, false);
        
        // Hide header elements
        SetActiveBadge((QuizMode)(-1));
        SetActive(menuButton?.gameObject, false);

        // Hide TopSection (the timer/progress areas that were overlapping)
        ShowTopSection(false, false, false);

        // Activate the results container in the MainMenuController
        if (mainMenuController != null) mainMenuController.ShowEndScreen();

        // Calculate score
        float pct = total > 0 ? (correct * 100f / total) : 0f;
        bool passed = pct >= passingPercentage;

        // Populate UI
        if (examResultPanel) examResultPanel.SetActive(true);
        if (resultProgressBar)
        {
            resultProgressBar.maxValue = total;
            resultProgressBar.value = correct;
        }
        if (resultProgressText) resultProgressText.text = $"{correct} / {total} \u2713"; 
        
        if (resultTitleText)
        {
            resultTitleText.text = passed ? "BESTANDEN" : "NICHT BESTANDEN";
            resultTitleText.color = passed ? correctColor : wrongColor;
        }

        if (resultPercentageText) resultPercentageText.text = $"{Mathf.RoundToInt(pct)}%";
        if (resultCountSubText) resultCountSubText.text = $"{correct} / {total}";
        if (resultThresholdText) resultThresholdText.text = $"Mindestens {passingPercentage}% zum Bestehen";

        // Show "Fragen anschauen" only in Exam mode or if results vary?
        // Usually Learn mode is already "reviewed" as you go, but keeping it won't hurt.
        if (resultViewQuestionsButton) resultViewQuestionsButton.gameObject.SetActive(true);

        // Record results if in Exam mode
        if (_currentMode == QuizMode.Exam)
        {
            ExamResult res = new ExamResult
            {
                language = language,
                level = _currentLevel,
                totalQuestions = total,
                correctAnswers = correct,
                percentageScore = pct,
                timeUsedSeconds = examTotalTime - _examTimeRemaining,
                passed = passed
            };
            ExamProgressStore.SaveResult(res);
        }
    }

    private void ShowReviewMode()
    {
        _isReviewMode = true;
        _answered = true; // Prevents re-answering
        _currentIndex = 0;

        SetActive(examResultPanel, false);
        SetActive(questionCardRoot, true);
        SetActive(answersContainerRoot, true);
        SetActive(bottomButtonsRoot, true);
        
        // Back to normal header UI?
        SetActiveBadge(_currentMode);
        SetActive(menuButton?.gameObject, true);

        ShowQuestion(_currentIndex);
        UpdateNavigationButtons();
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void ShuffleList(List<Question> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private List<Question> SelectRandom(List<Question> pool, int count)
    {
        var copy = new List<Question>(pool);
        ShuffleList(copy);
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }
}
