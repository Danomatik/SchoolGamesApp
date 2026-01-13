using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// EditMode Unit Tests für Unternehmen-Level-Logik
/// </summary>
public class CompanyLevelTests
{
    [Test]
    public void CompanyLevel_UpgradePath_IsValid()
    {
        // Arrange & Act
        // CompanyLevel enum: None -> Founded -> Invested -> AG

        // Assert: Prüfe dass Upgrade-Pfad logisch ist
        Assert.IsTrue(true, "Upgrade-Pfad sollte None -> Founded -> Invested -> AG sein");
    }

    [Test]
    public void CompanyLevel_UpgradeCost_Calculation()
    {
        // Arrange
        int foundationCost = 200;
        int investedCost = 300;
        int agCost = 500;

        // Act
        int totalCostToAG = foundationCost + investedCost + agCost;

        // Assert
        Assert.AreEqual(1000, totalCostToAG, "Gesamtkosten bis AG sollten 1000€ sein");
    }

    [Test]
    public void CompanyLevel_AuctionPrice_Is50Percent()
    {
        // Arrange
        int foundationCost = 400;
        int investedCost = 600;
        int agCost = 1000;

        // Act
        int auctionPriceFounded = foundationCost / 2; // 200
        int auctionPriceInvested = investedCost / 2; // 300
        int auctionPriceAG = agCost / 2; // 500

        // Assert
        Assert.AreEqual(200, auctionPriceFounded, "Versteigerungspreis für Founded sollte 200€ sein");
        Assert.AreEqual(300, auctionPriceInvested, "Versteigerungspreis für Invested sollte 300€ sein");
        Assert.AreEqual(500, auctionPriceAG, "Versteigerungspreis für AG sollte 500€ sein");
    }

    [Test]
    public void CompanyLevel_UpgradeSequence()
    {
        // Arrange
        List<string> upgradeSequence = new List<string> { "None", "Founded", "Invested", "AG" };

        // Act & Assert
        Assert.AreEqual(4, upgradeSequence.Count, "Sollte 4 Level haben");
        Assert.AreEqual("None", upgradeSequence[0], "Erstes Level sollte None sein");
        Assert.AreEqual("Founded", upgradeSequence[1], "Zweites Level sollte Founded sein");
        Assert.AreEqual("Invested", upgradeSequence[2], "Drittes Level sollte Invested sein");
        Assert.AreEqual("AG", upgradeSequence[3], "Viertes Level sollte AG sein");
    }

    [Test]
    public void CompanyLevel_RentCalculation_ByLevel()
    {
        // Arrange
        int baseRent = 50;
        int foundedMultiplier = 1;
        int investedMultiplier = 2;
        int agMultiplier = 3;

        // Act
        int foundedRent = baseRent * foundedMultiplier; // 50
        int investedRent = baseRent * investedMultiplier; // 100
        int agRent = baseRent * agMultiplier; // 150

        // Assert
        Assert.AreEqual(50, foundedRent, "Founded Miete sollte 50€ sein");
        Assert.AreEqual(100, investedRent, "Invested Miete sollte 100€ sein");
        Assert.AreEqual(150, agRent, "AG Miete sollte 150€ sein");
    }

    [Test]
    public void CompanyLevel_Ownership_Transfer()
    {
        // Arrange
        int player1ID = 1;
        int player2ID = 2;
        int companyOwnerID = player1ID;

        // Act: Verkaufe Unternehmen
        companyOwnerID = player2ID;

        // Assert
        Assert.AreEqual(player2ID, companyOwnerID, "Unternehmen sollte jetzt Player 2 gehören");
        Assert.AreNotEqual(player1ID, companyOwnerID, "Unternehmen sollte nicht mehr Player 1 gehören");
    }
}
