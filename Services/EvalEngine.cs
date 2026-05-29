using poker.net.Models;
using System.Runtime.CompilerServices;

namespace poker.net.Services
{
    public static class EvalEngine
    {

        /// <summary>
        /// Evaluates a full 9-player river using an array-based deck.
        /// Returns UI-ready results: player scores, rank order, and (optionally) each player’s best 5 cards as Card[][].
        /// This is the overload used by the web application.
        /// </summary>
#pragma warning disable CA2014
        public static (ushort[] scores, int[] ranks, Card[][] bestHands)
            EvaluateRiverNinePlayersArrays(Card[] deck, bool includeBestHands = false)
        {
            if (deck is null) throw new ArgumentNullException(nameof(deck));
            if (deck.Length < 23) throw new ArgumentException("Deck must have at least 23 cards.", nameof(deck));

            var scores = new ushort[9];
            var ranks = new int[9];
            var bestHands = includeBestHands ? new Card[9][] : Array.Empty<Card[]>();

            int b0 = deck[18].Value, b1 = deck[19].Value, b2 = deck[20].Value, b3 = deck[21].Value, b4 = deck[22].Value;
            var perm = PokerLib.Perm7Indices;

            for (int p = 0; p < 9; p++)
            {
                Span<int> seven = stackalloc int[7];
                seven[0] = deck[p].Value;
                seven[1] = deck[p + 9].Value;
                seven[2] = b0; seven[3] = b1; seven[4] = b2; seven[5] = b3; seven[6] = b4;

                ushort bestScore = ushort.MaxValue;
                int bestRow = 0;

                for (int row = 0; row < 21; row++)
                {
                    int i = row * 5;
                    ushort s = PokerLib.Eval5CardsFast(
                        seven[perm[i + 0]], seven[perm[i + 1]],
                        seven[perm[i + 2]], seven[perm[i + 3]],
                        seven[perm[i + 4]]);
                    if (s < bestScore) { bestScore = s; bestRow = row; }
                }

                scores[p] = bestScore;
                ranks[p] = PokerLib.HandRank(bestScore);

                if (includeBestHands)
                {
                    int baseIdx = bestRow * 5;
                    var a = new Card[5]
                    {
                        PickFromSeven(deck, p, perm[baseIdx + 0]),
                        PickFromSeven(deck, p, perm[baseIdx + 1]),
                        PickFromSeven(deck, p, perm[baseIdx + 2]),
                        PickFromSeven(deck, p, perm[baseIdx + 3]),
                        PickFromSeven(deck, p, perm[baseIdx + 4]),
                    };
                    bestHands[p] = a; 
                }
            }

            return (scores, ranks, bestHands);
        }
#pragma warning restore CA2014

        /// <summary>
        /// Evaluates a 9-player river and returns only index data for each player’s best 5-card hand.
        /// Designed for maximum throughput and minimal allocations—cards can be materialized later at the UI layer.
        /// </summary>

#pragma warning disable CA2014
        public static (ushort[] scores, int[] ranks, int[] bestIndexes, byte[][] bestIdx5)
            EvaluateRiverNinePlayersIdx(Card[] deck, bool includeBestIndices = false)
        {
            if (deck is null) throw new ArgumentNullException(nameof(deck));
            if (deck.Length < 23) throw new ArgumentException("Deck must have at least 23 cards.", nameof(deck));

            var scores = new ushort[9];
            var ranks = new int[9];
            var bestIndexes = new int[9];
            var bestIdx5 = includeBestIndices ? new byte[9][] : Array.Empty<byte[]>();

            int b0 = deck[18].Value, b1 = deck[19].Value, b2 = deck[20].Value, b3 = deck[21].Value, b4 = deck[22].Value;
            
            var perm = PokerLib.Perm7Indices;

            for (int p = 0; p < 9; p++)
            {
                Span<int> seven = stackalloc int[7];
                seven[0] = deck[p].Value;
                seven[1] = deck[p + 9].Value;
                seven[2] = b0; seven[3] = b1; seven[4] = b2; seven[5] = b3; seven[6] = b4;

                ushort bestScore = ushort.MaxValue;
                int bestRow = 0;

                for (int row = 0; row < 21; row++)
                {
                    int i = row * 5;
                    ushort s = PokerLib.Eval5CardsFast(
                        seven[perm[i + 0]], seven[perm[i + 1]],
                        seven[perm[i + 2]], seven[perm[i + 3]],
                        seven[perm[i + 4]]);
                    if (s < bestScore) { bestScore = s; bestRow = row; }
                }

                scores[p] = bestScore;
                ranks[p] = PokerLib.HandRank(bestScore);
                bestIndexes[p] = bestRow;

                if (includeBestIndices)
                {
                    int baseIdx = bestRow * 5;
                    bestIdx5[p] = new byte[5] {
                        (byte)perm[baseIdx + 0],
                        (byte)perm[baseIdx + 1],
                        (byte)perm[baseIdx + 2],
                        (byte)perm[baseIdx + 3],
                        (byte)perm[baseIdx + 4]
                    };
                }
            }

            return (scores, ranks, bestIndexes, bestIdx5);
        }
#pragma warning restore CA2014

        /// <summary>
        /// Evaluates the best 5-card hand from 5, 6, or 7 cards (hole cards + community cards).
        /// Returns the score, rank category, and the best 5 cards.
        /// </summary>
        public static (ushort score, int rank, Card[] bestHand) EvaluateBestHand(Card[] cards)
        {
            int n = cards.Length;
            if (n < 5 || n > 7)
                throw new ArgumentException("Must provide 5, 6, or 7 cards.", nameof(cards));

            var values = cards.Select(c => c.Value).ToArray();

            ushort bestScore = ushort.MaxValue;
            int bestA = 0, bestB = 1, bestC = 2, bestD = 3, bestE = 4;

            for (int a = 0; a < n - 4; a++)
            for (int b = a + 1; b < n - 3; b++)
            for (int c = b + 1; c < n - 2; c++)
            for (int d = c + 1; d < n - 1; d++)
            for (int e = d + 1; e < n; e++)
            {
                ushort s = PokerLib.Eval5CardsFast(values[a], values[b], values[c], values[d], values[e]);
                if (s < bestScore)
                {
                    bestScore = s;
                    bestA = a; bestB = b; bestC = c; bestD = d; bestE = e;
                }
            }

            return (bestScore, PokerLib.HandRank(bestScore),
                new[] { cards[bestA], cards[bestB], cards[bestC], cards[bestD], cards[bestE] });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Card PickFromSeven(IReadOnlyList<Card> d, int p, int sevenIdx) => sevenIdx switch
        {
            0 => d[p],       // hole 1
            1 => d[p + 9],   // hole 2
            2 => d[18],
            3 => d[19],
            4 => d[20],
            5 => d[21],
            6 => d[22],
            _ => throw new ArgumentOutOfRangeException(nameof(sevenIdx))
        };
    }
}