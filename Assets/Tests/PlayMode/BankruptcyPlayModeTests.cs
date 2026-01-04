using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// PlayMode Tests für Insolvenz-Mechanik
/// </summary>
public class BankruptcyTests
{
    private GameManager gm;
    private GameInitiator gi;
    private UIManager ui;

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
        ui = Object.FindFirstObjectByType<UIManager>();

        Assert.IsNotNull(gm, "GameManager nicht gefunden!");
        Assert.IsNotNull(gi, "GameInitiator nicht gefunden!");
        Assert.IsNotNull(ui, "UIManager nicht gefunden!");

        // Warte bis Initiative abgeschlossen ist
        yield return new WaitUntil(() => !gm.InitiativeInProgress);
        yield return new WaitForSeconds(1f);

        Debug.Log("══════════════════════════════════════");
        Debug.Log("✅ Test Setup abgeschlossen");
        Debug.Log($"   Spieler im Spiel: {gi.CurrentGame.AllPlayers.Count}");
        Debug.Log("══════════════════════════════════════");
    }

    /// <summary>
    /// Test 1: Spieler mit Geld und Unternehmen löst Insolvenz durch Versteigerung
    /// </summary>
    [UnityTest]
    public IEnumerator Test1_Bankruptcy_WithCompanies_CanAuction()
    {
        Debug.Log("🧪 TEST 1: Bankruptcy with companies - Can auction to resolve");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 50;
        player.companies.Clear();
        
        var companyFields = gi.GetCompanyFields();
        Assert.Greater(companyFields.Count, 0, "Keine Company Fields!");
        
        // Gib Spieler ein teures Unternehmen (mit hohen Gründungskosten)
        companyFields[0].ownerID = player.PlayerID;
        companyFields[0].level = CompanyLevel.Founded;
        player.companies.Add(companyFields[0].fieldIndex);

        var company = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == companyFields[0].companyID);
        int expectedAuctionPrice = company.costFound / 2;

        Debug.Log($"   Player: {player.PlayerName}, Money: {player.Money}€, Companies: {player.companies.Count}");
        Debug.Log($"   Company: {company.companyName}, Auction Price: {expectedAuctionPrice}€");

        // Act: Bankruptcy auslösen
        int requiredPayment = 300;
        gm.HandleBankruptcy(player, requiredPayment, "Test Payment");
        yield return new WaitForSeconds(0.3f);

        // Versteigere Unternehmen
        int moneyBefore = player.Money;
        gm.StartAuctionForCompany(companyFields[0]);
        yield return new WaitForSeconds(0.5f);

        // Assert
        int moneyGained = (moneyBefore + expectedAuctionPrice) - requiredPayment; // Was übrig bleibt
        Debug.Log($"   Money before auction: {moneyBefore}€");
        Debug.Log($"   Expected after payment: {moneyGained}€");
        Debug.Log($"   Actual money: {player.Money}€");
        
        Assert.AreEqual(moneyGained, player.Money, "Spieler sollte korrekten Betrag nach Versteigerung haben");
        Assert.AreEqual(0, player.companies.Count, "Spieler sollte keine Unternehmen mehr haben");

        yield return null;
    }

    /// <summary>
    /// Test 2: Spieler ohne Unternehmen kann nicht zahlen, Zug wird beendet
    /// </summary>
    [UnityTest]
    public IEnumerator Test2_Bankruptcy_NoCompanies_EndsTurn()
    {
        Debug.Log("🧪 TEST 2: Bankruptcy without companies - Turn ends");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        int originalTurnID = gi.CurrentGame.CurrentPlayerTurnID;
        
        player.Money = 0;
        player.companies.Clear();

        Debug.Log($"   Player: {player.PlayerName}, Money: {player.Money}€, Companies: {player.companies.Count}");
        Debug.Log($"   Original Turn ID: {originalTurnID}");

        // Act
        gm.HandleBankruptcy(player, 500, "Test Payment (No Companies)");
        yield return new WaitForSeconds(1f);

        // Assert
        int newTurnID = gi.CurrentGame.CurrentPlayerTurnID;
        Debug.Log($"   New Turn ID: {newTurnID}");
        
        Assert.AreNotEqual(originalTurnID, newTurnID, "Turn sollte gewechselt sein");
        Assert.AreEqual(0, player.Money, "Geld sollte 0€ sein");

        yield return null;
    }

    /// <summary>
    /// Test 3: Versteigerung gibt exakt 50% der Gründungskosten
    /// </summary>
    [UnityTest]
    public IEnumerator Test3_Auction_Gives50Percent()
    {
        Debug.Log("🧪 TEST 3: Auction gives exactly 50% of foundation cost");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 0;
        player.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        var field = companyFields[0];
        field.ownerID = player.PlayerID;
        field.level = CompanyLevel.Founded;
        player.companies.Add(field.fieldIndex);

        var company = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
        Assert.IsNotNull(company, "Company Config nicht gefunden!");

        int expectedAuctionPrice = company.costFound / 2;

        Debug.Log($"   Company: {company.companyName}");
        Debug.Log($"   Found Cost: {company.costFound}€");
        Debug.Log($"   Expected Auction: {expectedAuctionPrice}€");

        // Act
        gm.HandleBankruptcy(player, 1000, "Test");
        yield return new WaitForSeconds(0.3f);

        int moneyBefore = player.Money;
        gm.StartAuctionForCompany(field);
        yield return new WaitForSeconds(0.5f);

        // Assert
        int moneyGained = player.Money - moneyBefore;
        Debug.Log($"   Money gained: {moneyGained}€");
        
        Assert.AreEqual(expectedAuctionPrice, moneyGained, "Versteigerung sollte exakt 50% geben");
        Assert.AreEqual(-1, field.ownerID, "Feld sollte keinen Owner haben");
        Assert.AreEqual(CompanyLevel.None, field.level, "Level sollte None sein");

        yield return null;
    }

    /// <summary>
    /// Test 4: Mehrere Versteigerungen lösen Insolvenz auf
    /// </summary>
    [UnityTest]
    public IEnumerator Test4_MultipleAuctions_ResolvesBankruptcy()
    {
        Debug.Log("🧪 TEST 4: Multiple auctions resolve bankruptcy completely");

        // Arrange
        var player = gi.CurrentGame.AllPlayers[0];
        player.Money = 0;
        player.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        
        // Erstes Unternehmen
        companyFields[0].ownerID = player.PlayerID;
        companyFields[0].level = CompanyLevel.Founded;
        player.companies.Add(companyFields[0].fieldIndex);

        // Zweites Unternehmen
        companyFields[1].ownerID = player.PlayerID;
        companyFields[1].level = CompanyLevel.Founded;
        player.companies.Add(companyFields[1].fieldIndex);

        var company1 = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == companyFields[0].companyID);
        var company2 = gi.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == companyFields[1].companyID);
        
        int auction1 = company1.costFound / 2;
        int auction2 = company2.costFound / 2;
        int totalFromAuctions = auction1 + auction2;

        Debug.Log($"   Company 1: {company1.companyName} → {auction1}€");
        Debug.Log($"   Company 2: {company2.companyName} → {auction2}€");
        Debug.Log($"   Total from auctions: {totalFromAuctions}€");

        // Act
        int requiredPayment = 500;
        Debug.Log($"   Required payment: {requiredPayment}€");
        
        gm.HandleBankruptcy(player, requiredPayment, "Test Multiple Auctions");
        yield return new WaitForSeconds(0.3f);

        gm.StartAuctionForCompany(companyFields[0]);
        yield return new WaitForSeconds(0.3f);
        Debug.Log($"   After 1st auction: {player.Money}€");

        gm.StartAuctionForCompany(companyFields[1]);
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"   After 2nd auction: {player.Money}€");

        // Assert
        int expectedRemaining = totalFromAuctions - requiredPayment;
        Debug.Log($"   Expected remaining: {expectedRemaining}€");
        
        Assert.AreEqual(expectedRemaining, player.Money, $"Spieler sollte {expectedRemaining}€ übrig haben");
        Assert.AreEqual(0, player.companies.Count, "Alle Unternehmen sollten versteigert sein");

        yield return null;
    }

    /// <summary>
    /// Test 5: GetAuctionableCompanies gibt nur eigene Unternehmen zurück
    /// </summary>
    [UnityTest]
    public IEnumerator Test5_GetAuctionableCompanies_OnlyOwned()
    {
        Debug.Log("🧪 TEST 5: GetAuctionableCompanies returns only player's companies");

        // Arrange
        var player1 = gi.CurrentGame.AllPlayers[0];
        player1.companies.Clear();

        var companyFields = gi.GetCompanyFields();
        
        // Player 1 besitzt 2 Unternehmen
        companyFields[0].ownerID = player1.PlayerID;
        player1.companies.Add(companyFields[0].fieldIndex);
        
        companyFields[1].ownerID = player1.PlayerID;
        player1.companies.Add(companyFields[1].fieldIndex);

        // Anderer Spieler besitzt ein Unternehmen
        if (gi.CurrentGame.AllPlayers.Count > 1)
        {
            var player2 = gi.CurrentGame.AllPlayers[1];
            companyFields[2].ownerID = player2.PlayerID;
        }

        // Act
        var auctionable = gm.GetAuctionableCompanies(player1);

        // Assert
        Debug.Log($"   Auctionable companies: {auctionable.Count}");
        Assert.AreEqual(2, auctionable.Count, "Sollte genau 2 Unternehmen haben");
        
        foreach (var field in auctionable)
        {
            Assert.AreEqual(player1.PlayerID, field.ownerID, "Alle sollten Player 1 gehören");
            Debug.Log($"   ✓ Field {field.fieldIndex} gehört Player {field.ownerID}");
        }

        yield return null;
    }
}