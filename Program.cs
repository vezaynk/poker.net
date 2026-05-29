using Microsoft.Data.SqlClient;
using poker.net.Helper;
using poker.net.Models;
using poker.net.Services;
using poker.net.Interfaces;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

bool useSql = builder.Configuration.GetValue<bool>("UseSqlServer");

// Deck provider toggle
if (useSql)
{
    builder.Services.AddScoped<IDeckService, SqlDeckService>();

    // DB-only registrations
    builder.Services.AddScoped<IDbConnection>(sp =>
        new SqlConnection(builder.Configuration.GetConnectionString("DBConn")));
    builder.Services.AddScoped<DbHelper>();
}
else
{
    builder.Services.AddSingleton<IDeckService, StaticDeckService>();
}

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Optional: log which mode you're in
app.Logger.LogInformation("Startup mode: UseSqlServer = {UseSql}", useSql);

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.MapPost("/api/evaluate", (EvalRequest req) =>
{
    if (req.Players is null || req.Players.Count < 1 || req.Players.Count > 9)
        return Results.BadRequest("Provide between 1 and 9 players.");

    if (req.Board is null || req.Board.Length != 5)
        return Results.BadRequest("Provide exactly 5 board cards (flop + turn + river).");

    Card[] board;
    try { board = req.Board.Select(CardParser.Parse).ToArray(); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }

    List<Card[]> playerHoles;
    try
    {
        playerHoles = req.Players.Select(p =>
        {
            if (p is null || p.Length != 2)
                throw new ArgumentException("Each player must have exactly 2 hole cards.");
            return p.Select(CardParser.Parse).ToArray();
        }).ToList();
    }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }

    // Validate no duplicates across all cards
    var allCards = playerHoles.SelectMany(h => h).Concat(board).ToList();
    var ids = allCards.Select(c => c.ID).ToList();
    if (ids.Count != ids.Distinct().Count())
        return Results.BadRequest("Duplicate cards detected.");

    // Build a Card[23] in the layout the engine expects:
    // [0..8]  = player hole card 1 for players 0..8
    // [9..17] = player hole card 2 for players 0..8
    // [18..22] = board cards
    // Unused player slots are padded with placeholder cards (not returned in results).
    var usedIds = new HashSet<int>(ids);
    var padding = RawDeck.All.Where(c => !usedIds.Contains(c.ID)).GetEnumerator();

    Card Pad() { padding.MoveNext(); return padding.Current; }

    int n = playerHoles.Count;
    var deck = new Card[23];
    for (int i = 0; i < 9; i++) deck[i]     = i < n ? playerHoles[i][0] : Pad();
    for (int i = 0; i < 9; i++) deck[i + 9] = i < n ? playerHoles[i][1] : Pad();
    for (int i = 0; i < 5; i++) deck[i + 18] = board[i];

    var (scores, ranks, bestHands) = EvalEngine.EvaluateRiverNinePlayersArrays(deck, includeBestHands: true);

    ushort winScore = scores.Take(n).Min();
    var playerResults = new List<PlayerResult>(n);
    for (int i = 0; i < n; i++)
    {
        playerResults.Add(new PlayerResult
        {
            Score     = scores[i],
            HandRank  = HandRankName(ranks[i]),
            BestHand  = bestHands[i]?.Select(CardParser.Format).ToList() ?? new(),
            IsWinner  = scores[i] == winScore,
        });
    }

    var winners = playerResults
        .Select((p, idx) => (p, idx))
        .Where(x => x.p.IsWinner)
        .Select(x => x.idx)
        .ToList();

    return Results.Ok(new EvalResponse { Players = playerResults, Winners = winners });
});

app.MapPost("/api/evaluate/game", (GameEvalRequest req) =>
{
    if (req.MyCards is null || req.MyCards.Length != 2)
        return Results.BadRequest("Provide exactly 2 hole cards in myCards.");

    if (req.TableState is null)
        return Results.BadRequest("tableState is required.");

    var community = req.TableState.CommunityCards ?? new();
    if (community.Count > 5)
        return Results.BadRequest("communityCards cannot have more than 5 cards.");

    // Need at least 3 community cards (flop) to have a 5-card hand to evaluate
    if (community.Count < 3)
        return Results.Ok(new GameEvalResponse
        {
            Round            = req.TableState.Round,
            CommunityCardCount = community.Count,
            HandRank         = "Preflop — no evaluation yet",
            ActivePlayers    = ActivePlayers(req.TableState.Players),
        });

    Card[] myHole;
    try { myHole = req.MyCards.Select(CardParser.Parse).ToArray(); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }

    Card[] board;
    try { board = community.Select(CardParser.Parse).ToArray(); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }

    var allCards = myHole.Concat(board).ToArray();
    var ids = allCards.Select(c => c.ID).ToList();
    if (ids.Count != ids.Distinct().Count())
        return Results.BadRequest("Duplicate cards detected.");

    var (score, rank, bestHand) = EvalEngine.EvaluateBestHand(allCards);

    return Results.Ok(new GameEvalResponse
    {
        Round              = req.TableState.Round,
        CommunityCardCount = community.Count,
        Score              = score,
        HandRank           = HandRankName(rank),
        BestHand           = bestHand.Select(CardParser.Format).ToList(),
        ActivePlayers      = ActivePlayers(req.TableState.Players),
    });
});

static List<ActivePlayer> ActivePlayers(List<TablePlayer>? players) =>
    players?
        .Where(p => p.Status != "folded")
        .Select(p => new ActivePlayer
        {
            UserId      = p.UserId,
            Username    = p.Username,
            Seat        = p.Seat,
            Stack       = p.Stack,
            Bet         = p.Bet,
            IsDealer    = p.IsDealer,
            IsSmallBlind = p.IsSmallBlind,
            IsBigBlind  = p.IsBigBlind,
        })
        .ToList() ?? new();

static string HandRankName(int rank) => rank switch
{
    1 => "Straight Flush",
    2 => "Four of a Kind",
    3 => "Full House",
    4 => "Flush",
    5 => "Straight",
    6 => "Three of a Kind",
    7 => "Two Pair",
    8 => "Pair",
    9 => "High Card",
    _ => "Unknown"
};

app.Run();
