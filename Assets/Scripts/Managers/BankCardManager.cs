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

    void Awake()
    {
        LoadCards();
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

    public void PrintRandomBankCard()
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("No bank cards loaded.");
            return;
        }
        BankCard picked = cards[Random.Range(0, cards.Count)];
        Debug.Log($"[Bank Card #{picked.id}]: {picked.text}");
    }
}
