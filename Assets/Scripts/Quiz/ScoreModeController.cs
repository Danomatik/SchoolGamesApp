using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreModeController : MonoBehaviour
{
    [Header("Data/Provider")]
    public SimpleJsonQuestionProvider provider;
    public QuizLang language = QuizLang.DE;
    private LearnLevel level = LearnLevel.Junior;

    [Header("UI - Panels")]
    public GameObject levelSelectPanel;
    public Button btnJunior;
    public Button btnSenior;
    public Button levelBackButton;           // Zurück-Button im LevelSelectPanel
    public GameObject quizPanel;
    public GameObject gameOverPanel;
    public Button backToMenuButton;

    [Header("UI - Quiz")]
    public TMP_Text questionText;
    public Button[] answerButtons = new Button[3];
    public Button nextButton;

    [Header("UI - Score & Lives")]
    public TMP_Text livesText;
    public TMP_Text scoreText;
    public TMP_Text highscoreText;
    public Slider timerSlider;

    [Header("UI - Game Over")]
    public TMP_Text finalScoreText;
    public TMP_Text gameOverHighscoreText;
    public TMP_Text newHighscoreText;
    public Button restartButton;
    public Button backButton;

    [Header("Game Settings")]
    public float timePerQuestion = 20f;
    public int pointsPerCorrectAnswer = 100;
    public int startingLives = 3;

    [Header("Colors")]
    public Color neutralColor = new Color32(0x2A, 0x7C, 0xA6, 0xFF);
    public Color correctColor = new Color(0.35f, 0.85f, 0.45f);
    public Color wrongColor = new Color(0.85f, 0.30f, 0.30f);

    // Intern
    private List<Question> _pool = new();
    private int _currentIndex = -1;
    private Question _current;
    private bool _answered = false;

    private int _currentScore = 0;
    private int _livesRemaining = 3;
    private float _currentTime = 0f;
    private bool _timerRunning = false;
    private bool _gameOver = false;

    private void Awake()
    {
        // Timer Slider nicht bedienbar
        if (timerSlider) timerSlider.interactable = false;

        // Panels initial
        if (quizPanel) quizPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (nextButton) nextButton.gameObject.SetActive(false);

        // Level-Auswahl
        if (btnJunior) btnJunior.onClick.AddListener(() => StartLevel(LearnLevel.Junior));
        if (btnSenior) btnSenior.onClick.AddListener(() => StartLevel(LearnLevel.Senior));

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
                answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
            }
        }

        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextQuestion);
        }

        // Game Over Buttons
        if (restartButton) restartButton.onClick.AddListener(RestartGame);
        if (backButton) backButton.onClick.AddListener(BackToMenu);
    }

    private void Update()
    {
        if (_timerRunning && !_gameOver)
        {
            _currentTime -= Time.deltaTime;

            // Timer UI aktualisieren
            if (timerSlider)
            {
                timerSlider.value = Mathf.Max(0, _currentTime / timePerQuestion);
            }

            // Zeit abgelaufen
            if (_currentTime <= 0)
            {
                _timerRunning = false;
                OnTimeOut();
            }
        }
    }

    private void StartLevel(LearnLevel selected)
    {
        level = selected;

        _pool = provider != null ? provider.LoadQuestionsFlat(language, level) : new List<Question>();
        if (_pool == null || _pool.Count == 0)
        {
            Debug.LogError($"ScoreModeController: Keine Fragen gefunden für {level}-{language}");
            return;
        }

        // Fragen mischen für zufällige Reihenfolge
        ShuffleQuestions();

        // UI umschalten
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (quizPanel)
        {
            quizPanel.SetActive(true);
            // Sicherstellen, dass dieses Objekt aktiviert ist, damit Update() läuft
            if (!isActiveAndEnabled && gameObject != null) gameObject.SetActive(true);
        }

        // Game initialisieren
        _currentScore = 0;
        _livesRemaining = startingLives;
        _gameOver = false;
        _currentIndex = -1;

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateHighscoreUI();

        NextQuestion();
    }

    private void ShuffleQuestions()
    {
        // Fisher-Yates Shuffle
        for (int i = _pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Question temp = _pool[i];
            _pool[i] = _pool[j];
            _pool[j] = temp;
        }
    }

    private void ShowQuestion(Question q)
    {
        _current = q;
        _answered = false;

        if (questionText) questionText.text = q.text ?? "";

        // Alle Buttons zurücksetzen
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

        if (nextButton) nextButton.gameObject.SetActive(false);

        // Timer starten (nur wenn Panel aktiv ist)
        _currentTime = timePerQuestion;
        _timerRunning = gameObject.activeInHierarchy;
        if (timerSlider)
        {
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f;
        }
    }

    private void OnAnswerClicked(int index)
    {
        if (_answered || _current == null || _gameOver) return;
        _answered = true;
        _timerRunning = false;

        bool correct = (index == _current.correctIndex);

        // Buttons einfärben
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;

            btn.interactable = false;
            var img = btn.GetComponent<Image>();
            if (img == null) continue;

            if (i == _current.correctIndex)
                img.color = correctColor;
            else if (i == index)
                img.color = wrongColor;
            else
                img.color = neutralColor;
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

        if (nextButton) nextButton.gameObject.SetActive(true);

        // Prüfen ob Game Over
        if (_livesRemaining <= 0)
        {
            // Falls dieses GameObject inaktiv ist, kann keine Coroutine gestartet werden
            if (isActiveAndEnabled)
                StartCoroutine(ShowGameOverDelayed());
            else
                ShowGameOver();
        }
    }

    private void OnTimeOut()
    {
        if (_answered || _gameOver) return;

        // Zeit abgelaufen = falscher Antwort
        _livesRemaining--;
        UpdateLivesUI();

        // Richtige Antwort grün färben
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;

            btn.interactable = false;
            var img = btn.GetComponent<Image>();
            if (img == null) continue;

            if (i == _current.correctIndex)
                img.color = correctColor;
            else
                img.color = neutralColor;
        }

        _answered = true;

        if (_livesRemaining <= 0)
        {
            if (isActiveAndEnabled)
                StartCoroutine(ShowGameOverDelayed());
            else
                ShowGameOver();
        }
        else
        {
            if (nextButton) nextButton.gameObject.SetActive(true);
        }
    }

    private void NextQuestion()
    {
        _currentIndex++;

        // Alle Fragen durch?
        if (_currentIndex >= _pool.Count)
        {
            ShowGameOver();
            return;
        }

        ShowQuestion(_pool[_currentIndex]);
    }

    private IEnumerator ShowGameOverDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        _gameOver = true;
        _timerRunning = false;

        // Highscore prüfen und speichern
        int highscore = ScoreProgressStore.GetHighscore(language, level);
        bool isNewHighscore = ScoreProgressStore.IsNewHighscore(language, level, _currentScore);
        
        if (isNewHighscore)
        {
            ScoreProgressStore.SaveHighscore(language, level, _currentScore);
            highscore = _currentScore;
        }

        // Game Over Panel anzeigen
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (quizPanel) quizPanel.SetActive(false);

        if (finalScoreText)
            finalScoreText.text = $"Score: {_currentScore}";

        if (gameOverHighscoreText)
            gameOverHighscoreText.text = $"Highscore: {highscore}";

        if (newHighscoreText)
            newHighscoreText.gameObject.SetActive(isNewHighscore);
    }

    private void RestartGame()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        StartLevel(level);
    }

    private void BackToMenu()
    {
        SceneManager.LoadScene("Quiz");
    }

    private void UpdateScoreUI()
    {
        if (scoreText)
            scoreText.text = $"Score: {_currentScore}";
    }

    private void UpdateLivesUI()
    {
        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < _livesRemaining; i++)
            {
                hearts += "♥";
            }
            livesText.text = hearts;
        }
    }

    private void UpdateHighscoreUI()
    {
        if (highscoreText)
        {
            int highscore = ScoreProgressStore.GetHighscore(language, level);
            highscoreText.text = $"Best: {highscore}";
        }
    }
}

