# poker.net

ASP.NET Core 10 Texas Hold'em hand evaluator (Cactus Kev algorithm). Razor Pages UI + REST API.

## Running locally

```bash
dotnet run --launch-profile http
# Listening on http://localhost:5191
```

No database required — runs in static (in-memory) mode by default (`UseSqlServer: false`).

## API endpoints

### `POST /api/evaluate`
Evaluate 1–9 players at river with full hole cards known.

```json
{
  "players": [["As","Kd"], ["Jh","Jc"]],
  "board":   ["2h","5d","9c","Kh","Qd"]
}
```

### `POST /api/evaluate/game`
Evaluate your own hand from a Reddit poker WebSocket game state. Works at flop, turn, or river (preflop returns a stub).

```json
{
  "tableState": { ...TABLE_STATE.state object from WebSocket... },
  "myCards": ["Ks","Ac"]
}
```

`tableState` is the `state` field from a `TABLE_STATE` WebSocket message.  
`myCards` comes from the `YOUR_CARDS` response to a `GET_MY_CARDS` request.

## Card notation

Rank + suit (lowercase): `2`–`9`, `10`, `J`, `Q`, `K`, `A` + `s/h/d/c`.  
Examples: `As`, `Kd`, `10h`, `Jc`.

## Reddit poker WebSocket integration

The game runs over `wss://gql-realtime.reddit.com/query` (GraphQL subscriptions).

Relevant messages:
- `TABLE_STATE` — broadcast game state: community cards, players, pot, round, handId
- `GET_MY_CARDS` (send) / `YOUR_CARDS` (receive) — your private hole cards for the current hand

`handId` (e.g. `hand_1780075308327_we4ih5`) is an opaque server-generated ID: `hand_<timestamp_ms>_<slug>`.

## Key files

| File | Purpose |
|------|---------|
| `Services/EvalEngine.cs` | Core evaluator — `EvaluateRiverNinePlayersArrays` (9-player), `EvaluateBestHand` (1-player, 5–7 cards) |
| `Services/PokerLib.cs` | Cactus Kev primitives — `Eval5CardsFast`, `HandRank` |
| `Helper/CardParser.cs` | Parses/formats card notation ↔ `Card` objects |
| `Models/RawDeck.cs` | Static 52-card deck with Cactus Kev `Value` encodings |
| `Program.cs` | App bootstrap + all minimal API endpoints |
