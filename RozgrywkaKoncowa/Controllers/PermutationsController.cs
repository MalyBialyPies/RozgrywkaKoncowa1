using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using RozgrywkaKoncowa.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class PermutationResultViewModel
    {
        public string NHand { get; set; }
        public string SHand { get; set; }
        public int TotalPermutations { get; set; }
        public List<PermutationSummary> Summary { get; set; } = new();
        public List<PermutationItem> Items { get; set; } = new();
    }

    public class PermutationSummary
    {
        public int NSWins { get; set; }
        public double WeightedProbability { get; set; }
    }

    public class PermutationItem
    {
        public string WHand { get; set; }
        public string EHand { get; set; }
        public int NSWins { get; set; }
        public double Probability { get; set; }
    }

    public class PermutationsController : Controller
    {
        public IActionResult Index()
        {
            var spade = CDenomination.Spade;

            // Karty dla N i S
            var northCards = new[] { new CCard(spade, CRank.RA), new CCard(spade, CRank.RQ) };
            var southCards = new[] { new CCard(spade, CRank.R3), new CCard(spade, CRank.R2) };

            // Pula 9 pozostałych kart do rozdzielenia między W i E
            var wePool = new[] {
                new CCard(spade, CRank.RK),
                new CCard(spade, CRank.RJ),
                new CCard(spade, CRank.RT),
                new CCard(spade, CRank.R9),
                new CCard(spade, CRank.R8),
                new CCard(spade, CRank.R7),
                new CCard(spade, CRank.R6),
                new CCard(spade, CRank.R5),
                new CCard(spade, CRank.R4)
            };

            var vm = new PermutationResultViewModel
            {
                NHand = string.Join(", ", northCards.Select(c => c.ToString())),
                SHand = string.Join(", ", southCards.Select(c => c.ToString()))
            };

            int maxTricks = Math.Max(northCards.Length, southCards.Length);
            var club = CDenomination.Club;

            // Generujemy wszystkie możliwe podziały 9 kart między W i E (2^9 = 512 kombinacji)
            int totalCombos = 1 << wePool.Length;
            int n = wePool.Length; // liczba kart w puli obrońców

            // Całkowita suma wag kombinatorycznych = C(26, 13) (26 wolnych miejsc, W bierze 13)
            // Każdy konkretny podział k kart do W i (n-k) do E ma wagę:
            //   C(26-n, 13-k) / C(26, 13)
            // Suma po wszystkich k: suma C(26-n, 13-k)*C(n,k) / C(26,13) = 1.0 (z tożsamości Vandermonde'a)
            double totalWeight = Comb(26, 13);

            for (int i = 0; i < totalCombos; i++)
            {
                var wSpades = new List<CCard>();
                for (int bit = 0; bit < n; bit++)
                {
                    if ((i & (1 << bit)) != 0) wSpades.Add(wePool[bit]);
                }
                var eSpades = wePool.Except(wSpades).ToList();

                int wCount = wSpades.Count;

                // Prawdopodobieństwo a priori tego konkretnego układu:
                // W dostaje DOKŁADNIE te wCount kart spośród puli n kart.
                // Jest C(n, wCount) takich układów z tego samego podziału długości.
                // Prawdopodobieństwo jednego konkretnego układu = C(26-n, 13-wCount) / C(26,13)
                double probability = Comb(26 - n, 13 - wCount) / totalWeight;

                var wCards = wSpades.ToList();
                for (int j = wCards.Count; j < maxTricks; j++) wCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                var eCards = eSpades.ToList();
                for (int j = eCards.Count; j < maxTricks; j++) eCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                var northList = northCards.ToList();
                var north = new CHand(northList);
                for (int j = north.Count; j < maxTricks; j++) north.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                var southList = southCards.ToList();
                var south = new CHand(southList);
                for (int j = south.Count; j < maxTricks; j++) south.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                var west = new CHand(wCards);
                var east = new CHand(eCards);

                var contract = new CContract(CLevel.Level2, CDenomination.Spade, CDouble.Doubled);
                var board = new CBoard(north, east, south, west, CPlayer.PlayerNorth, contract);

                var result = MinimaxSolver.Solve(board);

                vm.Items.Add(new PermutationItem
                {
                    WHand = wSpades.Count > 0 ? string.Join(", ", wSpades.Select(c => c.ToString())) : "-",
                    EHand = eSpades.Count > 0 ? string.Join(", ", eSpades.Select(c => c.ToString())) : "-",
                    NSWins = result.NSWins,
                    Probability = probability
                });
            }

            vm.TotalPermutations = totalCombos;

            // Podsumowanie: grupujemy po liczbie lew, sumujemy ważone prawdopodobieństwa
            vm.Summary = vm.Items
                .GroupBy(x => x.NSWins)
                .Select(g => new PermutationSummary
                {
                    NSWins = g.Key,
                    WeightedProbability = Math.Round(g.Sum(x => x.Probability) * 100.0, 2)
                })
                .OrderByDescending(x => x.NSWins)
                .ToList();

            return View(vm);
        }

        // Współczynnik dwumianowy C(n, k)
        private static double Comb(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;
            if (k > n / 2) k = n - k;
            double result = 1;
            for (int i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
            }
            return result;
        }
    }
}
