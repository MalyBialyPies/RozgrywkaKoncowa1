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
            // Najpierw spróbuj split po spacjach (np. "A Q T")
            var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length == 1)
                {
                    // Pojedynczy znak – szukaj rangi
                    var rank = CRank.FromSymbol(token);
                    if (rank != null) cards.Add(new CCard(CDenomination.Spade, rank));
                }
                else
                {
                    // Ciąg znaków bez spacji np. "AQT" lub "234" – iteruj po znakach
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

            // Jeśli brak parametrów – pokaż tylko formularz
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

            // Karty WE = wszystkie piki z wyjątkiem kart NS
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

        // Rozgrywka zgodnie z daną strategią NS, WE optymalnie
        private static int PlayWithStrategy(CHand nHand, CHand eHand, CHand sHand, CHand wHand, NSStrategy strategy, int liczbaLew)
        {
            // Kopie rąk
            var hands = new[] { new CHand(nHand), new CHand(eHand), new CHand(sHand), new CHand(wHand) };
            int nsTricks = 0;
            int trickCount = liczbaLew;
            var seq = strategy.Sequence;
            int seqIdx = 0;
            for (int lewa = 0; lewa < trickCount; lewa++)
            {
                // Kto wychodzi pierwszy w tej lewie (z sekwencji strategii)
                var first = seq[seqIdx];
                var second = seq[seqIdx + 1];
                seqIdx += 2;
                var trick = new List<(int player, CCard card)>();
                // NS zagrywają wg strategii (kolejność i wybór kart)
                trick.Add((first.playerIdx, hands[first.playerIdx][0]));
                trick.Add((second.playerIdx, hands[second.playerIdx][0]));
                // WE dokładają optymalnie (minimalizują lewy NS)
                var wePlayers = new[] { 1, 3 };
                foreach (var we in wePlayers)
                {
                    var hand = hands[we];
                    // Do koloru jeśli można
                    var toColor = trick[0].card.Denomination;
                    var hasColor = hand.Any(c => c.Denomination == toColor);
                    var options = hasColor ? hand.Where(c => c.Denomination == toColor).ToList() : hand.ToList();
                    // Wybierz kartę minimalizującą wygraną NS
                    CCard best = null;
                    int bestScore = int.MaxValue;
                    foreach (var card in options)
                    {
                        // Symulacja: dołożenie tej karty
                        trick.Add((we, card));
                        int winner = Winner(trick, CDenomination.Spade);
                        bool nsWin = winner == 0 || winner == 2;
                        int score = nsWin ? 1 : 0;
                        if (score < bestScore || best == null)
                        {
                            bestScore = score;
                            best = card;
                        }
                        trick.RemoveAt(trick.Count - 1);
                    }
                    trick.Add((we, best));
                }
                // Wyznacz zwycięzcę lewy
                int trickWinner = Winner(trick, CDenomination.Spade);
                if (trickWinner == 0 || trickWinner == 2) nsTricks++;
            }
            return nsTricks;
        }

        // Pełny minimaks dla WE przy zadanej strategii NS
        private static int PlayWithStrategyMinimaxWE(CHand nHand, CHand eHand, CHand sHand, CHand wHand, NSStrategy strategy, int liczbaLew)
        {
            var hands = new[] { new CHand(nHand), new CHand(eHand), new CHand(sHand), new CHand(wHand) };
            return PlayRecursive(hands, strategy.Sequence, 0, liczbaLew, 0);
        }

        // Produkcyjna wersja bez debugowania
        private static int PlayRecursive(CHand[] hands, List<(int playerIdx, CCard card, int lewa)> seq, int seqIdx, int liczbaLew, int nsTricks)
        {
            if (seqIdx >= seq.Count)
                return nsTricks;
            int lewa = seq[seqIdx].lewa;
            var trick = new List<(int player, CCard card)>();
            var first = seq[seqIdx];
            var second = seq[seqIdx + 1];
            int idxFirst = hands[first.playerIdx].FindIndex(c => c.Denomination == first.card.Denomination && c.Rank.Value == first.card.Rank.Value);
            int idxSecond = hands[second.playerIdx].FindIndex(c => c.Denomination == second.card.Denomination && c.Rank.Value == second.card.Rank.Value);
            var cardFirst = hands[first.playerIdx][idxFirst];
            var cardSecond = hands[second.playerIdx][idxSecond];
            hands[first.playerIdx].RemoveAt(idxFirst);
            hands[second.playerIdx].RemoveAt(idxSecond);
            trick.Add((first.playerIdx, cardFirst));
            trick.Add((second.playerIdx, cardSecond));

            int minNS = int.MaxValue;
            var eCards = hands[1];
            var wCards = hands[3];
            for (int ei = 0; ei < eCards.Count; ei++)
            {
                var eCard = eCards[ei];
                hands[1].RemoveAt(ei);
                for (int wi = 0; wi < wCards.Count; wi++)
                {
                    var wCard = wCards[wi];
                    hands[3].RemoveAt(wi);
                    var trickCopy = new List<(int player, CCard card)>(trick)
                    {
                        (1, eCard),
                        (3, wCard)
                    };
                    int winner = Winner(trickCopy, CDenomination.Spade);
                    int nsTricksNext = nsTricks + ((winner == 0 || winner == 2) ? 1 : 0);
                    int res = PlayRecursive(hands, seq, seqIdx + 2, liczbaLew, nsTricksNext);
                    if (res < minNS) minNS = res;
                    hands[3].Insert(wi, wCard); // undo
                }
                hands[1].Insert(ei, eCard); // undo
            }
            hands[second.playerIdx].Insert(idxSecond, cardSecond);
            hands[first.playerIdx].Insert(idxFirst, cardFirst);
            return minNS;
        }

        // Pełna rozgrywka zegarowa: starter, W, N, E, z dynamicznym nadbijaniem i strategią NS
        private static int PlayClockwiseFull_Debug(CHand[] hands, List<(int playerIdx, CCard card, int lewa)> nsStrategy, int nsSeqIdx, int liczbaLew, int nsTricks, int starter, List<string> debugLog)
        {
            if (nsTricks < 0) nsTricks = 0;
            if (liczbaLew == 0)
                return nsTricks;
            var trick = new List<(int player, CCard card)>();
            int[] order = new int[] { starter, (starter + 1) % 4, (starter + 2) % 4, (starter + 3) % 4 };
            int localNsSeqIdx = nsSeqIdx;
            // Rozgałęzienie po wszystkich możliwych dołożeniach WE
            return PlayTrickBranch_Debug(hands, nsStrategy, localNsSeqIdx, liczbaLew, nsTricks, order, 0, trick, debugLog, starter);
        }

        // Produkcyjna wersja rozgrywki zegarowej (bez debugowania, minimalizuje lewy NS)
        private static int PlayClockwiseFull(CHand nHand, CHand eHand, CHand sHand, CHand wHand, NSStrategy nsStrategy, int liczbaLew, int starter)
        {
            var hands = new[] { new CHand(nHand), new CHand(eHand), new CHand(sHand), new CHand(wHand) };
            return PlayTrickBranch(hands, nsStrategy.Sequence, 0, liczbaLew, 0, starter);
        }

        // Rozgałęzienie po wszystkich możliwych dołożeniach WE (produkcyjnie, minimalizuje lewy NS)
        private static int PlayTrickBranch(CHand[] hands, List<(int playerIdx, CCard card, int lewa)> nsStrategy, int nsSeqIdx, int liczbaLew, int nsTricks, int starter)
        {
            if (liczbaLew == 0)
                return nsTricks;
            var trick = new List<(int player, CCard card)>();
            int[] order = new int[] { starter, (starter + 1) % 4, (starter + 2) % 4, (starter + 3) % 4 };
            int localNsSeqIdx = nsSeqIdx;
            return PlayTrickBranchInner(hands, nsStrategy, localNsSeqIdx, liczbaLew, nsTricks, order, 0, trick);
        }

        private static int PlayTrickBranchInner(CHand[] hands, List<(int playerIdx, CCard card, int lewa)> nsStrategy, int nsSeqIdx, int liczbaLew, int nsTricks, int[] order, int pos, List<(int player, CCard card)> trick)
        {
            if (pos == 4)
            {
                int winnerIdx = Winner(trick, CDenomination.Spade);
                int nsTricksNext = nsTricks + ((winnerIdx == 0 || winnerIdx == 2) ? 1 : 0);
                int nextStarter = nsSeqIdx < nsStrategy.Count ? nsStrategy[nsSeqIdx].playerIdx : winnerIdx;
                return PlayTrickBranch(hands, nsStrategy, nsSeqIdx, liczbaLew - 1, nsTricksNext, nextStarter);
            }
            int player = order[pos];
            if (player == 0 || player == 2) // NS
            {
                var nsHand = hands[player];
                // Sprawdź czy WE zagrało figurę którą można nadbić
                var najlepszeWE = trick
                    .Where(x => (x.player == 1 || x.player == 3) && x.card.Denomination == CDenomination.Spade)
                    .OrderByDescending(x => x.card.Rank.Value)
                    .Select(x => x.card)
                    .FirstOrDefault();
                if (najlepszeWE != null)
                {
                    // Szukamy najniższej wyższej karty w ręce NS
                    var nadbicie = nsHand
                        .Where(c => c.Denomination == CDenomination.Spade && c.Rank.Value > najlepszeWE.Rank.Value)
                        .OrderBy(c => c.Rank.Value)
                        .FirstOrDefault();
                    // Graj kartą nadbicia tylko jeśli jest niższa niż planowana (oszczędzamy wyższe)
                    var planned = nsSeqIdx < nsStrategy.Count && nsStrategy[nsSeqIdx].playerIdx == player
                        ? nsStrategy[nsSeqIdx].card : null;
                    bool planowanaBije = planned != null && planned.Denomination == CDenomination.Spade
                        && planned.Rank.Value > najlepszeWE.Rank.Value;
                    bool nadbicieTansze = nadbicie != null && planned != null
                        && nadbicie.Rank.Value < planned.Rank.Value;
                    if (nadbicie != null && (!planowanaBije || nadbicieTansze))
                    {
                        // Zamień planowaną kartę (nsSeqIdx) z kartą nadbicia w sekwencji
                        int nadbicieSeqIdx = nsStrategy.FindIndex(nsSeqIdx, x =>
                            x.playerIdx == player &&
                            x.card.Denomination == nadbicie.Denomination &&
                            x.card.Rank.Value == nadbicie.Rank.Value);
                        if (nadbicieSeqIdx > nsSeqIdx)
                        {
                            var tmp = nsStrategy[nsSeqIdx];
                            nsStrategy[nsSeqIdx] = nsStrategy[nadbicieSeqIdx];
                            nsStrategy[nadbicieSeqIdx] = tmp;
                        }

                        int idxNad = nsHand.FindIndex(c => c.Denomination == nadbicie.Denomination && c.Rank.Value == nadbicie.Rank.Value);
                        nsHand.RemoveAt(idxNad);
                        trick.Add((player, nadbicie));
                        int res = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick);
                        trick.RemoveAt(trick.Count - 1);
                        nsHand.Insert(idxNad, nadbicie);
                        // Cofnij zamianę
                        if (nadbicieSeqIdx > nsSeqIdx)
                        {
                            var tmp = nsStrategy[nsSeqIdx];
                            nsStrategy[nsSeqIdx] = nsStrategy[nadbicieSeqIdx];
                            nsStrategy[nadbicieSeqIdx] = tmp;
                        }
                        return res;
                    }
                }
                // Brak nadbicia – graj wg strategii
                if (nsSeqIdx >= nsStrategy.Count || nsStrategy[nsSeqIdx].playerIdx != player)
                    return nsTricks;
                var card = nsStrategy[nsSeqIdx].card;
                int idx = nsHand.FindIndex(c => c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                if (idx < 0) return nsTricks;
                nsHand.RemoveAt(idx);
                trick.Add((player, card));
                int res2 = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick);
                trick.RemoveAt(trick.Count - 1);
                nsHand.Insert(idx, card);
                return res2;
            }
            else // WE minimalizuje lewy NS
            {
                var hand = hands[player];
                var toColor = trick.Count > 0 ? trick[0].card.Denomination : CDenomination.Spade;
                var hasColor = hand.Any(c => c.Denomination == toColor);
                var options = hasColor ? hand.Where(c => c.Denomination == toColor).ToList() : hand.ToList();
                int minNS = int.MaxValue;
                foreach (var card in options)
                {
                    int idx = hand.FindIndex(c => c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                    hand.RemoveAt(idx);
                    trick.Add((player, card));
                    int res = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, pos + 1, trick);
                    if (res < minNS) minNS = res;
                    trick.RemoveAt(trick.Count - 1);
                    hand.Insert(idx, card);
                }
                return minNS == int.MaxValue ? nsTricks : minNS;
            }
        }

        // Dodaj lepsze logowanie: numer lewy, starter, kolejność graczy, nadbicia, winner, pusta linia po każdej lewie
        private static int PlayTrickBranch_Debug(CHand[] hands, List<(int playerIdx, CCard card, int lewa)> nsStrategy, int nsSeqIdx, int liczbaLew, int nsTricks, int[] order, int pos, List<(int player, CCard card)> trick, List<string> debugLog, int starter)
        {
            if (pos == 0)
            {
                debugLog.Add("");
                debugLog.Add($"--- Lewa {liczbaLew} (starter: {PlayerName(starter)}) ---");
            }
            if (pos == 4)
            {
                var sklad = string.Join(", ", trick.Select(x => $"{PlayerName(x.player)}:{x.card}"));
                debugLog.Add($"Kolejność: {sklad}");
                int winnerIdx = Winner(trick, CDenomination.Spade);
                debugLog.Add($"Winner: {PlayerName(winnerIdx)}");
                int nsTricksNext = nsTricks + ((winnerIdx == 0 || winnerIdx == 2) ? 1 : 0);
                // Starter następnej lewy pochodzi ze strategii (N lub S), nie od winnera
                int nextStarter = nsSeqIdx < nsStrategy.Count ? nsStrategy[nsSeqIdx].playerIdx : winnerIdx;
                return PlayClockwiseFull_Debug(hands, nsStrategy, nsSeqIdx, liczbaLew - 1, nsTricksNext, nextStarter, debugLog);
            }
            int player = order[pos];
            if (player == 0 || player == 2) // NS
            {
                var nsHand = hands[player];
                var najlepszeWE = trick
                    .Where(x => (x.player == 1 || x.player == 3) && x.card.Denomination == CDenomination.Spade)
                    .OrderByDescending(x => x.card.Rank.Value)
                    .Select(x => x.card)
                    .FirstOrDefault();
                if (najlepszeWE != null)
                {
                    var nadbicie = nsHand
                        .Where(c => c.Denomination == CDenomination.Spade && c.Rank.Value > najlepszeWE.Rank.Value)
                        .OrderBy(c => c.Rank.Value)
                        .FirstOrDefault();
                    var planned = nsSeqIdx < nsStrategy.Count && nsStrategy[nsSeqIdx].playerIdx == player
                        ? nsStrategy[nsSeqIdx].card : null;
                    bool planowanaBije = planned != null && planned.Denomination == CDenomination.Spade
                        && planned.Rank.Value > najlepszeWE.Rank.Value;
                    bool nadbicieTansze = nadbicie != null && planned != null
                        && nadbicie.Rank.Value < planned.Rank.Value;
                    if (nadbicie != null && (!planowanaBije || nadbicieTansze))
                    {
                        int nadbicieSeqIdx = nsStrategy.FindIndex(nsSeqIdx, x =>
                            x.playerIdx == player &&
                            x.card.Denomination == nadbicie.Denomination &&
                            x.card.Rank.Value == nadbicie.Rank.Value);
                        if (nadbicieSeqIdx > nsSeqIdx)
                        {
                            var tmp = nsStrategy[nsSeqIdx];
                            nsStrategy[nsSeqIdx] = nsStrategy[nadbicieSeqIdx];
                            nsStrategy[nadbicieSeqIdx] = tmp;
                        }

                        int idxNad = nsHand.FindIndex(c => c.Denomination == nadbicie.Denomination && c.Rank.Value == nadbicie.Rank.Value);
                        nsHand.RemoveAt(idxNad);
                        trick.Add((player, nadbicie));
                        debugLog.Add($"{PlayerName(player)}: {nadbicie} (nadbicie)");
                        int res = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                        trick.RemoveAt(trick.Count - 1);
                        nsHand.Insert(idxNad, nadbicie);
                        if (nadbicieSeqIdx > nsSeqIdx)
                        {
                            var tmp = nsStrategy[nsSeqIdx];
                            nsStrategy[nsSeqIdx] = nsStrategy[nadbicieSeqIdx];
                            nsStrategy[nadbicieSeqIdx] = tmp;
                        }
                        return res;
                    }
                }
                // Brak nadbicia – graj wg strategii
                if (nsSeqIdx >= nsStrategy.Count || nsStrategy[nsSeqIdx].playerIdx != player)
                    return nsTricks;
                var card = nsStrategy[nsSeqIdx].card;
                int idx = nsHand.FindIndex(c => c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                if (idx < 0) return nsTricks;
                nsHand.RemoveAt(idx);
                trick.Add((player, card));
                debugLog.Add($"{PlayerName(player)}: {card}");
                int res2 = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                trick.RemoveAt(trick.Count - 1);
                nsHand.Insert(idx, card);
                return res2;
            }
            else // WE minimalizuje lewy NS
            {
                var hand = hands[player];
                var toColor = trick.Count > 0 ? trick[0].card.Denomination : CDenomination.Spade;
                var hasColor = hand.Any(c => c.Denomination == toColor);
                var options = hasColor ? hand.Where(c => c.Denomination == toColor).ToList() : hand.ToList();
                int minNS = int.MaxValue;
                foreach (var card in options)
                {
                    int idx = hand.FindIndex(c => c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                    hand.RemoveAt(idx);
                    trick.Add((player, card));
                    debugLog.Add($"{PlayerName(player)}: {card}");
                    int res = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                    if (res < minNS) minNS = res;
                    trick.RemoveAt(trick.Count - 1);
                    hand.Insert(idx, card);
                }
                return minNS == int.MaxValue ? nsTricks : minNS;
            }
        }

        // --- DODAJ NA KOŃCU PLIKU ---
        private static string PlayerName(int idx)
        {
            return idx switch
            {
                0 => "N",
                1 => "E",
                2 => "S",
                3 => "W",
                _ => "?"
            };
        }

        private static int Winner(List<(int player, CCard card)> trick, CDenomination trump)
        {
            var leadColor = trick[0].card.Denomination;
            int winner = trick[0].player;
            CCard best = trick[0].card;
            foreach (var (player, card) in trick.Skip(1))
            {
                if (trump != null && trump != CDenomination.NT && card.Denomination == trump)
                {
                    if (best.Denomination != trump || card.Rank.Value > best.Rank.Value)
                    {
                        best = card;
                        winner = player;
                    }
                }
                else if ((best.Denomination != trump || trump == CDenomination.NT) && card.Denomination == leadColor && card.Rank.Value > best.Rank.Value)
                {
                    best = card;
                    winner = player;
                }
            }
            return winner;
        }
    }
}
           
