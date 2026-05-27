using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using RozgrywkaKoncowa.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class StrategyEvalDetail
    {
        public string WDist { get; set; }
        public string EDist { get; set; }
        public double Probability { get; set; }
        public int NSTricks { get; set; }
    }

    public class StrategyEvalResult
    {
        public string Strategy { get; set; }
        public double ExpectedTricks { get; set; }
        // PTricks[k] = prawdopodobieĹ„stwo (%) wziÄ™cia dokĹ‚adnie k lew, k=0..MaxTricks
        public double[] PTricks { get; set; }
        public int MaxTricks { get; set; }
        public List<StrategyEvalDetail> Details { get; set; } = new();
    }

    public class StrategyEvalController : Controller
    {
        // Parsuje string kart np. "AQT" lub "A Q T" lub "A,Q,T" lub "10" -> lista CCard (pik)
        private static List<CCard> ParseCards(string input)
        {
            var cards = new List<CCard>();
            if (string.IsNullOrWhiteSpace(input)) return cards;
            var normalized = input.ToUpperInvariant()
                .Replace(",", " ").Replace(";", " ");
            // Najpierw sprĂłbuj split po spacjach (np. "A Q T")
            var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length == 1)
                {
                    // Pojedynczy znak â€“ szukaj rangi
                    var rank = CRank.FromSymbol(token);
                    if (rank != null) cards.Add(new CCard(CDenomination.Spade, rank));
                }
                else
                {
                    // CiÄ…g znakĂłw bez spacji np. "AQT" lub "234" â€“ iteruj po znakach
                    foreach (var ch in token)
                    {
                        var rank = CRank.FromSymbol(ch.ToString());
                        if (rank != null) cards.Add(new CCard(CDenomination.Spade, rank));
                    }
                }
            }
            return cards;
        }

        [HttpGet]
        public IActionResult Index(string northCards = null, string southCards = null, int? targetTricks = null)
        {
            ViewBag.NorthCards = northCards ?? "AQT";
            ViewBag.SouthCards = southCards ?? "234";
            ViewBag.TargetTricksInput = targetTricks ?? 2;

            // JeĹ›li brak parametrĂłw â€“ pokaĹĽ tylko formularz
            if (northCards == null && southCards == null)
                return View(new List<StrategyEvalResult>());

            var spade = CDenomination.Spade;
            var club = CDenomination.Club;

            var northList = ParseCards(northCards);
            var southList = ParseCards(southCards);
            if (!northList.Any() || !southList.Any())
            {
                ViewBag.Error = "Podaj poprawne karty dla N i S (np. AQT i 234).";
                return View(new List<StrategyEvalResult>());
            }

            int target = targetTricks ?? 2;

            var north = new CHand(northList);
            var south = new CHand(southList);
            int maxLen = Math.Max(north.Count, south.Count);
            while (north.Count < maxLen)
                north.Add(new CCard(club, CRank.FromValue(north.Count + 2)));
            while (south.Count < maxLen)
                south.Add(new CCard(club, CRank.FromValue(south.Count + 2)));
            int liczbaLew = maxLen;
            if (target < 1) target = 1;
            if (target > liczbaLew) target = liczbaLew;

            // Karty WE = wszystkie piki z wyjÄ…tkiem kart NS
            var nsRanks = northList.Concat(southList).Select(c => c.Rank.Value).ToHashSet();
            var allSpadeRanks = new[] { CRank.RA, CRank.RK, CRank.RQ, CRank.RJ, CRank.RT,
                                        CRank.R9, CRank.R8, CRank.R7, CRank.R6, CRank.R5,
                                        CRank.R4, CRank.R3, CRank.R2 };
            var wePool = allSpadeRanks
                .Where(r => !nsRanks.Contains(r.Value))
                .Select(r => new CCard(spade, r))
                .ToArray();

            var strategies = StrategyGenerator.GenerateAllStrategies(north, south, liczbaLew);

            int n = wePool.Length;
            int totalCombos = 1 << n;
            double totalWeight = Comb(26, 13);

            var results = new List<StrategyEvalResult>();

            foreach (var strategy in strategies)
            {
                double expected = 0.0;
                double[] pTricks = new double[liczbaLew + 1];
                var details = new List<StrategyEvalDetail>();
                foreach (var i in Enumerable.Range(0, totalCombos))
                {
                    var wSpades = new List<CCard>();
                    for (int bit = 0; bit < n; bit++)
                        if ((i & (1 << bit)) != 0) wSpades.Add(wePool[bit]);
                    var eSpades = wePool.Except(wSpades).ToList();
                    int wCount = wSpades.Count;
                    double probability = Comb(26 - n, 13 - wCount) / totalWeight;

                    var wCards = wSpades.ToList();
                    for (int j = wCards.Count; j < liczbaLew; j++) wCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));
                    var eCards = eSpades.ToList();
                    for (int j = eCards.Count; j < liczbaLew; j++) eCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                    var nHand = new CHand(north);
                    var sHand = new CHand(south);
                    var wHand = new CHand(wCards);
                    var eHand = new CHand(eCards);

                    int nsWins = PlayClockwiseFull(nHand, eHand, sHand, wHand, strategy, liczbaLew, strategy.Sequence[0].playerIdx);
                    expected += nsWins * probability;
                    if (nsWins >= 0 && nsWins <= liczbaLew)
                        pTricks[nsWins] += probability;

                    details.Add(new StrategyEvalDetail
                    {
                        WDist = string.Join(" ", wSpades.Select(c => c.Rank.Symbol)),
                        EDist = string.Join(" ", eSpades.Select(c => c.Rank.Symbol)),
                        Probability = Math.Round(probability * 100, 2),
                        NSTricks = nsWins
                    });
                }
                results.Add(new StrategyEvalResult
                {
                    Strategy = strategy.ToString(),
                    ExpectedTricks = Math.Round(expected, 3),
                    PTricks = pTricks.Select(p => Math.Round(p * 100, 2)).ToArray(),
                    MaxTricks = liczbaLew,
                    Details = details
                });
            }

            var best = results
                .OrderByDescending(r => r.PTricks[Math.Min(target, r.MaxTricks)])
                .ThenByDescending(r => r.ExpectedTricks)
                .FirstOrDefault();
            ViewBag.Best = best;
            ViewBag.TargetTricks = target;
            ViewBag.NorthStr = string.Join(" ", northList.Select(c => c.Rank.Symbol));
            ViewBag.SouthStr = string.Join(" ", southList.Select(c => c.Rank.Symbol));

            return View(results
                .OrderByDescending(r => r.PTricks[Math.Min(target, r.MaxTricks)])
                .ThenByDescending(r => r.ExpectedTricks)
                .ToList());
        }

        private static double Comb(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;
            if (k > n / 2) k = n - k;
            double result = 1;
            for (int i = 1; i <= k; i++)
                result = result * (n - k + i) / i;
            return result;
        }

        private static int PlayClockwiseFull(CHand nHand, CHand eHand, CHand sHand, CHand wHand, NSStrategy nsStrategy, int liczbaLew, int starter)
            => StrategyPlayer.PlayClockwiseFull(nHand, eHand, sHand, wHand, nsStrategy, liczbaLew, starter);

        private static string PlayerName(int idx)
            => StrategyPlayer.PlayerName(idx);

        private static int Winner(List<(int player, CCard card)> trick, CDenomination trump)
            => StrategyPlayer.Winner(trick, trump);
    }
}
