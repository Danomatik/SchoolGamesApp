using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.AI;
using System.Linq; 

public class GameInitiator : MonoBehaviour
{
    public GameState CurrentGame;

    public CompanyConfigCollection companyConfigs;

    public FieldType[] boardLayout = new FieldType[40];

    private List<CompanyField> companyFields = new List<CompanyField>();





    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        LoadCompanyConfigs();

        CurrentGame = new GameState();

        // Initialize board layout - all fields are Company by default
        InitializeBoardLayout();     // <-- ZUERST das Layout setzen
        InitializeCompanyFields();   // <-- DANN die companyFields daraus bauen


        // Spieler 1
        PlayerData Player1 = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0, PlayerName = "Hanx", hasToSkip = false, companies = new List<int>()};
        CurrentGame.AllPlayers.Add(Player1);

        // Spieler 2
        PlayerData Player2 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0, PlayerName = "Momo", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player2);

        // Spieler 3
        PlayerData Player3 = new PlayerData { PlayerID = 3, Money = 2500, BoardPosition = 0, PlayerName = "Simoan", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player3);

        // Spieler 4
        PlayerData Player4 = new PlayerData { PlayerID = 4, Money = 2500, BoardPosition = 0, PlayerName = "Chidi", hasToSkip = false, companies = new List<int>()};
        CurrentGame.AllPlayers.Add(Player4);

        // Spieler 5
        PlayerData Player5 = new PlayerData { PlayerID = 5, Money = 2500, BoardPosition = 0, PlayerName = "Dan", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player5);

        // Spieler 6
        PlayerData Player6 = new PlayerData { PlayerID = 6, Money = 2500, BoardPosition = 0, PlayerName = "Mußbacher", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player6);

        Debug.Log("Neues Spiel gestartet!");

    }

    void LoadCompanyConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Companies");
        if (jsonFile == null)
        {
            Debug.LogError("Schoolgames_Companies.json nicht in Assets/Resources/Data/ gefunden!");
            companyConfigs = new CompanyConfigCollection { companies = new List<CompanyConfigData>() };
            return;
        }

        companyConfigs = JsonUtility.FromJson<CompanyConfigCollection>(jsonFile.text);
        if (companyConfigs?.companies == null)
            companyConfigs = new CompanyConfigCollection { companies = new List<CompanyConfigData>() };

        Debug.Log($"Companies geladen: {companyConfigs.companies.Count}");
    }
    
    void InitializeCompanyFields()
    {
        companyFields.Clear();

        if (companyConfigs?.companies == null || companyConfigs.companies.Count == 0)
        {
            Debug.LogError("Kein Unternehmen in JSON gefunden!");
            return;
        }

        // Create a dictionary to quickly lookup companies by ID
        var companyDict = companyConfigs.companies.ToDictionary(c => c.companyID);
        Debug.Log($"Companies in JSON: {string.Join(", ", companyConfigs.companies.Select(c => $"ID:{c.companyID}"))}");

        // Iterate through board layout
        for (int i = 0; i < boardLayout.Length; i++)
        {
            // Only process Company fields (skip Start, Bank, etc.)
            if (boardLayout[i] == FieldType.Company)
            {
                // Check if there's a company for this field index
                if (companyDict.ContainsKey(i))
                {
                    var company = companyDict[i];
                    companyFields.Add(new CompanyField
                    {
                        fieldIndex = i,
                        companyID = company.companyID,
                        ownerID = -1,
                        level = CompanyLevel.None
                    });
                    Debug.Log($"Company '{company.companyName}' (ID: {company.companyID}) assigned to field {i}");
                }
                else
                {
                    // This is a Company field but no company assigned
                    Debug.LogWarning($"Field {i} is Company type but no company found in JSON for ID {i}");
                }
            }
            else
            {
                Debug.Log($"Field {i} is {boardLayout[i]} (not Company)");
            }
        }

        Debug.Log($"Total company fields created: {companyFields.Count}");
    }

    private void InitializeBoardLayout()
    {
        // Set all fields to Bank by default
        for (int i = 0; i < boardLayout.Length; i++)
        {
            boardLayout[i] = FieldType.Company;
        }

        // Corner fields (Start)
        // boardLayout[0] = FieldType.Start;
        // boardLayout[10] = FieldType.Start;
        // boardLayout[20] = FieldType.Start;
        // boardLayout[30] = FieldType.Start;

        // Bank fields (fields without companies in JSON): 5, 7, 13, 23, 27, 37
        boardLayout[5] = FieldType.Bank;
        boardLayout[7] = FieldType.Bank;
        boardLayout[13] = FieldType.Bank;
        boardLayout[23] = FieldType.Bank;
        boardLayout[27] = FieldType.Bank;
        boardLayout[37] = FieldType.Bank;
    }
    
     public List<CompanyField> GetCompanyFields()
    {
        return companyFields;
    }
}