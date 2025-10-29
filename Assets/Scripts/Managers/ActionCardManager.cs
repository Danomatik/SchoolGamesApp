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
            gameManager.actionManager.RollAgain();
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
                gameManager.actionManager.MovePlayerToField(0); // TODO: Implement company selection UI
                break;

            case 2: // Volksbank Präsentation - Springe zu einem Unternehmen deiner Wahl
                Debug.Log("Action Card 2: Player can jump to any company (not implemented yet - going to field 0)");
                gameManager.actionManager.MovePlayerToField(0); // TODO: Implement company selection UI
                break;

            case 3: // Business Angels - Springe zu deinem nächsten Unternehmen
                Debug.Log("Action Card 3: Jump to player's next owned company");
                gameManager.actionManager.MoveToNextCompanyField();
                break;

            case 4: // Landesregierung - Springe zu einem deiner Unternehmen
                Debug.Log("Action Card 4: Jump to one of player's companies (not implemented yet)");
                gameManager.actionManager.MoveToNextCompanyField(); // TODO: Implement owned company selection UI
                break;

            case 5: // Quiz für kostenloses AG-Upgrade
                Debug.Log("Action Card 5: Free AG upgrade if quiz passed (not fully implemented)");
                // TODO: Implement special quiz + free AG upgrade logic
                gameManager.EndTurn();
                break;

            case 6: // Stadt gestalten - EUR 200 Bonus
                Debug.Log("Action Card 6: Player receives 200€");
                gameManager.actionManager.AddMoney(200);
                break;

            case 7: // Gesetzliche Auflagen - Setze eine Runde aus
                Debug.Log("Action Card 7: Player skips next turn");
                gameManager.actionManager.SkipTurn();
                break;

            case 8: // Stadt Graz Praktikum - Noch einmal würfeln
                Debug.Log("Action Card 8: Player can roll again");
                lastCardWasRollAgain = true;
                gameManager.actionManager.RollAgain();
                break;

            default:
                Debug.LogWarning($"Action Card #{cardId}: No action implemented yet.");
                break;
        }
    }

}