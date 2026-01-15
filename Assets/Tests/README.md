# Unity Test Framework - Anleitung

## Übersicht

Dieses Projekt verwendet **Unity Test Framework** (basierend auf NUnit) für Unit Tests und Integration Tests.

## Test-Struktur

```
Assets/Tests/
├── EditMode/          # Unit Tests (laufen im Editor ohne Play Mode - SCHNELL)
│   ├── PlayerDataTests.cs              # Tests für PlayerData Klasse (4 Tests)
│   ├── GameStateTests.cs               # Tests für GameState Klasse (4 Tests)
│   ├── MoneyCalculationsTests.cs       # Tests für Geld-Berechnungen (8 Tests)
│   ├── DiceCalculationsTests.cs        # Tests für Würfel-Berechnungen (6 Tests)
│   ├── CompanyLevelTests.cs            # Tests für Unternehmen-Level (6 Tests)
│   ├── TimerCalculationsTests.cs       # Tests für Timer-Berechnungen (8 Tests)
│   ├── PlayerSetupManagerTests.cs      # Tests für PlayerSetupManager (14 Tests) ⭐ NEU
│   └── GameSaveDataTests.cs            # Tests für GameSaveData (6 Tests) ⭐ NEU
└── PlayMode/          # Integration Tests (laufen im Play Mode - benötigen Scene)
    ├── MoneyManagerTests.cs            # Tests für MoneyManager (9 Tests)
    ├── BankruptcyTests.cs              # Tests für Insolvenz-Mechanik (5 Tests)
    ├── GameTimerTests.cs               # Tests für GameTimerManager (5 Tests)
    ├── GameSaveManagerTests.cs         # Tests für GameSaveManager (6 Tests)
    ├── QuestionManagerTests.cs         # Tests für QuestionManager (7 Tests)
    ├── ActionManagerTests.cs           # Tests für ActionManager (7 Tests) ⭐ NEU - KRITISCH
    ├── ActionCardManagerTests.cs       # Tests für ActionCardManager (5 Tests) ⭐ NEU - KRITISCH
    └── BankCardManagerTests.cs         # Tests für BankCardManager (5 Tests) ⭐ NEU - KRITISCH
```

## Tests ausführen

### Methode 1: Unity Test Runner Window (EMPFOHLEN)
1. **Window → General → Test Runner** öffnen
   - Oder: **Window → Analysis → Test Runner**
2. Tab **EditMode** oder **PlayMode** wählen
3. **Run All** klicken (oder einzelne Tests mit Rechtsklick → Run)
4. Ergebnisse werden im unteren Bereich angezeigt

### Methode 2: Command Line (für CI/CD)
```bash
Unity -runTests -batchmode -projectPath . -testResults TestResults.xml
```

### Methode 3: Im Code
- Tests haben `[Test]` Attribute für Unit Tests (EditMode)
- Tests haben `[UnityTest]` Attribute für PlayMode Tests

## Test-Typen

### EditMode Tests (Unit Tests) ⚡ SCHNELL
- ✅ Laufen **sehr schnell** im Editor (ohne Play Mode)
- ✅ Testen einzelne Funktionen/Methoden
- ✅ Keine Unity-Komponenten die Start/Update benötigen
- ✅ Ideal für: Datenstrukturen, Berechnungen, Logik
- **Beispiele**: `PlayerDataTests`, `GameStateTests`, `MoneyCalculationsTests`

### PlayMode Tests (Integration Tests) 🎮 VOLLSTÄNDIG
- ⚠️ Laufen im **Play Mode** (benötigen Scene)
- ✅ Testen vollständige Gameplay-Flows
- ✅ Können Coroutines verwenden (`IEnumerator`)
- ✅ Haben Zugriff auf alle Unity-Komponenten
- **Beispiele**: `MoneyManagerTests`, `BankruptcyTests`

## Verfügbare Tests

### EditMode Tests (42 Tests)

#### PlayerDataTests (4 Tests)
- `PlayerData_Initialization_DefaultValues()` - Prüft Standardwerte
- `PlayerData_Initialization_WithValues()` - Prüft Initialisierung mit Werten
- `PlayerData_Companies_AddRemove()` - Testet Unternehmen-Liste
- `PlayerData_Elimination_State()` - Testet Eliminierungs-Status

#### GameStateTests (4 Tests)
- `GameState_Initialization_DefaultValues()` - Prüft Standardwerte
- `GameState_AddPlayers()` - Testet Spieler hinzufügen
- `GameState_CurrentPlayerTurnID_Changes()` - Testet Zug-Wechsel
- `GameState_FindPlayerByID()` - Testet Spieler-Suche

#### MoneyCalculationsTests (8 Tests)
- `CalculateTotalAssets_OnlyCash()` - Nur Bargeld
- `CalculateTotalAssets_WithCompanies()` - Mit Unternehmen
- `CanAffordPayment_WithEnoughCash()` - Zahlungsfähigkeit mit Geld
- `CanAffordPayment_WithInsufficientCash_ButEnoughAssets()` - Zahlungsfähigkeit mit Vermögen
- `AuctionPrice_Is50PercentOfFoundationCost()` - Versteigerungspreis
- `AuctionPrice_MultipleCompanies()` - Mehrere Versteigerungen
- `PaymentCalculation_AfterAuction()` - Zahlung nach Versteigerung

#### DiceCalculationsTests (6 Tests) ⭐ NEU
- `DiceRoll_ValueRange_IsValid()` - Würfelwert-Bereich
- `DiceRoll_AllPossibleValues()` - Alle möglichen Werte
- `DiceRoll_Probability_AllValuesPossible()` - Wahrscheinlichkeiten
- `DiceRoll_MovementCalculation()` - Bewegungs-Berechnung
- `DiceRoll_MovementCalculation_WithWrapAround()` - Bewegungs-Berechnung mit Wrap-Around
- `DiceRoll_StartFieldBonus_Calculation()` - Start-Feld Bonus

#### CompanyLevelTests (6 Tests) ⭐ NEU
- `CompanyLevel_UpgradePath_IsValid()` - Upgrade-Pfad
- `CompanyLevel_UpgradeCost_Calculation()` - Upgrade-Kosten
- `CompanyLevel_AuctionPrice_Is50Percent()` - Versteigerungspreis
- `CompanyLevel_UpgradeSequence()` - Upgrade-Sequenz
- `CompanyLevel_RentCalculation_ByLevel()` - Miete nach Level
- `CompanyLevel_Ownership_Transfer()` - Besitz-Übertragung

#### TimerCalculationsTests (8 Tests) ⭐ NEU
- `Timer_ConvertMinutesToSeconds()` - Minuten zu Sekunden
- `Timer_ConvertSecondsToMinutes()` - Sekunden zu Minuten
- `Timer_FormatTime_MMSS()` - Zeit-Formatierung
- `Timer_FormatTime_Zero()` - Zeit bei 0
- `Timer_FormatTime_MaxDuration()` - Maximale Dauer
- `Timer_Countdown_Decreases()` - Countdown reduziert
- `Timer_Countdown_ReachesZero()` - Countdown erreicht 0
- `Timer_GameDuration_MinMax()` - Min/Max Dauer

#### PlayerSetupManagerTests (14 Tests) ⭐ NEU
- `PlayerSetupManager_SetPlayerCount_SavesCorrectly()` - Spieleranzahl speichern
- `PlayerSetupManager_GetPlayerCount_ReturnsDefaultWhenNotSet()` - Standard-Wert
- `PlayerSetupManager_SetPlayerName_SavesCorrectly()` - Spielername speichern
- `PlayerSetupManager_GetPlayerName_ReturnsDefaultWhenNotSet()` - Standard-Name
- `PlayerSetupManager_SetPlayerName_HandlesEmptyString()` - Leerer String
- `PlayerSetupManager_SetPlayerName_HandlesNull()` - Null-Handling
- `PlayerSetupManager_GetAllPlayerNames_ReturnsCorrectNames()` - Alle Namen
- `PlayerSetupManager_SetGameDuration_SavesCorrectly()` - Spiel-Dauer speichern
- `PlayerSetupManager_GetGameDuration_ReturnsDefaultWhenNotSet()` - Standard-Dauer
- `PlayerSetupManager_GetGameDuration_ClampsToMax30Minutes()` - Max-Begrenzung
- `PlayerSetupManager_GetGameDuration_ClampsToMin0Minutes()` - Min-Begrenzung
- `PlayerSetupManager_HasPlayerData_ReturnsFalseWhenEmpty()` - Daten-Prüfung
- `PlayerSetupManager_HasPlayerData_ReturnsTrueWhenDataExists()` - Daten vorhanden
- `PlayerSetupManager_ClearPlayerData_RemovesAllData()` - Daten löschen
- `PlayerSetupManager_MultiplePlayers_SavesAllCorrectly()` - Mehrere Spieler

#### GameSaveDataTests (6 Tests) ⭐ NEU
- `GameSaveData_Initialization_DefaultValues()` - Standardwerte
- `PlayerSaveData_Initialization_DefaultValues()` - Spieler-Daten Standardwerte
- `GameSaveData_AddPlayers_StoresCorrectly()` - Spieler hinzufügen
- `GameSaveData_AddCompanyFields_StoresCorrectly()` - Unternehmen-Felder
- `GameSaveData_Serialization_WorksWithJsonUtility()` - JSON Serialisierung
- `PlayerSaveData_Companies_AddRemove()` - Unternehmen-Liste

### PlayMode Tests (49 Tests)

#### MoneyManagerTests (9 Tests)
- `Test1_PlayerWithNoAssets_GetsEliminated()` - Eliminierung ohne Vermögen
- `Test2_PlayerWithEnoughMoney_CanPay()` - Zahlung mit genug Geld
- `Test3_CalculateTotalAssets_IncludesCompanies()` - Gesamtwert-Berechnung
- `Test4_PlayerWithCompanies_TriggersInsolvency_NotEliminated()` - Insolvenz mit Unternehmen
- `Test5_CanAffordPayment_ChecksCorrectly()` - Zahlungsfähigkeit prüfen
- `Test6_AddMoney_IncreasesPlayerMoney()` - Geld hinzufügen
- `Test7_RemoveMoney_OnlyWorksWithSufficientFunds()` - Geld entfernen
- `Test8_EliminatedPlayer_ReleasesCompanies()` - Unternehmen freigeben
- `Test9_TryPayAmount_WithRecipient_TransfersMoney()` - Zahlung an anderen Spieler

#### BankruptcyTests (5 Tests)
- `Test1_Bankruptcy_WithCompanies_CanAuction()` - Versteigerung bei Insolvenz
- `Test2_Bankruptcy_NoCompanies_EndsTurn()` - Insolvenz ohne Unternehmen
- `Test3_Auction_Gives50Percent()` - Versteigerungspreis (50%)
- `Test4_MultipleAuctions_ResolvesBankruptcy()` - Mehrere Versteigerungen
- `Test5_GetAuctionableCompanies_OnlyOwned()` - Versteigerbare Unternehmen

#### GameTimerTests (5 Tests) ⭐ NEU
- `Test1_Timer_StartsCorrectly()` - Timer startet korrekt
- `Test2_Timer_StopsCorrectly()` - Timer stoppt korrekt
- `Test3_Timer_EndsGame_WhenTimeRunsOut()` - Spielende bei Zeitablauf
- `Test4_Timer_DisplaysCorrectTime()` - Korrekte Zeit-Anzeige
- `Test5_Timer_ResetsOnTurnChange()` - Timer-Reset beim Zugwechsel

#### GameSaveManagerTests (6 Tests) ⭐ NEU
- `Test1_SaveGame_SavesSuccessfully()` - Spiel speichern
- `Test2_LoadGame_LoadsSuccessfully()` - Spiel laden
- `Test3_SaveLoad_CompaniesArePreserved()` - Unternehmen werden gespeichert
- `Test4_SaveLoad_CurrentTurnIsPreserved()` - Aktueller Zug wird gespeichert
- `Test5_SaveLoad_MultiplePlayersArePreserved()` - Mehrere Spieler werden gespeichert
- `Test6_SaveGame_SetsTimestamp()` - Timestamp wird gesetzt

#### QuestionManagerTests (7 Tests) ⭐ NEU
- `Test1_GetRandomQuestion_ReturnsQuestion()` - Zufällige Frage
- `Test2_GetRandomQuestion_ReturnsDifferentQuestions()` - Verschiedene Fragen
- `Test3_SetLanguage_ChangesLanguage()` - Sprache ändern
- `Test4_SetDifficulty_ChangesDifficulty()` - Schwierigkeit ändern
- `Test5_QuestionData_StructureIsValid()` - Frage-Struktur
- `Test6_StartQuizSeries_InitializesSeries()` - Quiz-Serie initialisieren
- `Test7_GetRandomQuestion_ReturnsNullWhenNoQuestions()` - Null bei fehlenden Fragen

#### ActionManagerTests (7 Tests) ⭐ NEU - KRITISCH
- `Test1_AddMoney_IncreasesPlayerMoney()` - Geld hinzufügen
- `Test2_AddMoneyAndMove_IncreasesPlayerMoney()` - Geld hinzufügen (mit Bewegung)
- `Test3_SkipTurn_SetsHasToSkipFlag()` - Zug aussetzen
- `Test4_RollAgain_SetsRollAgainFlag()` - Nochmal würfeln
- `Test5_ShouldRollAgain_ReturnsCorrectStatus()` - Roll Again Status prüfen
- `Test6_MovePlayerToField_CalculatesStepsCorrectly()` - Bewegung zu Feld
- `Test7_MoveToNextCompanyField_FindsNextCompany()` - Bewegung zum nächsten Unternehmen

#### ActionCardManagerTests (5 Tests) ⭐ NEU - KRITISCH
- `Test1_ShowRandomActionCard_DrawsCard()` - Karte ziehen
- `Test2_ActionCard6_Adds200Money()` - Action Card 6 (Geld)
- `Test3_ActionCard7_SkipsTurn()` - Action Card 7 (Skip Turn)
- `Test4_ActionCard8_RollsAgain()` - Action Card 8 (Roll Again)
- `Test5_ActionCards_AreLoaded()` - Karten werden geladen

#### BankCardManagerTests (5 Tests) ⭐ NEU - KRITISCH
- `Test1_ShowRandomBankCard_DrawsCard()` - Karte ziehen
- `Test2_BankCard_MoneyEffect_AddsMoney()` - Bank Card mit Geld-Effekt
- `Test3_BankCard_MovementEffect_MovesPlayer()` - Bank Card mit Bewegung
- `Test4_BankCard_RollAgain_SetsFlag()` - Bank Card mit Roll Again
- `Test5_BankCards_AreLoaded()` - Karten werden geladen

## Neue Tests hinzufügen

### EditMode Test erstellen
1. Erstelle neue Datei in `Assets/Tests/EditMode/`
2. Verwende `[Test]` Attribute
3. Beispiel:
   ```csharp
   using NUnit.Framework;
   
   public class MyClassTests
   {
       [Test]
       public void MyMethod_WithInput_ReturnsExpected()
       {
           // Arrange
           var input = 5;
           
           // Act
           var result = MyMethod(input);
           
           // Assert
           Assert.AreEqual(10, result);
       }
   }
   ```

### PlayMode Test erstellen
1. Erstelle neue Datei in `Assets/Tests/PlayMode/`
2. Verwende `[UnityTest]` Attribute (gibt `IEnumerator` zurück)
3. Beispiel:
   ```csharp
   using UnityEngine;
   using UnityEngine.TestTools;
   using System.Collections;
   
   public class MyPlayModeTests
   {
       [UnitySetUp]
       public IEnumerator SetUp()
       {
           // Wird vor jedem Test ausgeführt
           yield return null;
       }
       
       [UnityTest]
       public IEnumerator MyTest_Scenario_ExpectedResult()
       {
           // Arrange
           // Act
           yield return new WaitForSeconds(1f);
           // Assert
           Assert.IsTrue(true);
       }
   }
   ```

## Best Practices

- **Arrange-Act-Assert Pattern**: Klare Struktur in jedem Test
- **Isolierte Tests**: Jeder Test sollte unabhängig sein
- **Aussagekräftige Namen**: Test-Namen sollten klar sein was getestet wird
- **SetUp/TearDown**: Verwende `[SetUp]` und `[TearDown]` für gemeinsame Initialisierung

## Weitere Ressourcen

- [Unity Test Framework Dokumentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Dokumentation](https://docs.nunit.org/)

