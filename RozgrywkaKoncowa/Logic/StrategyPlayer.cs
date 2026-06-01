using RozgrywkaKoncowa.Models;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Logic
{
    public static class StrategyPlayer
    {
        public static string PlayerName(int idx) => idx switch
        {
            0 => "N",
            1 => "E",
            2 => "S",
            3 => "W",
            _ => "?"
        };

        public static int Winner(List<(int player, CCard card)> trick, CDenomination trump)
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
                         && card.Denomination == leadColor && card.Rank.Value > best.Rank.Value)
                { best = card; winner = player; }
            }
            return winner;
        }

        // Przesuwa kartę nadbicia na pierwszy slot gracza (od nsSeqIdx wzwyż),
        // pozostałe karty gracza zachowują oryginalną kolejność.
        // Zwraca origEntries do cofnięcia (undo).
        public static List<(int playerIdx, CCard card, int lewa)> ApplyNadbicieRotation(
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            int nsSeqIdx, int player, CCard nadbicie,
            out List<int> playerSlots)
        {
            playerSlots = new List<int>();
            for (int k = nsSeqIdx; k < nsStrategy.Count; k++)
                if (nsStrategy[k].playerIdx == player)
                    playerSlots.Add(k);
            var origEntries = playerSlots.Select(i => nsStrategy[i]).ToList();
            int nadbicieLocalIdx = origEntries.FindIndex(x =>
                x.card.Denomination == nadbicie.Denomination &&
                x.card.Rank.Value == nadbicie.Rank.Value);
            var newEntries = new List<(int playerIdx, CCard card, int lewa)> { origEntries[nadbicieLocalIdx] };
            for (int k = 0; k < origEntries.Count; k++)
                if (k != nadbicieLocalIdx) newEntries.Add(origEntries[k]);
            for (int k = 0; k < playerSlots.Count; k++)
                nsStrategy[playerSlots[k]] = newEntries[k];
            return origEntries;
        }

        public static void UndoNadbicieRotation(
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            List<int> playerSlots,
            List<(int playerIdx, CCard card, int lewa)> origEntries)
        {
            for (int k = 0; k < playerSlots.Count; k++)
                nsStrategy[playerSlots[k]] = origEntries[k];
        }

        public static int PlayClockwiseFull(
            CHand nHand, CHand eHand, CHand sHand, CHand wHand,
            NSStrategy nsStrategy, int liczbaLew, int starter)
        {
            var hands = new[] { new CHand(nHand), new CHand(eHand), new CHand(sHand), new CHand(wHand) };
            return PlayTrickBranch(hands, nsStrategy.Sequence, 0, liczbaLew, 0, starter);
        }

        public static int PlayClockwiseFull_Debug(
            CHand[] hands,
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            int nsSeqIdx, int liczbaLew, int nsTricks, int starter,
            List<string> debugLog)
        {
            if (nsTricks < 0) nsTricks = 0;
            if (liczbaLew == 0) return nsTricks;
            var trick = new List<(int player, CCard card)>();
            int[] order = { starter, (starter + 1) % 4, (starter + 2) % 4, (starter + 3) % 4 };
            return PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, 0, trick, debugLog, starter);
        }

        private static int PlayTrickBranch(
            CHand[] hands,
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            int nsSeqIdx, int liczbaLew, int nsTricks, int starter)
        {
            if (liczbaLew == 0) return nsTricks;
            int[] order = { starter, (starter + 1) % 4, (starter + 2) % 4, (starter + 3) % 4 };
            return PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, 0,
                new List<(int, CCard)>());
        }

        private static int PlayTrickBranchInner(
            CHand[] hands,
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            int nsSeqIdx, int liczbaLew, int nsTricks,
            int[] order, int pos, List<(int player, CCard card)> trick)
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
                // Sprawdź jaka karta jest planowana w strategii
                CCard planowanaKarta = null;
                if (nsSeqIdx < nsStrategy.Count && nsStrategy[nsSeqIdx].playerIdx == player)
                {
                    planowanaKarta = nsStrategy[nsSeqIdx].card;
                }
                // Znajdź najwyższą kartę WE w pikach
                var najlepszeWE = trick
                    .Where(x => (x.player == 1 || x.player == 3) && x.card.Denomination == CDenomination.Spade)
                    .OrderByDescending(x => x.card.Rank.Value)
                    .Select(x => x.card)
                    .FirstOrDefault();
                // Nadbijanie: jeśli WE zagrało kartę wyższą niż planowana karta ze strategii
                if (najlepszeWE != null && planowanaKarta != null && 
                    planowanaKarta.Denomination == CDenomination.Spade &&
                    najlepszeWE.Rank.Value > planowanaKarta.Rank.Value)
                {
                    // Znajdź najniższą kartę wyższą od karty przeciwnika
                    var nadbicie = nsHand
                        .Where(c => c.Denomination == CDenomination.Spade && c.Rank.Value > najlepszeWE.Rank.Value)
                        .OrderBy(c => c.Rank.Value)
                        .FirstOrDefault();
                    if (nadbicie != null)
                    {
                        // Nadbijamy najniższą wyższą kartą
                        var origEntries = ApplyNadbicieRotation(nsStrategy, nsSeqIdx, player, nadbicie, out var playerSlots);
                        int idxNad = nsHand.FindIndex(c =>
                            c.Denomination == nadbicie.Denomination && c.Rank.Value == nadbicie.Rank.Value);
                        nsHand.RemoveAt(idxNad);
                        trick.Add((player, nadbicie));
                        int res = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick);
                        trick.RemoveAt(trick.Count - 1);
                        nsHand.Insert(idxNad, nadbicie);
                        UndoNadbicieRotation(nsStrategy, playerSlots, origEntries);
                        return res;
                    }
                    else
                    {
                        // Nie mamy wyższej karty - dorzucamy najniższą pikę lub inny kolor
                        var najnizsza = nsHand
                            .Where(c => c.Denomination == CDenomination.Spade)
                            .OrderBy(c => c.Rank.Value)
                            .FirstOrDefault();
                        if (najnizsza == null)
                            najnizsza = nsHand.OrderBy(c => c.Rank.Value).FirstOrDefault();
                        if (najnizsza != null)
                        {
                            var origEntries = ApplyNadbicieRotation(nsStrategy, nsSeqIdx, player, najnizsza, out var playerSlots);
                            int idxNaj = nsHand.FindIndex(c =>
                                c.Denomination == najnizsza.Denomination && c.Rank.Value == najnizsza.Rank.Value);
                            nsHand.RemoveAt(idxNaj);
                            trick.Add((player, najnizsza));
                            int res = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick);
                            trick.RemoveAt(trick.Count - 1);
                            nsHand.Insert(idxNaj, najnizsza);
                            UndoNadbicieRotation(nsStrategy, playerSlots, origEntries);
                            return res;
                        }
                    }
                }
                // Gramy zgodnie ze strategią
                if (nsSeqIdx >= nsStrategy.Count || nsStrategy[nsSeqIdx].playerIdx != player)
                    return nsTricks;
                var card = nsStrategy[nsSeqIdx].card;
                int idx = nsHand.FindIndex(c =>
                    c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
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

                // Optymalizacja: sortuj po randze malejąco i cachuj wynik dla sekwensów
                options = options.OrderByDescending(c => c.Rank.Value).ToList();

                int minNS = int.MaxValue;
                int? cachedResult = null;

                for (int i = 0; i < options.Count; i++)
                {
                    var card = options[i];

                    // Sprawdź czy to początek sekwensu (karta o 1 niższa niż poprzednia)
                    bool isSequenceContinuation = i > 0 && 
                        options[i - 1].Rank.Value == card.Rank.Value + 1 &&
                        options[i - 1].Denomination == card.Denomination;

                    int res;
                    if (isSequenceContinuation && cachedResult.HasValue)
                    {
                        // Użyj cachowanego wyniku dla sekwensu
                        res = cachedResult.Value;
                    }
                    else
                    {
                        // Oblicz rekurencyjnie
                        int idx = hand.FindIndex(c =>
                            c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                        hand.RemoveAt(idx);
                        trick.Add((player, card));
                        res = PlayTrickBranchInner(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, pos + 1, trick);
                        trick.RemoveAt(trick.Count - 1);
                        hand.Insert(idx, card);

                        // Cachuj wynik dla potencjalnego sekwensu
                        cachedResult = res;
                    }

                    if (res < minNS) minNS = res;
                }

                return minNS == int.MaxValue ? nsTricks : minNS;
            }
        }

        private static int PlayTrickBranch_Debug(
            CHand[] hands,
            List<(int playerIdx, CCard card, int lewa)> nsStrategy,
            int nsSeqIdx, int liczbaLew, int nsTricks,
            int[] order, int pos, List<(int player, CCard card)> trick,
            List<string> debugLog, int starter)
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
                int nextStarter = nsSeqIdx < nsStrategy.Count ? nsStrategy[nsSeqIdx].playerIdx : winnerIdx;
                return PlayClockwiseFull_Debug(hands, nsStrategy, nsSeqIdx, liczbaLew - 1, nsTricksNext, nextStarter, debugLog);
            }
            int player = order[pos];
            if (player == 0 || player == 2) // NS
            {
                var nsHand = hands[player];
                // Sprawdź jaka karta jest planowana w strategii
                CCard planowanaKarta = null;
                if (nsSeqIdx < nsStrategy.Count && nsStrategy[nsSeqIdx].playerIdx == player)
                {
                    planowanaKarta = nsStrategy[nsSeqIdx].card;
                }
                // Znajdź najwyższą kartę WE w pikach
                var najlepszeWE = trick
                    .Where(x => (x.player == 1 || x.player == 3) && x.card.Denomination == CDenomination.Spade)
                    .OrderByDescending(x => x.card.Rank.Value)
                    .Select(x => x.card)
                    .FirstOrDefault();
                // Nadbijanie: jeśli WE zagrało kartę wyższą niż planowana karta ze strategii
                if (najlepszeWE != null && planowanaKarta != null && 
                    planowanaKarta.Denomination == CDenomination.Spade &&
                    najlepszeWE.Rank.Value > planowanaKarta.Rank.Value)
                {
                    // Znajdź najniższą kartę wyższą od karty przeciwnika
                    var nadbicie = nsHand
                        .Where(c => c.Denomination == CDenomination.Spade && c.Rank.Value > najlepszeWE.Rank.Value)
                        .OrderBy(c => c.Rank.Value)
                        .FirstOrDefault();
                    if (nadbicie != null)
                    {
                        // Nadbijamy najniższą wyższą kartą
                        var origEntries = ApplyNadbicieRotation(nsStrategy, nsSeqIdx, player, nadbicie, out var playerSlots);
                        int idxNad = nsHand.FindIndex(c =>
                            c.Denomination == nadbicie.Denomination && c.Rank.Value == nadbicie.Rank.Value);
                        nsHand.RemoveAt(idxNad);
                        trick.Add((player, nadbicie));
                        debugLog.Add($"{PlayerName(player)}: {nadbicie} (nadbicie zamiast {planowanaKarta})");
                        int res = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                        trick.RemoveAt(trick.Count - 1);
                        nsHand.Insert(idxNad, nadbicie);
                        UndoNadbicieRotation(nsStrategy, playerSlots, origEntries);
                        return res;
                    }
                    else
                    {
                        // Nie mamy wyższej karty - dorzucamy najniższą pikę lub inny kolor
                        var najnizsza = nsHand
                            .Where(c => c.Denomination == CDenomination.Spade)
                            .OrderBy(c => c.Rank.Value)
                            .FirstOrDefault();
                        if (najnizsza == null)
                            najnizsza = nsHand.OrderBy(c => c.Rank.Value).FirstOrDefault();
                        if (najnizsza != null)
                        {
                            var origEntries = ApplyNadbicieRotation(nsStrategy, nsSeqIdx, player, najnizsza, out var playerSlots);
                            int idxNaj = nsHand.FindIndex(c =>
                                c.Denomination == najnizsza.Denomination && c.Rank.Value == najnizsza.Rank.Value);
                            nsHand.RemoveAt(idxNaj);
                            trick.Add((player, najnizsza));
                            debugLog.Add($"{PlayerName(player)}: {najnizsza} (najniższa, brak nadbicia dla {planowanaKarta})");
                            int res = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx + 1, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                            trick.RemoveAt(trick.Count - 1);
                            nsHand.Insert(idxNaj, najnizsza);
                            UndoNadbicieRotation(nsStrategy, playerSlots, origEntries);
                            return res;
                        }
                    }
                }
                // Gramy zgodnie ze strategią
                if (nsSeqIdx >= nsStrategy.Count || nsStrategy[nsSeqIdx].playerIdx != player)
                    return nsTricks;
                var card = nsStrategy[nsSeqIdx].card;
                int idx = nsHand.FindIndex(c =>
                    c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
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

                // Optymalizacja: sortuj po randze malejąco i cachuj wynik dla sekwensów
                options = options.OrderByDescending(c => c.Rank.Value).ToList();

                int minNS = int.MaxValue;
                int? cachedResult = null;

                for (int i = 0; i < options.Count; i++)
                {
                    var card = options[i];

                    // Sprawdź czy to początek sekwensu (karta o 1 niższa niż poprzednia)
                    bool isSequenceContinuation = i > 0 && 
                        options[i - 1].Rank.Value == card.Rank.Value + 1 &&
                        options[i - 1].Denomination == card.Denomination;

                    int res;
                    if (isSequenceContinuation && cachedResult.HasValue)
                    {
                        // Użyj cachowanego wyniku dla sekwensu
                        res = cachedResult.Value;
                        debugLog.Add($"{PlayerName(player)}: {card} (równoważne z poprzednią kartą w sekwensie)");
                    }
                    else
                    {
                        // Oblicz rekurencyjnie
                        int idx = hand.FindIndex(c =>
                            c.Denomination == card.Denomination && c.Rank.Value == card.Rank.Value);
                        hand.RemoveAt(idx);
                        trick.Add((player, card));
                        debugLog.Add($"{PlayerName(player)}: {card}");
                        res = PlayTrickBranch_Debug(hands, nsStrategy, nsSeqIdx, liczbaLew, nsTricks, order, pos + 1, trick, debugLog, starter);
                        trick.RemoveAt(trick.Count - 1);
                        hand.Insert(idx, card);

                        // Cachuj wynik dla potencjalnego sekwensu
                        cachedResult = res;
                    }

                    if (res < minNS) minNS = res;
                }

                return minNS == int.MaxValue ? nsTricks : minNS;
            }
        }
    }
}
