using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// PlayMode Tests für GameSaveManager
/// Testet das Speichern und Laden von Spielständen
/// </summary>
public class GameSaveManagerTests
{
    private GameManager gm;
    private GameInitiator gi;
    private GameSaveManager saveManager;
    private string originalSavePath;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Lade MainScene
        SceneManager.LoadScene("MainScene");
        yield return null;
        yield return new WaitForSeconds(2f);

        // Finde Manager
        gm = Object.FindFirstObjectByType<GameManager>();
        gi = Object.FindFirstObjectByType<GameInitiator>();
        saveManager = Object.FindFirstObjectByType<GameSaveManager>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");
        Assert.IsNotNull(saveManager, "GameSaveManager nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        // Speichere originalen Save-Pfad und lösche alte Save-Datei
        string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ GameSaveManager Test Setup abgeschlossen");
        Debug.Log($"   Spieler im Spiel: {gi.CurrentGame.AllPlayers.Count}");
        Debug.Log($"   Save Path: {savePath}");
        Debug.Log("══════════════════════════════════════");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Lösche Save-Datei nach Tests
        string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        yield return null;
    }

    /// <summary>
    /// Test 1: Spiel speichern funktioniert
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_SaveGame_SavesSuccessfully()
    {
        Debug.Log("🧪 TEST 1: Save game saves successfully");

        // Arrange
        int originalPlayerCount = gi.CurrentGame.AllPlayers.Count;
        int originalTurnID = gi.CurrentGame.CurrentPlayerTurnID;
        string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");

        // Stelle sicher, dass Datei nicht existiert
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        // Act
        bool saveSuccess = saveManager.SaveGame(gi);
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");
        Assert.IsTrue(File.Exists(savePath), "Save-Datei sollte existieren");
        Assert.AreEqual(originalPlayerCount, gi.CurrentGame.AllPlayers.Count, "Spieleranzahl sollte gleich bleiben");

        yield return null;
    }

    /// <summary>
    /// Test 2: Gespeichertes Spiel laden funktioniert
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_LoadGame_LoadsSuccessfully()
    {
        Debug.Log("🧪 TEST 2: Load game loads successfully");

        // Arrange: Speichere zuerst ein Spiel
        var player = gi.CurrentGame.AllPlayers[0];
        var playerCTRL = gm.players?.Find(p => p.PlayerID == player.PlayerID);
        
        int originalMoney = player.Money;
        int originalPosition = player.BoardPosition;
        string originalName = player.PlayerName;

        // Ändere Spielerdaten
        player.Money = 5000;
        player.PlayerName = "Test Spieler";
        
        // WICHTIG: GameSaveManager nimmt Position von PlayerCTRL.currentPos, nicht von PlayerData.BoardPosition
        if (playerCTRL != null)
        {
            playerCTRL.currentPos = 15;
            player.BoardPosition = 15; // Synchronisiere PlayerData
            Debug.Log($"   Set playerCTRL.currentPos to: {playerCTRL.currentPos}");
            Debug.Log($"   Set player.BoardPosition to: {player.BoardPosition}");
        }
        else
        {
            // Fallback: Wenn kein PlayerCTRL, verwende BoardPosition
            player.BoardPosition = 15;
            Debug.Log($"   No PlayerCTRL found, using BoardPosition: {player.BoardPosition}");
        }

        // Speichere
        bool saveSuccess = saveManager.SaveGame(gi);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");
        
        // Prüfe dass Position in Save-Datei gespeichert wurde
        string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            GameSaveData savedData = JsonUtility.FromJson<GameSaveData>(json);
            var savedPlayer = savedData.players.Find(p => p.PlayerID == player.PlayerID);
            if (savedPlayer != null)
            {
                Debug.Log($"   Position in save file: {savedPlayer.BoardPosition}");
                Assert.AreEqual(15, savedPlayer.BoardPosition, "Position sollte 15 in save file sein");
            }
        }
        
        Debug.Log($"   After save - playerCTRL.currentPos: {(playerCTRL != null ? playerCTRL.currentPos.ToString() : "null")}");
        Debug.Log($"   After save - player.BoardPosition: {player.BoardPosition}");

        // Ändere Daten wieder (um zu testen dass Laden funktioniert)
        player.Money = 1000;
        if (playerCTRL != null)
        {
            playerCTRL.currentPos = 0;
        }
        player.BoardPosition = 0;

        // Act: Lade Spiel
        GameSaveData loadedData = saveManager.LoadGame();
        yield return new WaitForSeconds(1f);

        // Assert
        Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
        Assert.Greater(loadedData.players.Count, 0, "Sollte mindestens einen Spieler haben");

        // Prüfe ob Daten korrekt geladen wurden
        var loadedPlayer = loadedData.players.Find(p => p.PlayerID == player.PlayerID);
        Assert.IsNotNull(loadedPlayer, $"Spieler {player.PlayerID} sollte in geladenen Daten vorhanden sein");
        
        Debug.Log($"   Loaded player money: {loadedPlayer.Money}€");
        Debug.Log($"   Loaded player position: {loadedPlayer.BoardPosition}");
        Debug.Log($"   Loaded player name: {loadedPlayer.PlayerName}");
        
        Assert.AreEqual(5000, loadedPlayer.Money, "Geld sollte korrekt geladen werden");
        Assert.AreEqual("Test Spieler", loadedPlayer.PlayerName, "Name sollte korrekt geladen werden");
        
        // Position: Prüfe ob sie gespeichert wurde (kann 0 sein wenn playerCTRL null war beim Speichern)
        // Wenn playerCTRL vorhanden war, sollte Position 15 sein
        if (playerCTRL != null)
        {
            // Position sollte von playerCTRL.currentPos kommen
            Assert.AreEqual(15, loadedPlayer.BoardPosition, $"Position sollte 15 sein (von playerCTRL.currentPos), aber ist {loadedPlayer.BoardPosition}");
        }
        else
        {
            // Wenn kein playerCTRL, sollte BoardPosition verwendet werden
            // Aber da wir es auf 15 gesetzt haben, sollte es auch 15 sein
            Assert.AreEqual(15, loadedPlayer.BoardPosition, $"Position sollte 15 sein (von BoardPosition), aber ist {loadedPlayer.BoardPosition}");
        }

        yield return null;
    }

    /// <summary>
    /// Test 3: Unternehmen werden korrekt gespeichert und geladen
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_SaveLoad_CompaniesArePreserved()
    {
        Debug.Log("🧪 TEST 3: Companies are preserved in save/load");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        var companyFields = gi.GetCompanyFields();
        
        if (companyFields.Count > 0)
        {
            // Gib Spieler ein Unternehmen
            var field = companyFields[0];
            field.ownerID = player.PlayerID;
            field.level = CompanyLevel.Founded;
            player.companies.Add(field.fieldIndex);

            // Speichere
            bool saveSuccess = saveManager.SaveGame(gi);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");

            // Entferne Unternehmen (um zu testen dass Laden funktioniert)
            player.companies.Clear();
            field.ownerID = -1;

            // Act: Lade Spiel
            GameSaveData loadedData = saveManager.LoadGame();
            yield return new WaitForSeconds(1f);

            // Assert
            Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
            var loadedPlayer = loadedData.players.Find(p => p.PlayerID == player.PlayerID);
            if (loadedPlayer != null)
            {
                Assert.Greater(loadedPlayer.companies.Count, 0, "Spieler sollte Unternehmen haben");
                Assert.Contains(field.fieldIndex, loadedPlayer.companies, "Unternehmen sollte in Liste sein");
            }
        }

        yield return null;
    }

    /// <summary>
    /// Test 4: Aktueller Spieler-Zug wird gespeichert
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_SaveLoad_CurrentTurnIsPreserved()
    {
        Debug.Log("🧪 TEST 4: Current turn is preserved in save/load");

        // Arrange
        int originalTurnID = gi.CurrentGame.CurrentPlayerTurnID;
        gi.CurrentGame.CurrentPlayerTurnID = 2; // Ändere Zug

        // Speichere
        bool saveSuccess = saveManager.SaveGame(gi);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");

        // Ändere zurück
        gi.CurrentGame.CurrentPlayerTurnID = 0;

        // Act: Lade Spiel
        GameSaveData loadedData = saveManager.LoadGame();
        yield return new WaitForSeconds(1f);

        // Assert
        Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
        Assert.AreEqual(2, loadedData.currentPlayerTurnID, "Aktueller Zug sollte korrekt geladen werden");

        yield return null;
    }

    /// <summary>
    /// Test 5: Mehrere Spieler werden korrekt gespeichert
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_SaveLoad_MultiplePlayersArePreserved()
    {
        Debug.Log("🧪 TEST 5: Multiple players are preserved in save/load");

        // Arrange
        int originalPlayerCount = gi.CurrentGame.AllPlayers.Count;
        Assert.Greater(originalPlayerCount, 0, "Sollte mindestens einen Spieler haben");

        // Ändere Geld aller Spieler
        foreach (var player in gi.CurrentGame.AllPlayers)
        {
            player.Money = player.PlayerID * 1000; // Unterschiedliche Beträge
        }

        // Speichere
        bool saveSuccess = saveManager.SaveGame(gi);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");

        // Act: Lade Spiel
        GameSaveData loadedData = saveManager.LoadGame();
        yield return new WaitForSeconds(1f);

        // Assert
        Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
        Assert.AreEqual(originalPlayerCount, loadedData.players.Count, "Sollte alle Spieler haben");

        // Prüfe dass alle Spieler korrekt geladen wurden
        foreach (var originalPlayer in gi.CurrentGame.AllPlayers)
        {
            var loadedPlayer = loadedData.players.Find(p => p.PlayerID == originalPlayer.PlayerID);
            Assert.IsNotNull(loadedPlayer, $"Spieler {originalPlayer.PlayerID} sollte geladen werden");
            Assert.AreEqual(originalPlayer.PlayerID * 1000, loadedPlayer.Money, $"Geld von Spieler {originalPlayer.PlayerID} sollte korrekt sein");
        }

        yield return null;
    }

    /// <summary>
    /// Test 6: Save-Timestamp wird gesetzt
    /// </summary>
    [UnityTest]
    public IEnumerator Test6_SaveGame_SetsTimestamp()
    {
        Debug.Log("🧪 TEST 6: Save game sets timestamp");

        // Arrange
        string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        // Act
        bool saveSuccess = saveManager.SaveGame(gi);
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(saveSuccess, "Speichern sollte erfolgreich sein");
        
        // Lade und prüfe Timestamp
        GameSaveData loadedData = saveManager.LoadGame();
        yield return new WaitForSeconds(0.5f);
        
        Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
        Assert.IsNotNull(loadedData.saveTimestamp, "Timestamp sollte gesetzt sein");
        Assert.IsNotEmpty(loadedData.saveTimestamp, "Timestamp sollte nicht leer sein");

        yield return null;
    }
}
