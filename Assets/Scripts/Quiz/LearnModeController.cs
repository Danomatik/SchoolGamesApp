using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LearnModeController : MonoBehaviour
{
   [Header("Data")]
    public SimpleJsonQuestionProvider provider;   // <— HIER den Typ ändern!

    [Header("UI")]
    public TMP_Text categoryLabel;
    public TMP_Text questionText;

    public AnswerButton answerBtn0;
    public AnswerButton answerBtn1;
    public AnswerButton answerBtn2;

    public Button nextButton;

    private List<Question> _pool = new();
    private int _currentIndex = -1;
    private Question _current;
    void Start()
    {
        if (categoryLabel) categoryLabel.text = provider != null ? provider.categoryKey : "";
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextQuestion);
            nextButton.gameObject.SetActive(false);
        }

        LoadQuestions();
        NextQuestion();
    }

    void LoadQuestions()
    {
        if (provider == null)
        {
            Debug.LogError("[LearnMode] Kein Provider gesetzt.");
            return;
        }

        _pool = provider.LoadQuestions();
        if (_pool.Count == 0)
        {
            Debug.LogWarning("[LearnMode] Keine Fragen geladen.");
        }
        Shuffle(_pool); // Fragenreihenfolge mischen (Antwort-Slots bleiben fix)
    }

    void NextQuestion()
    {
        _currentIndex++;
        if (_currentIndex >= _pool.Count)
        {
            _currentIndex = 0;
            Shuffle(_pool);
        }

        _current = _pool.Count > 0 ? _pool[_currentIndex] : null;

        // UI reset
        if (nextButton) nextButton.gameObject.SetActive(false);

        if (_current == null)
        {
            questionText.text = "Keine Fragen verfügbar.";
            answerBtn0.SetInteractable(false);
            answerBtn1.SetInteractable(false);
            answerBtn2.SetInteractable(false);
            return;
        }

        questionText.text = _current.text;

        // Buttons neu initialisieren (Antwort-Slot-Reihenfolge wie in JSON)
        answerBtn0.Init(_current.options[0], 0, OnAnswerClicked);
        answerBtn1.Init(_current.options[1], 1, OnAnswerClicked);
        answerBtn2.Init(_current.options[2], 2, OnAnswerClicked);
    }

    void OnAnswerClicked(int index)
    {
        // Eingaben sperren
        answerBtn0.SetInteractable(false);
        answerBtn1.SetInteractable(false);
        answerBtn2.SetInteractable(false);

        // Visualisieren
        if (index == _current.correctIndex)
        {
            GetBtn(index).SetStateCorrect();
        }
        else
        {
            GetBtn(index).SetStateWrong();
            GetBtn(_current.correctIndex).SetStateCorrect();
        }

        if (nextButton) nextButton.gameObject.SetActive(true);
    }

    AnswerButton GetBtn(int i)
    {
        switch (i)
        {
            case 0: return answerBtn0;
            case 1: return answerBtn1;
            default: return answerBtn2;
        }
    }

    // Fisher-Yates
    void Shuffle<T>(IList<T> list)
    {
        var rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }
}
