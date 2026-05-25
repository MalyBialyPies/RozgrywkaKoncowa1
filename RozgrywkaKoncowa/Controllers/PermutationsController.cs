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
        public decimal WeightedProbability { get; set; }
    }

    public class PermutationItem
    {
        public string WHand { get; set; }
        public string EHand { get; set; }
        public int NSWins { get; set; }
        public decimal Probability { get; set; }
    }

    public class PermutationsController : Controller
    {
        public IActionResult Index()
        {
            var spade = CDenomination.Spade;

            var northCards = new[] { new CCard(spade, CRank.RA), new CCard(spade, CRank.RQ) };
            var southCards = new[] { new CCard(spade, CRank.R3), new CCard(spade, CRank.R2) };

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
            int totalCombos = 1 << wePool.Length;
            int n = wePool.Length;
            long totalWeight = CombLong(26, 13);

            var deals = new List<WeightedDeal>();
            var itemDescriptions = new List<(string WHand, string EHand, decimal Probability)>();

            for (int i = 0; i < totalCombos; i++)
            {
                var wSpades = new List<CCard>();
                for (int bit = 0; bit < n; bit++)
                    if ((i & (1 << bit)) != 0) wSpades.Add(wePool[bit]);
                var eSpades = wePool.Except(wSpades).ToList();

                int wCount = wSpades.Count;
                decimal probability = (decimal)CombLong(26 - n, 13 - wCount) / totalWeight;

                var wCards = wSpades.ToList();
                for (int j = wCards.Count; j < maxTricks; j++)
                    wCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));
                var eCards = eSpades.ToList();
                for (int j = eCards.Count; j < maxTricks; j++)
                    eCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                var north = new CHand(northCards);
                for (int j = north.Count; j < maxTricks; j++)
                    north.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));
                var south = new CHand(southCards);
                for (int j = south.Count; j < maxTricks; j++)
                    south.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                deals.Add(new WeightedDeal
                {
                    Hands = new CHand[] { north, new CHand(eCards), south, new CHand(wCards) },
                    Weight = probability
                });

                itemDescriptions.Add((
                    wSpades.Count > 0 ? string.Join(", ", wSpades.Select(c => c.ToString())) : "-",
                    eSpades.Count > 0 ? string.Join(", ", eSpades.Select(c => c.ToString())) : "-",
                    probability
                ));
            }

            // Single-dummy solver: NS podejmuje wspólną decyzję po wszystkich układach WE
            var result = SingleDummySolver.Solve(deals, CDenomination.Spade);

            vm.TotalPermutations = totalCombos;

            vm.Items = itemDescriptions.Select((d, idx) => new PermutationItem
            {
                WHand = d.WHand,
                EHand = d.EHand,
                NSWins = result.NSWinsPerDeal[idx],
                Probability = d.Probability
            }).ToList();

            vm.Summary = result.Distribution
                .Select(d => new PermutationSummary
                {
                    NSWins = d.NSWins,
                    WeightedProbability = d.WeightedProbability
                })
                .ToList();

            return View(vm);
        }

        private static long CombLong(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;
            if (k > n / 2) k = n - k;
            long result = 1;
            for (int i = 1; i <= k; i++)
                result = result * (n - k + i) / i;
            return result;
        }
    }
}

