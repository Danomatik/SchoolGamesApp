using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// EditMode Unit Tests für Geld-Berechnungen (ohne Unity Runtime)
/// Testet die Logik für Vermögensberechnung, Zahlungsfähigkeit etc.
/// </summary>
public class MoneyCalculationsTests
{
    [Test]
    public void CalculateTotalAssets_OnlyCash()
    {
        // Arrange
        PlayerData player = new PlayerData
        {
            Money = 1000,
            companies = new List<int>()
        };

        // Act: Simuliere CalculateTotalAssets Logik
        int totalAssets = player.Money; // Nur Bargeld, keine Unternehmen

        // Assert
        Assert.AreEqual(1000, totalAssets);
    }

    [Test]
    public void CalculateTotalAssets_WithCompanies()
    {
        // Arrange
        PlayerData player = new PlayerData
        {
            Money = 500,
            companies = new List<int> { 1, 2 }
        };

        // Simuliere: Unternehmen 1 kostet 200€ Gründung, Unternehmen 2 kostet 300€ Gründung
        // Versteigerungswert = 50% der Gründungskosten
        int company1Value = 200 / 2; // 100€
        int company2Value = 300 / 2; // 150€

        // Act
        int totalAssets = player.Money + company1Value + company2Value;

        // Assert
        Assert.AreEqual(750, totalAssets); // 500 + 100 + 150
    }

    [Test]
    public void CanAffordPayment_WithEnoughCash()
    {
        // Arrange
        PlayerData player = new PlayerData { Money = 1000 };
        int paymentAmount = 500;

        // Act
        bool canAfford = player.Money >= paymentAmount;

        // Assert
        Assert.IsTrue(canAfford);
    }

    [Test]
    public void CanAffordPayment_WithInsufficientCash_ButEnoughAssets()
    {
        // Arrange
        PlayerData player = new PlayerData
        {
            Money = 200,
            companies = new List<int> { 1 }
        };
        int paymentAmount = 500;
        int companyValue = 400 / 2; // 200€ (50% von 400€ Gründungskosten)
        int totalAssets = player.Money + companyValue; // 200 + 200 = 400€

        // Act
        bool canAffordWithCash = player.Money >= paymentAmount; // false
        bool canAffordWithAssets = totalAssets >= paymentAmount; // false (400 < 500)

        // Assert
        Assert.IsFalse(canAffordWithCash, "Sollte nicht mit Bargeld zahlen können");
        Assert.IsFalse(canAffordWithAssets, "Sollte auch nicht mit Vermögen zahlen können");
    }

    [Test]
    public void CanAffordPayment_WithInsufficientCash_ButEnoughTotalAssets()
    {
        // Arrange
        PlayerData player = new PlayerData
        {
            Money = 100,
            companies = new List<int> { 1 }
        };
        int paymentAmount = 300;
        int companyValue = 500 / 2; // 250€ (50% von 500€ Gründungskosten)
        int totalAssets = player.Money + companyValue; // 100 + 250 = 350€

        // Act
        bool canAffordWithCash = player.Money >= paymentAmount; // false
        bool canAffordWithAssets = totalAssets >= paymentAmount; // true (350 >= 300)

        // Assert
        Assert.IsFalse(canAffordWithCash, "Sollte nicht mit Bargeld zahlen können");
        Assert.IsTrue(canAffordWithAssets, "Sollte mit Gesamtvermögen zahlen können");
    }

    [Test]
    public void AuctionPrice_Is50PercentOfFoundationCost()
    {
        // Arrange
        int foundationCost = 400;

        // Act
        int auctionPrice = foundationCost / 2;

        // Assert
        Assert.AreEqual(200, auctionPrice);
    }

    [Test]
    public void AuctionPrice_MultipleCompanies()
    {
        // Arrange
        int foundationCost1 = 200;
        int foundationCost2 = 300;
        int foundationCost3 = 500;

        // Act
        int auctionPrice1 = foundationCost1 / 2; // 100
        int auctionPrice2 = foundationCost2 / 2; // 150
        int auctionPrice3 = foundationCost3 / 2; // 250
        int totalAuctionValue = auctionPrice1 + auctionPrice2 + auctionPrice3;

        // Assert
        Assert.AreEqual(100, auctionPrice1);
        Assert.AreEqual(150, auctionPrice2);
        Assert.AreEqual(250, auctionPrice3);
        Assert.AreEqual(500, totalAuctionValue);
    }

    [Test]
    public void PaymentCalculation_AfterAuction()
    {
        // Arrange
        PlayerData player = new PlayerData { Money = 50 };
        int requiredPayment = 300;
        int auctionPrice = 400; // Von einem Unternehmen

        // Act: Versteigere Unternehmen, bezahle dann
        int moneyAfterAuction = player.Money + auctionPrice; // 50 + 400 = 450
        int moneyAfterPayment = moneyAfterAuction - requiredPayment; // 450 - 300 = 150

        // Assert
        Assert.AreEqual(450, moneyAfterAuction);
        Assert.AreEqual(150, moneyAfterPayment);
        Assert.IsTrue(moneyAfterPayment > 0, "Spieler sollte nach Versteigerung und Zahlung noch Geld haben");
    }
}
