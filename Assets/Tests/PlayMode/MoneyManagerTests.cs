using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// PlayMode Tests für MoneyManager und Spieler-Eliminierung
/// </summary>
public class MoneyManagerTests
{
    private GameManager gm;
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
        gi = Object.FindFirstObjectByType<GameInitiator>();
        moneyManager = Object.FindFirstObjectByType<MoneyManager>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");
        Assert.IsNotNull(moneyManager, "MoneyManager nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ MoneyManager Test Setup abgeschlossen");
        Debug.Log($"   Spieler im Spiel: {gi.CurrentGame.AllPlayers.Count}");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: Spieler ohne Geld und ohne Unternehmen wird eliminiert
    /// </summary>
[UnityTest]
public IEnumerator Test1_PlayerWithNoAssets_GetsEliminated()
{
    Debug.Log("🧪 TEST 1: Player with no assets gets eliminated");

    // ✅ Reset current player - CRITICAL
    gi.CurrentGame.CurrentPlayerTurnID = 0;

    // Arrange
    var player = gi.CurrentGame.AllPlayers[0];
    int originalPlayerCount = gi.CurrentGame.AllPlayers.Count;
    
    player.Money = 0;
    player.companies.Clear();
    player.isEliminated = false;

    Debug.Log($"   Player: {player.PlayerName}");
    Debug.Log($"   Money: {player.Money}€");
    Debug.Log($"   Companies: {player.companies.Count}");
    Debug.Log($"   Total Assets: {moneyManager.CalculateTotalAssets(player)}€");
    Debug.Log($"   Players in game: {originalPlayerCount}");

    // Erwarte Error-Logs für Eliminierung (Reihenfolge ist wichtig!)
    // 1. "ist zahlungsunfähig"
    // 2. "[DEBUG] About to call EliminatePlayer"
    // 3. "[DEBUG] After calling EliminatePlayer"
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*ist zahlungsunfähig.*"));
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*\\[DEBUG\\].*About to call EliminatePlayer.*"));
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*\\[DEBUG\\].*After calling EliminatePlayer.*"));

    // Act
    bool canPay = moneyManager.TryPayAmount(player, 500, "Test Elimination");
    yield return new WaitForSeconds(1.5f);

    // Assert
    int newPlayerCount = gi.CurrentGame.AllPlayers.Count;
    Debug.Log($"   Can pay: {canPay}");
    Debug.Log($"   Players after: {newPlayerCount}");
    Debug.Log($"   Is eliminated: {player.isEliminated}");
    
    Assert.IsFalse(canPay, "Payment should fail");
    Assert.IsTrue(player.isEliminated, "Player should be marked as eliminated");
    Assert.AreEqual(originalPlayerCount - 1, newPlayerCount, "Player count should decrease by 1");
    Assert.IsFalse(gi.CurrentGame.AllPlayers.Contains(player), "Player should not be in active players list");

    yield return null;
}

    /// <summary>
    /// Test 2: Spieler mit genug Geld kann zahlen und wird NICHT eliminiert
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_PlayerWithEnoughMoney_CanPay()
    {
        Debug.Log("🧪 TEST 2: Player with enough money can pay");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        int originalPlayerCount = gi.CurrentGame.AllPlayers.Count;
        int originalMoney = 1000;
        
        player.Money = originalMoney;

        Debug.Log($"   Player: {player.PlayerName}");
        Debug.Log($"   Money: {player.Money}€");
        Debug.Log($"   Payment amount: 300€");

        // Act
        bool canPay = moneyManager.TryPayAmount(player, 300, "Test Payment");
        yield return new WaitForSeconds(0.3f);

        // Assert
        Debug.Log($"   Can pay: {canPay}");
        Debug.Log($"   Money after: {player.Money}€");
        Debug.Log($"   Is eliminated: {player.isEliminated}");
        
        Assert.IsTrue(canPay, "Payment should succeed");
        Assert.AreEqual(700, player.Money, "Money should be 700€ (1000 - 300)");
        Assert.IsFalse(player.isEliminated, "Player should NOT be eliminated");
        Assert.AreEqual(originalPlayerCount, gi.CurrentGame.AllPlayers.Count, "Player count should stay the same");

        yield return null;
    }

    /// <summary>
    /// Test 3: Berechnung des Gesamtwerts (Bargeld + Unternehmen)
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_CalculateTotalAssets_IncludesCompanies()
    {
        Debug.Log("🧪 TEST 3: CalculateTotalAssets includes companies");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 100;
        player.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        var field1 = companyFields[0];
        var field2 = companyFields[1];

        // Gib Spieler 2 Unternehmen
        field1.ownerID = player.PlayerID;
        field1.level = CompanyLevel.Founded;
        player.companies.Add(field1.fieldIndex);

        field2.ownerID = player.PlayerID;
        field2.level = CompanyLevel.Founded;
        player.companies.Add(field2.fieldIndex);

        var company1 = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field1.companyID);
        var company2 = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field2.companyID);

        // WICHTIG: CalculateTotalAssets verwendet VOLLE Kosten (nicht 50% wie bei Versteigerung)
        // Die 50% Regel gilt nur für Versteigerungen, nicht für Vermögensberechnung
        int expectedAssets = player.Money + company1.costFound + company2.costFound;

        Debug.Log($"   Player money: {player.Money}€");
        Debug.Log($"   Company 1: {company1.companyName} → {company1.costFound}€ (volle Kosten)");
        Debug.Log($"   Company 2: {company2.companyName} → {company2.costFound}€ (volle Kosten)");
        Debug.Log($"   Expected total assets: {expectedAssets}€");

        // Act
        int totalAssets = moneyManager.CalculateTotalAssets(player);
        yield return null;

        // Assert
        Debug.Log($"   Calculated total assets: {totalAssets}€");
        Assert.AreEqual(expectedAssets, totalAssets, "Total assets should include money + company values");

        yield return null;
    }

    /// <summary>
    /// Test 4: Spieler mit Unternehmen aber wenig Geld löst Insolvenz aus (keine Eliminierung)
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_PlayerWithCompanies_TriggersInsolvency_NotEliminated()
    {
        Debug.Log("🧪 TEST 4: Player with companies triggers insolvency but is NOT eliminated");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        int originalPlayerCount = gi.CurrentGame.AllPlayers.Count;
        
        player.Money = 50;
        player.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        var field = companyFields[0];
        field.ownerID = player.PlayerID;
        field.level = CompanyLevel.Founded;
        player.companies.Add(field.fieldIndex);

        var company = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
        int totalAssets = moneyManager.CalculateTotalAssets(player);

        Debug.Log($"   Player: {player.PlayerName}");
        Debug.Log($"   Money: {player.Money}€");
        Debug.Log($"   Companies: {player.companies.Count}");
        Debug.Log($"   Total assets: {totalAssets}€");
        Debug.Log($"   Required payment: 300€");

        // Act: Versuche Zahlung die Insolvenz auslöst
        bool canPay = moneyManager.TryPayAmount(player, 300, "Test Insolvency");
        yield return new WaitForSeconds(1f);

        // Assert
        Debug.Log($"   Can pay immediately: {canPay}");
        Debug.Log($"   Is eliminated: {player.isEliminated}");
        Debug.Log($"   Players in game: {gi.CurrentGame.AllPlayers.Count}");
        
        Assert.IsFalse(canPay, "Payment should fail (triggers insolvency)");
        Assert.IsFalse(player.isEliminated, "Player should NOT be eliminated (has companies to auction)");
        Assert.AreEqual(originalPlayerCount, gi.CurrentGame.AllPlayers.Count, "Player count should stay the same");

        yield return null;
    }

    /// <summary>
    /// Test 5: CanAffordPayment prüft korrekt ob Zahlung möglich ist
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_CanAffordPayment_ChecksCorrectly()
    {
        Debug.Log("🧪 TEST 5: CanAffordPayment checks correctly");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 200;
        player.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        var field = companyFields[0];
        field.ownerID = player.PlayerID;
        field.level = CompanyLevel.Founded;
        player.companies.Add(field.fieldIndex);

        var company = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
        
        // WICHTIG: CanAffordPayment verwendet CalculateTotalAssets, welches VOLLE Kosten verwendet
        // (nicht 50% wie bei Versteigerung)
        int companyValue = company.costFound; // Volle Kosten, nicht 50%
        int totalAssets = player.Money + companyValue;

        Debug.Log($"   Money: {player.Money}€");
        Debug.Log($"   Company value: {companyValue}€ (volle Kosten)");
        Debug.Log($"   Total assets: {totalAssets}€");

        // Act & Assert
        bool canAfford100 = moneyManager.CanAffordPayment(player, 100);
        bool canAfford200 = moneyManager.CanAffordPayment(player, 200);
        bool canAffordTotal = moneyManager.CanAffordPayment(player, totalAssets);
        bool canAffordMore = moneyManager.CanAffordPayment(player, totalAssets + 100);

        yield return null;

        Debug.Log($"   Can afford 100€: {canAfford100}");
        Debug.Log($"   Can afford 200€: {canAfford200}");
        Debug.Log($"   Can afford {totalAssets}€: {canAffordTotal}");
        Debug.Log($"   Can afford {totalAssets + 100}€: {canAffordMore}");

        Assert.IsTrue(canAfford100, "Should afford 100€");
        Assert.IsTrue(canAfford200, "Should afford 200€ (has exact cash)");
        Assert.IsTrue(canAffordTotal, "Should afford total assets (cash + companies)");
        Assert.IsFalse(canAffordMore, "Should NOT afford more than total assets");

        yield return null;
    }

    /// <summary>
    /// Test 6: AddMoney funktioniert korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test6_AddMoney_IncreasesPlayerMoney()
    {
        Debug.Log("🧪 TEST 6: AddMoney increases player money");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        int originalMoney = player.Money;

        Debug.Log($"   Original money: {originalMoney}€");

        // Act
        moneyManager.AddMoney(500);
        yield return new WaitForSeconds(0.3f);

        // Assert
        Debug.Log($"   Money after adding 500€: {player.Money}€");
        Assert.AreEqual(originalMoney + 500, player.Money, "Money should increase by 500€");

        yield return null;
    }

    /// <summary>
    /// Test 7: RemoveMoney funktioniert nur wenn genug Geld vorhanden
    /// </summary>
    [UnityTest]
    public IEnumerator Test7_RemoveMoney_OnlyWorksWithSufficientFunds()
    {
        Debug.Log("🧪 TEST 7: RemoveMoney only works with sufficient funds");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 300;

        Debug.Log($"   Starting money: {player.Money}€");

        // Act & Assert: Successful removal
        bool success1 = moneyManager.RemoveMoney(200);
        yield return new WaitForSeconds(0.3f);

        Debug.Log($"   Remove 200€: {success1}, Money: {player.Money}€");
        Assert.IsTrue(success1, "Should successfully remove 200€");
        Assert.AreEqual(100, player.Money, "Money should be 100€");

        // Act & Assert: Failed removal
        bool success2 = moneyManager.RemoveMoney(200);
        yield return new WaitForSeconds(0.3f);

        Debug.Log($"   Remove 200€ again: {success2}, Money: {player.Money}€");
        Assert.IsFalse(success2, "Should fail to remove 200€ (insufficient funds)");
        Assert.AreEqual(100, player.Money, "Money should still be 100€");

        yield return null;
    }

    /// <summary>
    /// Test 8: Eliminierte Spieler haben ihre Unternehmen freigegeben
    /// </summary>
[UnityTest]
public IEnumerator Test8_EliminatedPlayer_ReleasesCompanies()
{
    Debug.Log("🧪 TEST 8: Eliminated player releases all companies");

    // ✅ Reset current player - CRITICAL
    gi.CurrentGame.CurrentPlayerTurnID = 0;

    // Arrange
    var player = gi.CurrentGame.AllPlayers[0];
    player.Money = 0;
    player.companies.Clear();
    player.isEliminated = false;

    var companyFields = gi.GetCompanyFields();
    
    var field1 = companyFields[0];
    var field2 = companyFields[1];
    var field3 = companyFields[2];

    field1.ownerID = player.PlayerID;
    field1.level = CompanyLevel.Founded;
    player.companies.Add(field1.fieldIndex);

    field2.ownerID = player.PlayerID;
    field2.level = CompanyLevel.Founded; // ✅ Changed to Founded for consistency
    player.companies.Add(field2.fieldIndex);

    field3.ownerID = player.PlayerID;
    field3.level = CompanyLevel.Founded; // ✅ Changed to Founded for consistency
    player.companies.Add(field3.fieldIndex);

    // ✅ Calculate total assets dynamically
    int totalAssets = moneyManager.CalculateTotalAssets(player);
    int paymentAmount = totalAssets + 100; // More than player can afford

    Debug.Log($"   Player companies: {player.companies.Count}");
    Debug.Log($"   Total assets: {totalAssets}€");
    Debug.Log($"   Payment required: {paymentAmount}€ (exceeds assets)");

    // Erwarte Error-Logs für Eliminierung (Reihenfolge ist wichtig!)
    // 1. "ist zahlungsunfähig"
    // 2. "[DEBUG] About to call EliminatePlayer"
    // 3. "[DEBUG] After calling EliminatePlayer"
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*ist zahlungsunfähig.*"));
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*\\[DEBUG\\].*About to call EliminatePlayer.*"));
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*\\[DEBUG\\].*After calling EliminatePlayer.*"));

    // Act: Eliminiere Spieler
    moneyManager.TryPayAmount(player, paymentAmount, "Test Company Release");
    yield return new WaitForSeconds(1.5f);

    // Assert
    Debug.Log($"   Player is eliminated: {player.isEliminated}");
    Debug.Log($"   Player companies remaining: {player.companies.Count}");
    
    Assert.IsTrue(player.isEliminated, "Player should be eliminated");
    Assert.AreEqual(0, player.companies.Count, "Player should have 0 companies");
    Assert.AreEqual(-1, field1.ownerID, "Company 1 should have no owner");
    Assert.AreEqual(-1, field2.ownerID, "Company 2 should have no owner");
    Assert.AreEqual(-1, field3.ownerID, "Company 3 should have no owner");
    Assert.AreEqual(CompanyLevel.None, field1.level, "Company 1 level should be None");
    Assert.AreEqual(CompanyLevel.None, field2.level, "Company 2 level should be None");
    Assert.AreEqual(CompanyLevel.None, field3.level, "Company 3 level should be None");

    yield return null;
}

    /// <summary>
    /// Test 9: PayRent mit Empfänger funktioniert korrekt
    /// </summary>
    [UnityTest]
    public IEnumerator Test9_TryPayAmount_WithRecipient_TransfersMoney()
    {
        Debug.Log("🧪 TEST 9: TryPayAmount with recipient transfers money correctly");

        // Arrange
        var payer = gi.CurrentGame.AllPlayers[0];
        var recipient = gi.CurrentGame.AllPlayers[1];

        payer.Money = 500;
        recipient.Money = 200;

        Debug.Log($"   Payer: {payer.PlayerName}, Money: {payer.Money}€");
        Debug.Log($"   Recipient: {recipient.PlayerName}, Money: {recipient.Money}€");
        Debug.Log($"   Payment amount: 300€");

        // Act
        bool success = moneyManager.TryPayAmount(payer, 300, recipient, "Test Rent Payment");
        yield return new WaitForSeconds(0.5f);

        // Assert
        Debug.Log($"   Payment successful: {success}");
        Debug.Log($"   Payer money after: {payer.Money}€");
        Debug.Log($"   Recipient money after: {recipient.Money}€");

        Assert.IsTrue(success, "Payment should succeed");
        Assert.AreEqual(200, payer.Money, "Payer should have 200€ (500 - 300)");
        Assert.AreEqual(500, recipient.Money, "Recipient should have 500€ (200 + 300)");

        yield return null;
    }
}