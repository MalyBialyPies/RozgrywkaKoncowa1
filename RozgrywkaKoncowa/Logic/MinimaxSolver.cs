using RozgrywkaKoncowa.Models;
using System.Collections.Generic;
using System.Linq;

namespace RozgrywkaKoncowa.Logic
{
    public class MinimaxResult
    {
        public int NSWins { get; set; }
        public List<string> SequenceLog { get; set; } = new();
    }

    public static class MinimaxSolver
    {
        public static MinimaxResult Solve(CBoard board)
        {
            // Deep copy hands
            var hands = board.Hands.Select(h => new CHand(h)).ToArray();
            var trump = board.Contract?.Denomination;
            var log = new List<string>();
            int maxNS = -1;
            List<string> bestSeq = null;

            // Rozpoczynamy od N lub S
            foreach (var starter in new[] { 0, 2 }) // 0=N, 2=S
            {
                var (ns, seq) = Minimax(hands, starter, trump, 0, new List<string>(), 0, 0);
                if (ns > maxNS)
                {
                    maxNS = ns;
                    bestSeq = seq;
                }
            }
            return new MinimaxResult
            {
                NSWins = maxNS,
                SequenceLog = bestSeq ?? new List<string>()
            };
        }

        // starter: 0=N, 1=E, 2=S, 3=W
        // Zwraca: (liczba lew NS, sekwencja)
        private static (int, List<string>) Minimax(CHand[] hands, int starter, CDenomination trump, int depth, List<string> seq, int nsWins, int weWins)
        {
            // Koniec rozdania
            if (hands.All(h => h.Count == 0))
                return (nsWins, new List<string>(seq));

            return EvaluateTrickSequence(hands, new List<(int player, CCard card)>(), starter, trump, depth, seq, nsWins, weWins);
        }

        // Buduje rozgałęzienia dla kolejnych kart w pojedynczej lewie (dla każdego po kolei)
        private static (int, List<string>) EvaluateTrickSequence(CHand[] hands, List<(int player, CCard card)> trick, int starter, CDenomination trump, int depth, List<string> seq, int nsWins, int weWins)
        {
            // Kiedy lewa jest pełna (4 karty) – oceniamy wynik lewy
            if (trick.Count == 4)
            {
                int winner = Winner(trick, trump);
                int ns = nsWins, we = weWins;
                if (winner == 0 || winner == 2) ns++; else we++;

                var trickStr = $"{PlayerName(starter)} zagrywa {trick[0].card}, " +
                    string.Join(", ", trick.Skip(1).Select(t => $"{PlayerName(t.player)} {t.card}"));

                var newSeq = new List<string>(seq) { trickStr };

                // Jeśli gracze nie mają już kart, kończymy
                if (hands.All(h => h.Count == 0))
                    return (ns, newSeq);

                // Sedno problemu analizy pojedynczego koloru:
                // Zwycięzca nie gra następnej lewy, to NS ZAWSZE decyduje, z czyjej ręki (N czy S) bardziej opłaca się wyjść.
                int maxNS = -1;
                List<string> bestTrickSeq = null;

                if (hands[0].Count > 0)
                {
                    var (rN, sN) = Minimax(hands, 0, trump, depth + 1, newSeq, ns, we);
                    if (rN > maxNS) { maxNS = rN; bestTrickSeq = sN; }
                }

                if (hands[2].Count > 0)
                {
                    var (rS, sS) = Minimax(hands, 2, trump, depth + 1, newSeq, ns, we);
                    if (rS > maxNS) { maxNS = rS; bestTrickSeq = sS; }
                }

                return (maxNS, bestTrickSeq ?? newSeq);
            }

            // Kto ma teraz dołożyć kartę? (pierwszą rzuca starter, kolejne idą po kolei)
            int currentPlayer = (starter + trick.Count) % 4;
            var hand = hands[currentPlayer];

            // Wybierz dozwolone karty (do koloru jak to możliwe, w przeciwnym razie cokolwiek)
            List<CCard> options;
            if (trick.Count == 0)
            {
                options = hand.ToList();
            }
            else
            {
                var toColor = trick[0].card.Denomination;
                var hasColor = hand.Any(c => c.Denomination == toColor);
                options = hasColor ? hand.Where(c => c.Denomination == toColor).ToList() : hand.ToList();
            }

            // Każdy gracz dba o wynik "swojej" pary: NS (0 i 2) maksymalizuje, WE (1 i 3) minimalizuje wynik NS.
            bool isNS = currentPlayer == 0 || currentPlayer == 2;

            int? bestScore = null;
            CCard bestCardSelected = null;
            List<string> bestSeq = null;

            foreach (var card in options)
            {
                hand.Remove(card);
                trick.Add((currentPlayer, card));

                var (score, currentSeq) = EvaluateTrickSequence(hands, trick, starter, trump, depth, seq, nsWins, weWins);

                bool isBetter = false;
                bool isEquivalent = false;

                if (bestScore == null)
                {
                    isBetter = true;
                }
                else if (isNS && score > bestScore) isBetter = true;
                else if (!isNS && score < bestScore) isBetter = true;
                else if (score == bestScore) isEquivalent = true;

                // Jeśli rzucenie danej karty daje obiektywnie lepszy wynik – bierzemy ją. 
                // Jeśli wynik jest równoważny (isEquivalent), preferujemy zagrać *jak najniższą* kartę dla estetyki rozwiązania.
                if (isBetter || (isEquivalent && bestCardSelected != null && card.Rank.Value < bestCardSelected.Rank.Value))
                {
                    bestScore = score;
                    bestSeq = currentSeq;
                    bestCardSelected = card;
                }

                trick.RemoveAt(trick.Count - 1);
                hand.Add(card);
            }

            return (bestScore ?? 0, bestSeq ?? new List<string>());
        }

        

        // Rekurencyjnie permutuje dołożenia do lewy
        private static void PermuteTrick(CHand[] hands, List<(int player, CCard card)> trick, int starter, CDenomination trump, List<string> seq, int nsWins, int weWins, int depth, ref int bestNS, ref List<string> bestSeq)
        {
            if (trick.Count == 4)
            {
                // Kto wygrał lewę?
                int winner = Winner(trick, trump);
                int ns = nsWins, we = weWins;
                if (winner == 0 || winner == 2) ns++; else we++;
                var trickStr = $"{PlayerName(starter)} zagrywa {trick[0].card}, " +
                    string.Join(", ", trick.Skip(1).Select(t => $"{PlayerName(t.player)} {t.card}"));
                var newSeq = new List<string>(seq) { trickStr };
                var (res, resSeq) = Minimax(hands, winner, trump, depth + 1, newSeq, ns, we);
                if (res > bestNS)
                {
                    bestNS = res;
                    bestSeq = resSeq;
                }
                return;
            }

            int p = (starter + trick.Count) % 4;
            var hand = hands[p];
            var toColor = trick[0].card.Denomination;
            var hasColor = hand.Any(c => c.Denomination == toColor);
            List<CCard> options;
            if (hasColor)
                options = hand.Where(c => c.Denomination == toColor).ToList();
            else
                options = hand.ToList();

            foreach (var c in options)
            {
                hand.Remove(c);
                trick.Add((p, c));
                PermuteTrick(hands, trick, starter, trump, seq, nsWins, weWins, depth, ref bestNS, ref bestSeq);
                trick.RemoveAt(trick.Count - 1);
                hand.Add(c);
            }
        }

        // Zwraca index gracza, który wygrał lewę
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
    }
}
