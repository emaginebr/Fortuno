# Contract — POST /tickets/qrcode

**Controller**: `TicketsController` (`Fortuno.API/Controllers/TicketsController.cs`)
**Service**: `TicketService.CreateQRCodeAsync(long currentUserId, TicketOrderRequest request)`
**Auth**: `[Authorize]` — requer `Authorization: Basic {token}` + `X-Tenant-Id`

## Request

```http
POST /tickets/qrcode HTTP/1.1
Authorization: Basic {{accessToken}}
X-Tenant-Id: fortuna
Content-Type: application/json
```

Body (`TicketOrderRequest`):

```json
{
  "lotteryId": 1,
  "quantity": 5,
  "mode": "Random",
  "pickedNumbers": null,
  "referralCode": null
}
```

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `lotteryId` | long | sim | `> 0`, Lottery em status `Open` |
| `quantity` | int | sim | `> 0`, dentro de `Lottery.TicketMin..TicketMax` quando definidos |
| `mode` | string (enum) | sim | `"Random"` ou `"UserPicks"` |
| `pickedNumbers` | long[] | condicional | obrigatório quando `mode == "UserPicks"`; `Count == quantity`; sem duplicatas; dentro de `Lottery.NumberValueMin..NumberValueMax` (Int64) ou validado por `NumberCompositionService` (compostos); todos disponíveis |
| `referralCode` | string | não | ≤ 8 chars, `[A-Z0-9]*` |

## Response — 201 Created

```json
{
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "brCode": "00020101021126580014BR.GOV.BCB.PIX...",
  "brCodeBase64": "data:image/png;base64,iVBORw0KGgo...",
  "expiredAt": "2026-04-19T14:20:00.348+00:00"
}
```

Shape mapeia 1:1 da resposta do ProxyPay `POST /payment/qrcode` (FR-007).

## Response — Erros

| Status | Quando | Body |
|---|---|---|
| 400 | Validação FluentValidation falha (quantity, mode, pickedNumbers, etc.) | `ApiResponse.Fail(...)` |
| 400 | Lottery fora de `Open` | `ApiResponse.Fail("Compras disponíveis apenas em Lottery Open.")` |
| 400 | Comprador sem campos NAuth (`name`/`email`/`documentId`/`cellphone`) | `ApiResponse.Fail("Complete seu cadastro no NAuth antes de comprar (faltam: ...).")` |
| 400 | `UserPicks` com número indisponível ou inválido | `ApiResponse.Fail("...")` |
| 400 | Lottery sem `StoreClientId` (criada antes desta feature) | `ApiResponse.Fail("Lottery desatualizada — recrie.")` |
| 401 | Token ausente/inválido | — |
| 500 | ProxyPay indisponível | `ApiResponse.Fail("ProxyPay indisponível (status {n})")` |

## Efeitos colaterais

1. Consulta `INAuthAppService.GetCurrentAsync()` para obter `customer` (name, email, documentId, cellphone).
2. Valida/calcula `totalAmount = Quantity * TicketPrice - comboDiscount`.
3. Em `UserPicks`: cria `NumberReservation`s com TTL alinhado a `expiredAt` (calculado antes de chamar o provedor).
4. Chama `POST /payment/qrcode` no ProxyPay com `clientId = Lottery.StoreClientId`, `customer` e `items` (1 linha consolidada — R-003).
5. Persiste `TicketOrder` com tudo que o `ProcessPaymentAsync` vai precisar depois.
6. Retorna o QR Code para o frontend.

## Idempotência

**Não é idempotente.** Cada chamada cria um novo `TicketOrder` + novo `invoiceId`. O frontend deve garantir que o usuário não dispara duas vezes (ex.: debounce no botão).

## Sequência

```
Frontend ─► POST /tickets/qrcode
              │
              ▼
          TicketsController.CreateQRCode
              │
              ├─► NAuth.GetCurrentAsync()          ── valida customer
              ├─► LotteryRepo.GetByIdAsync         ── valida Open + StoreClientId
              ├─► ComboRepo.FindMatchingCombo     ── calcula desconto
              ├─► (UserPicks) reservationRepo.InsertBatch com TTL=provisional
              ├─► ProxyPay.CreateQRCodeAsync       ── POST /payment/qrcode
              ├─► (UserPicks) atualiza reservations.InvoiceId = response.invoiceId
              ├─► intentRepo.InsertAsync          ── persiste TicketOrder
              └─► 201 com TicketQRCodeInfo
```
