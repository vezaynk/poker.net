namespace poker.net.Models
{
    public class GameEvalResponse
    {
        public string Round { get; set; } = string.Empty;

        /// <summary>Number of community cards currently visible.</summary>
        public int CommunityCardCount { get; set; }

        public int Score { get; set; }
        public string HandRank { get; set; } = string.Empty;

        /// <summary>Best 5-card hand from available cards, in standard notation.</summary>
        public List<string> BestHand { get; set; } = new();

        public List<ActivePlayer> ActivePlayers { get; set; } = new();
    }

    public class ActivePlayer
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Seat { get; set; }
        public int Stack { get; set; }
        public int Bet { get; set; }
        public bool IsDealer { get; set; }
        public bool IsSmallBlind { get; set; }
        public bool IsBigBlind { get; set; }
    }
}
