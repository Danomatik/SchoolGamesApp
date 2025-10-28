using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject modeSelectPanel;
    public GameObject learnModePanel;
    public GameObject scoreModePanel;
    public GameObject examModePanel;

    [Header("Mode Selection Buttons")]
    public Button learnModeButton;
    public Button scoreModeButton;
    public Button examModeButton;
    public Button backToMenuButton;

    [Header("Controllers")]
    public LearnModeController learnModeController;
    public ScoreModeController scoreModeController;

    private void Awake()
    {
        // Buttons verdrahten
        if (learnModeButton)
            learnModeButton.onClick.AddListener(OnLearnModeClicked);
        
        if (scoreModeButton)
        {
            scoreModeButton.onClick.AddListener(OnScoreModeClicked);
            scoreModeButton.interactable = true; // Aktiviert!
        }
        
        if (examModeButton)
        {
            examModeButton.onClick.AddListener(OnExamModeClicked);
            examModeButton.interactable = false; // Deaktivieren, da noch nicht implementiert
        }
        
        if (backToMenuButton)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
    }

    private void Start()
    {
        // Beim Start: ModeSelectPanel anzeigen, alle anderen ausblenden
        ShowModeSelectPanel();
    }

    public void ShowModeSelectPanel()
    {
        if (modeSelectPanel) modeSelectPanel.SetActive(true);
        if (learnModePanel) learnModePanel.SetActive(false);
        if (scoreModePanel) scoreModePanel.SetActive(false);
        if (examModePanel) examModePanel.SetActive(false);
    }

    private void OnLearnModeClicked()
    {
        if (modeSelectPanel) modeSelectPanel.SetActive(false);
        if (learnModePanel) learnModePanel.SetActive(true);
    }

    private void OnScoreModeClicked()
    {
        if (modeSelectPanel) modeSelectPanel.SetActive(false);
        if (scoreModePanel) scoreModePanel.SetActive(true);
    }

    private void OnExamModeClicked()
    {
        // Noch nicht implementiert
        Debug.Log("ExamMode noch nicht verfügbar");
    }

    private void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("MenuScene");
    }
}

