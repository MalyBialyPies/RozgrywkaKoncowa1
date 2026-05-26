using RozgrywkaKoncowa.Models;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Logic
{
    public class StrategyNode
    {
        public string MoveDescription { get; set; }
        public double Probability { get; set; }
        public List<StrategyNode> Children { get; set; } = new();
        public int TricksTaken { get; set; } // tylko w liściach
    }

    public static class ProbabilisticSolver
    {
        public static StrategyNode Solve(CBoard board)
        {
            // 1. Wyznacz wszystkie możliwe rozkłady kart EW
            var allCards = AllCards();
            var nsCards = board.Hands[0].Concat(board.Hands[2]).ToList();
            var unknown = allCards.Except(nsCards, new CardComparer()).ToList();
            int ewCount = unknown.Count;
            int eCount = board.Hands[0].Count; // zakładamy rozgrywka końcówek, E i W mają tyle samo co N/S

            var allDistributions = GenerateDistributions(unknown, eCount);
            int total = allDistributions.Count;

            // 2. Dla każdego rozkładu uruchom double dummy solver
            var results = new List<(List<CCard> east, List<CCard> west, MinimaxResult result)>();
            foreach (var (east, west) in allDistributions)
            {
                var hands = new[] {
                    new CHand(board.Hands[0]),
                    new CHand(east),
                    new CHand(board.Hands[2]),
                    new CHand(west)
                };
                var b = new CBoard(hands[0], hands[1], hands[2], hands[3], board.Declarer, board.Contract);
                var res = MinimaxSolver.Solve(b);
                results.Add((east, west, res));
            }

            // 3. Buduj drzewo strategii (wersja uproszczona: tylko prawdopodobieństwo wzięcia wszystkich lew)
            int maxTricks = board.Hands[0].Count + board.Hands[2].Count;
            int success = results.Count(r => r.result.NSWins == maxTricks);
            double prob = (double)success / total;

            return new StrategyNode
            {
                MoveDescription = "Prawdopodobieństwo wzięcia wszystkich lew",
                Probability = prob,
                TricksTaken = maxTricks
            };
        }

        private static List<(List<CCard> east, List<CCard> west)> GenerateDistributions(List<CCard> unknown, int eCount)
        {
            var result = new List<(List<CCard>, List<CCard>)>();
            foreach (var e in Combinations(unknown, eCount))
            {
                var w = unknown.Except(e, new CardComparer()).ToList();
                result.Add((e.ToList(), w));
            }
            return result;
        }

        // Generator kombinacji k kart z listy
        private static IEnumerable<IEnumerable<CCard>> Combinations(List<CCard> list, int k)
        {
            if (k == 0) yield return new List<CCard>();
            else
            {
                for (int i = 0; i <= list.Count - k; i++)
                {
                    foreach (var tail in Combinations(list.Skip(i + 1).ToList(), k - 1))
                    {
                        yield return (new[] { list[i] }).Concat(tail);
                    }
                }
            }
        }

        // Zwraca pełną talię (możesz dostosować do swojego modelu)
        private static List<CCard> AllCards()
        {
            var result = new List<CCard>();
            foreach (var d in RozgrywkaKoncowa.Models.CDenomination.All)
                foreach (var r in RozgrywkaKoncowa.Models.CRank.All)
                    result.Add(new CCard(d, r));
            return result;
        }

        private class CardComparer : IEqualityComparer<CCard>
        {
            public bool Equals(CCard x, CCard y) => x.Denomination == y.Denomination && x.Rank == y.Rank;
            public int GetHashCode(CCard obj) => obj.Denomination.GetHashCode() ^ obj.Rank.GetHashCode();
        }
    }
}
