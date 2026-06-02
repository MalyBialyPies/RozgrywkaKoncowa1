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
            return string.Join("   ", Sequence.GroupBy(x => x.lewa).Select(g => string.Join(", ", g.Select(x => $"{(x.playerIdx == 0 ? "N" : "S")}:{x.card.Rank.Symbol}"))));
        }
    }

    public static class StrategyGenerator
    {
        // Rekurencyjny generator strategii: dla każdej lewy wybiera jedną kartę z N i jedną z S, permutuje kolejność w lewy
        // Optymalizacja: karty w sekwensie (np. 4-3-2) są równoważne strategicznie
        public static List<NSStrategy> GenerateAllStrategies(CHand north, CHand south, int tricks)
        {
            var result = new List<NSStrategy>();
            GenerateRecursive(north.ToList(), south.ToList(), 0, tricks, new List<(int, CCard, int)>(), result);
            return result;
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

            // Grupuj karty według sekwensów
            var nGroups = GroupBySequences(n);
            var sGroups = GroupBySequences(s);

            // Dla każdej grupy sekwensu używamy tylko najwyższej karty (reprezentanta)
            foreach (var nGroup in nGroups)
            {
                foreach (var sGroup in sGroups)
                {
                    var nCard = nGroup[0]; // Najwyższa karta w sekwensie N
                    var sCard = sGroup[0]; // Najwyższa karta w sekwensie S

                    // Usuń wybraną kartę z ręki
                    var nextN = n.Where(c => c != nCard).ToList();
                    var nextS = s.Where(c => c != sCard).ToList();

                    // Najpierw N, potem S
                    var nextCurrent1 = new List<(int, CCard, int)>(current) { (0, nCard, lewaIdx), (2, sCard, lewaIdx) };
                    GenerateRecursive(nextN, nextS, lewaIdx + 1, tricks, nextCurrent1, result);

                    // Najpierw S, potem N
                    var nextCurrent2 = new List<(int, CCard, int)>(current) { (2, sCard, lewaIdx), (0, nCard, lewaIdx) };
                    GenerateRecursive(nextN, nextS, lewaIdx + 1, tricks, nextCurrent2, result);
                }
            }
        }
    }
}
