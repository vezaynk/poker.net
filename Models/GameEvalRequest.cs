namespace poker.net.Models
{
    /// <summary>
    /// Request body for POST /api/evaluate/game.
    /// Accepts the TABLE_STATE payload from the Reddit poker WebSocket
    /// plus your hole cards from the YOUR_CARDS message.
    /// </summary>
    public class GameEvalRequest
    {
        public TableState TableState { get; set; } = new();

        /// <summary>Your hole cards from YOUR_CARDS, e.g. ["Ks","Ac"].</summary>
        public string[] MyCards { get; set; } = Array.Empty<string>();
    }

    public class TableState
    {
        public string Round { get; set; } = string.Empty;
        public List<string> CommunityCards { get; set; } = new();
        public List<TablePlayer> Players { get; set; } = new();
    }

    public class TablePlayer
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Seat { get; set; }
        public int Stack { get; set; }
        public int Bet { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsDealer { get; set; }
        public bool IsSmallBlind { get; set; }
        public bool IsBigBlind { get; set; }
        public bool IsActive { get; set; }
    }
}
