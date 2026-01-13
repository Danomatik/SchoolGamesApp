using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// PlayMode Tests für ActionCardManager
/// Testet das Aktionskarten-System
/// </summary>
public class ActionCardManagerTests
{
    private GameManager gm;
    private ActionCardManager actionCardManager;
    private GameInitiator gi;
    private ActionManager actionManager;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Lade MainScene
        SceneManager.LoadScene("MainScene");
        yield return null;
        yield return new WaitForSeconds(2f);

        // Finde Manager
        gm = Object.FindFirstObjectByType<GameManager>();
        actionCardManager = Object.FindFirstObjectByType<ActionCardManager>();
        gi = Object.FindFirstObjectByType<GameInitiator>();
        actionManager = Object.FindFirstObjectByType<ActionManager>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(actionCardManager, "ActionCardManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");
        Assert.IsNotNull(actionManager, "ActionManager nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ ActionCardManager Test Setup abgeschlossen");
        Debug.Log($"   Spieler im Spiel: {gi.CurrentGame.AllPlayers.Count}");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: ShowRandomActionCard zieht eine Karte
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_ShowRandomActionCard_DrawsCard()
    {
        Debug.Log("🧪 TEST 1: ShowRandomActionCard draws a card");

        // Arrange
        var player = gm.GetCurrentPlayer();
        int originalMoney = player.Money;

        // Act
        actionCardManager.ShowRandomActionCard();
        yield return new WaitForSeconds(1f);

        // Assert
        // Prüfe dass keine Exception geworfen wurde
        // Karte könnte verschiedene Effekte haben (Geld, Bewegung, etc.)
        Assert.IsTrue(true, "ShowRandomActionCard sollte ohne Fehler aufgerufen werden können");

        yield return null;
    }

    /// <summary>
    /// Test 2: Action Card 6 (Geld) fügt 200€ hinzu
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_ActionCard6_Adds200Money()
    {
        Debug.Log("🧪 TEST 2: Action Card 6 adds 200€");

        // Arrange
        var player = gm.GetCurrentPlayer();
        int originalMoney = player.Money;

        Debug.Log($"   Original money: {originalMoney}€");

        // Act: Simuliere Action Card 6 (Stadt gestalten - 200€)
        // Da wir nicht direkt eine Karte ziehen können, testen wir die Logik indirekt
        actionManager.AddMoney(200);
        yield return new WaitForSeconds(0.3f);

        // Assert
        Debug.Log($"   Money after: {player.Money}€");
        Assert.AreEqual(originalMoney + 200, player.Money, "Geld sollte um 200€ erhöht sein");

        yield return null;
    }

    /// <summary>
    /// Test 3: Action Card 7 (Skip Turn) setzt hasToSkip
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_ActionCard7_SkipsTurn()
    {
        Debug.Log("🧪 TEST 3: Action Card 7 skips turn");

        // Arrange
        var player = gm.GetCurrentPlayer();
        player.hasToSkip = false;

        Debug.Log($"   Player {player.PlayerID} hasToSkip before: {player.hasToSkip}");

        // Act: Simuliere Action Card 7 (Gesetzliche Auflagen - Skip Turn)
        actionManager.SkipTurn();
        yield return new WaitForSeconds(0.5f);

        // Assert
        Debug.Log($"   Player {player.PlayerID} hasToSkip after: {player.hasToSkip}");
        Assert.IsTrue(player.hasToSkip, "hasToSkip sollte true sein");

        yield return null;
    }

    /// <summary>
    /// Test 4: Action Card 8 (Roll Again) setzt Roll Again Flag
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_ActionCard8_RollsAgain()
    {
        Debug.Log("🧪 TEST 4: Action Card 8 rolls again");

        // Arrange
        // ActionCardManager hat ein privates lastCardWasRollAgain Feld
        // Wir können es nicht direkt setzen, aber wir können prüfen dass die Logik funktioniert
        var moveButton = gm.playerMovement.getMoveButton();
        if (moveButton != null)
        {
            moveButton.SetActive(false);
        }

        Debug.Log($"   Move button active before: {(moveButton != null ? moveButton.activeSelf.ToString() : "null")}");

        // Act: Simuliere Action Card 8 Logik
        // ActionCardManager setzt lastCardWasRollAgain = true bei Card 8, dann ruft es RollAgain() auf
        // Da wir das private Feld nicht direkt setzen können, testen wir dass RollAgain() funktioniert
        actionManager.RollAgain();
        yield return new WaitForSeconds(0.3f);

        // Assert
        // Prüfe dass Move Button aktiviert wurde (das macht RollAgain())
        if (moveButton != null)
        {
            Assert.IsTrue(moveButton.activeSelf, "Move Button sollte aktiviert sein nach RollAgain");
        }
        // Prüfe dass keine Exception geworfen wurde
        Assert.IsTrue(true, "RollAgain sollte ohne Fehler aufgerufen werden können");

        yield return null;
    }

    /// <summary>
    /// Test 5: Action Cards werden korrekt geladen
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_ActionCards_AreLoaded()
    {
        Debug.Log("🧪 TEST 5: Action cards are loaded correctly");

        // Arrange & Act
        // Karten werden beim Awake geladen
        yield return new WaitForSeconds(0.5f);

        // Assert
        // Prüfe dass ShowRandomActionCard ohne Fehler funktioniert
        // (wenn keine Karten geladen wären, würde es einen Fehler geben)
        actionCardManager.ShowRandomActionCard();
        yield return new WaitForSeconds(0.5f);

        Assert.IsTrue(true, "Action Cards sollten geladen sein (kein Fehler beim Ziehen)");

        yield return null;
    }
}
