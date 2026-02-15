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

    // [SerializeField] private ActionCardPopup popup; // OBSOLETE

    // 🎮 TEST MODE - Set this in the Inspector!
    [Header("Testing")]
    [SerializeField] private bool debugOnlyMovementCards = false; // ✅ New simple flag
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
    
    // Karten, die zu owned companies bewegen (nur wenn Spieler Unternehmen besitzt)
    private readonly HashSet<int> ownedCompanyCards = new HashSet<int> { 3, 4 }; // 3 = nächste, 4 = eines deiner

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
        // ✅ Priority check for the simple flag
        if (debugOnlyMovementCards)
        {
            Debug.Log("[ActionCardManager] Debug Flag 'Only Movement Cards' is ACTIVE.");
            List<ActionCard> movementOnly = new List<ActionCard>();
            foreach (var card in cards)
            {
                if (movementCards.Contains(card.id))
                    movementOnly.Add(card);
            }
            return movementOnly;
        }

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

    private HashSet<int> usedCardIds = new HashSet<int>();

    public void ShowRandomActionCard()
    {
        List<ActionCard> availableCards = GetFilteredCards();
        
        // ✅ NEU: Filtere Karten heraus, die zu owned companies bewegen, wenn Spieler keine Unternehmen besitzt
        availableCards = FilterCardsByPlayerOwnership(availableCards);

        if (availableCards == null || availableCards.Count == 0)
        {
            Debug.LogWarning($"No action cards available for test mode: {testMode}");
            gameManager.EndTurn();
            return;
        }

        // Filter out already used cards
        List<ActionCard> candidates = new List<ActionCard>();
        foreach (var card in availableCards)
        {
            if (!usedCardIds.Contains(card.id))
            {
                candidates.Add(card);
            }
        }

        // If deck is empty (all valid cards used), reshuffle
        if (candidates.Count == 0)
        {
            Debug.Log("[ActionCardManager] All valid action cards shown! Reshuffling deck.");
            usedCardIds.Clear();
            candidates.AddRange(availableCards);
        }

        pendingCard = candidates[Random.Range(0, candidates.Count)];
        usedCardIds.Add(pendingCard.id);

        if (enableTestMode)
        {
            Debug.Log($"[TEST MODE: {testMode}] Selected Action Card #{pendingCard.id}: {pendingCard.text}");
        }

        lastCardWasRollAgain = false;

        // NEW: use UIManager
        if (gameManager != null && gameManager.uiManager != null)
        {
            gameManager.uiManager.ShowActionCard(pendingCard.id, pendingCard.text, ResolvePendingCard);
        }
        else
        {
             Debug.LogWarning("[ActionCardManager] GameManager or UIManager missing -> executing immediately.");
             ResolvePendingCard();
        }
    }

    private void ResolvePendingCard()
    {
        if (pendingCard == null)
        {
            gameManager.EndTurn();
            return;
        }

        int cardId = pendingCard.id;
        bool shouldEndTurnImmediately = ExecuteActionCardAction(cardId);

        if (lastCardWasRollAgain)
        {
            gameManager.actionManager.RollAgain();
        }
        else if (shouldEndTurnImmediately)
        {
            // ✅ Nur EndTurn() aufrufen, wenn die Aktion synchron war (z.B. Geld, Skip)
            // Bei Bewegung (Cards 1-4) oder Quiz (Card 5) übernimmt der jeweilige Manager das EndTurn()
            gameManager.EndTurn();
        }

        pendingCard = null;
    }

    /// <summary>
    /// Executes the action. Returns true if the turn should end immediately after.
    /// Returns false if the action is asynchronous (e.g. movement, quiz) and handles EndTurn itself.
    /// </summary>
    private bool ExecuteActionCardAction(int cardId)
    {
        if (gameManager == null)
        {
            Debug.LogError("ActionCardManager: GameManager not found!");
            return true;
        }

        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("ActionCardManager: Current player is null!");
            return true;
        }

        Debug.Log($"Executing action card {cardId} for Player {currentPlayer.PlayerID}");

        switch (cardId)
        {
            case 1: // Possibly - Rücke vor zu einem Unternehmen deiner Wahl
                Debug.Log("Action Card 1: Player can move to any company");
                gameManager.actionManager.MoveToChosenCompanyField(); 
                return false; // Async movement

            case 2: // Volksbank Präsentation - Springe zu einem Unternehmen deiner Wahl
                Debug.Log("Action Card 2: Player can move to any company");
                gameManager.actionManager.MoveToChosenCompanyField();
                return false; // Async movement

            case 3: // Business Angels - Springe zu deinem nächsten Unternehmen
                Debug.Log("Action Card 3: Jump to player's next owned company");
                gameManager.actionManager.MoveToNextCompanyField();
                return false; // Async movement

            case 4: // Landesregierung - Springe zu einem deiner Unternehmen
                Debug.Log("Action Card 4: Jump to one of player's companies");
                gameManager.actionManager.MoveToOwnedCompanyField();
                return false; // Async movement

            case 5: // Quiz für kostenloses AG-Upgrade
                {
                    var player = gameManager.GetCurrentPlayer();
                    if (player == null) { Debug.LogWarning("No current player."); return true; }

                    // Reuse GameManager logic
                    gameManager.StartQuizForAG();
                    return false; // Async quiz
                }

            case 6: // Stadt gestalten - EUR 200 Bonus
                Debug.Log("Action Card 6: Player receives 200€");
                gameManager.actionManager.AddMoney(200);
                return true; // Instant

            case 7: // Gesetzliche Auflagen - Setze eine Runde aus
                Debug.Log("Action Card 7: Player skips next turn");
                gameManager.actionManager.SkipTurn();
                return true; // Instant (ActionManager.SkipTurn calls EndTurn internally? Let's check. Yes it does.)
                // Wait, ActionManager.SkipTurn calls gameManager.EndTurn() itself. 
                // So if we return true, we might double call EndTurn.
                // ActionManager.SkipTurn:
                // public void SkipTurn() { ... gameManager.EndTurn(); }
                // So we should return FALSE here to avoid double EndTurn, OR change ActionManager.SkipTurn.
                // Changing this return to FALSE for safety if ActionManager handles it.
                return false; 

            case 8: // Stadt Graz Praktikum - Noch einmal würfeln
                Debug.Log("Action Card 8: Player can roll again");
                lastCardWasRollAgain = true;
                gameManager.actionManager.RollAgain();
                return false; // RollAgain logic handles flow

            default:
                Debug.LogWarning($"Action Card #{cardId}: No action implemented yet.");
                return true;
        }
    }
    
    /// <summary>
    /// Filtert Karten heraus, die zu owned companies bewegen, wenn der Spieler keine Unternehmen besitzt
    /// </summary>
    private List<ActionCard> FilterCardsByPlayerOwnership(List<ActionCard> cards)
    {
        if (gameManager == null) return cards;
        
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer == null) return cards;
        
        // Prüfe ob Spieler Unternehmen besitzt
        bool hasCompanies = currentPlayer.companies != null && currentPlayer.companies.Count > 0;
        
        if (hasCompanies)
        {
            // Spieler hat Unternehmen - alle Karten erlaubt
            return cards;
        }
        
        // Spieler hat keine Unternehmen - filtere owned company Karten heraus
        List<ActionCard> filtered = new List<ActionCard>();
        int removedCount = 0;

        foreach (var card in cards)
        {
            // Prüfe, ob Karte eine "Owned Company"-Karte ist (ID 3 oder 4)
            if (ownedCompanyCards.Contains(card.id))
            {
                // Überspringen -> wird nicht hinzugefügt
                removedCount++;
                // Debug.Log($"ActionCardManager: Filtered out Card {card.id} because Player {currentPlayer.PlayerID} has no companies.");
            }
            else
            {
                filtered.Add(card);
            }
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"[ActionCardManager] Filtered out {removedCount} 'Owned Company' cards (Player {currentPlayer.PlayerID} has 0 companies). Remaining: {filtered.Count}");
        }

        return filtered;
    }
}
