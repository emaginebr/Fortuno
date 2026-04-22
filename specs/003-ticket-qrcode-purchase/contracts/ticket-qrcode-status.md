# Contract — GET /tickets/qrcode/{invoiceId}/status

**Controller**: `TicketsController`
**Service**: `TicketService.CheckQRCodeStatusAsync(long invoiceId)` → internamente chama `ProcessPaymentAsync` quando o provedor retorna `Paid` e o `TicketOrder` ainda está `Pending`.
**Auth**: `[Authorize]` — requer apenas autenticação válida; **não** verifica posse (FR-004 revisado).

## Request

```http
GET /tickets/qrcode/{invoiceId}/status HTTP/1.1
Authorization: Basic {{accessToken}}
X-Tenant-Id: fortuna
```

## Response — 200 OK

Shape **sempre** inclui `status` e `invoiceId`. Campos adicionais dependem do estado (FR-002).

### `Pending`

```json
{
  "status": "Pending",
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "expiredAt": "2026-04-19T14:20:00.348+00:00",
  "brCode": "00020101021126580014BR.GOV.BCB.PIX...",
  "brCodeBase64": "data:image/png;base64,iVBORw0KGgo..."
}
```

### `Paid` — tickets emitidos

```json
{
  "status": "Paid",
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "expiredAt": "2026-04-19T14:20:00.348+00:00",
  "tickets": [
    { "ticketId": 101, "lotteryId": 1, "userId": 42, "invoiceId": 1, "ticketNumber": 523, "refundState": "None", "createdAt": "..." },
    { "ticketId": 102, "lotteryId": 1, "userId": 42, "invoiceId": 1, "ticketNumber": 807, "refundState": "None", "createdAt": "..." }
  ]
}
```

### `Paid` — emissão não ocorreu (refund manual)

```json
{
  "status": "Paid",
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "expiredAt": "2026-04-19T14:20:00.348+00:00",
  "refundHint": "Pagamento confirmado mas Lottery fora de Open no momento do processamento — refund manual necessário."
}
```

`refundHint` pode assumir 3 variações conforme o motivo:

| Motivo | Mensagem |
|---|---|
| Lottery não `Open` | "Lottery não estava Open no momento do pagamento — refund manual necessário." |
| Reserva UserPicks expirada/incompleta | "Reservas expiraram antes do pagamento — refund manual necessário." |
| Pool Random insuficiente | "Pool de números insuficiente no momento do pagamento — refund manual necessário." |

### `Expired`

```json
{
  "status": "Expired",
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "expiredAt": "2026-04-19T14:20:00.348+00:00"
}
```

### `Cancelled`

```json
{ "status": "Cancelled", "invoiceId": 1, "invoiceNumber": "INV-0001-000001", "expiredAt": "..." }
```

### `Unknown`

Usado quando (a) o ProxyPay retorna estado fora do vocabulário conhecido, (b) o ProxyPay está indisponível, ou (c) o `invoiceId` não existe em `TicketOrder`.

```json
{ "status": "Unknown", "invoiceId": 1 }
```

## Response — Erros

| Status | Quando |
|---|---|
| 401 | Token ausente/inválido |
| 500 | Erro inesperado no `TicketService` (exceção não tratada). ProxyPay indisponível **não** dá 500 — vira `Unknown`. |

> Nota: `invoiceId` não encontrado **não** dá 404 (coerente com política "sem NotFound"). Retorna `Unknown`.

## Lógica (pseudocódigo)

```csharp
public async Task<TicketQRCodeStatusInfo> CheckQRCodeStatusAsync(long invoiceId)
{
    // 1. Pergunta ao ProxyPay
    var providerStatus = await _proxyPay.GetQRCodeStatusAsync(invoiceId);

    // 2. Lookup local
    var intent = await _intentRepo.GetByInvoiceIdAsync(invoiceId);
    if (intent is null)
        return new TicketQRCodeStatusInfo { Status = Unknown, InvoiceId = invoiceId };

    // 3. Reconciliação de estado conforme o provedor
    var result = providerStatus switch
    {
        Paid       => await ProcessPaymentAsync(intent),
        Expired    => await MarkExpiredIfPendingAsync(intent),
        Cancelled  => await MarkCancelledIfPendingAsync(intent),
        Pending    => ProcessResult.StillPending,
        Unknown    => ProcessResult.Unknown,
        _          => ProcessResult.Unknown
    };

    // 4. Monta resposta
    return BuildStatusInfo(intent, result);
}
```

`ProcessPaymentAsync` usa `TryMarkPaidAsync` (R-002) para garantir idempotência.

## Idempotência

**Totalmente idempotente**. Chamadas repetidas:
- Primeira que pega `Paid` do provedor: emite tickets, registra `InvoiceReferrer`, responde com `tickets: [...]`.
- Subsequentes: vêm do cache local (`TicketOrder.Status == Paid`), leem tickets por `invoice_id`, respondem com mesma lista. Zero duplicação.

## Idempotência em reconciliação

Se a primeira chamada caiu entre `TryMarkPaidAsync == 1` e `Ticket.InsertBatchAsync`, a segunda chamada detecta:

```csharp
if (intent.Status == Paid && !await _ticketRepo.AnyByInvoiceIdAsync(invoiceId))
{
    // Reconcilia: volta a emitir (ainda respeitando Lottery Open e reservas/pool)
    return await EmitTicketsAsync(intent);
}
```

## Sequência típica (happy path Paid)

```
Frontend (polling) ─► GET /tickets/qrcode/1/status
                        │
                        ▼
                    TicketsController.CheckQRCodeStatus
                        │
                        ├─► ProxyPay.GetQRCodeStatusAsync(1)
                        │       GET /payment/qrcode/status/1 (X-Tenant-Id)
                        │       response: { status: "paid" }
                        │
                        ├─► intentRepo.GetByInvoiceIdAsync(1)
                        │       intent.Status = Pending
                        │
                        ├─► ProcessPaymentAsync(intent):
                        │       intentRepo.TryMarkPaidAsync(intent.Id) → 1 row
                        │       BEGIN TX
                        │         ticketRepo.InsertBatchAsync([...])
                        │         invoiceReferrerRepo.InsertAsync(...) (se houver)
                        │       COMMIT
                        │
                        └─► 200 OK
                            { status: "Paid", tickets: [...] }
```
