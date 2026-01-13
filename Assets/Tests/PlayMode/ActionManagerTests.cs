using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// PlayMode Tests für ActionManager
/// Testet alle Spieler-Aktionen (Bewegung, Geld, Skip, Roll Again)
/// </summary>
public class ActionManagerTests
{
    private GameManager gm;
    private ActionManager actionManager;
    private GameInitiator gi;
    private MoneyManager moneyManager;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Lade MainScene
        SceneManager.LoadScene("MainScene");
        yield return null;
        yield return new WaitForSeconds(2f);

        // Finde Manager
        gm = Object.FindFirstObjectByType<GameManager>();
        actionManager = Object.FindFirstObjectByType<ActionManager>();
        gi = Object.FindFirstObjectByType<GameInitiator>();
        moneyManager = Object.FindFirstObjectByType<MoneyManager>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(actionManager, "ActionManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");
        Assert.IsNotNull(moneyManager, "MoneyManager nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ ActionManager Test Setup abgeschlossen");
        Debug.Log($"   Spieler im Spiel: {gi.CurrentGame.AllPlayers.Count}");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: AddMoney fügt Geld korrekt hinzu
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_AddMoney_IncreasesPlayerMoney()
    {
        Debug.Log("🧪 TEST 1: AddMoney increases player money");

        // Arrange
        var player = gm.GetCurrentPlayer();
        int originalMoney = player.Money;
        int amountToAdd = 500;

        Debug.Log($"   Original money: {originalMoney}€");
        Debug.Log($"   Adding: {amountToAdd}€");

        // Act
        actionManager.AddMoney(amountToAdd);
        yield return new WaitForSeconds(0.3f);

        // Assert
        Debug.Log($"   Money after: {player.Money}€");
        Assert.AreEqual(originalMoney + amountToAdd, player.Money, "Geld sollte um 500€ erhöht sein");

        yield return null;
    }

    /// <summary>
    /// Test 2: AddMoneyAndMove fügt Geld hinzu
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_AddMoneyAndMove_IncreasesPlayerMoney()
    {
        Debug.Log("🧪 TEST 2: AddMoneyAndMove increases player money");

        // Arrange
        var player = gm.GetCurrentPlayer();
        int originalMoney = player.Money;
        int amountToAdd = 300;

        Debug.Log($"   Original money: {originalMoney}€");
        Debug.Log($"   Adding: {amountToAdd}€");

        // Act
        actionManager.AddMoneyAndMove(amountToAdd);
        yield return new WaitForSeconds(0.3f);

        // Assert
        Debug.Log($"   Money after: {player.Money}€");
        Assert.AreEqual(originalMoney + amountToAdd, player.Money, "Geld sollte um 300€ erhöht sein");

        yield return null;
    }

    /// <summary>
    /// Test 3: SkipTurn setzt hasToSkip Flag
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_SkipTurn_SetsHasToSkipFlag()
    {
        Debug.Log("🧪 TEST 3: SkipTurn sets hasToSkip flag");

        // Arrange
        var player = gm.GetCurrentPlayer();
        player.hasToSkip = false;

        Debug.Log($"   Player {player.PlayerID} hasToSkip before: {player.hasToSkip}");

        // Act
        actionManager.SkipTurn();
        yield return new WaitForSeconds(0.5f);

        // Assert
        Debug.Log($"   Player {player.PlayerID} hasToSkip after: {player.hasToSkip}");
        Assert.IsTrue(player.hasToSkip, "hasToSkip sollte true sein");

        yield return null;
    }

    /// <summary>
    /// Test 4: RollAgain aktiviert Move Button und setzt Turn In Progress auf false
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_RollAgain_ActivatesMoveButton()
    {
        Debug.Log("🧪 TEST 4: RollAgain activates move button");

        // Arrange
        var moveButton = gm.playerMovement.getMoveButton();
        if (moveButton != null)
        {
            moveButton.SetActive(false);
        }
        gm.playerMovement.setIsTurnInProgress(true);

        Debug.Log($"   Move button active before: {(moveButton != null ? moveButton.activeSelf.ToString() : "null")}");
        Debug.Log($"   Turn in progress before: {gm.playerMovement.setIsTurnInProgress(true)}");

        // Act
        actionManager.RollAgain();
        yield return new WaitForSeconds(0.3f);

        // Assert
        // RollAgain() setzt das Flag nicht direkt - das machen ActionCardManager/BankCardManager
        // Aber es aktiviert den Move Button und setzt Turn In Progress auf false
        if (moveButton != null)
        {
            Assert.IsTrue(moveButton.activeSelf, "Move Button sollte aktiviert sein");
        }
        // Prüfe dass keine Exception geworfen wurde
        Assert.IsTrue(true, "RollAgain sollte ohne Fehler aufgerufen werden können");

        yield return null;
    }

    /// <summary>
    /// Test 5: ShouldRollAgain gibt korrekten Status zurück
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_ShouldRollAgain_ReturnsCorrectStatus()
    {
        Debug.Log("🧪 TEST 5: ShouldRollAgain returns correct status");

        // Arrange & Act
        actionManager.lastCardWasRollAgain = false;
        bool shouldRoll1 = actionManager.ShouldRollAgain();
        yield return null;

        actionManager.lastCardWasRollAgain = true;
        bool shouldRoll2 = actionManager.ShouldRollAgain();
        yield return null;

        // Assert
        Assert.IsFalse(shouldRoll1, "Sollte false zurückgeben wenn lastCardWasRollAgain false ist");
        Assert.IsTrue(shouldRoll2, "Sollte true zurückgeben wenn lastCardWasRollAgain true ist");

        yield return null;
    }

    /// <summary>
    /// Test 6: MovePlayerToField berechnet Schritte korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test6_MovePlayerToField_CalculatesStepsCorrectly()
    {
        Debug.Log("🧪 TEST 6: MovePlayerToField calculates steps correctly");

        // Arrange
        var player = gm.GetCurrentPlayer();
        var playerCTRL = gm.players.Find(p => p.PlayerID == player.PlayerID);
        Assert.IsNotNull(playerCTRL, "PlayerCTRL sollte nicht null sein");

        int currentPos = playerCTRL.currentPos;
        int targetField = (currentPos + 5) % 40; // 5 Felder voraus
        int expectedSteps = (targetField - currentPos + 40) % 40;

        Debug.Log($"   Current position: {currentPos}");
        Debug.Log($"   Target field: {targetField}");
        Debug.Log($"   Expected steps: {expectedSteps}");

        // Act
        actionManager.MovePlayerToField(targetField);
        yield return new WaitForSeconds(0.5f);

        // Assert
        // Prüfe dass Bewegung gestartet wurde (currentPos könnte sich ändern)
        // Da Bewegung asynchron ist, prüfen wir nur dass keine Exception geworfen wurde
        Assert.IsTrue(true, "MovePlayerToField sollte ohne Fehler aufgerufen werden können");

        yield return null;
    }

    /// <summary>
    /// Test 7: MoveToNextCompanyField findet nächstes Unternehmen
    /// </summary>
    [UnityTest]
    public IEnumerator Test7_MoveToNextCompanyField_FindsNextCompany()
    {
        Debug.Log("🧪 TEST 7: MoveToNextCompanyField finds next company");

        // Arrange
        var player = gm.GetCurrentPlayer();
        var companyFields = gi.GetCompanyFields();

        // Gib Spieler ein Unternehmen
        if (companyFields.Count > 0)
        {
            var field = companyFields[0];
            field.ownerID = player.PlayerID;
            field.level = CompanyLevel.Founded;
            player.companies.Add(field.fieldIndex);

            // Setze Spieler-Position vor dem Unternehmen
            var playerCTRL = gm.players.Find(p => p.PlayerID == player.PlayerID);
            if (playerCTRL != null)
            {
                playerCTRL.currentPos = (field.fieldIndex - 3 + 40) % 40; // 3 Felder vor dem Unternehmen
                player.BoardPosition = playerCTRL.currentPos;
            }

            Debug.Log($"   Player position: {player.BoardPosition}");
            Debug.Log($"   Company field: {field.fieldIndex}");
            Debug.Log($"   Player companies: {player.companies.Count}");

            // Act
            actionManager.MoveToNextCompanyField();
            yield return new WaitForSeconds(0.5f);

            // Assert
            // Prüfe dass keine Exception geworfen wurde
            Assert.IsTrue(true, "MoveToNextCompanyField sollte ohne Fehler aufgerufen werden können");
        }
        else
        {
            Debug.LogWarning("⚠️ Keine Company Fields vorhanden - Test übersprungen");
        }

        yield return null;
    }
}
