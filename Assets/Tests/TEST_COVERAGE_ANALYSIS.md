# Test-Abdeckungs-Analyse

## ✅ Bereits getestet (44 Tests)

### EditMode Tests (25 Tests)
- ✅ **PlayerData** - Datenstruktur (4 Tests)
- ✅ **GameState** - Spielzustand (4 Tests)
- ✅ **MoneyCalculations** - Geld-Berechnungen (8 Tests)
- ✅ **DiceCalculations** - Würfel-Logik (6 Tests)
- ✅ **CompanyLevel** - Unternehmen-Level (6 Tests)
- ✅ **TimerCalculations** - Timer-Berechnungen (8 Tests)

### PlayMode Tests (19 Tests)
- ✅ **MoneyManager** - Geld-Management (9 Tests)
- ✅ **Bankruptcy** - Insolvenz-Mechanik (5 Tests)
- ✅ **GameTimer** - Timer-Management (5 Tests)

---

## ⚠️ WICHTIGE Bereiche die noch NICHT getestet sind

### 🔴 KRITISCH (sollten getestet werden)

#### 1. **GameSaveManager** - Save/Load System
**Warum wichtig:** 
- Spielstände müssen korrekt gespeichert und geladen werden
- Datenintegrität ist kritisch
- Fehler führen zu Frustration der Spieler

**Was testen:**
- ✅ Spiel speichern (alle Spielerdaten, Unternehmen, Positionen)
- ✅ Spiel laden (Daten korrekt wiederherstellen)
- ✅ Mehrere Spielstände
- ✅ Fehlerbehandlung (Datei nicht gefunden, ungültige Daten)

**Schwierigkeit:** ⭐⭐ (Mittel - benötigt File I/O)

---

#### 2. **QuestionManager** - Quiz-System
**Warum wichtig:**
- Quiz ist Kernmechanismus für Unternehmens-Kauf/Upgrade
- Falsche Antworten = Spieler kann nicht kaufen
- Fragen müssen korrekt geladen und angezeigt werden

**Was testen:**
- ✅ Fragen laden (verschiedene Kategorien: Gründung, Investition, AG)
- ✅ Quiz-Serie (3 Fragen für AG-Upgrade)
- ✅ Antwort-Validierung (richtig/falsch)
- ✅ Quiz-Ergebnis-Callback

**Schwierigkeit:** ⭐⭐⭐ (Schwer - benötigt UI und JSON-Daten)

---

#### 3. **PlayerSetupManager** - Spieler-Setup
**Warum wichtig:**
- Spieleranzahl und Namen müssen korrekt gespeichert werden
- Fehler führen zu falscher Spieleranzahl im Spiel

**Was testen:**
- ✅ Spieleranzahl speichern/laden
- ✅ Spielernamen speichern/laden
- ✅ Spiel-Dauer speichern/laden
- ✅ Standard-Werte (Fallback)

**Schwierigkeit:** ⭐ (Einfach - nur PlayerPrefs)

---

### 🟡 WICHTIG (könnten getestet werden)

#### 4. **ActionCardManager** - Aktionskarten
**Warum wichtig:**
- Aktionskarten beeinflussen Spielverlauf
- Verschiedene Karten-Typen (Bewegung, Geld, Skip Turn)

**Was testen:**
- ✅ Karten ziehen (zufällig, keine Duplikate)
- ✅ Karten-Typen (Bewegung, Geld, etc.)
- ✅ Karten-Effekte (Geld hinzufügen, Position ändern)

**Schwierigkeit:** ⭐⭐ (Mittel)

---

#### 5. **BankCardManager** - Bankkarten
**Warum wichtig:**
- Bankkarten geben Geld oder andere Effekte
- Muss korrekt funktionieren

**Was testen:**
- ✅ Karten ziehen
- ✅ Geld-Effekte
- ✅ Karten-Stack (keine Duplikate)

**Schwierigkeit:** ⭐⭐ (Mittel)

---

#### 6. **PlayerMovement** - Spieler-Bewegung
**Warum wichtig:**
- Spieler muss korrekt über das Spielfeld bewegt werden
- Position muss mit BoardPosition synchronisiert sein

**Was testen:**
- ✅ Bewegung basierend auf Würfelwert
- ✅ Wrap-Around (Feld 39 → Feld 0)
- ✅ Start-Feld Bonus (bei Überquerung)

**Schwierigkeit:** ⭐⭐⭐ (Schwer - benötigt Scene und Animationen)

---

### 🟢 OPTIONAL (nice to have)

#### 7. **CompanyField** - Feld-Logik
**Warum wichtig:**
- Unternehmen-Felder müssen korrekt funktionieren
- Owner, Level müssen korrekt gesetzt werden

**Was testen:**
- ✅ Feld-Initialisierung
- ✅ Owner-Zuweisung
- ✅ Level-Upgrade

**Schwierigkeit:** ⭐ (Einfach)

---

#### 8. **UIManager** - UI-Funktionalität
**Warum wichtig:**
- UI muss korrekt anzeigen/verstecken
- Panels müssen funktionieren

**Was testen:**
- ✅ Panel anzeigen/verstecken
- ✅ Geld-Anzeige aktualisieren
- ✅ Timer-Anzeige aktualisieren

**Schwierigkeit:** ⭐⭐⭐ (Schwer - benötigt UI-Setup)

---

## 📊 Test-Abdeckungs-Übersicht

| System | Status | Priorität | Schwierigkeit |
|--------|--------|-----------|---------------|
| PlayerData | ✅ Getestet | - | - |
| GameState | ✅ Getestet | - | - |
| MoneyManager | ✅ Getestet | - | - |
| Bankruptcy | ✅ Getestet | - | - |
| GameTimer | ✅ Getestet | - | - |
| **GameSaveManager** | ❌ Nicht getestet | 🔴 KRITISCH | ⭐⭐ |
| **QuestionManager** | ❌ Nicht getestet | 🔴 KRITISCH | ⭐⭐⭐ |
| **PlayerSetupManager** | ❌ Nicht getestet | 🔴 KRITISCH | ⭐ |
| ActionCardManager | ❌ Nicht getestet | 🟡 WICHTIG | ⭐⭐ |
| BankCardManager | ❌ Nicht getestet | 🟡 WICHTIG | ⭐⭐ |
| PlayerMovement | ❌ Nicht getestet | 🟡 WICHTIG | ⭐⭐⭐ |
| CompanyField | ❌ Nicht getestet | 🟢 OPTIONAL | ⭐ |
| UIManager | ❌ Nicht getestet | 🟢 OPTIONAL | ⭐⭐⭐ |

---

## 🎯 Empfehlung

### Minimum (für gute Abdeckung):
1. ✅ **GameSaveManager Tests** - Save/Load ist kritisch
2. ✅ **PlayerSetupManager Tests** - Einfach, aber wichtig
3. ✅ **QuestionManager Tests** - Quiz ist Kernmechanismus

### Ideal (für sehr gute Abdeckung):
+ **ActionCardManager Tests**
+ **BankCardManager Tests**

### Optional (für perfekte Abdeckung):
+ **PlayerMovement Tests** (schwer zu testen)
+ **UIManager Tests** (schwer zu testen)

---

## 📈 Aktuelle Test-Abdeckung

**Geschätzte Abdeckung:** ~60-70%

**Mit empfohlenen Tests:** ~85-90%

**Mit allen Tests:** ~95%
