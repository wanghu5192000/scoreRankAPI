# API reference

The API is implemented by `CustomerAPI/Controllers/CustomerController.cs`.
All routes are rooted at `/`. The API uses JSON for leaderboard results and
returns the updated score as a JSON number.

Interactive documentation is available at `/swagger`, with the generated
OpenAPI document at `/swagger/v1/swagger.json`.

## Add a score delta

```http
POST /customer/{customerid}/score/{score}
```

| Parameter | Location | Rules |
| --- | --- | --- |
| `customerid` | path | Must be greater than zero. |
| `score` | path | Score delta; must be between -1000 and 1000, inclusive. |

The delta is added to the customer's existing total. If the customer does not
exist, the submitted value becomes the initial total. The response is the new
total, for example:

```http
POST /customer/42/score/25
```

```json
25
```

A request with an invalid customer ID or delta returns `400 Bad Request`.
Negative and zero totals are retained in storage but are not ranked.

## Get a leaderboard range

```http
GET /leaderboard?start={start}&end={end}
```

| Parameter | Location | Rules |
| --- | --- | --- |
| `start` | query | One-based rank; must be greater than zero. |
| `end` | query | Inclusive final rank; must be greater than zero and at least `start`. |

Example:

```http
GET /leaderboard?start=1&end=3
```

```json
[
  { "customerID": "42", "score": 25, "rank": 1 },
  { "customerID": "19", "score": 20, "rank": 2 },
  { "customerID": "21", "score": 20, "rank": 3 }
]
```

`CustomerID` is serialized as a string. Results are ordered by descending
score, then ascending customer ID. Equal scores have distinct ordinal ranks.
If the range contains no ranked customers, the endpoint returns `404 Not Found`.
Invalid ranges return `400 Bad Request`.

## Get a customer's rank and neighbors

```http
GET /leaderboard/{customerid}?high={high}&low={low}
```

| Parameter | Location | Rules |
| --- | --- | --- |
| `customerid` | path | Must be greater than zero. |
| `high` | query | Number of preceding ranks to include; must not be negative. Defaults to `0` when omitted. |
| `low` | query | Number of following ranks to include; must not be negative. Defaults to `0` when omitted. |

For example, `?high=2&low=1` returns the customer, up to two ranked entries
above, and up to one below. With both values omitted or zero, only the
customer's own entry is returned.

If the customer is unknown, has a score of zero or less, or otherwise has no
ranked entry, the endpoint returns `404 Not Found`. Invalid IDs or negative
neighbor counts return `400 Bad Request`.
