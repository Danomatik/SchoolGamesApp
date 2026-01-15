using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode Unit Tests für Timer-Berechnungen
/// </summary>
public class TimerCalculationsTests
{
    [Test]
    public void Timer_ConvertMinutesToSeconds()
    {
        // Arrange
        float minutes = 5f;

        // Act
        float seconds = minutes * 60f;

        // Assert
        Assert.AreEqual(300f, seconds, "5 Minuten sollten 300 Sekunden sein");
    }

    [Test]
    public void Timer_ConvertSecondsToMinutes()
    {
        // Arrange
        float seconds = 300f;

        // Act
        float minutes = seconds / 60f;

        // Assert
        Assert.AreEqual(5f, minutes, "300 Sekunden sollten 5 Minuten sein");
    }

    [Test]
    public void Timer_FormatTime_MMSS()
    {
        // Arrange
        float timeRemaining = 125f; // 2 Minuten 5 Sekunden

        // Act
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        // Assert
        Assert.AreEqual(2, minutes, "Sollte 2 Minuten sein");
        Assert.AreEqual(5, seconds, "Sollte 5 Sekunden sein");
    }

    [Test]
    public void Timer_FormatTime_Zero()
    {
        // Arrange
        float timeRemaining = 0f;

        // Act
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        // Assert
        Assert.AreEqual(0, minutes, "Sollte 0 Minuten sein");
        Assert.AreEqual(0, seconds, "Sollte 0 Sekunden sein");
    }

    [Test]
    public void Timer_FormatTime_MaxDuration()
    {
        // Arrange
        float maxDuration = 30f * 60f; // 30 Minuten = 1800 Sekunden

        // Act
        int minutes = Mathf.FloorToInt(maxDuration / 60f);
        int seconds = Mathf.FloorToInt(maxDuration % 60f);

        // Assert
        Assert.AreEqual(30, minutes, "Sollte 30 Minuten sein");
        Assert.AreEqual(0, seconds, "Sollte 0 Sekunden sein");
    }

    [Test]
    public void Timer_Countdown_Decreases()
    {
        // Arrange
        float timeRemaining = 100f;
        float deltaTime = 1f; // 1 Sekunde vergangen

        // Act
        float newTime = timeRemaining - deltaTime;

        // Assert
        Assert.AreEqual(99f, newTime, "Zeit sollte um 1 Sekunde reduziert sein");
    }

    [Test]
    public void Timer_Countdown_ReachesZero()
    {
        // Arrange
        float timeRemaining = 1f;
        float deltaTime = 1f;

        // Act
        float newTime = timeRemaining - deltaTime;
        if (newTime < 0) newTime = 0; // Clamp to 0

        // Assert
        Assert.AreEqual(0f, newTime, "Zeit sollte 0 sein wenn abgelaufen");
    }

    [Test]
    public void Timer_GameDuration_MinMax()
    {
        // Arrange
        float minDuration = 0f;
        float maxDuration = 30f; // 30 Minuten Maximum

        // Act & Assert
        Assert.GreaterOrEqual(minDuration, 0f, "Minimum Dauer sollte >= 0 sein");
        Assert.LessOrEqual(maxDuration, 30f, "Maximum Dauer sollte <= 30 Minuten sein");
    }
}
