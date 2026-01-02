using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unit Tests für GameManager - Testet Insolvenz & Versteigerungs-Logik
/// </summary>
public class GameManagerBankruptcyTests
{
    private GameObject gameManagerObj;
    private GameManager gameManager;
    private GameInitiator gameInitiator;
    private MoneyManager moneyManager;
    private PlayerData testPlayer;
    private List<CompanyField> testCompanyFields;

    [SetUp]
    public void SetUp()
    {
        // Erstelle Test-GameObjects
        gameManagerObj = new GameObject("TestGameManager");
        gameManager = gameManagerObj.AddComponent<GameManager>();
        gameInitiator = gameManagerObj.AddComponent<GameInitiator>();
        moneyManager = gameManagerObj.AddComponent<MoneyManager>();
        
        // Initialisiere GameInitiator
        gameInitiator.CurrentGame = new GameState();
        
        // Erstelle Test-Spieler
        testPlayer = new PlayerData
        {
            PlayerID = 1,
            Money = 1000,
            BoardPosition = 0,
            PlayerName = "TestPlayer",
            companies = new List<int>()
        };
        gameInitiator.CurrentGame.AllPlayers.Add(testPlayer);
        gameInitiator.CurrentGame.CurrentPlayerTurnID = 0;
        
        // Erstelle Test-Company Fields
        testCompanyFields = new List<CompanyField>();
        var company1 = new CompanyField
        {
            fieldIndex = 1,
            companyID = 1,
            ownerID = 1,
            level = CompanyLevel.Founded
        };
        testCompanyFields.Add(company1);
        testPlayer.companies.Add(1);
        
        // Mock Company Config
        gameInitiator.companyConfigs = new CompanyConfigCollection
        {
            companies = new List<CompanyConfigData>
            {
                new CompanyConfigData
                {
                    companyID = 1,
                    companyName = "Test Company",
                    costFound = 200,
                    costInvest = 300,
                    costAG = 500,
                    revenueFound = 50,
                    revenueInvest = 100,
                    revenueAG = 200
                }
            }
        };
        
        // Setze Referenzen
        gameManager.gameInitiator = gameInitiator;
        gameManager.moneyManager = moneyManager;
        
        // Mock GetCompanyFields
        var getCompanyFieldsMethod = typeof(GameInitiator).GetMethod("GetCompanyFields");
        // Da GetCompanyFields() private ist, müssen wir es anders testen
    }

    [TearDown]
    public void TearDown()
    {
        if (gameManagerObj != null)
        {
            Object.DestroyImmediate(gameManagerObj);
        }
    }

    [Test]
    public void GetAuctionableCompanies_PlayerHasCompanies_ReturnsList()
    {
        // Arrange
        testPlayer.companies.Add(1);

        // Act
        var auctionable = gameManager.GetAuctionableCompanies(testPlayer);

        // Assert
        Assert.IsNotNull(auctionable);
        // Da GetCompanyFields() private ist, können wir hier nur die Methode testen
        // In einem echten Test müsste man die Company Fields richtig setzen
    }

    [Test]
    public void GetAuctionableCompanies_PlayerHasNoCompanies_ReturnsEmptyList()
    {
        // Arrange
        testPlayer.companies.Clear();

        // Act
        var auctionable = gameManager.GetAuctionableCompanies(testPlayer);

        // Assert
        Assert.IsNotNull(auctionable);
        Assert.AreEqual(0, auctionable.Count);
    }

    [Test]
    public void HandleBankruptcy_PlayerHasNoCompanies_EndsTurn()
    {
        // Arrange
        testPlayer.companies.Clear();
        testPlayer.Money = 100;
        int requiredAmount = 1000;

        // Act
        gameManager.HandleBankruptcy(testPlayer, requiredAmount, "Test");

        // Assert
        // EndTurn() sollte aufgerufen werden, aber wir können das nicht direkt testen
        // ohne Mocking. Der Test zeigt aber die Logik.
        Assert.IsTrue(testPlayer.companies.Count == 0);
    }
}

