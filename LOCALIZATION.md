# System lokalizacji / Localization System

## Konfiguracja / Configuration

Język aplikacji jest ustawiany w pliku `appsettings.json`:

```json
"AppSettings": {
  "DefaultLanguage": "pl"  // "pl" dla polskiego, "en" dla angielskiego
}
```

## Struktura aplikacji / Application Structure

Aplikacja zawiera **tylko jedną stronę**: **Ewaluacja strategii NS** (`StrategyEval`), która jest jednocześnie stroną główną.

- **URL:** http://localhost:5010 → automatycznie przekierowuje do `/StrategyEval/Index`
- **Menu:** Zawiera tylko tytuł aplikacji (bez dodatkowych linków)
- **Routing:** Domyślny kontroler to `StrategyEval`

## Dostępne języki / Available Languages

- **pl** - Polski (domyślny)
- **en** - English

## Zmiana języka / Changing Language

### Opcja 1: Edycja appsettings.json

Otwórz `RozgrywkaKoncowa\appsettings.json` i zmień wartość:

```json
"DefaultLanguage": "en"  // Dla angielskiego
```

### Opcja 2: Zmienna środowiskowa (opcjonalnie)

Możesz również użyć zmiennej środowiskowej:

```powershell
$env:AppSettings__DefaultLanguage = "en"
dotnet run
```

## Struktura systemu / System Structure

### 1. Plik zasobów: `Resources\Strings.cs`

Zawiera wszystkie teksty w obu językach w słowniku:

```csharp
["pl"] = new Dictionary<string, string>
{
	["AppTitle"] = "Rozgrywka Końcowa",
	["Calculate"] = "Oblicz",
	...
}
```

### 2. Użycie w kontrolerach

```csharp
using RozgrywkaKoncowa.Resources;
...
ViewBag.Error = Strings.Get("ErrorInvalidCardsServer");
ViewBag.Error = Strings.Get("ErrorDuplicatesNSServer", cardsList); // z parametrem
```

### 3. Użycie w widokach

```razor
<h2>@Strings.Get("MenuStrategyEval")</h2>
<label>@Strings.Get("FormCardsNorth")</label>
<button>@Strings.Get("Calculate")</button>
```

### 4. Użycie w JavaScript

```javascript
var errorMsgs = {
	invalidCards: '@Html.Raw(Strings.Get("ErrorInvalidCards"))',
	emptyField: '@Html.Raw(Strings.Get("ErrorEmptyField"))'
};

document.getElementById('error').innerText = errorMsgs.invalidCards;
```

## Dodawanie nowych tekstów / Adding New Texts

1. Otwórz `Resources\Strings.cs`
2. Dodaj nowy klucz do obu słowników (pl i en):

```csharp
["pl"] = new Dictionary<string, string>
{
	...
	["NowyKlucz"] = "Polski tekst",
}

["en"] = new Dictionary<string, string>
{
	...
	["NowyKlucz"] = "English text",
}
```

3. Użyj w kodzie:

```csharp
var text = Strings.Get("NowyKlucz");
```

## Lista kluczy / Key List

### Wspólne / Common
- `AppTitle` - Tytuł aplikacji
- `Calculate` - Przycisk oblicz
- `Calculating` - Status obliczania

### Menu
- `MenuHome` - Strona główna
- `MenuStrategyEval` - Ewaluacja strategii NS
- `MenuStrategyTest` - Test strategii

### Formularz / Form
- `FormCardsNorth` - Etykieta kart North
- `FormCardsSouth` - Etykieta kart South
- `FormTargetTricks` - Oczekiwana liczba lew
- `PlaceholderNorth` - Placeholder dla North
- `PlaceholderSouth` - Placeholder dla South

### Walidacja / Validation
- `ErrorInvalidCards` - Niepoprawne karty
- `ErrorEmptyField` - Puste pole
- `ErrorDuplicatesNorth` - Duplikaty u North
- `ErrorDuplicatesSouth` - Duplikaty u South
- `ErrorDuplicatesNS` - Duplikaty między NS
- `ErrorTargetTricks` - Błąd liczby lew

### Wyniki / Results
- `Deal` - Rozdanie
- `SortCriterion` - Kryterium sortowania
- `AtLeastTricks` - Co najmniej {0}
- `Tricks` - lewy/tricks
- `Trick` - lewa/trick
- `BestStrategy` - Najlepsza strategia
- `ExpectedTricks` - Oczekiwana liczba lew
- `ChanceAtLeast` - Szansa na co najmniej
- `ChanceExactly` - Szansa na dokładnie
- `ControlSum` - Suma kontrolna
- `NoData` - Brak danych
- `Strategy` - Strategia
- `Details` - Szczegóły
- `WestDist` - Rozdanie W
- `EastDist` - Rozdanie E
- `Probability` - Prawdopodobieństwo
- `Result` - Wynik

## Zaimplementowane pliki / Implemented Files

✅ `Program.cs` - Inicjalizacja języka z konfiguracji
✅ `Resources\Strings.cs` - Słowniki tłumaczeń
✅ `Views\_ViewImports.cshtml` - Import Strings dla wszystkich widoków
✅ `Views\Shared\_Layout.cshtml` - Menu i nagłówki
✅ `Views\StrategyEval\Index.cshtml` - Pełna lokalizacja formularza i wyników
✅ `Controllers\StrategyEvalController.cs` - Komunikaty błędów

## Testowanie / Testing

1. Ustaw język polski w `appsettings.json`:
```json
"DefaultLanguage": "pl"
```

2. Uruchom aplikację:
```powershell
cd RozgrywkaKoncowa
dotnet run
```

3. Otwórz http://localhost:5010

4. Zmień na angielski i uruchom ponownie:
```json
"DefaultLanguage": "en"
```

## Uwagi / Notes

- System jest statyczny - zmiana języka wymaga restartu aplikacji
- Wszystkie teksty są zakodowane w `Strings.cs`
- Brakujące tłumaczenia zwracają klucz jako fallback
- Format: `{0}`, `{1}` itp. dla parametrów w `Strings.Get(key, param1, param2)`
