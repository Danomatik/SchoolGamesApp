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

public class BankCardManager : MonoBehaviour
{
    private List<BankCard> cards = new List<BankCard>();
    private GameManager gameManager;
    private bool lastCardWasRollAgain = false;

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

    public void ExecuteRandomBankCard()
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("No bank cards loaded.");
            return;
        }
        
        BankCard picked = cards[Random.Range(0, cards.Count)];
        Debug.Log($"[Bank Card #{picked.id}]: {picked.text}");
        
        // Reset roll again flag
        lastCardWasRollAgain = false;
        
        // Execute action based on card ID
        ExecuteBankCardAction(picked.id);
    }

    private void ExecuteBankCardAction(int cardId)
    {
        if (gameManager == null)
        {
            Debug.LogError("BankCardManager: GameManager not found!");
            return;
        }

        // Store the current player ID before any actions
        int currentPlayerID = gameManager.GetCurrentPlayer().PlayerID;
        Debug.Log($"Executing bank card action for Player {currentPlayerID}");

        switch (cardId)
        {
            // Movement cases - move forward
            case 1: // "Rücke 3 Felder vor"
                gameManager.MovePlayer(3);
                break;
            case 2: // "Springe zu einem Unternehmen deiner Wahl"
                gameManager.MovePlayerToField(0); // Go to start for now
                break;
            case 7: // "Springe zu einem Feld deiner Wahl (ausgenommen Start)"
                gameManager.MovePlayerToField(5); // Go to field 5 for now
                break;
            case 12: // "Springe zu einem Unternehmen deiner Wahl"
                gameManager.MovePlayerToField(0); // Go to start for now
                break;
            case 19: // "Du darfst 4 Felder weiterfahren"
                gameManager.MovePlayer(4);
                break;
            case 23: // "Du darfst ein Feld vorrücken"
                gameManager.MovePlayer(1);
                break;
            case 24: // "Du darfst 2 Felder vorrücken"
                gameManager.MovePlayer(2);
                break;
            case 30: // "Springe zu einem Unternehmen deiner Wahl"
                gameManager.MovePlayerToField(0); // Go to start for now
                break;
            case 32: // "Du springst 3 Felder vor"
                gameManager.MovePlayer(3);
                break;
            case 35: // "Springe dafür zum Spielfeld von Siemens"
                gameManager.MovePlayerToField(10); // Go to field 10 for now
                break;
            case 38: // "Rücke 3 Felder vor"
                gameManager.MovePlayer(3);
                break;
            case 44: // "Du darfst zum Spielfeld von Pankl springen"
                gameManager.MovePlayerToField(15); // Go to field 15 for now
                break;
            case 45: // "Du darfst 2 Felder vorrücken"
                gameManager.MovePlayer(2);
                break;
            case 52: // "Rücke vor auf das Feld von Gebrüder Weiss"
                gameManager.MovePlayerToField(20); // Go to field 20 for now
                break;
            case 56: // "Rücke 3 Felder vor"
                gameManager.MovePlayer(3);
                break;
            case 70: // "Rücke 4 Felder vor"
                gameManager.MovePlayer(4);
                break;
            case 72: // "Springe dafür auf das Feld von OMICRON"
                gameManager.MovePlayerToField(25); // Go to field 25 for now
                break;
            case 74: // "Rücke zum Städtebund-Feld vor"
                gameManager.MovePlayerToField(30); // Go to field 30 for now
                break;
            case 77: // "Rücke auf ein beliebiges Feld vor(ausgenommen Start)"
                gameManager.MovePlayerToField(5); // Go to field 5 for now
                break;
            case 79: // "Rücke 2 Felder vor"
                gameManager.MovePlayer(2);
                break;
            case 85: // "Rücke zu deinem nächsten Unternehmen vor"
                gameManager.MovePlayerToField(0); // Go to start for now
                break;

            // Roll again cases
            case 4: // "Du darfst noch einmal würfeln"
            case 5: // "Du darfst noch einmal würfeln"
            case 11: // "Würfle noch einmal"
            case 14: // "Würfle noch einmal"
            case 16: // "Du darfst noch einmal würfeln"
            case 21: // "Würfle noch einmal"
            case 25: // "Du darfst noch einmal würfeln"
            case 27: // "Du darfst noch einmal würfeln"
            case 37: // "Du darfst noch einmal würfeln"
            case 39: // "Du darfst noch einmal würfeln"
            case 43: // "Würfle noch einmal"
            case 46: // "Würfel noch einmal"
            case 49: // "Würfle nochmal"
            case 58: // "Würfle noch einmal"
            case 62: // "Würfle noch einmal"
            case 66: // "Du darfst erneut würfeln"
            case 67: // "Du darfst noch einmal würfeln"
            case 69: // "Würfel noch einmal"
            case 76: // "Würfle noch einmal"
                lastCardWasRollAgain = true;
                gameManager.RollAgain();
                break;

            // Skip turn cases
            case 3: // "Setze dafür eine Runde aus"
            case 9: // "Setze eine Runde aus"
            case 15: // "Setze eine Runde aus"
            case 40: // "Setze daher eine Runde aus"
            case 57: // "Setze eine Runde aus"
            case 60: // "Setze eine Runde aus"
            case 80: // "Setze eine Runde aus"
            case 207: // "Setze einmal aus"
                gameManager.SkipTurn();
                break;

            // Money rewards - player gets money directly
            case 13: // "Du erhältst EUR 200"
            case 17: // "Du erhältst eine Erfolgsprämie von EUR 200"
            case 18: // "Du erhältst eine Prämie von EUR 150"
            case 22: // "Du erhältst eine Prämie von EUR 50"
            case 26: // "Du erhältst einen Zuschuss von EUR 200"
            case 28: // "Du erhältst 150 EUR als Bonus"
            case 31: // "Du erhältst eine finanzielle Unterstützung von EUR 200"
            case 33: // "Du erhältst ... EUR 200 Urlaubsgeld"
            case 34: // "Als Prämie erhältst du EUR 100"
            case 36: // "Du erhältst EUR 250"
            case 41: // "Du kassierst eine Prämie in der Höhe von EUR 100"
            case 47: // "Du bekommst dafür EUR 300 als Führerscheinprämie"
            case 48: // "Du erhältst eine Erfolgsprämie über EUR 300"
            case 55: // "Du erhältst EUR 200"
            case 59: // "Du erhältst eine Prämie von EUR 150"
            case 64: // "Du erhältst EUR 100"
            case 65: // "Als Prämie erhältst du EUR 250"
            case 71: // "Du erhältst EUR 250 als Förderung"
            case 73: // "Du bekommst einen Umweltpreis in der Höhe von EUR 100"
            case 75: // "Das bringt dir einen Bonus von EUR 150"
            case 78: // "Du erhältst einen Bonus von EUR 150"
            case 81: // "Du erhältst EUR 500"
            case 82: // "Du erhältst EUR 100 Prämie"
                gameManager.AddMoneyFromBankCard(GetCardRewardAmount(cardId));
                break;

            // Special cases: Money + roll again
            case 50: // "Du erhältst EUR 500 und darfst noch einmal würfeln"
            case 83: // "Du erhältst EUR 100 und darfst noch einmal würfeln"
                lastCardWasRollAgain = true;
                gameManager.AddMoneyFromBankCard(GetCardRewardAmount(cardId));
                gameManager.RollAgain();
                break;

            // Default case - no movement action
            default:
                Debug.Log($"Bank Card #{cardId}: No movement action implemented yet.");
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
}
