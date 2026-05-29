namespace poker.net.Models
{
    /// <summary>
    /// Request body for POST /api/evaluate.
    /// Cards use standard notation: rank + suit (e.g. "As", "Kd", "10h", "Jc").
    /// Ranks: 2-9, 10, J, Q, K, A. Suits: s (spades), h (hearts), d (diamonds), c (clubs).
    /// </summary>
    public class EvalRequest
    {
        /// <summary>1–9 players. Each element is a two-card array, e.g. ["As","Kd"].</summary>
        public List<string[]> Players { get; set; } = new();

        /// <summary>Exactly 5 board cards (flop + turn + river).</summary>
        public string[] Board { get; set; } = Array.Empty<string>();
    }
}
