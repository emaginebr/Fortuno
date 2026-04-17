# Webhook Contract — ProxyPay → Fortuno

**Endpoint**: `POST /webhooks/proxypay/invoice-paid`
**Origem**: Microserviço ProxyPay
**Autenticação**: HMAC-SHA256 no header `X-ProxyPay-Signature`
**Idempotência**: garantida por `UNIQUE (invoice_id, event_type)` na tabela
`fortuna_webhook_events`.

## Autenticação

O ProxyPay assina o corpo bruto da requisição com um segredo compartilhado
e envia o hash em `X-ProxyPay-Signature`:

```
X-ProxyPay-Signature: sha256=<hex-hmac>
```

O Fortuno recalcula o HMAC com o segredo configurado em
`ProxyPaySettings:WebhookSecret` e **rejeita com 401** se não coincidir.

## Payload

```json
{
  "eventId": "evt_01H9Z8K...",
  "eventType": "invoice.paid",
  "occurredAt": "2026-04-17T12:34:56Z",
  "tenant": "fortuna",
  "data": {
    "invoiceId": 123456789,
    "storeId": 42,
    "amount": 90.00,
    "paidAt": "2026-04-17T12:34:55Z",
    "status": "paid",
    "metadata": {
      "fortunoPurchaseId": "9876"
    }
  }
}
```

| Campo | Tipo | Uso no Fortuno |
|---|---|---|
| `eventId` | string | Opcional. Usado para logs. |
| `eventType` | string | Deve ser `invoice.paid`. Outros tipos são 200 OK sem ação. |
| `tenant` | string | Deve ser `"fortuna"`. Caso contrário, 401/403. |
| `data.invoiceId` | int64 | Chave primária da deduplicação. |
| `data.amount` | decimal | Registrado como `Invoice.PaidAmount` para cálculo de comissão. |
| `data.metadata.fortunoPurchaseId` | string | Correlaciona com o purchase iniciado em `/api/purchases/confirm`. |

## Processamento (comportamento obrigatório)

1. **Validar assinatura HMAC**. Se inválida → `401 Unauthorized`.
2. **Validar tenant** = `"fortuna"`. Se não → `401 Unauthorized`.
3. **Inserir registro** em `fortuna_webhook_events (invoice_id, event_type)`.
   Se UNIQUE violation → **`200 OK`** (duplicata, ignorada silenciosamente).
4. **Carregar o purchase-intent** correspondente (via `metadata.fortunoPurchaseId`).
5. **Gerar tickets**:
   - Modo `Random`: sorteia N números disponíveis do pool da Lottery em
     transação `SERIALIZABLE`.
   - Modo `UserPicks`: promove os números reservados em
     `fortuna_number_reservations` para `fortuna_tickets`; se qualquer
     reserva expirou, rejeita a emissão e dispara fluxo de estorno
     off-platform (via marcação `refund_state = PendingRefund`).
6. **Registrar InvoiceReferrer** se `referralCode` foi aplicado no
   purchase-intent.
7. **Responder `200 OK`** com body `{"status":"processed"}` — o ProxyPay
   considera a entrega bem-sucedida.

## Retry (responsabilidade do ProxyPay)

- O Fortuno espera que o ProxyPay re-entregue o webhook em caso de
  timeout ou status ≥ 500.
- Retries com o mesmo `invoice_id` são tratados como duplicatas e
  respondidos com `200 OK` sem reprocessamento (passo 3).

## SLO esperado

- Latência p95 do handler: ≤ 5 s (inclui escrita transacional no Postgres).
- SC-003 da spec: tickets visíveis no "Meus Tickets" em ≤ 30 s após o
  `occurredAt` — margem confortável para retry única.

## Teste de contrato

Em `Fortuno.Tests/API/Controllers/WebhooksControllerTests.cs` validar:

- Assinatura inválida retorna 401.
- Tenant diferente de `fortuna` retorna 401.
- Primeira entrega com payload válido emite tickets e grava em
  `fortuna_webhook_events`.
- Segunda entrega idêntica retorna 200 sem emitir tickets adicionais
  (verificação por count de tickets).
- Modo `UserPicks` com reserva expirada marca tickets como
  `PendingRefund` em vez de emitir.
