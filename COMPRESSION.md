# Kompresja strategii według sekwensów

## Problem

Dla kart **N: AK, S: 23** bez kompresji generuje się **16 strategii**, ale ze względu na sekwensy faktycznie istnieją tylko **2 unikalne klasy**:

1. **N wychodzi pierwsza:** N:(A|K) S:(2|3)
2. **S wychodzi pierwsza:** S:(2|3) N:(A|K)

Gdzie `(A|K)` oznacza "A lub K - bez różnicy", bo tworzą sekwens.

## Analiza

### Bez kompresji (16 strategii):
Dla 2 lew z 2 kartami każdej strony:
- Lewa 1: N może zagrać A lub K (2 opcje), S może zagrać 2 lub 3 (2 opcje)
- Kolejność w lewie: N wychodzi lub S wychodzi (2 opcje)
- Lewa 2: Pozostała para (1 opcja każda strona, 2 kolejności)
- **Razem:** 2×2×2×2×2 = 32 kombinacje → po optymalizacji generatora ~16

### Z kompresją sekwensów (2 klasy):
Sekwensy: N: {A-K}, S: {2-3}
- Lewa 1, N wychodzi: N:Seq0 S:Seq0 → reprezentant: N:A S:2
- Lewa 1, S wychodzi: S:Seq0 N:Seq0 → reprezentant: S:2 N:A

Każda klasa ma 8 wariantów (A/K × 2/3 × kolejność lewy 2).

## Implementacja

### 1. Zidentyfikuj sekwensy

```csharp
private static List<List<CCard>> GroupBySequences(List<CCard> cards)
{
	// Zwraca grupy kart w sekwensach
	// Np. A-K-Q → {A,K,Q}, 9-8 → {9,8}, 5 → {5}
}
```

### 2. Normalizuj strategie

Każda strategia ma **klucz normalizacji** zamiast konkretnych kart:

```
N:A S:2, N:K S:3    →   N:Seq0[0] S:Seq0[0], N:Seq0[1] S:Seq0[1]
N:A S:3, N:K S:2    →   N:Seq0[0] S:Seq0[1], N:Seq0[1] S:Seq0[0]
S:2 N:A, S:3 N:K    →   S:Seq0[0] N:Seq0[0], S:Seq0[1] N:Seq0[1]
```

Pierwsze dwa mają różne klucze (różna kolejność kart w sekwensach), trzecie też jest inne (inna kolejność graczy).

### 3. Grupuj według kluczy

```csharp
public static List<StrategyGroup> GroupStrategiesByEquivalence(
	CHand north, CHand south, List<NSStrategy> strategies)
{
	var northSeqs = GroupBySequences(north.ToList());
	var southSeqs = GroupBySequences(south.ToList());

	var groups = new Dictionary<string, StrategyGroup>();

	foreach (var strategy in strategies)
	{
		var key = strategy.GetNormalizedKey(northSeqs, southSeqs);
		if (!groups.ContainsKey(key))
			groups[key] = new StrategyGroup { Representative = strategy };
		groups[key].AllVariants.Add(strategy);
	}

	return groups.Values.ToList();
}
```

### 4. Wyświetl skompresowane wyniki

**Przed:**
```
N:A S:2, N:K S:3    (P≥2: 75.0%)
N:A S:3, N:K S:2    (P≥2: 75.0%)
N:K S:2, N:A S:3    (P≥2: 75.0%)
N:K S:3, N:A S:2    (P≥2: 75.0%)
S:2 N:A, S:3 N:K    (P≥2: 50.0%)
S:2 N:K, S:3 N:A    (P≥2: 50.0%)
...
```

**Po kompresji:**
```
N:(A|K) S:(2|3), N:(A|K) S:(2|3)    [×8 wariantów]  (P≥2: 75.0%)
S:(2|3) N:(A|K), S:(2|3) N:(A|K)    [×8 wariantów]  (P≥2: 50.0%)
```

## Status implementacji

✅ **Gotowe:**
- Klasa `NSStrategy` z metodą `GetNormalizedKey()`
- Klasa `StrategyGroup` do reprezentowania grup
- Metoda `GroupStrategiesByEquivalence()` w `StrategyGenerator`
- Metoda `GroupBySequences()` (już istniała)

❌ **Do zrobienia:**
- Integracja z kontrolerem (parametr `compress=true`)
- Aktualizacja widoku z opcją kompresji
- Formatowanie wyświetlania z symbolami sekwensów (A|K)
- Tłumaczenia dla nowych tekstów

## Przykład użycia (przyszłość)

```csharp
// W kontrolerze:
var strategies = StrategyGenerator.GenerateAllStrategies(north, south, tricks);

if (compress)
{
	var groups = StrategyGenerator.GroupStrategiesByEquivalence(north, south, strategies);
	// Przetwarzaj tylko reprezentantów grup
	foreach (var group in groups)
	{
		var representative = group.Representative;
		// Oblicz wyniki dla reprezentanta
		// Wyświetl z informacją: [×{group.Count} wariantów]
	}
}
else
{
	// Standardowa ścieżka - wszystkie strategie
}
```

## Korzyści

1. **Redukcja liczby wyświetlanych wierszy**
   - N: A-K, S: 2-3 → z 16 do 2 wierszy
   - N: A-K-Q, S: 2-3-4 → z ~216 do ~6 wierszy

2. **Łatwiejsza analiza**
   - Użytkownik widzi tylko unikalne wzorce gry
   - Mniej scrollowania

3. **Zachowanie kompletności**
   - Wszystkie warianty są policzone
   - Można rozwinąć i zobaczyć szczegóły

## Uwagi

- Kompresja działa **tylko dla wyświetlania**
- Obliczenia prawdopodobieństw wykonywane są **dla wszystkich wariantów**
- Reprezentant grupy ma te same statystyki co wszystkie jego warianty (ze względu na równoważność)
