using RozgrywkaKoncowa.Models;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Logic
{
    public class NSStrategy
    {
        public List<(int playerIdx, CCard card, int lewa)> Sequence { get; set; } = new(); // playerIdx: 0=N, 2=S, lewa: nr lewy

        public override string ToString()
        {
            // Format: "S:4 N:T, S:3 N:Q, N:A S:2"
            // Ruchy w tej samej lewie oddzielone spacją, lewy oddzielone przecinkiem i spacją
            return string.Join(", ", Sequence.GroupBy(x => x.lewa)
                .Select(g => string.Join(" ", g.Select(x => $"{(x.playerIdx == 0 ? "N" : "S")}:{x.card.Rank.Symbol}"))));
        }

        // Generuje klucz normalizacji dla grupowania równoważnych strategii
        // Karty w sekwensach są zastępowane wspólnym reprezentantem
        // TYLKO w ostatniej lewie kolejność nie ma znaczenia
        public string GetNormalizedKey(List<List<CCard>> northSequences, List<List<CCard>> southSequences)
        {
            var trickGroups = Sequence.GroupBy(x => x.lewa).OrderBy(g => g.Key).ToList();
            var normalized = new List<string>();
            var lastTrick = trickGroups.Count - 1;

            for (int i = 0; i < trickGroups.Count; i++)
            {
                var trick = trickGroups[i];
                var isLastTrick = (i == lastTrick);

                IEnumerable<(int playerIdx, CCard card, int lewa)> orderedMoves;

                if (isLastTrick)
                {
                    // W ostatniej lewie sortuj ruchy (kolejność nie ma znaczenia)
                    orderedMoves = trick.OrderBy(m => m.playerIdx);
                }
                else
                {
                    // W pozostałych lewach zachowaj oryginalną kolejność (kto wychodzi jest ważne)
                    orderedMoves = trick;
                }

                var trickMoves = orderedMoves.Select(move =>
                {
                    var sequences = move.playerIdx == 0 ? northSequences : southSequences;
                    var seqIndex = FindSequenceIndex(sequences, move.card);
                    var posInSeq = seqIndex >= 0 ? FindPositionInSequence(sequences[seqIndex], move.card) : 0;
                    var player = move.playerIdx == 0 ? "N" : "S";
                    return $"{player}:Seq{seqIndex}[{posInSeq}]";
                }).ToList();

                normalized.Add(string.Join(" ", trickMoves));
            }

            return string.Join(", ", normalized);
        }

        private int FindSequenceIndex(List<List<CCard>> sequences, CCard card)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].Any(c => c.Rank.Value == card.Rank.Value))
                    return i;
            }
            return -1;
        }

        private int FindPositionInSequence(List<CCard> sequence, CCard card)
        {
            return sequence.FindIndex(c => c.Rank.Value == card.Rank.Value);
        }
    }

    public class StrategyGroup
    {
        public string NormalizedKey { get; set; }
        public NSStrategy Representative { get; set; }
        public List<NSStrategy> AllVariants { get; set; } = new();
        public int Count => AllVariants.Count;

        public string GetDisplayString()
        {
            if (Count == 1)
                return Representative.ToString();

            // Formatuj z oznaczeniem sekwensów, np. "N:(A|K) S:(2|3)"
            return Representative.ToString() + $" [×{Count} wariantów]";
        }
    }

    public static class StrategyGenerator
    {
        // Rekurencyjny generator strategii dla MAX(N,S) lew
        // Wychodzić może tylko gracz, który ma jeszcze piki
        // Optymalizacja: karty w sekwensie są równoważne strategicznie
        public static List<NSStrategy> GenerateAllStrategies(CHand north, CHand south, int tricks)
        {
            var result = new List<NSStrategy>();
            GenerateRecursive(north.ToList(), south.ToList(), 0, tricks, new List<(int, CCard, int)>(), result);
            return result;
        }

        // Grupuje strategie według klas równoważności (sekwensów)
        // Zwraca tylko reprezentantów każdej klasy
        public static List<StrategyGroup> GroupStrategiesByEquivalence(CHand north, CHand south, List<NSStrategy> strategies)
        {
            var northSeqs = GroupBySequences(north.ToList());
            var southSeqs = GroupBySequences(south.ToList());

            var groups = new Dictionary<string, StrategyGroup>();

            foreach (var strategy in strategies)
            {
                var key = strategy.GetNormalizedKey(northSeqs, southSeqs);

                if (!groups.ContainsKey(key))
                {
                    groups[key] = new StrategyGroup
                    {
                        NormalizedKey = key,
                        Representative = strategy,
                        AllVariants = new List<NSStrategy>()
                    };
                }

                groups[key].AllVariants.Add(strategy);
            }

            return groups.Values.OrderBy(g => g.NormalizedKey).ToList();
        }

        // Grupuje karty według sekwensów i zwraca listę grup (każda grupa to sekwens kart)
        // Sekwens = karty o kolejnych wartościach (np. 7-6-5 albo A-K-Q)
        private static List<List<CCard>> GroupBySequences(List<CCard> cards)
        {
            if (cards.Count == 0) return new List<List<CCard>>();

            var sorted = cards.OrderByDescending(c => c.Rank.Value).ToList();
            var groups = new List<List<CCard>>();
            var currentSequence = new List<CCard> { sorted[0] };

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Rank.Value == sorted[i-1].Rank.Value - 1)
                {
                    // Kontynuacja sekwensu
                    currentSequence.Add(sorted[i]);
                }
                else
                {
                    // Koniec sekwensu, zacznij nowy
                    groups.Add(currentSequence);
                    currentSequence = new List<CCard> { sorted[i] };
                }
            }
            groups.Add(currentSequence);

            return groups;
        }

        private static void GenerateRecursive(List<CCard> n, List<CCard> s, int lewaIdx, int tricks, List<(int, CCard, int)> current, List<NSStrategy> result)
        {
            if (lewaIdx == tricks)
            {
                result.Add(new NSStrategy { Sequence = new List<(int, CCard, int)>(current) });
                return;
            }

            bool nHasCards = n.Count > 0;
            bool sHasCards = s.Count > 0;

            // Grupuj karty według sekwensów
            var nGroups = nHasCards ? GroupBySequences(n) : new List<List<CCard>>();
            var sGroups = sHasCards ? GroupBySequences(s) : new List<List<CCard>>();

            // Przypadek 1: Oboje mają karty - dwie możliwości (N wychodzi lub S wychodzi)
            if (nHasCards && sHasCards)
            {
                // Opcja A: N wychodzi, S odpowiada
                foreach (var nGroup in nGroups)
                {
                    foreach (var sGroup in sGroups)
                    {
                        var nCard = nGroup[0]; // Najwyższa karta w sekwensie N
                        var sCard = sGroup[0]; // Najwyższa karta w sekwensie S

                        var nextN = n.Where(c => c != nCard).ToList();
                        var nextS = s.Where(c => c != sCard).ToList();

                        var nextCurrent = new List<(int, CCard, int)>(current) { (0, nCard, lewaIdx), (2, sCard, lewaIdx) };
                        GenerateRecursive(nextN, nextS, lewaIdx + 1, tricks, nextCurrent, result);
                    }
                }

                // Opcja B: S wychodzi, N odpowiada
                foreach (var sGroup in sGroups)
                {
                    foreach (var nGroup in nGroups)
                    {
                        var sCard = sGroup[0];
                        var nCard = nGroup[0];

                        var nextS = s.Where(c => c != sCard).ToList();
                        var nextN = n.Where(c => c != nCard).ToList();

                        var nextCurrent = new List<(int, CCard, int)>(current) { (2, sCard, lewaIdx), (0, nCard, lewaIdx) };
                        GenerateRecursive(nextN, nextS, lewaIdx + 1, tricks, nextCurrent, result);
                    }
                }
            }
            // Przypadek 2: Tylko N ma karty - N wychodzi, S dorzuca trefla (obsłużone w symulacji)
            else if (nHasCards && !sHasCards)
            {
                foreach (var nGroup in nGroups)
                {
                    var nCard = nGroup[0];
                    var nextN = n.Where(c => c != nCard).ToList();

                    // S nie ma kart, więc dodajemy tylko N (S dorzuci trefla w symulacji)
                    var nextCurrent = new List<(int, CCard, int)>(current) { (0, nCard, lewaIdx) };
                    GenerateRecursive(nextN, s, lewaIdx + 1, tricks, nextCurrent, result);
                }
            }
            // Przypadek 3: Tylko S ma karty - S wychodzi, N dorzuca trefla
            else if (!nHasCards && sHasCards)
            {
                foreach (var sGroup in sGroups)
                {
                    var sCard = sGroup[0];
                    var nextS = s.Where(c => c != sCard).ToList();

                    // N nie ma kart, więc dodajemy tylko S (N dorzuci trefla w symulacji)
                    var nextCurrent = new List<(int, CCard, int)>(current) { (2, sCard, lewaIdx) };
                    GenerateRecursive(n, nextS, lewaIdx + 1, tricks, nextCurrent, result);
                }
            }
            // Przypadek 4: Nikt nie ma kart - nie powinno się zdarzyć jeśli tricks jest poprawne
            else
            {
                // Zakończ generowanie (to nie powinno się wydarzyć)
                return;
            }
        }
    }
}
