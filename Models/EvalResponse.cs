namespace poker.net.Models
{
    public class EvalResponse
    {
        public List<PlayerResult> Players { get; set; } = new();

        /// <summary>0-based indices of the winning player(s). More than one means a tie.</summary>
        public List<int> Winners { get; set; } = new();
    }

    public class PlayerResult
    {
        /// <summary>Raw evaluator score — lower is stronger.</summary>
        public int Score { get; set; }

        /// <summary>Hand category name, e.g. "Straight Flush", "Full House".</summary>
        public string HandRank { get; set; } = string.Empty;

        /// <summary>Best 5-card hand in standard notation, e.g. ["As","Ks","Qs","Js","10s"].</summary>
        public List<string> BestHand { get; set; } = new();

        public bool IsWinner { get; set; }
    }
}
