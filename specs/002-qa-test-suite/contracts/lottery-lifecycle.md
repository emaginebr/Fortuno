# Contract — Lottery Lifecycle (ApiTests + Bruno)

**Feature**: 002-qa-test-suite

Contratos dos endpoints exercitados pelo `Fortuno.ApiTests` (User Story 1) e pela Bruno collection (User Story 3).

---

## POST `/lotteries` — Create

**Auth**: obrigatório (`Authorization: Basic {token}`).

Request:

```json
{
  "storeId":     123,
  "name":        "test-lottery-{{uniqueId}}",
  "ticketPrice": 10.00,
  "numberType":  "Int64",
  "description": "QA suite lottery"
}
```

Response `201 Created` (subset relevante):

```json
{
  "lotteryId": 1,
  "slug":      "test-lottery-20260418153042-a3f1c900",
  "status":    "Draft",
  "storeId":   123
}
```

Assertions dos testes (US1 #2):

- `status` == `"Draft"`.
- `lotteryId` > 0.
- `slug` não vazio e contém o `uniqueId`.

Assertions negativas (unit tests do validator):

- 400 se `storeId` <= 0, `name` vazio, `ticketPrice` <= 0, `numberType` fora do enum.

---

## POST `/lotteries/{lotteryId}/publish`

**Auth**: obrigatório.

Request: body vazio.

Response `200 OK`:

```json
{ "status": "Open" }
```

Assertions (US1 #3):
- A chamada subsequente `GET /lotteries/{id}` retorna `status: "Open"`.

Assertion negativa (US1 #6):
- Em Lottery `Cancelled`, retorna 4xx; fetch subsequente mantém `status: "Cancelled"`.

---

## POST `/lotteries/{lotteryId}/close`

**Auth**: obrigatório.

Response `200 OK`:

```json
{ "status": "Closed" }
```

Assertion (US1 #4).

---

## POST `/lotteries/{lotteryId}/cancel`

**Auth**: obrigatório.

Request:

```json
{ "reason": "QA — lifecycle test" }
```

Response `200 OK`:

```json
{ "status": "Cancelled" }
```

Assertion (US1 #5).

---

## GET `/lotteries/{lotteryId}` — Public query

**Auth**: `[AllowAnonymous]` (header `Authorization` ausente).

Response `200 OK`:

```json
{
  "lotteryId": 1,
  "slug":      "test-lottery-…",
  "status":    "Open",
  "name":      "…"
}
```

Assertion (US1 #7):

- Chamada sem header funciona.
- `lotteryId` e `slug` batem com os gerados na criação.

---

## GET `/lotteries/slug/{slug}` — Public query by slug

**Auth**: `[AllowAnonymous]`.

Response: idêntica ao `GET /lotteries/{id}`.

Assertion (US1 #7):

- Mesmo objeto retornado pelo GET por ID.

---

## GET `/lotteries/store/{storeId}` — Consulta por store

**Auth**: obrigatório.

Retorna a lista de Lotteries da Store especificada. O `StoreOwnershipGuard` rejeita (403) se o usuário autenticado não for dono.

> **Nota (R-001 v2)**: o bootstrap da `ApiSessionFixture` **não** usa mais este endpoint para descobrir a Store. A Store é resolvida via `POST {proxyPayUrl}/graphql` com a query `{ myStore { storeId } }`. Este endpoint continua válido para consultas autenticadas de lista por Store.

---

## Endpoints explicitamente fora do escopo de ApiTests nesta entrega

Estes contratos **não** são validados pelos ApiTests (apenas unit tests), conforme FR-016:

| Endpoint | Status |
|---|---|
| `POST /raffles` | Deferred (aguarda payment simulado) |
| `POST /raffles/{id}/winners/preview` | Deferred |
| `POST /raffles/{id}/winners/confirm` | Deferred |
| `POST /raffles/{id}/close` | Deferred |
| `POST /purchases/preview` | Deferred |
| `POST /purchases/confirm` | Deferred |
| `POST /webhooks/proxypay/invoice-paid` | Deferred (coberto por unit test do filtro HMAC) |

Esses endpoints **permanecem** na Bruno collection (US3) para exploração manual, mas sem execução automatizada.
