# Architecture and behavior

## Projects

- `CustomerAPI` is the ASP.NET Core controller host.
- `Model` contains the score store, partitioned index, and leaderboard DTO.
- `Test` contains NUnit tests for storage, score updates, rank ordering, and
  partition splitting.

The API host registers controllers and maps their routes in `Program.cs`. It
generates an OpenAPI document from controller XML comments and response
metadata, and serves Swagger UI at `/swagger`. It does not register a database,
persistent cache, or authentication.

## Score and rank lifecycle

`Storage` keeps customer records in a static `ConcurrentDictionary<long,
Customer>`. Each accepted update is an additive delta. A lock on the customer
serializes updates for that customer. On a transition into or out of a
positive score, the corresponding leaderboard entry is added or removed; for
positive-to-positive updates, its indexed score is replaced.

The leaderboard index stores only positive-score customers. Its `Index`
maintains score partitions, each containing a sorted list ordered by descending
score and ascending customer ID. When a partition exceeds 25,000 customers, it
is split around the average score rounded to five decimal places. Rank ranges
for an API request are one-based and inclusive. Partition rank bounds are
refreshed lazily after index mutations.

The index uses a reader/writer lock to coordinate leaderboard reads and
mutations. These mechanisms support concurrent requests within one process;
they do not provide shared state between processes or persistence across
restarts.

## Operational constraints

- **Volatile, process-local state:** a restart clears all customers and scores.
  Running multiple instances creates independent leaderboards.
- **Delta validation:** the API accepts individual deltas from -1000 to 1000,
  but does not limit the accumulated total to that range.
- **Positive-only ranks:** customers with non-positive totals remain stored but
  are omitted from leaderboard queries.
- **No authentication:** the API routes are unauthenticated as configured.
- **Framework support:** both projects target .NET 6, which is out of support.

Treat this as a small demonstration/interview implementation rather than a
production-ready shared leaderboard service without additional persistence,
deployment, and security work.
