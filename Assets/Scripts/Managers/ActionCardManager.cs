using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActionCard
{
    public int id;
    public string text;
}

[System.Serializable]
public class ActionCardDeck
{
    public string name;
    public List<ActionCard> karten;
}

// Add this enum for testing
public enum ActionCardTestMode
{
    All,              // All cards
    MovementOnly,     // Only movement cards (IDs 1-4)
    MoneyOnly,        // Only money reward cards (ID 6)
    SkipTurnOnly,     // Only skip turn cards (ID 7)
    RollAgainOnly,    // Only roll again cards (ID 8)
    SpecialOnly       // Only special cards (ID 5 - AG upgrade)
}

public class ActionCardManager : MonoBehaviour
{
    private List<ActionCard> cards = new List<ActionCard>();
    private GameManager gameManager;

    [SerializeField] private ActionCardPopup popup;  // im Inspector setzen

    // 🎮 TEST MODE - Set this in the Inspector!
    [Header("Testing")]
    [SerializeField] private ActionCardTestMode testMode = ActionCardTestMode.All;
    [SerializeField] private bool enableTestMode = false;  // Toggle testing on/off

    private ActionCard pendingCard;
    private bool lastCardWasRollAgain = false;

    // Card ID lists for filtering
    private readonly HashSet<int> movementCards = new HashSet<int> { 1, 2, 3, 4 };
    private readonly HashSet<int> moneyCards = new HashSet<int> { 6 };
    private readonly HashSet<int> skipTurnCards = new HashSet<int> { 7 };
    private readonly HashSet<int> rollAgainCards = new HashSet<int> { 8 };
    private readonly HashSet<int> specialCards = new HashSet<int> { 5 };

    void Awake()
    {
        LoadCards();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void LoadCards()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Aktionskarte_DE");
        if (jsonFile == null)
        {
            Debug.LogError("ActionCardManager: Could not load aktionskarten_de.json from Resources/Data/");
            return;
        }

        ActionCardDeck loaded = JsonUtility.FromJson<ActionCardDeck>(jsonFile.text);
        if (loaded != null && loaded.karten != null)
        {
            cards = loaded.karten;
            Debug.Log($"ActionCardManager: Loaded {cards.Count} action cards from German JSON file.");
        }
        else
        {
            Debug.LogError("ActionCardManager: Failed to parse action cards from JSON file.");
        }
    }

    // 🎮 Get filtered cards based on test mode
    private List<ActionCard> GetFilteredCards()
    {
        if (!enableTestMode || testMode == ActionCardTestMode.All)
        {
            return cards;
        }

        List<ActionCard> filtered = new List<ActionCard>();
        HashSet<int> allowedIds = GetAllowedCardIds();

        foreach (var card in cards)
        {
            if (allowedIds.Contains(card.id))
            {
                filtered.Add(card);
            }
        }

        Debug.Log($"[TEST MODE: {testMode}] Filtered to {filtered.Count} action cards");
        return filtered;
    }

    private HashSet<int> GetAllowedCardIds()
    {
        switch (testMode)
        {
            case ActionCardTestMode.MovementOnly:
                return movementCards;
            case ActionCardTestMode.MoneyOnly:
                return moneyCards;
            case ActionCardTestMode.SkipTurnOnly:
                return skipTurnCards;
            case ActionCardTestMode.RollAgainOnly:
                return rollAgainCards;
            case ActionCardTestMode.SpecialOnly:
                return specialCards;
            default:
                return new HashSet<int>();
        }
    }

    public void ShowRandomActionCard()
    {
        List<ActionCard> availableCards = GetFilteredCards();

        if (availableCards == null || availableCards.Count == 0)
        {
            Debug.LogWarning($"No action cards available for test mode: {testMode}");
            gameManager.EndTurn();
            return;
        }

        pendingCard = availableCards[Random.Range(0, availableCards.Count)];

        if (enableTestMode)
        {
            Debug.Log($"[TEST MODE: {testMode}] Selected Action Card #{pendingCard.id}: {pendingCard.text}");
        }

        lastCardWasRollAgain = false;

        if (popup == null)
        {
            Debug.LogWarning("[ActionCardManager] popup is NULL -> executing immediately (no UI).");
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

        ExecuteActionCardAction(pendingCard.id);

        if (lastCardWasRollAgain)
        {
            RollAgain();
        }
        else
        {
            gameManager.EndTurn();
        }

        pendingCard = null;
    }

    private void ExecuteActionCardAction(int cardId)
    {
        if (gameManager == null)
        {
            Debug.LogError("ActionCardManager: GameManager not found!");
            return;
        }

        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("ActionCardManager: Current player is null!");
            return;
        }

        Debug.Log($"Executing action card {cardId} for Player {currentPlayer.PlayerID}");

        switch (cardId)
        {
            case 1: // Possibly - Rücke vor zu einem Unternehmen deiner Wahl
                Debug.Log("Action Card 1: Player can move to any company (not implemented yet - going to field 0)");
                MovePlayerToField(0); // TODO: Implement company selection UI
                break;

            case 2: // Volksbank Präsentation - Springe zu einem Unternehmen deiner Wahl
                Debug.Log("Action Card 2: Player can jump to any company (not implemented yet - going to field 0)");
                MovePlayerToField(0); // TODO: Implement company selection UI
                break;

            case 3: // Business Angels - Springe zu deinem nächsten Unternehmen
                Debug.Log("Action Card 3: Jump to player's next owned company");
                JumpToNextOwnedCompany();
                break;

            case 4: // Landesregierung - Springe zu einem deiner Unternehmen
                Debug.Log("Action Card 4: Jump to one of player's companies (not implemented yet)");
                JumpToNextOwnedCompany(); // TODO: Implement owned company selection UI
                break;

            case 5: // Quiz für kostenloses AG-Upgrade
                Debug.Log("Action Card 5: Free AG upgrade if quiz passed (not fully implemented)");
                // TODO: Implement special quiz + free AG upgrade logic
                gameManager.EndTurn();
                break;

            case 6: // Stadt gestalten - EUR 200 Bonus
                Debug.Log("Action Card 6: Player receives 200€");
                AddMoneyFromActionCard(200);
                break;

            case 7: // Gesetzliche Auflagen - Setze eine Runde aus
                Debug.Log("Action Card 7: Player skips next turn");
                SkipTurn();
                break;

            case 8: // Stadt Graz Praktikum - Noch einmal würfeln
                Debug.Log("Action Card 8: Player can roll again");
                lastCardWasRollAgain = true;
                RollAgain();
                break;

            default:
                Debug.LogWarning($"Action Card #{cardId}: No action implemented yet.");
                break;
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

            Debug.Log($"Action Card: Moving player {currentPlayer.PlayerID} to field {fieldPosition} ({stepsNeeded} steps)");
            activePlayer.StartMove(stepsNeeded);
        }
        else
        {
            Debug.LogError($"Could not find PlayerCTRL for player {currentPlayer.PlayerID}");
        }
    }

    private void JumpToNextOwnedCompany()
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        
        if (currentPlayer.companies == null || currentPlayer.companies.Count == 0)
        {
            Debug.Log("Player has no companies to jump to!");
            gameManager.EndTurn();
            return;
        }

        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        if (activePlayer == null)
        {
            Debug.LogError($"Could not find PlayerCTRL for player {currentPlayer.PlayerID}");
            return;
        }

        int currentPos = activePlayer.currentPos;
        
        // Find next owned company (clockwise from current position)
        int targetField = -1;
        int minDistance = 40;

        foreach (int companyField in currentPlayer.companies)
        {
            int distance = (companyField - currentPos + 40) % 40;
            if (distance > 0 && distance < minDistance)
            {
                minDistance = distance;
                targetField = companyField;
            }
        }

        // If no company ahead, take the first one (wrap around)
        if (targetField == -1 && currentPlayer.companies.Count > 0)
        {
            targetField = currentPlayer.companies[0];
        }

        if (targetField != -1)
        {
            Debug.Log($"Action Card: Player {currentPlayer.PlayerID} jumps to their company at field {targetField}");
            MovePlayerToField(targetField);
        }
        else
        {
            Debug.LogError("Could not find a valid company to jump to!");
            gameManager.EndTurn();
        }
    }

    public void SkipTurn()
    {
        var current = gameManager.GetCurrentPlayer();
        if (current == null)
        {
            Debug.LogError("SkipTurn: no current player!");
            gameManager.EndTurn();
            return;
        }

        current.hasToSkip = true;
        Debug.Log($"Action Card: Player {current.PlayerID} will skip their next turn.");
        gameManager.EndTurn();
    }

    public void RollAgain()
    {
        Debug.Log($"Action Card: Player {gameManager.GetCurrentPlayer().PlayerID} gets to roll again!");

        gameManager.playerMovement.setIsTurnInProgress(false);
        GameObject moveButton = gameManager.playerMovement.getMoveButton();
        moveButton.SetActive(true);
        gameManager.uiManager.UpdateMoneyDisplay();

        Debug.Log("Player can now roll again!");
    }

    public void AddMoneyFromActionCard(int amount)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gameManager.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Action Card: Player {currentPlayer.PlayerID} receives {amount}€");
        }
        else
        {
            Debug.LogError("AddMoneyFromActionCard: Current player is null!");
        }
    }

    public bool ShouldRollAgain()
    {
        return lastCardWasRollAgain;
    }
}