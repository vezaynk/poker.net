using poker.net.Models;

namespace poker.net.Helper
{
    public static class CardParser
    {
        // Face string → 0-based rank index (A=0, 2=1, ..., K=12)
        private static readonly Dictionary<string, int> FaceIndex = new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"]  = 0,  ["2"]  = 1,  ["3"]  = 2,  ["4"]  = 3,
            ["5"]  = 4,  ["6"]  = 5,  ["7"]  = 6,  ["8"]  = 7,
            ["9"]  = 8,  ["10"] = 9,  ["J"]  = 10, ["Q"]  = 11,
            ["K"]  = 12
        };

        // Suit char → offset within rank group (s=1, h=2, d=3, c=4)
        private static readonly Dictionary<char, int> SuitOffset = new()
        {
            ['s'] = 1, ['S'] = 1,
            ['h'] = 2, ['H'] = 2,
            ['d'] = 3, ['D'] = 3,
            ['c'] = 4, ['C'] = 4,
        };

        /// <summary>
        /// Parses a card notation like "As", "10h", "Kd", "Jc" into the matching Card from RawDeck.
        /// Throws <see cref="ArgumentException"/> if the notation is invalid or the card is not found.
        /// </summary>
        public static Card Parse(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation))
                throw new ArgumentException("Card notation must not be empty.");

            notation = notation.Trim();
            if (notation.Length < 2)
                throw new ArgumentException($"Invalid card notation: '{notation}'.");

            char suitChar = notation[^1];
            string faceStr = notation[..^1];

            if (!FaceIndex.TryGetValue(faceStr, out int rankIdx))
                throw new ArgumentException($"Unknown rank '{faceStr}' in notation '{notation}'.");

            if (!SuitOffset.TryGetValue(suitChar, out int suitOff))
                throw new ArgumentException($"Unknown suit '{suitChar}' in notation '{notation}'.");

            int id = rankIdx * 4 + suitOff; // IDs are 1-52; formula gives 1-52 directly

            var card = RawDeck.All.FirstOrDefault(c => c.ID == id)
                ?? throw new ArgumentException($"Card '{notation}' (ID={id}) not found in deck.");

            return card;
        }

        /// <summary>Formats a Card back to standard notation, e.g. "As", "10h".</summary>
        public static string Format(Card card)
        {
            char suit = ((card.ID - 1) % 4) switch
            {
                0 => 's',
                1 => 'h',
                2 => 'd',
                3 => 'c',
                _ => '?'
            };
            return card.Face + suit;
        }
    }
}
