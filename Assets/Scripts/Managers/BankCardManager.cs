using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BankCard
{
    public int id;
    public string text;
}

[System.Serializable]
public class BankCardDeck
{
    public string name;
    public List<BankCard> karten;
}

// Add this enum for testing
public enum BankCardTestMode
{
    All,              // All cards
    MoneyOnly,        // Only money reward cards
    SkipTurnOnly,     // Only skip turn cards
    RollAgainOnly,    // Only roll again cards
    MovementOnly      // Only movement cards
}

public class BankCardManager : MonoBehaviour
{
    private List<BankCard> cards = new List<BankCard>();
    private GameManager gameManager;

    [SerializeField] private BankCardPopup popup;

    // 🎮 TEST MODE - Set this in the Inspector!
    [Header("Testing")]
    [SerializeField] private BankCardTestMode testMode = BankCardTestMode.All;
    [SerializeField] private bool enableTestMode = false;  // Toggle testing on/off

    private BankCard pendingCard;
    private bool lastCardWasRollAgain = false;

    [HideInInspector] public Dictionary<int, int> _skipCounters = new Dictionary<int, int>();

    // Card ID lists for filtering
    private readonly HashSet<int> moneyCards = new HashSet<int> {
        13, 17, 18, 22, 26, 28, 31, 33, 34, 36, 41, 47, 48, 50, 55, 59, 64, 65, 71, 73, 75, 78, 81, 82, 83
    };

    private readonly HashSet<int> skipTurnCards = new HashSet<int> {
        3, 9, 15, 40, 57, 60, 80, 207
    };

    private readonly HashSet<int> rollAgainCards = new HashSet<int> {
        4, 5, 11, 14, 16, 21, 25, 27, 37, 39, 43, 46, 49, 50, 58, 62, 66, 67, 69, 76, 83
    };

    private readonly HashSet<int> movementCards = new HashSet<int> {
        1, 2, 7, 12, 19, 23, 24, 29, 30, 32, 35, 38, 44, 45, 52, 56, 70, 72, 74, 77, 79, 85
    };

    void Awake()
    {
        LoadCards();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void LoadCards()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Bankkarten_DE");
        if (jsonFile == null)
        {
            Debug.LogError("BankCardManager: Could not load Schoolgames_Bankkarten_DE.json from Resources/Data/");
            return;
        }
        BankCardDeck loaded = JsonUtility.FromJson<BankCardDeck>(jsonFile.text);
        if (loaded != null && loaded.karten != null)
        {
            cards = loaded.karten;
            Debug.Log($"BankCardManager: Loaded {cards.Count} bank cards from German JSON file.");
        }
        else
        {
            Debug.LogError("BankCardManager: Failed to parse bank cards from JSON file.");
        }
    }

    // 🎮 NEW: Get filtered cards based on test mode
    private List<BankCard> GetFilteredCards()
    {
        if (!enableTestMode || testMode == BankCardTestMode.All)
        {
            return cards;
        }

        List<BankCard> filtered = new List<BankCard>();
        HashSet<int> allowedIds = GetAllowedCardIds();

        foreach (var card in cards)
        {
            if (allowedIds.Contains(card.id))
            {
                filtered.Add(card);
            }
        }

        Debug.Log($"[TEST MODE: {testMode}] Filtered to {filtered.Count} cards");
        return filtered;
    }

    private HashSet<int> GetAllowedCardIds()
    {
        switch (testMode)
        {
            case BankCardTestMode.MoneyOnly:
                return moneyCards;
            case BankCardTestMode.SkipTurnOnly:
                return skipTurnCards;
            case BankCardTestMode.RollAgainOnly:
                return rollAgainCards;
            case BankCardTestMode.MovementOnly:
                return movementCards;
            default:
                return new HashSet<int>();
        }
    }

    public void ShowRandomBankCard()
    {
        List<BankCard> availableCards = GetFilteredCards();

        if (availableCards == null || availableCards.Count == 0)
        {
            Debug.LogWarning($"No bank cards available for test mode: {testMode}");
            gameManager.EndTurn();
            return;
        }

        pendingCard = availableCards[Random.Range(0, availableCards.Count)];
        
        if (enableTestMode)
        {
            Debug.Log($"[TEST MODE: {testMode}] Selected Card #{pendingCard.id}: {pendingCard.text}");
        }

        lastCardWasRollAgain = false;

        if (popup == null)
        {
            Debug.LogWarning("[BankCardManager] popup is NULL -> executing immediately (no UI).");
            ResolvePendingCard();
            return;
        }

        popup.Show(pendingCard.id, pendingCard.text, ResolvePendingCard);
    }

    private void ResolvePendingCard()
    {
        if (pendingCard == null)
        {
            gameManager.EndTurn();
            return;
        }

        ExecuteBankCardAction(pendingCard.id);
        pendingCard = null;
    }

    private void ExecuteBankCardAction(int cardId)
    {
        if (gameManager == null)
        {
            Debug.LogError("BankCardManager: GameManager not found!");
            return;
        }

        int currentPlayerID = gameManager.GetCurrentPlayer().PlayerID;
        Debug.Log($"Executing bank card action for Player {currentPlayerID}");

        switch (cardId)
        {
            // Movement cases - NO EndTurn()
            case 1: MovePlayer(3); break;
            case 2: MoveToChosenCompanyField(); break;
            case 7: MoveToChosenField(); break;
            case 12: MoveToChosenCompanyField(); break;
            case 19: MovePlayer(4); break;
            case 23: MovePlayer(1); break;
            case 24: MovePlayer(2); break;
            case 29: MovePlayerToField(2); break;
            case 30: MoveToChosenCompanyField(); break;
            case 32: MovePlayer(3); break;
            case 35: MovePlayerToField(39); break;
            
            // ToDo Frage Beantworten hinzufügen
            case 38: MovePlayer(3); break;

            case 44: MovePlayerToField(1); break;
            case 45: MovePlayer(2); break;
            case 52: MovePlayerToField(31); break;
            case 56: MovePlayer(3); break;
            case 70: MovePlayer(4); break;
            case 72: MovePlayerToField(9); break;
            case 74: MovePlayerToField(27); break;
            case 77: MoveToChosenField(); break;
            case 79: MovePlayer(2); break;
            case 85: MoveToNextCompanyField(); break;

            // Roll again cases - NO EndTurn()
            case 4: case 5: case 11: case 14: case 16: case 21: case 25: case 27:
            case 37: case 39: case 43: case 46: case 49: case 58: case 62: case 66:
            case 67: case 69: case 76:
                RollAgain();
                break;

            // Skip turn cases - CALLS EndTurn()
            case 3: case 9: case 15: case 40: case 57: case 60: case 80: case 207:
                SkipTurn();
                break;

            // Money rewards - CALLS EndTurn()
            case 13: case 17: case 18: case 22: case 26: case 28: case 31: case 33:
            case 34: case 36: case 41: case 47: case 48: case 55: case 59: case 64:
            case 65: case 71: case 73: case 75: case 78: case 81: case 82:
                AddMoneyFromBankCard(GetCardRewardAmount(cardId));
                break;

            // Special: Money + roll again - NO EndTurn() (roll again takes precedence)
            case 50: case 83:
                AddMoneyFromBankCardAndMove(GetCardRewardAmount(cardId));
                RollAgain();
                break;

            default:
                Debug.Log($"Bank Card #{cardId}: No action implemented.");
                gameManager.EndTurn();
                break;
        }
    }

    private int GetCardRewardAmount(int cardId)
    {
        switch (cardId)
        {
            case 22: return 50;
            case 18: case 59: case 75: case 78: return 150;
            case 17: case 26: case 28: case 31: case 33: case 55: return 200;
            case 47: case 48: return 300;
            case 81: return 500;
            case 50: return 500;
            case 13: case 34: case 41: case 64: case 73: case 82: case 83: return 100;
            case 36: case 65: case 71: return 250;
            default: return 0;
        }
    }

    public bool ShouldRollAgain()
    {
        return lastCardWasRollAgain;
    }

    public void MovePlayer(int steps)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        if (activePlayer != null)
        {
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} {steps} steps forward");
            activePlayer.StartMove(steps);
        }
        else
        {
            Debug.LogError($"Could not find PlayerCTRL for player {currentPlayer.PlayerID}");
        }
    }

    public void MovePlayerToField(int fieldPosition)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        if (activePlayer != null)
        {
            int currentPos = activePlayer.currentPos;
            int stepsNeeded = (fieldPosition - currentPos + 40) % 40;
            
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} to field {fieldPosition} ({stepsNeeded} steps)");
            activePlayer.StartMove(stepsNeeded);
        }
    }

    public void SkipTurn()
    {
        PlayerData current = gameManager.GetCurrentPlayer();
        if (current == null)
        {
            Debug.LogError("SkipTurn: no current player!");
            gameManager.EndTurn();
            return;
        }

        current.hasToSkip = true;
        Debug.Log($"Bank Card Action: Player {current.PlayerID} will skip their next turn.");
        gameManager.EndTurn();
    }

    public void RollAgain()
    {
        Debug.Log($"Bank Card Action: Player {gameManager.GetCurrentPlayer().PlayerID} gets to roll again!");
        gameManager.playerMovement.setIsTurnInProgress(false);
        GameObject moveButton = gameManager.playerMovement.getMoveButton();
        moveButton.SetActive(true);
        gameManager.uiManager.UpdateMoneyDisplay();
        Debug.Log("Player can now roll again!");
    }

    public void AddMoneyFromBankCardAndMove(int amount)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gameManager.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Bank Card Action: Player {currentPlayer.PlayerID} receives {amount}€");
        }
        else
        {
            Debug.LogError("AddMoneyFromBankCard: Current player is null!");
            gameManager.EndTurn();
        }
    }

    public void AddMoneyFromBankCard(int amount)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gameManager.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Bank Card Action: Player {currentPlayer.PlayerID} receives {amount}€");
            gameManager.EndTurn();
        }
        else
        {
            Debug.LogError("AddMoneyFromBankCard: Current player is null!");
            gameManager.EndTurn();
        }
    }

    public void MoveToNextCompanyField()
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("MoveToNextCompanyField: Current player is null!");
            gameManager.EndTurn();
            return;
        }

        
        List<int> companyFields = currentPlayer.companies; 
        if (companyFields == null || companyFields.Count == 0)
        {
            Debug.LogError("MoveToNextCompanyField: No company fields owned by Player!");
            gameManager.EndTurn();
            return;
        }

        int nearestCompany = -1;
        int shortestDistance = 40;

        foreach (int companyField in companyFields)
        {
            int distance = (companyField - currentPlayer.BoardPosition + 40) % 40;
            if (distance > 0 && distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestCompany = companyField;
            }
        }

        if (nearestCompany == -1)
        {
            Debug.LogWarning("MoveToNextCompanyField: No next company found.");
            gameManager.EndTurn();
            return;
        }

        Debug.Log($"Moving player {currentPlayer.PlayerID} to next company field {nearestCompany} ({shortestDistance} steps ahead)");
        
        MovePlayerToField(nearestCompany);
    }

    public void MoveToChosenCompanyField()
    {
        gameManager.cameraManager.SetTopView();
    }
    
    public void MoveToChosenField()
    {
        gameManager.cameraManager.SetTopView();
    }
}