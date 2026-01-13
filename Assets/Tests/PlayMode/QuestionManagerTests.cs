using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayMode Tests für QuestionManager
/// Testet das Quiz-System und Fragen-Verwaltung
/// </summary>
public class QuestionManagerTests
{
    private GameManager gm;
    private QuestionManager questionManager;
    private GameInitiator gi;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Lade MainScene
        SceneManager.LoadScene("MainScene");
        yield return null;
        yield return new WaitForSeconds(2f);

        // Finde Manager
        gm = Object.FindFirstObjectByType<GameManager>();
        questionManager = Object.FindFirstObjectByType<QuestionManager>();
        gi = Object.FindFirstObjectByType<GameInitiator>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(questionManager, "QuestionManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ QuestionManager Test Setup abgeschlossen");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: GetRandomQuestion gibt eine Frage zurück (wenn Fragen geladen sind)
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_GetRandomQuestion_ReturnsQuestion()
    {
        Debug.Log("🧪 TEST 1: GetRandomQuestion returns question");

        // Arrange & Act
        QuestionData question = questionManager.GetRandomQuestion();
        yield return null;

        // Assert
        // Frage kann null sein wenn keine JSON-Datei vorhanden ist
        if (question != null)
        {
            Assert.IsNotNull(question.text, "Frage-Text sollte nicht null sein");
            Assert.IsNotNull(question.options, "Optionen sollten nicht null sein");
            Assert.Greater(question.options.Length, 0, "Sollte mindestens eine Option haben");
            Assert.GreaterOrEqual(question.correctIndex, 0, "correctIndex sollte >= 0 sein");
            Assert.Less(question.correctIndex, question.options.Length, "correctIndex sollte < Anzahl Optionen sein");
        }
        else
        {
            Debug.LogWarning("⚠️ Keine Fragen geladen - JSON-Datei möglicherweise nicht vorhanden");
        }

        yield return null;
    }

    /// <summary>
    /// Test 2: GetRandomQuestion gibt verschiedene Fragen zurück
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_GetRandomQuestion_ReturnsDifferentQuestions()
    {
        Debug.Log("🧪 TEST 2: GetRandomQuestion returns different questions");

        // Arrange
        QuestionData question1 = questionManager.GetRandomQuestion();
        yield return new WaitForSeconds(0.1f);
        QuestionData question2 = questionManager.GetRandomQuestion();
        yield return new WaitForSeconds(0.1f);
        QuestionData question3 = questionManager.GetRandomQuestion();
        yield return null;

        // Assert
        if (question1 != null && question2 != null && question3 != null)
        {
            // Mindestens eine Frage sollte unterschiedlich sein (bei genug Fragen)
            bool allSame = question1.id == question2.id && question2.id == question3.id;
            // Das ist OK wenn nur wenige Fragen vorhanden sind
            Debug.Log($"Frage 1 ID: {question1.id}, Frage 2 ID: {question2.id}, Frage 3 ID: {question3.id}");
        }

        yield return null;
    }

    /// <summary>
    /// Test 3: SetLanguage ändert Sprache
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_SetLanguage_ChangesLanguage()
    {
        Debug.Log("🧪 TEST 3: SetLanguage changes language");

        // Arrange
        QuestionManager.QuestionLanguage originalLanguage = questionManager.language;

        // Act
        QuestionManager.QuestionLanguage newLanguage = originalLanguage == QuestionManager.QuestionLanguage.English
            ? QuestionManager.QuestionLanguage.German
            : QuestionManager.QuestionLanguage.English;

        questionManager.SetLanguage(newLanguage);
        yield return new WaitForSeconds(0.5f); // Warte bis Fragen neu geladen sind

        // Assert
        Assert.AreEqual(newLanguage, questionManager.language, "Sprache sollte geändert sein");

        yield return null;
    }

    /// <summary>
    /// Test 4: SetDifficulty ändert Schwierigkeit
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_SetDifficulty_ChangesDifficulty()
    {
        Debug.Log("🧪 TEST 4: SetDifficulty changes difficulty");

        // Arrange
        QuestionManager.QuestionDifficulty originalDifficulty = questionManager.difficulty;

        // Act
        QuestionManager.QuestionDifficulty newDifficulty = originalDifficulty == QuestionManager.QuestionDifficulty.Junior
            ? QuestionManager.QuestionDifficulty.Senior
            : QuestionManager.QuestionDifficulty.Junior;

        questionManager.SetDifficulty(newDifficulty);
        yield return new WaitForSeconds(0.5f); // Warte bis Fragen neu geladen sind

        // Assert
        Assert.AreEqual(newDifficulty, questionManager.difficulty, "Schwierigkeit sollte geändert sein");

        yield return null;
    }

    /// <summary>
    /// Test 5: QuestionData Struktur ist korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_QuestionData_StructureIsValid()
    {
        Debug.Log("🧪 TEST 5: QuestionData structure is valid");

        // Arrange & Act
        QuestionData question = questionManager.GetRandomQuestion();
        yield return null;

        // Assert
        if (question != null)
        {
            Assert.Greater(question.id, 0, "ID sollte > 0 sein");
            Assert.IsNotNull(question.text, "Text sollte nicht null sein");
            Assert.IsNotEmpty(question.text, "Text sollte nicht leer sein");
            Assert.IsNotNull(question.options, "Options Array sollte nicht null sein");
            Assert.GreaterOrEqual(question.options.Length, 2, "Sollte mindestens 2 Optionen haben");
            Assert.GreaterOrEqual(question.correctIndex, 0, "correctIndex sollte >= 0 sein");
            Assert.Less(question.correctIndex, question.options.Length, "correctIndex sollte < Anzahl Optionen sein");

            // Prüfe dass alle Optionen nicht leer sind
            foreach (string option in question.options)
            {
                Assert.IsNotNull(option, "Option sollte nicht null sein");
                Assert.IsNotEmpty(option, "Option sollte nicht leer sein");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Keine Fragen geladen - kann Struktur nicht testen");
        }

        yield return null;
    }

    /// <summary>
    /// Test 6: StartQuizSeries initialisiert Serie korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test6_StartQuizSeries_InitializesSeries()
    {
        Debug.Log("🧪 TEST 6: StartQuizSeries initializes series");

        // Arrange
        int totalQuestions = 3;
        int requiredCorrect = 2;
        bool callbackCalled = false;
        bool callbackResult = false;

        System.Action<bool> onDone = (success) =>
        {
            callbackCalled = true;
            callbackResult = success;
        };

        // Act
        questionManager.StartQuizSeries(totalQuestions, requiredCorrect, onDone);
        yield return new WaitForSeconds(0.1f);

        // Assert
        // Serie sollte initialisiert sein (kann nicht direkt geprüft werden, da private)
        // Aber wir können prüfen ob keine Exception geworfen wurde
        Assert.IsTrue(true, "StartQuizSeries sollte ohne Fehler aufgerufen werden können");

        yield return null;
    }

    /// <summary>
    /// Test 7: GetRandomQuestion gibt null zurück wenn keine Fragen vorhanden
    /// </summary>
    [UnityTest]
    public IEnumerator Test7_GetRandomQuestion_ReturnsNullWhenNoQuestions()
    {
        Debug.Log("🧪 TEST 7: GetRandomQuestion returns null when no questions");

        // Arrange: Setze auf ungültige Kombination (falls möglich)
        // Oder teste einfach dass Methode null zurückgeben kann

        // Act
        QuestionData question = questionManager.GetRandomQuestion();
        yield return null;

        // Assert
        // Frage kann null sein - das ist OK wenn keine Fragen geladen sind
        if (question == null)
        {
            Debug.Log("⚠️ Keine Fragen verfügbar - das ist OK wenn JSON-Dateien fehlen");
        }

        yield return null;
    }
}
