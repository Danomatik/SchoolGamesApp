# Testbericht - SchoolGames Unity Projekt

## 1. Einleitung

Dieses Dokument beschreibt die implementierten automatisierten Tests für das SchoolGames Unity-Projekt. Die Tests wurden mit dem Unity Test Framework entwickelt, welches auf NUnit basiert. Ziel der Tests ist es, die Kernfunktionalitäten des Spiels zu verifizieren und Regressionen bei Codeänderungen frühzeitig zu erkennen.

Das Projekt verwendet zwei Testarten:
- **EditMode Tests** (Unit Tests): Schnelle Tests, die ohne Unity-Laufzeitumgebung ausgeführt werden
- **PlayMode Tests** (Integrationstests): Tests, die eine vollständige Unity-Szene laden und das Zusammenspiel der Komponenten prüfen

---

## 2. Testabdeckung im Überblick

| Kategorie | Anzahl Tests | Testtyp |
|-----------|-------------|---------|
| PlayerData | 4 | EditMode |
| GameState | 4 | EditMode |
| Geldberechnungen | 8 | EditMode |
| Würfelberechnungen | 6 | EditMode |
| Unternehmens-Level | 6 | EditMode |
| Timer-Berechnungen | 8 | EditMode |
| Spieler-Setup | 14 | EditMode |
| Speicherdaten | 6 | EditMode |
| MoneyManager | 9 | PlayMode |
| Insolvenz | 5 | PlayMode |
| Spieltimer | 5 | PlayMode |
| Spielstand speichern | 6 | PlayMode |
| Fragenmanager | 7 | PlayMode |
| ActionManager | 7 | PlayMode |
| Aktionskarten | 5 | PlayMode |
| Bankkarten | 5 | PlayMode |

**Gesamt: 105 Tests** (56 EditMode, 49 PlayMode)

---

## 3. EditMode Tests (Unit Tests)

Die EditMode Tests prüfen isolierte Logik ohne Unity-Komponenten. Sie laufen im Editor und benötigen keine geladene Szene, was sie besonders schnell macht (typischerweise unter 1 Sekunde pro Test).

### 3.1 PlayerData Tests

Die `PlayerDataTests` Klasse prüft die grundlegende Spielerdatenstruktur:

```csharp
[Test]
public void PlayerData_Initialization_DefaultValues()
{
    PlayerData player = new PlayerData();
    
    Assert.AreEqual(0, player.PlayerID);
    Assert.AreEqual(0, player.Money);
    Assert.IsNotNull(player.companies);
}
```

**Getestete Szenarien:**
- Standardwerte bei Initialisierung (PlayerID, Money, BoardPosition alle auf 0)
- Initialisierung mit benutzerdefinierten Werten
- Hinzufügen und Entfernen von Unternehmen aus der companies-Liste
- Änderung des Eliminierungsstatus

Diese Tests stellen sicher, dass die `PlayerData`-Klasse konsistent initialisiert wird und die Unternehmensliste korrekt verwaltet wird.

### 3.2 GameState Tests

Die `GameStateTests` prüfen die Spielzustandsverwaltung:

**Getestete Szenarien:**
- Leere Spielerliste bei Initialisierung
- Hinzufügen mehrerer Spieler
- Korrekte Verwaltung des aktuellen Spielerzugs (CurrentPlayerTurnID)
- Suche nach Spielern anhand der ID

### 3.3 Geldberechnungen

Die `MoneyCalculationsTests` testen die Finanzlogik des Spiels ohne Unity-Abhängigkeiten:

```csharp
[Test]
public void CalculateTotalAssets_WithCompanies()
{
    PlayerData player = new PlayerData
    {
        Money = 500,
        companies = new List<int> { 1, 2 }
    };
    
    int company1Value = 200 / 2; // 100€ (50% der Gründungskosten)
    int company2Value = 300 / 2; // 150€
    int totalAssets = player.Money + company1Value + company2Value;
    
    Assert.AreEqual(750, totalAssets);
}
```

**Getestete Szenarien:**
- Vermögensberechnung nur mit Bargeld
- Vermögensberechnung mit Unternehmen (berücksichtigt Gründungskosten)
- Zahlungsfähigkeit mit ausreichend Bargeld
- Zahlungsfähigkeit mit Gesamtvermögen (Bargeld + Unternehmenswerte)
- Versteigerungspreis (50% der Gründungskosten)
- Berechnung nach Versteigerung und Zahlung

Die 50%-Regel für Versteigerungen ist ein zentrales Spielelement: Wenn ein Spieler zahlungsunfähig ist, kann er Unternehmen für die Hälfte der ursprünglichen Gründungskosten versteigern.

### 3.4 Würfelberechnungen

Die `DiceCalculationsTests` prüfen die Würfelmechanik:

**Getestete Szenarien:**
- Gültiger Wertebereich (1-6 pro Würfel)
- Alle möglichen Würfelkombinationen
- Bewegungsberechnung basierend auf Würfelergebnis
- Wrap-Around bei Überschreiten des Spielfelds (Position 40 → Position 0)
- Startfeld-Bonus bei Überqueren

### 3.5 Timer-Berechnungen

Die `TimerCalculationsTests` verifizieren die Spielzeit-Logik:

```csharp
[Test]
public void Timer_FormatTime_MMSS()
{
    float seconds = 125f; // 2:05
    
    int minutes = (int)(seconds / 60);
    int secs = (int)(seconds % 60);
    string formatted = $"{minutes:00}:{secs:00}";
    
    Assert.AreEqual("02:05", formatted);
}
```

**Getestete Szenarien:**
- Umrechnung Minuten ↔ Sekunden
- Zeitformatierung (MM:SS)
- Formatierung bei 0 Sekunden
- Maximale Spieldauer (30 Minuten)
- Countdown-Reduktion
- Min/Max-Grenzen der Spieldauer

### 3.6 PlayerSetupManager Tests

Die `PlayerSetupManagerTests` prüfen die Spielereinrichtung über PlayerPrefs:

**Getestete Szenarien:**
- Speichern und Laden der Spieleranzahl
- Standardwert wenn keine Daten vorhanden
- Speichern und Laden von Spielernamen
- Behandlung von leeren Strings und Null-Werten
- Abrufen aller Spielernamen
- Speichern der Spieldauer
- Begrenzung auf 0-30 Minuten
- Prüfung ob Spielerdaten vorhanden sind
- Löschen aller Spielerdaten

### 3.7 GameSaveData Tests

Die `GameSaveDataTests` prüfen die Serialisierungsstrukturen:

**Getestete Szenarien:**
- Standardwerte bei Initialisierung
- Korrekte Speicherung von Spielerdaten
- Speicherung von Unternehmensfeld-Daten
- JSON-Serialisierung mit JsonUtility
- Unternehmensliste bei PlayerSaveData

---

## 4. PlayMode Tests (Integrationstests)

Die PlayMode Tests laden die vollständige MainScene und testen das Zusammenspiel der Unity-Komponenten. Sie sind langsamer (2-5 Sekunden pro Test), aber prüfen realistische Spielszenarien.

### 4.1 Setup und TearDown

Jeder PlayMode Test beginnt mit dem Laden der MainScene und dem Finden der benötigten Manager:

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    SceneManager.LoadScene("MainScene");
    yield return new WaitForSeconds(2f);
    
    gm = Object.FindFirstObjectByType<GameManager>();
    gi = Object.FindFirstObjectByType<GameInitiator>();
    moneyManager = Object.FindFirstObjectByType<MoneyManager>();
    
    // Warte bis Initiative abgeschlossen ist
    yield return new WaitUntil(() => !gm.InitiativeInProgress);
}
```

Das Warten auf `InitiativeInProgress == false` ist wichtig, da am Spielbeginn die Zugreihenfolge durch Würfeln ermittelt wird.

### 4.2 MoneyManager Tests

Die `MoneyManagerTests` prüfen das Finanzsystem im laufenden Spiel:

#### Test 1: Spieler ohne Vermögen wird eliminiert

```csharp
[UnityTest]
public IEnumerator Test1_PlayerWithNoAssets_GetsEliminated()
{
    var player = gi.CurrentGame.AllPlayers[0];
    player.Money = 0;
    player.companies.Clear();
    
    LogAssert.Expect(LogType.Error, 
        new Regex(".*ist zahlungsunfähig.*"));
    
    bool canPay = moneyManager.TryPayAmount(player, 500, "Test");
    yield return new WaitForSeconds(1.5f);
    
    Assert.IsTrue(player.isEliminated);
    Assert.IsFalse(gi.CurrentGame.AllPlayers.Contains(player));
}
```

Dieser Test verifiziert die Kernmechanik der Spieler-Eliminierung: Ein Spieler ohne Bargeld und ohne Unternehmen wird aus dem Spiel entfernt, wenn er eine Zahlung leisten muss.

#### Weitere MoneyManager Tests:
- **Test 2:** Spieler mit genug Geld kann zahlen
- **Test 3:** Gesamtvermögensberechnung inklusive Unternehmen
- **Test 4:** Spieler mit Unternehmen löst Insolvenz aus (wird nicht eliminiert)
- **Test 5:** CanAffordPayment prüft korrekt
- **Test 6:** AddMoney erhöht Spielergeld
- **Test 7:** RemoveMoney nur bei ausreichendem Guthaben
- **Test 8:** Eliminierte Spieler geben Unternehmen frei
- **Test 9:** Zahlung an anderen Spieler transferiert Geld korrekt

### 4.3 Insolvenz-Tests (BankruptcyTests)

Die `BankruptcyTests` prüfen die Versteigerungsmechanik:

#### Test 1: Versteigerung bei Insolvenz

```csharp
[UnityTest]
public IEnumerator Test1_Bankruptcy_WithCompanies_CanAuction()
{
    var player = gi.CurrentGame.AllPlayers[0];
    player.Money = 50;
    
    // Gib Spieler ein Unternehmen
    companyFields[0].ownerID = player.PlayerID;
    companyFields[0].level = CompanyLevel.Founded;
    player.companies.Add(companyFields[0].fieldIndex);
    
    // Löse Insolvenz aus
    gm.HandleBankruptcy(player, 300, "Test Payment");
    gm.StartAuctionForCompany(companyFields[0]);
    
    // Prüfe: Spieler erhält 50% der Gründungskosten
    int expectedAuctionPrice = company.costFound / 2;
    Assert.AreEqual(expectedAuctionPrice, player.Money - 50);
}
```

#### Weitere Insolvenz-Tests:
- **Test 2:** Insolvenz ohne Unternehmen beendet den Zug
- **Test 3:** Versteigerung gibt exakt 50% der Gründungskosten
- **Test 4:** Mehrere Versteigerungen lösen Insolvenz auf
- **Test 5:** GetAuctionableCompanies gibt nur eigene Unternehmen zurück

### 4.4 GameTimer Tests

Die `GameTimerTests` prüfen die Spielzeit-Verwaltung:

**Getestete Szenarien:**
- Timer startet mit korrekter Dauer
- Timer stoppt korrekt
- Spielende bei Zeitablauf
- Korrekte Zeitanzeige
- Timer-Verhalten beim Zugwechsel

### 4.5 GameSaveManager Tests

Die `GameSaveManagerTests` prüfen das Speichern und Laden:

**Getestete Szenarien:**
- Spiel wird erfolgreich gespeichert
- Spiel wird erfolgreich geladen
- Unternehmen bleiben nach Laden erhalten
- Aktueller Spielerzug bleibt erhalten
- Mehrere Spieler werden korrekt gespeichert
- Zeitstempel wird gesetzt

### 4.6 QuestionManager Tests

Die `QuestionManagerTests` prüfen das Quiz-System:

**Getestete Szenarien:**
- Zufällige Frage wird zurückgegeben
- Verschiedene Fragen bei mehreren Aufrufen
- Sprachwechsel funktioniert
- Schwierigkeitswechsel funktioniert
- Fragestruktur ist valide
- Quiz-Serie wird initialisiert
- Null bei fehlenden Fragen

### 4.7 ActionManager Tests

Die `ActionManagerTests` prüfen die Spielaktionen:

**Getestete Szenarien:**
- AddMoney erhöht Spielergeld
- AddMoneyAndMove kombiniert Geld und Bewegung
- SkipTurn setzt hasToSkip-Flag
- RollAgain ermöglicht erneutes Würfeln
- ShouldRollAgain gibt korrekten Status zurück
- MovePlayerToField berechnet Schritte korrekt
- MoveToNextCompanyField findet nächstes Unternehmen

### 4.8 Aktions- und Bankkarten Tests

Die `ActionCardManagerTests` und `BankCardManagerTests` prüfen das Kartensystem:

**ActionCard Tests:**
- Karte wird gezogen
- Karte 6 gibt 200€
- Karte 7 überspringt Zug
- Karte 8 ermöglicht erneutes Würfeln
- Karten werden aus JSON geladen

**BankCard Tests:**
- Karte wird gezogen
- Geldeffekt fügt Geld hinzu
- Bewegungseffekt bewegt Spieler
- Roll-Again setzt Flag
- Karten werden aus JSON geladen

---

## 5. Testmuster und Best Practices

### 5.1 Arrange-Act-Assert Pattern

Alle Tests folgen dem AAA-Muster:

```csharp
[Test]
public void TestName_Scenario_ExpectedResult()
{
    // Arrange - Vorbereitung der Testdaten
    int startMoney = 1000;
    
    // Act - Ausführung der zu testenden Aktion
    int result = startMoney + 500;
    
    // Assert - Überprüfung des Ergebnisses
    Assert.AreEqual(1500, result);
}
```

### 5.2 Test-Isolation

Jeder Test ist unabhängig und setzt seine eigenen Daten:

```csharp
// Richtig: Explizit alle Werte setzen
player.Money = 0;
player.companies.Clear();
player.isEliminated = false;

// Falsch: Auf vorherigen Zustand verlassen
// player.Money -= 100; // Unklar, was der Ausgangswert war
```

### 5.3 Asynchrone Wartezeiten

PlayMode Tests verwenden Coroutines für asynchrone Operationen:

```csharp
yield return new WaitForSeconds(1f);  // Feste Wartezeit
yield return new WaitUntil(() => condition);  // Bedingungsbasiert
yield return null;  // Ein Frame warten
```

### 5.4 Log-Assertions

Tests können erwartete Fehler-Logs prüfen:

```csharp
LogAssert.Expect(LogType.Error, 
    new Regex(".*ist zahlungsunfähig.*"));
```

---

## 6. Namenskonventionen

Tests folgen dem Muster: `MethodName_Scenario_ExpectedBehavior`

Beispiele:
- `PlayerData_Initialization_DefaultValues`
- `AddMoney_IncreasesPlayerMoney`
- `CanAffordPayment_WithEnoughCash_ReturnsTrue`
- `PlayerElimination_RemovesFromActiveList`

---

## 7. Tests ausführen

### Im Unity Editor

1. **Window → General → Test Runner** öffnen
2. Tab **EditMode** oder **PlayMode** wählen
3. **Run All** klicken

### Kommandozeile

```bash
Unity -runTests -batchmode -projectPath . -testResults TestResults.xml
```

---

## 8. Bekannte Einschränkungen

1. **PlayMode Tests sind langsam:** Das Laden der MainScene dauert ca. 2 Sekunden pro Test. Bei 49 Tests summiert sich das auf mehrere Minuten.

2. **Initiative-Wartezeit:** Die Würfel-Initiative am Spielbeginn muss abgewartet werden, was zusätzliche Zeit kostet.

3. **UI-Tests:** Einige UI-Elemente (wie Popups) werden nicht vollständig automatisiert getestet, da sie Benutzerinteraktion erfordern.

4. **Netzwerk-Tests:** Multiplayer-Funktionalität ist in den Tests nicht abgedeckt.

---

## 9. Empfehlungen für weitere Tests

1. **Mehr Randfälle testen:** Z.B. was passiert, wenn alle Spieler bis auf einen eliminiert werden?

2. **Performance-Tests:** Prüfen, ob das Spiel bei vielen Unternehmen und langen Spielzeiten stabil bleibt.

3. **UI-Integrationstests:** Automatisierte Tests für UI-Flows mit dem Unity UI Test Framework.

4. **Snapshot-Tests:** Prüfen, ob Speicherstände über Versionen hinweg kompatibel bleiben.

---

## 10. Fazit

Die implementierte Testsuite deckt die Kernfunktionalitäten des SchoolGames-Projekts ab. Die Kombination aus schnellen EditMode Tests für isolierte Logik und gründlichen PlayMode Tests für das Gesamtsystem bietet ein gutes Gleichgewicht zwischen Testgeschwindigkeit und Testtiefe.

Besonders kritische Mechaniken wie Spieler-Eliminierung, Insolvenz-Handling und die 50%-Versteigerungsregel sind durch mehrere Tests abgesichert. Die Verwendung von LogAssert ermöglicht das Testen von Fehlerszenarien, ohne dass Tests aufgrund erwarteter Fehlermeldungen fehlschlagen.

---

*Erstellt für das SchoolGames Unity-Projekt*
*Stand: Januar 2026*
