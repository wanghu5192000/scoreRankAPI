# scoreRankAPI

A small ASP.NET Core REST API for accumulating customer scores and retrieving
leaderboard positions. The project was written as an interview-task solution.

## What it does

- Adds score deltas to customers.
- Ranks customers with positive scores.
- Returns a leaderboard rank range or a customer's rank and nearby entries.
- Maintains a sorted, partitioned in-memory index; partitions split as they grow.
- Uses per-customer synchronization for score updates and a reader/writer lock
  for index operations.

Scores are ordered from highest to lowest. Customers with equal scores are
ordered by ascending customer ID and receive distinct, consecutive (ordinal)
ranks.

## Requirements and running

The solution targets **.NET 6**. Install a compatible .NET SDK, then run these
commands from the repository root:

```shell
dotnet restore CustomerAPI.sln
dotnet build CustomerAPI.sln
dotnet test CustomerAPI.sln
dotnet run --project CustomerAPI/CustomerAPI.csproj
```

The checked-in development launch profile listens at `http://localhost:5154`.
The API has no database or external service configuration. Swagger UI is
available at `http://localhost:5154/swagger`; its OpenAPI JSON is at
`http://localhost:5154/swagger/v1/swagger.json`.

## Quick examples

Add 25 points to customer 42 (the response is the customer's new total):

```shell
curl -X POST http://localhost:5154/customer/42/score/25
```

Get ranks 1 through 10:

```shell
curl "http://localhost:5154/leaderboard?start=1&end=10"
```

Get customer 42 and up to two entries above and below:

```shell
curl "http://localhost:5154/leaderboard/42?high=2&low=2"
```

See [docs/api.md](docs/api.md) for endpoint parameters, response shapes, and
validation behavior. See [docs/architecture.md](docs/architecture.md) for the
storage and ranking design and its limitations.

## Important limitations

- All scores and customers are held in process memory. They are lost when the
  process restarts, and separate API instances do not share state.
- The score on the POST endpoint is a **delta**, not a replacement value. Each
  submitted delta must be between -1000 and 1000; the resulting total is not
  capped to that range.
- Only customers whose current score is greater than zero appear on the
  leaderboard. A negative update can remove a customer from it.
- The API does not configure authentication or persistence.
- The projects target .NET 6, which is out of support. Upgrade the target
  framework before relying on this application in production.

The original interview prompt is included as
[`interview Codes for donnet-updated.pdf`](interview%20Codes%20for%20donnet-updated.pdf).
