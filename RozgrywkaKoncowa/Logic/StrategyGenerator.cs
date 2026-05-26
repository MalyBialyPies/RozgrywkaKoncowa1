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
        // Rekurencyjny generator strategii: dla każdej lewy wybiera jedną kartę z N i jedną z S, permutuje kolejność w lewie
        public static List<NSStrategy> GenerateAllStrategies(CHand north, CHand south, int tricks)
        {
            var result = new List<NSStrategy>();
            GenerateRecursive(north.ToList(), south.ToList(), 0, tricks, new List<(int, CCard, int)>(), result);
            return result;
        }

        private static void GenerateRecursive(List<CCard> n, List<CCard> s, int lewaIdx, int tricks, List<(int, CCard, int)> current, List<NSStrategy> result)
        {
            if (lewaIdx == tricks)
            {
                result.Add(new NSStrategy { Sequence = new List<(int, CCard, int)>(current) });
                return;
            }
            for (int i = 0; i < n.Count; i++)
            {
                for (int j = 0; j < s.Count; j++)
                {
                    var nCard = n[i];
                    var sCard = s[j];
                    var nextN = n.Where((x, idx) => idx != i).ToList();
                    var nextS = s.Where((x, idx) => idx != j).ToList();
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
