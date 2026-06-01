using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using RozgrywkaKoncowa.Logic;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class StrategyPresetController : Controller
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

        // S:2, N:T  S:3, N:Q  S:4, N:A  W:KJ9  E:8765
        // N: A Q T   S: 2 3 4   W: K J 9   E: 8 7 6 5
        private static readonly CRank[] NorthRanks = { CRank.RA, CRank.RQ, CRank.RT };
        private static readonly CRank[] SouthRanks = { CRank.R2, CRank.R3, CRank.R4 };
        private static readonly CRank[] WestRanks  = { CRank.RK, CRank.RJ, CRank.R9 };
        private static readonly CRank[] EastRanks  = { CRank.R8, CRank.R7, CRank.R6, CRank.R5 };
        private const int TargetTricks = 1;

        [HttpGet]
        public IActionResult Run()
        {
            var spade = CDenomination.Spade;
            var club  = CDenomination.Club;

            var northCards = NorthRanks.Select(r => new CCard(spade, r)).ToList();
            var southCards = SouthRanks.Select(r => new CCard(spade, r)).ToList();
            var westCards  = WestRanks .Select(r => new CCard(spade, r)).ToList();
            var eastCards  = EastRanks .Select(r => new CCard(spade, r)).ToList();

            // Uzupełnij do równej liczby kart treflami (filler)
            int liczbaLew = 3;
            while (westCards.Count < liczbaLew) westCards.Add(new CCard(club, CRank.FromValue(westCards.Count + 2)));
            while (eastCards.Count < liczbaLew) eastCards.Add(new CCard(club, CRank.FromValue(eastCards.Count + 2)));

            var north = new CHand(northCards);
            var south = new CHand(southCards);
            var west  = new CHand(westCards);
            var east  = new CHand(eastCards);

            var allStrategies = StrategyGenerator.GenerateAllStrategies(north, south, liczbaLew);

            // Ogranicz tylko do strategii: S:2 N:T, S:3 N:Q, S:4 N:A
            // (firstPlayerIdx, firstRank, secondPlayerIdx, secondRank) per lewa
            var presetMoves = new[]
            {
                (2, CRank.R2, 0, CRank.RT),  // lewa 0: S:2, N:T
                (2, CRank.R3, 0, CRank.RQ),  // lewa 1: S:3, N:Q
                (2, CRank.R4, 0, CRank.RA),  // lewa 2: S:4, N:A
            };
            var strategies = allStrategies.Where(st =>
            {
                var byLewa = st.Sequence.GroupBy(x => x.lewa).OrderBy(g => g.Key).ToList();
                if (byLewa.Count != presetMoves.Length) return false;
                for (int li = 0; li < byLewa.Count; li++)
                {
                    var moves = byLewa[li].ToList();
                    if (moves.Count != 2) return false;
                    if (moves[0].playerIdx != presetMoves[li].Item1) return false;
                    if (moves[0].card.Rank.Value != presetMoves[li].Item2.Value) return false;
                    if (moves[1].playerIdx != presetMoves[li].Item3) return false;
                    if (moves[1].card.Rank.Value != presetMoves[li].Item4.Value) return false;
                }
                return true;
            }).ToList();

            var results = new List<StrategyEvalResult>();
            foreach (var strategy in strategies)
            {
                var nH = new CHand(north);
                var eH = new CHand(east);
                var sH = new CHand(south);
                var wH = new CHand(west);

                int nsWins = PlayClockwiseFull(nH, eH, sH, wH, strategy, liczbaLew, strategy.Sequence[0].playerIdx);

                results.Add(new StrategyEvalResult
                {
                    Strategy       = strategy.ToString(),
                    ExpectedTricks = nsWins,
                    PTricks        = Enumerable.Range(0, liczbaLew + 1)
                                        .Select(k => k == nsWins ? 100.0 : 0.0).ToArray(),
                    MaxTricks      = liczbaLew,
                    Details        = new List<StrategyEvalDetail>
                    {
                        new StrategyEvalDetail
                        {
                            WDist      = string.Join(" ", WestRanks.Select(r => r.Symbol)),
                            EDist      = string.Join(" ", EastRanks.Select(r => r.Symbol)),
                            Probability = 100.0,
                            NSTricks   = nsWins
                        }
                    }
                });
            }

            // Sortowanie: P(≥TargetTricks) malejąco, potem EX malejąco
            results = results
                .OrderByDescending(r => GetProbabilityAtLeast(r, TargetTricks))
                .ThenByDescending(r => r.ExpectedTricks)
                .ToList();

            ViewBag.NorthStr     = string.Join(" ", NorthRanks.Select(r => r.Symbol));
            ViewBag.SouthStr     = string.Join(" ", SouthRanks.Select(r => r.Symbol));
            ViewBag.WestStr      = string.Join(" ", WestRanks .Select(r => r.Symbol));
            ViewBag.EastStr      = string.Join(" ", EastRanks .Select(r => r.Symbol));
            ViewBag.TargetTricks = TargetTricks;
            ViewBag.Best         = results.FirstOrDefault();

            return View(results);
        }

        // ── wszystkie metody rozgrywki są w StrategyPlayer ──────────────

        private static int PlayClockwiseFull(CHand nHand, CHand eHand, CHand sHand, CHand wHand,
            NSStrategy nsStrategy, int liczbaLew, int starter)
            => StrategyPlayer.PlayClockwiseFull(nHand, eHand, sHand, wHand, nsStrategy, liczbaLew, starter);
    }
}
