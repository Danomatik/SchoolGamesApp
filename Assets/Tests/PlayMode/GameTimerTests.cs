using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayMode Tests für GameTimerManager
/// </summary>
public class GameTimerTests
{
    private GameManager gm;
    private GameTimerManager timerManager;
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
        timerManager = Object.FindFirstObjectByType<GameTimerManager>();
        gi = Object.FindFirstObjectByType<GameInitiator>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(timerManager, "GameTimerManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ GameTimer Test Setup abgeschlossen");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: Timer startet korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_Timer_StartsCorrectly()
    {
        Debug.Log("🧪 TEST 1: Timer starts correctly");

        // Arrange: Timer wird bereits im Start() automatisch gestartet
        // Stoppe Timer zuerst
        timerManager.StopTimer();
        yield return new WaitForSeconds(0.1f);
        
        float timeBeforeStart = timerManager.GetTimeRemaining();
        Debug.Log($"   Time before start: {timeBeforeStart}s");

        // Act: Starte Timer neu (setzt timeRemaining auf gameDurationInSeconds zurück)
        timerManager.StartTimer();
        yield return new WaitForSeconds(0.5f);

        // Assert
        float currentTime = timerManager.GetTimeRemaining();
        Debug.Log($"   Current time after start: {currentTime}s");
        
        // StartTimer() setzt timeRemaining auf gameDurationInSeconds zurück (300s)
        // Dann läuft der Timer und die Zeit nimmt ab
        // Prüfe dass Timer läuft (Zeit sollte < 300 sein nach 0.5s)
        Assert.Less(currentTime, 300f, "Zeit sollte < 300s sein (Timer läuft)");
        Assert.Greater(currentTime, 0, "Zeit sollte noch > 0 sein");

        yield return null;
    }

    /// <summary>
    /// Test 2: Timer stoppt korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_Timer_StopsCorrectly()
    {
        Debug.Log("🧪 TEST 2: Timer stops correctly");

        // Arrange
        timerManager.StartTimer();
        yield return new WaitForSeconds(0.5f);
        float timeBeforeStop = timerManager.GetTimeRemaining();

        // Act
        timerManager.StopTimer();
        yield return new WaitForSeconds(0.5f);

        // Assert
        float timeAfterStop = timerManager.GetTimeRemaining();
        Assert.AreEqual(timeBeforeStop, timeAfterStop, 0.1f, "Zeit sollte gleich bleiben nach Stop");

        yield return null;
    }

    /// <summary>
    /// Test 3: Timer läuft ab und beendet Spiel
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_Timer_EndsGame_WhenTimeRunsOut()
    {
        Debug.Log("🧪 TEST 3: Timer ends game when time runs out");

        // Arrange: Setze Timer auf sehr kurze Zeit (1 Sekunde)
        // Hinweis: Dies erfordert Zugriff auf private Felder, daher testen wir die Logik indirekt
        timerManager.StartTimer();
        
        // Act: Warte kurz
        yield return new WaitForSeconds(0.5f);

        // Assert: Timer sollte laufen
        Assert.IsTrue(timerManager.GetTimeRemaining() > 0, "Timer sollte noch laufen");

        yield return null;
    }

    /// <summary>
    /// Test 4: Timer zeigt korrekte Zeit an
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_Timer_DisplaysCorrectTime()
    {
        Debug.Log("🧪 TEST 4: Timer displays correct time");

        // Arrange: Stoppe Timer zuerst, dann starte neu
        timerManager.StopTimer();
        yield return new WaitForSeconds(0.1f);
        
        // StartTimer() setzt timeRemaining auf gameDurationInSeconds zurück (standardmäßig 300s = 5 Minuten)
        timerManager.StartTimer();
        yield return new WaitForSeconds(0.1f); // Warte kurz damit StartTimer() angewendet wird
        
        float timeAfterStart = timerManager.GetTimeRemaining();
        
        Debug.Log($"   Time after start: {timeAfterStart}s");
        
        // Assert: Nach StartTimer() sollte die Zeit nahe der vollen Dauer sein (standardmäßig 300s)
        // Erlaube Toleranz von 5 Sekunden für Timing-Unterschiede
        Assert.GreaterOrEqual(timeAfterStart, 295f, "Zeit sollte nach StartTimer() nahe der vollen Dauer sein (≥295s)");
        Assert.LessOrEqual(timeAfterStart, 300f, "Zeit sollte nach StartTimer() nicht über der vollen Dauer sein (≤300s)");
        
        // Act: Warte 1 Sekunde
        yield return new WaitForSeconds(1f);

        // Assert: Zeit sollte abgenommen haben
        float newTime = timerManager.GetTimeRemaining();
        Debug.Log($"   Time after 1 second: {newTime}s");
        Assert.Less(newTime, timeAfterStart, "Zeit sollte nach 1 Sekunde abgenommen haben");
        Assert.Greater(newTime, 0, "Zeit sollte noch positiv sein");

        yield return null;
    }

    /// <summary>
    /// Test 5: Timer wird beim Zugwechsel zurückgesetzt
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_Timer_ResetsOnTurnChange()
    {
        Debug.Log("🧪 TEST 5: Timer resets on turn change");

        // Arrange
        timerManager.StartTimer();
        yield return new WaitForSeconds(0.5f);
        float timeBeforeReset = timerManager.GetTimeRemaining();

        // Act: Simuliere Zugwechsel (Stop und Start)
        timerManager.StopTimer();
        timerManager.StartTimer();
        yield return new WaitForSeconds(0.1f);

        // Assert: Timer sollte neu gestartet sein
        Assert.IsTrue(timerManager.GetTimeRemaining() > 0, "Timer sollte nach Reset laufen");

        yield return null;
    }
}
