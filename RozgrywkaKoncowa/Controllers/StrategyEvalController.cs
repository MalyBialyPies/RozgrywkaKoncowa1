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
        // PTricks[k] = prawdopodobieństwo (%) wzięcia dokładnie k lew, k=0..MaxTricks
        public double[] PTricks { get; set; }
        public int MaxTricks { get; set; }
        public List<StrategyEvalDetail> Details { get; set; } = new();

        // Metoda pomocnicza: prawdopodobieństwo wzięcia co najmniej N lew
        public double GetProbabilityAtLeast(int targetTricks)
        {
            double sum = 0.0;
            for (int k = targetTricks; k <= MaxTricks; k++)
            {
                sum += PTricks[k];
            }
            return sum;
        }
    }

    public class StrategyEvalController : Controller
    {
        // Oblicza prawdopodobieństwo (%) wzięcia co najmniej N lew
        private static double GetProbabilityAtLeast(StrategyEvalResult result, int targetTricks)
        {
            double sum = 0.0;
            for (int k = targetTricks; k <= result.MaxTricks; k++)
            {
                sum += result.PTricks[k];
            }
            return sum;
        }

        // Parsuje string kart np. "AQT" lub "A Q T" lub "A,Q,T" lub "10" -> lista CCard (pik)
        private static List<CCard> ParseCards(string input)
        {
            var cards = new List<CCard>();
            if (string.IsNullOrWhiteSpace(input)) return cards;
            var normalized = input.ToUpperInvariant()
                .Replace(",", " ").Replace(";", " ")
                .Replace("10", "T"); // Zamień "10" na "T" przed parsowaniem
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

            // Walidacja: puste pola
            if (!northList.Any() || !southList.Any())
            {
                ViewBag.Error = "Podaj poprawne karty dla N i S (np. AQT lub A10, 234). Dozwolone: A, K, Q, J, T (lub 10), 9-2.";
                return View(new List<StrategyEvalResult>());
            }

            // Walidacja: duplikaty w jednej ręce
            var northRanks = northList.Select(c => c.Rank.Value).ToList();
            var southRanks = southList.Select(c => c.Rank.Value).ToList();
            if (northRanks.Distinct().Count() != northRanks.Count)
            {
                ViewBag.Error = "Karty North się powtarzają. Każda karta może wystąpić tylko raz.";
                return View(new List<StrategyEvalResult>());
            }
            if (southRanks.Distinct().Count() != southRanks.Count)
            {
                ViewBag.Error = "Karty South się powtarzają. Każda karta może wystąpić tylko raz.";
                return View(new List<StrategyEvalResult>());
            }

            // Walidacja: przecięcie NS
            var northSet = new HashSet<int>(northRanks);
            var southSet = new HashSet<int>(southRanks);
            var intersection = northSet.Intersect(southSet).ToList();
            if (intersection.Any())
            {
                var symbols = intersection.Select(v => CRank.FromValue(v)?.Symbol ?? v.ToString());
                ViewBag.Error = $"Karty powtarzają się między North i South: {string.Join(", ", symbols)}";
                return View(new List<StrategyEvalResult>());
            }

            int target = targetTricks ?? 2;

            // Liczba lew = maksymalna liczba pików (dłuższa ręka)
            int liczbaLew = Math.Max(northList.Count, southList.Count);
            if (target < 1) target = 1;
            if (target > liczbaLew) target = liczbaLew;

            // Karty WE = wszystkie piki z wyjątkiem kart NS
            var nsRanks = northList.Concat(southList).Select(c => c.Rank.Value).ToHashSet();
            var allSpadeRanks = new[] { CRank.RA, CRank.RK, CRank.RQ, CRank.RJ, CRank.RT,
                                        CRank.R9, CRank.R8, CRank.R7, CRank.R6, CRank.R5,
                                        CRank.R4, CRank.R3, CRank.R2 };
            var wePool = allSpadeRanks
                .Where(r => !nsRanks.Contains(r.Value))
                .Select(r => new CCard(spade, r))
                .ToArray();

            // Generuj strategie TYLKO dla pików (bez treflów)
            var northSpades = new CHand(northList);
            var southSpades = new CHand(southList);
            var strategies = StrategyGenerator.GenerateAllStrategies(northSpades, southSpades, liczbaLew);

            int n = wePool.Length;
            int totalCombos = 1 << n;
            // Każdy rozkład pików WE jest równie prawdopodobny (uzupełniamy nieznaczącymi treflami)
            double uniformProb = 1.0 / totalCombos;

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

                    // Równomierne prawdopodobieństwo dla każdego rozkładu pików
                    double probability = uniformProb;

                    // Uzupełnij ręce WE treflami do liczby lew
                    var wCards = wSpades.ToList();
                    for (int j = wCards.Count; j < liczbaLew; j++) wCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));
                    var eCards = eSpades.ToList();
                    for (int j = eCards.Count; j < liczbaLew; j++) eCards.Add(new CCard(club, CRank.FromValue((j % 13) + 2)));

                    // Uzupełnij ręce NS treflami do liczby lew (tylko dla symulacji)
                    var nCards = northList.ToList();
                    while (nCards.Count < liczbaLew) nCards.Add(new CCard(club, CRank.FromValue(nCards.Count + 2)));
                    var sCards = southList.ToList();
                    while (sCards.Count < liczbaLew) sCards.Add(new CCard(club, CRank.FromValue(sCards.Count + 2)));

                    var nHand = new CHand(nCards);
                    var sHand = new CHand(sCards);
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
                        Probability = Math.Round(probability * 100, 6),
                        NSTricks = nsWins
                    });
                }
                // Normalizacja: upewnij się, że suma prawdopodobieństw wynosi dokładnie 100%
                double totalProb = pTricks.Sum();
                if (totalProb > 0)
                {
                    for (int k = 0; k < pTricks.Length; k++)
                    {
                        pTricks[k] = pTricks[k] / totalProb;
                    }
                }

                // Zaokrąglij do 6 miejsc po przecinku
                var pTricksPercent = pTricks.Select(p => Math.Round(p * 100, 6)).ToArray();

                // Dostosuj ostatnią wartość tak, aby suma wynosiła dokładnie 100%
                double sumPercent = pTricksPercent.Sum();
                if (sumPercent != 100.0 && pTricksPercent.Length > 0)
                {
                    // Znajdź indeks z największą wartością (która nie jest 0)
                    int maxIdx = 0;
                    for (int k = 1; k < pTricksPercent.Length; k++)
                    {
                        if (pTricksPercent[k] > pTricksPercent[maxIdx])
                            maxIdx = k;
                    }
                    pTricksPercent[maxIdx] += (100.0 - sumPercent);
                }

                results.Add(new StrategyEvalResult
                {
                    Strategy = strategy.ToString(),
                    ExpectedTricks = Math.Round(expected, 3),
                    PTricks = pTricksPercent,
                    MaxTricks = liczbaLew,
                    Details = details
                });
            }

            // Sortowanie: P(≥target) malejąco, potem EX malejąco
            var best = results
                .OrderByDescending(r => GetProbabilityAtLeast(r, target))
                .ThenByDescending(r => r.ExpectedTricks)
                .FirstOrDefault();

            ViewBag.Best = best;
            ViewBag.TargetTricks = target;
            ViewBag.NorthStr = string.Join(" ", northList.Select(c => c.Rank.Symbol));
            ViewBag.SouthStr = string.Join(" ", southList.Select(c => c.Rank.Symbol));

            return View(results
                .OrderByDescending(r => GetProbabilityAtLeast(r, target))
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
