using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode Unit Tests für PlayerSetupManager
/// Testet das Speichern und Laden von Spieler-Einstellungen
/// </summary>
public class PlayerSetupManagerTests
{
    private PlayerSetupManager setupManager;
    private GameObject testObject;

    [SetUp]
    public void SetUp()
    {
        // Erstelle Test-Objekt
        testObject = new GameObject("TestPlayerSetupManager");
        setupManager = testObject.AddComponent<PlayerSetupManager>();
        
        // Lösche alle PlayerPrefs für saubere Tests
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        // Aufräumen
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [Test]
    public void PlayerSetupManager_SetPlayerCount_SavesCorrectly()
    {
        // Arrange
        int testCount = 4;

        // Act
        setupManager.SetPlayerCount(testCount);

        // Assert
        int savedCount = setupManager.GetPlayerCount();
        Assert.AreEqual(testCount, savedCount, "Spieleranzahl sollte korrekt gespeichert werden");
    }

    [Test]
    public void PlayerSetupManager_GetPlayerCount_ReturnsDefaultWhenNotSet()
    {
        // Arrange - Keine Daten gespeichert

        // Act
        int count = setupManager.GetPlayerCount();

        // Assert
        Assert.AreEqual(0, count, "Sollte 0 zurückgeben wenn nicht gesetzt (Standard)");
    }

    [Test]
    public void PlayerSetupManager_SetPlayerName_SavesCorrectly()
    {
        // Arrange
        int playerID = 1;
        string playerName = "Test Spieler";

        // Act
        setupManager.SetPlayerName(playerID, playerName);

        // Assert
        string savedName = setupManager.GetPlayerName(playerID);
        Assert.AreEqual(playerName, savedName, "Spielername sollte korrekt gespeichert werden");
    }

    [Test]
    public void PlayerSetupManager_GetPlayerName_ReturnsDefaultWhenNotSet()
    {
        // Arrange - Keine Daten gespeichert
        int playerID = 1;

        // Act
        string name = setupManager.GetPlayerName(playerID);

        // Assert
        Assert.AreEqual("Spieler 1", name, "Sollte Fallback-Name zurückgeben");
    }

    [Test]
    public void PlayerSetupManager_SetPlayerName_HandlesEmptyString()
    {
        // Arrange
        int playerID = 2;
        string emptyName = "";

        // Act
        setupManager.SetPlayerName(playerID, emptyName);

        // Assert
        string savedName = setupManager.GetPlayerName(playerID);
        Assert.AreEqual("Spieler 2", savedName, "Sollte Fallback-Name verwenden bei leerem String");
    }

    [Test]
    public void PlayerSetupManager_SetPlayerName_HandlesNull()
    {
        // Arrange
        int playerID = 3;

        // Act
        setupManager.SetPlayerName(playerID, null);

        // Assert
        string savedName = setupManager.GetPlayerName(playerID);
        Assert.AreEqual("Spieler 3", savedName, "Sollte Fallback-Name verwenden bei null");
    }

    [Test]
    public void PlayerSetupManager_GetAllPlayerNames_ReturnsCorrectNames()
    {
        // Arrange
        setupManager.SetPlayerCount(3);
        setupManager.SetPlayerName(1, "Alice");
        setupManager.SetPlayerName(2, "Bob");
        setupManager.SetPlayerName(3, "Charlie");

        // Act
        var names = setupManager.GetAllPlayerNames();

        // Assert
        Assert.AreEqual(3, names.Count, "Sollte 3 Namen zurückgeben");
        Assert.Contains("Alice", names);
        Assert.Contains("Bob", names);
        Assert.Contains("Charlie", names);
    }

    [Test]
    public void PlayerSetupManager_SetGameDuration_SavesCorrectly()
    {
        // Arrange
        float testDuration = 7.5f;

        // Act
        setupManager.SetGameDuration(testDuration);

        // Assert
        float savedDuration = setupManager.GetGameDuration();
        Assert.AreEqual(testDuration, savedDuration, 0.01f, "Spiel-Dauer sollte korrekt gespeichert werden");
    }

    [Test]
    public void PlayerSetupManager_GetGameDuration_ReturnsDefaultWhenNotSet()
    {
        // Arrange - Keine Daten gespeichert

        // Act
        float duration = setupManager.GetGameDuration();

        // Assert
        Assert.AreEqual(5f, duration, 0.01f, "Sollte Standard-Wert (5 Minuten) zurückgeben");
    }

    [Test]
    public void PlayerSetupManager_GetGameDuration_ClampsToMax30Minutes()
    {
        // Arrange
        float tooHighDuration = 35f;

        // Act
        setupManager.SetGameDuration(tooHighDuration);
        float savedDuration = setupManager.GetGameDuration();

        // Assert
        Assert.LessOrEqual(savedDuration, 30f, "Sollte auf max 30 Minuten begrenzt werden");
    }

    [Test]
    public void PlayerSetupManager_GetGameDuration_ClampsToMin0Minutes()
    {
        // Arrange
        float negativeDuration = -5f;

        // Act
        setupManager.SetGameDuration(negativeDuration);
        float savedDuration = setupManager.GetGameDuration();

        // Assert
        Assert.GreaterOrEqual(savedDuration, 0f, "Sollte auf min 0 Minuten begrenzt werden");
    }

    [Test]
    public void PlayerSetupManager_HasPlayerData_ReturnsFalseWhenEmpty()
    {
        // Arrange - Keine Daten

        // Act
        bool hasData = setupManager.HasPlayerData();

        // Assert
        Assert.IsFalse(hasData, "Sollte false zurückgeben wenn keine Daten vorhanden");
    }

    [Test]
    public void PlayerSetupManager_HasPlayerData_ReturnsTrueWhenDataExists()
    {
        // Arrange
        setupManager.SetPlayerCount(4);

        // Act
        bool hasData = setupManager.HasPlayerData();

        // Assert
        Assert.IsTrue(hasData, "Sollte true zurückgeben wenn Daten vorhanden");
    }

    [Test]
    public void PlayerSetupManager_ClearPlayerData_RemovesAllData()
    {
        // Arrange
        setupManager.SetPlayerCount(4);
        setupManager.SetPlayerName(1, "Test");
        setupManager.SetGameDuration(8f);

        // Act
        setupManager.ClearPlayerData();

        // Assert
        Assert.IsFalse(setupManager.HasPlayerData(), "Sollte keine Daten mehr haben");
        Assert.AreEqual(0, setupManager.GetPlayerCount(), "Spieleranzahl sollte 0 sein");
    }

    [Test]
    public void PlayerSetupManager_MultiplePlayers_SavesAllCorrectly()
    {
        // Arrange
        setupManager.SetPlayerCount(6);
        setupManager.SetPlayerName(1, "Spieler 1");
        setupManager.SetPlayerName(2, "Spieler 2");
        setupManager.SetPlayerName(3, "Spieler 3");
        setupManager.SetPlayerName(4, "Spieler 4");
        setupManager.SetPlayerName(5, "Spieler 5");
        setupManager.SetPlayerName(6, "Spieler 6");

        // Act & Assert
        Assert.AreEqual(6, setupManager.GetPlayerCount());
        Assert.AreEqual("Spieler 1", setupManager.GetPlayerName(1));
        Assert.AreEqual("Spieler 2", setupManager.GetPlayerName(2));
        Assert.AreEqual("Spieler 3", setupManager.GetPlayerName(3));
        Assert.AreEqual("Spieler 4", setupManager.GetPlayerName(4));
        Assert.AreEqual("Spieler 5", setupManager.GetPlayerName(5));
        Assert.AreEqual("Spieler 6", setupManager.GetPlayerName(6));
    }
}
