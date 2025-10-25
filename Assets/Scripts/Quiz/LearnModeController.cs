using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LearnModeController : MonoBehaviour
{
    [Header("Data/Provider")]
    public SimpleJsonQuestionProvider provider;
    public QuizLang language = QuizLang.DE;            // aus Settings übernehmen, wenn vorhanden
    private LearnLevel level = LearnLevel.Junior;      // wird über UI gesetzt

    [Header("UI - Panels")]
    public GameObject levelSelectPanel;
    public Button btnJunior;
    public Button btnSenior;
    public GameObject quizPanel;

    [Header("UI - Quiz")]
    public TMP_Text categoryLabel;               // z.B. "DE • Junior • gruendung"
    public TMP_Text questionText;
    public Transform answersContainer;           // optional (nur fürs Layout)
    public Button[] answerButtons = new Button[3];
    public Button nextButton;

    [Header("UI - Progress")]
    public Slider progressSlider;
    public TMP_Text progressText;                // "x / y"

    [Header("Colors")]
    public Color neutralColor = new Color32(0x2A, 0x7C, 0xA6, 0xFF); // #2A7CA6
    public Color correctColor = new Color(0.35f, 0.85f, 0.45f);      // dunkleres Grün
    public Color wrongColor   = new Color(0.85f, 0.30f, 0.30f);      // dunkleres Rot

    // intern
    private List<Question> _pool = new();
    private int _currentIndex = -1;
    private Question _current;
    private bool _answered = false;

    private void Awake()
    {
        // Slider nicht vom User bedienbar
        if (progressSlider) progressSlider.interactable = false;

        // Panels initial
        if (quizPanel) quizPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
        if (nextButton) nextButton.gameObject.SetActive(false);

        // Level-Auswahl
        if (btnJunior) btnJunior.onClick.AddListener(() => StartLevel(LearnLevel.Junior));
        if (btnSenior) btnSenior.onClick.AddListener(() => StartLevel(LearnLevel.Senior));

        // Answer-Button Listener (fixe 3 Buttons)
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
    }

    private void StartLevel(LearnLevel selected)
    {
        level = selected;

        _pool = provider != null ? provider.LoadQuestionsFlat(language, level) : new List<Question>();
        if (_pool == null || _pool.Count == 0)
        {
            Debug.LogError($"LearnModeController: Keine Fragen gefunden unter {provider?.resourcesFolder}/{provider?.filePattern}"
                + $" (Level={level}, Lang={language})");
            return;
        }

        // UI umschalten
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (quizPanel) quizPanel.SetActive(true);

        UpdateProgressUI();

        _currentIndex = -1;
        NextQuestion();
    }

    private void ShowQuestion(Question q)
    {
        _current = q;
        _answered = false;

        if (categoryLabel)
        {
            categoryLabel.text = $"{language} • {level} • {q.category}";
        }
        if (questionText) questionText.text = q.text ?? "";

        // Alle Buttons zurücksetzen & ausblenden
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn) continue;
            btn.gameObject.SetActive(false);
            btn.interactable = true;

            var img = btn.GetComponent<Image>();
            if (img) img.color = neutralColor;   // NEUTRAL statt Weiß

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
    }

    private void OnAnswerClicked(int index)
    {
        if (_answered || _current == null) return;
        _answered = true;

        bool correct = (index == _current.correctIndex);

        // Buttons einfärben & locken
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;

            btn.interactable = false;
            var img = btn.GetComponent<Image>();
            if (img == null) continue;

            if (i == _current.correctIndex)
                img.color = correctColor;             // dunkles Grün
            else if (i == index)
                img.color = wrongColor;               // dunkles Rot
            else
                img.color = neutralColor;             // neutral bleibt dunkelblau
        }

        if (correct)
        {
            LearnProgressStore.MarkSolved(language, level, _current.storageKey);
            UpdateProgressUI();
        }

        if (nextButton) nextButton.gameObject.SetActive(true);
    }

    private void NextQuestion()
    {
        int nextIdx = GetNextUnsolvedIndex();
        if (nextIdx < 0)
        {
            ShowAllDone();
            return;
        }

        _currentIndex = nextIdx;
        ShowQuestion(_pool[_currentIndex]);
    }

    private int GetNextUnsolvedIndex()
    {
        if (_pool.Count == 0) return -1;

        // Ab aktueller Position vorwärts, dann wrap-around
        for (int step = 1; step <= _pool.Count; step++)
        {
            int idx = (_currentIndex + step) % _pool.Count;
            var q = _pool[idx];
            bool solved = LearnProgressStore.IsSolved(language, level, q.storageKey);
            if (!solved) return idx;
        }
        return -1; // alles gelöst
    }

    private void UpdateProgressUI()
    {
        int solved = LearnProgressStore.CountSolved(language, level, _pool);
        int total = _pool.Count;

        if (progressSlider)
        {
            progressSlider.maxValue = total;
            progressSlider.value = solved;
            progressSlider.interactable = false; // Benutzer kann nicht sliden
        }
        if (progressText)
        {
            progressText.text = $"{solved}/{total}";
        }
    }

    private void ShowAllDone()
    {
        if (questionText) questionText.text = "Super! Du hast alle Fragen richtig beantwortet.";
        // Buttons weg
        foreach (var b in answerButtons) if (b) b.gameObject.SetActive(false);

        if (nextButton)
        {
            nextButton.gameObject.SetActive(true);
            var txt = nextButton.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = "Erneut starten";
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(RestartLevel);
        }
    }

    private void RestartLevel()
    {
        LearnProgressStore.Reset(language, level);
        UpdateProgressUI();

        if (nextButton)
        {
            var txt = nextButton.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = "Nächste Frage";
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextQuestion);
        }

        _currentIndex = -1;
        NextQuestion();
    }
}
