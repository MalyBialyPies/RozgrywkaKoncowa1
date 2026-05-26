using RozgrywkaKoncowa.Models;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Logic
{
    public class WeightedDeal
    {
        /// <summary>Rêce wszystkich 4 graczy: [0]=N, [1]=E, [2]=S, [3]=W</summary>
        public CHand[] Hands { get; set; }
        /// <summary>Waga (prawdopodobieñstwo a priori) tego uk³adu.</summary>
        public decimal Weight { get; set; }
    }

    public class SingleDummyResult
    {
        public decimal ExpectedNSWins { get; set; }
        /// <summary>Rozk³ad: liczba lew NS ? wa¿ona szansa (%).</summary>
        public List<(int NSWins, decimal WeightedProbability)> Distribution { get; set; } = new();
        /// <summary>Wynik NS dla ka¿dego uk³adu (w tej samej kolejnoœci co wejœciowa lista deals).</summary>
        public List<int> NSWinsPerDeal { get; set; } = new();
    }

    /// <summary>
    /// Single-dummy solver:
    ///   NS nie widzi kart WE, podejmuje jedn¹ wspóln¹ decyzjê optymaln¹
    ///   dla wartoœci oczekiwanej po WSZYSTKICH mo¿liwych uk³adach WE.
    ///   WE w ka¿dym uk³adzie gra double-dummy (optymalnie dla siebie).
    ///   Po ka¿dej lewie NS wybiera kto prowadzi nastêpn¹ (komunikacja boczna).
    /// </summary>
    public static class SingleDummySolver
    {
        public static SingleDummyResult Solve(List<WeightedDeal> deals, CDenomination trump)
        {
            var states = deals.Select(d => new DealState
            {
                Hands = d.Hands.Select(h => new CHand(h)).ToArray(),
                Weight = d.Weight,
                NSWins = 0,
                WEWins = 0
            }).ToList();

            SolveFromNSLead(states, trump);

            var distribution = states
                .GroupBy(s => s.NSWins)
                .Select(g => (g.Key, g.Sum(s => s.Weight) * 100m))
                .OrderByDescending(x => x.Key)
                .ToList();

            return new SingleDummyResult
            {
                ExpectedNSWins = states.Sum(s => s.Weight * s.NSWins),
                Distribution = distribution,
                NSWinsPerDeal = states.Select(s => s.NSWins).ToList()
            };
        }

        // ?? Stan pojedynczego uk³adu ?????????????????????????????????????????

        private class DealState
        {
            public CHand[] Hands { get; set; }
            public decimal Weight { get; set; }
            public int NSWins { get; set; }
            public int WEWins { get; set; }

            public DealState DeepCopy() => new DealState
            {
                Hands = Hands.Select(h => new CHand(h)).ToArray(),
                Weight = Weight,
                NSWins = NSWins,
                WEWins = WEWins
            };
        }

        // ?? NS wybiera kto prowadzi lewê (wspólna decyzja) ??????????????????

        private static void SolveFromNSLead(List<DealState> states, CDenomination trump)
        {
            if (states[0].Hands[0].Count == 0 && states[0].Hands[2].Count == 0)
                return;

            decimal bestExpected = decimal.MinValue;
            List<DealState> bestResultStates = null;

            foreach (var starter in new[] { 0, 2 })
            {
                if (states[0].Hands[starter].Count == 0) continue;

                var trialStates = states.Select(s => s.DeepCopy()).ToList();
                PlayTrickStep(trialStates, starter, trump, new List<(int, CCard)>());

                decimal expected = trialStates.Sum(s => s.Weight * s.NSWins);
                if (expected > bestExpected)
                {
                    bestExpected = expected;
                    bestResultStates = trialStates;
                }
            }

            if (bestResultStates != null)
                CopyResults(bestResultStates, states);
        }

        // ?? Krok rozgrywki lewy ??????????????????????????????????????????????

        private static void PlayTrickStep(
            List<DealState> states,
            int starter,
            CDenomination trump,
            List<(int player, CCard card)> trick)
        {
            if (trick.Count == 4)
            {
                int winner = WinnerOf(trick, trump);
                bool nsWon = winner == 0 || winner == 2;
                foreach (var s in states)
                    if (nsWon) s.NSWins++; else s.WEWins++;

                // Kolejna lewa – NS wybiera kto prowadzi
                SolveFromNSLead(states, trump);
                return;
            }

            int currentPlayer = (starter + trick.Count) % 4;
            bool isNS = currentPlayer == 0 || currentPlayer == 2;

            if (isNS)
                PlayNSStep(states, starter, trump, trick, currentPlayer);
            else
                PlayWEStep(states, starter, trump, trick, currentPlayer);
        }

        // ?? NS zagrywa kartê: wspólna decyzja po wszystkich uk³adach ????????

        private static void PlayNSStep(
            List<DealState> states,
            int starter,
            CDenomination trump,
            List<(int player, CCard card)> trick,
            int nsPlayer)
        {
            var hand = states[0].Hands[nsPlayer];
            var options = GetLegalCards(hand, trick);

            decimal bestExpected = decimal.MinValue;
            CCard bestCard = null;
            List<DealState> bestResultStates = null;

            foreach (var card in options)
            {
                foreach (var s in states) s.Hands[nsPlayer].Remove(card);
                trick.Add((nsPlayer, card));

                var trialStates = states.Select(s => s.DeepCopy()).ToList();
                PlayTrickStep(trialStates, starter, trump, new List<(int, CCard)>(trick));

                decimal expected = trialStates.Sum(s => s.Weight * s.NSWins);
                if (expected > bestExpected ||
                    (expected == bestExpected && bestCard != null && card.Rank.Value < bestCard.Rank.Value))
                {
                    bestExpected = expected;
                    bestCard = card;
                    bestResultStates = trialStates;
                }

                trick.RemoveAt(trick.Count - 1);
                foreach (var s in states) s.Hands[nsPlayer].Add(card);
            }

            if (bestResultStates != null)
                CopyResults(bestResultStates, states);
        }

        // ?? WE zagrywa kartê: ka¿dy uk³ad niezale¿nie (double-dummy) ?????????

        private static void PlayWEStep(
            List<DealState> states,
            int starter,
            CDenomination trump,
            List<(int player, CCard card)> trick,
            int wePlayer)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var singleList = new List<DealState> { states[i] };
                PlayWEStepSingle(singleList, starter, trump, new List<(int, CCard)>(trick), wePlayer);
                states[i] = singleList[0];
            }
        }

        private static void PlayWEStepSingle(
            List<DealState> states,
            int starter,
            CDenomination trump,
            List<(int player, CCard card)> trick,
            int currentPlayer)
        {
            if (!IsWE(currentPlayer))
            {
                // Kolej NS – w kontekœcie single-deal u¿ywamy tej samej logiki NS
                PlayNSStep(states, starter, trump, trick, currentPlayer);
                return;
            }

            var state = states[0];
            var hand = state.Hands[currentPlayer];
            var options = GetLegalCards(hand, trick);

            int bestNS = int.MaxValue;
            CCard bestCard = null;
            DealState bestResult = null;

            foreach (var card in options)
            {
                hand.Remove(card);
                trick.Add((currentPlayer, card));

                var trialState = state.DeepCopy();
                var trialList = new List<DealState> { trialState };
                var trialTrick = new List<(int, CCard)>(trick);
                int next = (currentPlayer + 1) % 4;

                PlayTrickStep(trialList, starter, trump, trialTrick);

                int ns = trialList[0].NSWins;
                if (ns < bestNS ||
                    (ns == bestNS && bestCard != null && card.Rank.Value < bestCard.Rank.Value))
                {
                    bestNS = ns;
                    bestCard = card;
                    bestResult = trialList[0];
                }

                trick.RemoveAt(trick.Count - 1);
                hand.Add(card);
            }

            if (bestResult != null)
                states[0] = bestResult;
        }

        // ?? Pomocnicze ???????????????????????????????????????????????????????

        private static bool IsWE(int player) => player == 1 || player == 3;

        private static List<CCard> GetLegalCards(CHand hand, List<(int player, CCard card)> trick)
        {
            if (trick.Count == 0) return hand.ToList();
            var leadColor = trick[0].card.Denomination;
            return hand.Any(c => c.Denomination == leadColor)
                ? hand.Where(c => c.Denomination == leadColor).ToList()
                : hand.ToList();
        }

        private static int WinnerOf(List<(int player, CCard card)> trick, CDenomination trump)
        {
            var leadColor = trick[0].card.Denomination;
            int winner = trick[0].player;
            CCard best = trick[0].card;
            foreach (var (player, card) in trick.Skip(1))
            {
                if (trump != null && trump != CDenomination.NT && card.Denomination == trump)
                {
                    if (best.Denomination != trump || card.Rank.Value > best.Rank.Value)
                    { best = card; winner = player; }
                }
                else if ((best.Denomination != trump || trump == CDenomination.NT)
                         && card.Denomination == leadColor
                         && card.Rank.Value > best.Rank.Value)
                {
                    best = card; winner = player;
                }
            }
            return winner;
        }

        private static void CopyResults(List<DealState> source, List<DealState> target)
        {
            for (int i = 0; i < target.Count; i++)
            {
                target[i].Hands = source[i].Hands;
                target[i].NSWins = source[i].NSWins;
                target[i].WEWins = source[i].WEWins;
            }
        }
    }
}
