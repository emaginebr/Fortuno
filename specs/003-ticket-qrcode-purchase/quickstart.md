# Quickstart — Validação manual do fluxo de compra via QR Code

**Objetivo**: validar ponta a ponta os 2 novos endpoints do `TicketsController` após a implementação.

## Pré-requisitos

- `Fortuno.API` rodando em Docker (`fortuno-api` container na porta 5000) com migração aplicada.
- Banco com Lottery em `Open`, ao menos 1 imagem, 1 raffle, 1 award cadastrados (ver `LotteryActivationTests`).
- A Lottery deve ter `store_client_id` populado (criada após o merge desta feature).
- Usuário de teste no NAuth com: `name`, `email`, `documentId` (CPF) e `cellphone` preenchidos no perfil.
- Acesso ao endpoint externo do ProxyPay em `https://proxypay.online/api` — `clientId` da Store cacheado na Lottery.
- Token válido do NAuth (obter via `POST {NAuthUrl}/user/loginWithEmail`).

## Variáveis

```bash
export TOKEN="<base64-token-do-nauth>"
export TENANT="fortuna"
export API="http://localhost:5000"
export LOTTERY_ID=<id-da-lottery-Open>
```

## 1. Criar QR Code — modo Random

```bash
curl -s -X POST "$API/tickets/qrcode" \
  -H "Authorization: Basic $TOKEN" \
  -H "X-Tenant-Id: $TENANT" \
  -H "Content-Type: application/json" \
  -d "{
    \"lotteryId\": $LOTTERY_ID,
    \"quantity\": 3,
    \"mode\": \"Random\"
  }" | jq
```

**Esperado** (201):

```json
{
  "invoiceId": 42,
  "invoiceNumber": "INV-0001-000042",
  "brCode": "00020101021126...",
  "brCodeBase64": "data:image/png;base64,iVBOR...",
  "expiredAt": "2026-04-19T14:35:00.000+00:00"
}
```

Guardar o `invoiceId`:

```bash
export INVOICE_ID=42
```

## 2. Consultar status (ainda não pago)

```bash
curl -s "$API/tickets/qrcode/$INVOICE_ID/status" \
  -H "Authorization: Basic $TOKEN" \
  -H "X-Tenant-Id: $TENANT" | jq
```

**Esperado** (200):

```json
{
  "status": "Pending",
  "invoiceId": 42,
  "invoiceNumber": "INV-0001-000042",
  "expiredAt": "2026-04-19T14:35:00.000+00:00",
  "brCode": "00020101021126...",
  "brCodeBase64": "data:image/png;base64,iVBOR..."
}
```

## 3. Pagar o PIX (manualmente, fora do Fortuno)

- Usar o `brCode` num app bancário de homologação, OU
- Usar ferramenta do ProxyPay para marcar o invoice como pago em devmode.

## 4. Consultar status (pago — primeira vez)

```bash
curl -s "$API/tickets/qrcode/$INVOICE_ID/status" \
  -H "Authorization: Basic $TOKEN" \
  -H "X-Tenant-Id: $TENANT" | jq
```

**Esperado** (200):

```json
{
  "status": "Paid",
  "invoiceId": 42,
  "invoiceNumber": "INV-0001-000042",
  "expiredAt": "2026-04-19T14:35:00.000+00:00",
  "tickets": [
    { "ticketId": 201, "lotteryId": 1, "ticketNumber": 523, ... },
    { "ticketId": 202, "lotteryId": 1, "ticketNumber": 807, ... },
    { "ticketId": 203, "lotteryId": 1, "ticketNumber": 142, ... }
  ]
}
```

Validar no banco:

```sql
SELECT * FROM fortuna_tickets WHERE invoice_id = 42;
SELECT status FROM fortuna_ticket_orders WHERE invoice_id = 42;   -- 2 (Paid)
```

## 5. Consultar status (idempotente — segunda vez)

Mesma chamada do passo 4. **Esperado**: resposta idêntica (mesmos `ticketIds`), nenhuma nova inserção em `fortuna_tickets`.

## 6. Modo UserPicks

```bash
curl -s -X POST "$API/tickets/qrcode" \
  -H "Authorization: Basic $TOKEN" \
  -H "X-Tenant-Id: $TENANT" \
  -H "Content-Type: application/json" \
  -d "{
    \"lotteryId\": $LOTTERY_ID,
    \"quantity\": 2,
    \"mode\": \"UserPicks\",
    \"pickedNumbers\": [100, 200]
  }" | jq
```

Verificar reservas:

```sql
SELECT * FROM fortuna_number_reservations
 WHERE lottery_id = 1 AND ticket_number IN (100, 200);
```

Deve mostrar 2 reservas com `invoice_id` preenchido e `expires_at` no futuro.

## 7. Cenário de expiração

1. Criar QR Code em `UserPicks`.
2. **Não** pagar.
3. Após `expiredAt` passar, consultar status → retorna `Expired`.
4. Consultar disponibilidade ou tentar outra compra nos mesmos números: deve funcionar (reservas expiradas são ignoradas em leituras — FR-024).

## 8. Cenário de Lottery fechada pós-pagamento

1. Criar QR Code.
2. Antes de pagar, **fechar** a Lottery via `POST /lotteries/{id}/close` (requer estado `Open` + outras precondições; pode ser necessário cancelar em vez de fechar).
3. Pagar o PIX.
4. Consultar status → retorna `Paid` **com `refundHint`**, sem tickets:

```json
{
  "status": "Paid",
  "invoiceId": 42,
  "refundHint": "Lottery não estava Open no momento do pagamento — refund manual necessário."
}
```

5. Validar que `fortuna_tickets` não tem linhas para esse `invoice_id` e que o refund fica visível em `GET /refunds/pending/{lotteryId}`.

## 9. Erros esperados

### Comprador sem campos NAuth

Se o `NAuthUserInfo` do usuário não tiver CPF:

```bash
curl -X POST "$API/tickets/qrcode" -H "Authorization: ..." -d '{"lotteryId":1,"quantity":1,"mode":"Random"}'
```

**Esperado** (400):

```json
{ "sucesso": false, "mensagem": "Complete seu cadastro no NAuth antes de comprar (faltam: documentId)." }
```

### Lottery não Open

```bash
# Lottery em Draft
```

**Esperado** (400): `"Compras disponíveis apenas em Lottery Open."`

### invoiceId inexistente em status

```bash
curl -s "$API/tickets/qrcode/999999/status" -H "Authorization: ..." -H "X-Tenant-Id: ..."
```

**Esperado** (200): `{ "status": "Unknown", "invoiceId": 999999 }`

## 10. Logs para debug

Container `fortuno-api` deve emitir:

```
INFO ProxyPay: POST /payment/qrcode clientId=5b2a40... quantity=3 totalAmount=30.00
INFO ProxyPay: resposta CreateQRCode status=201 body={...}
INFO ProxyPay: GET /payment/qrcode/status/42
INFO ProxyPay: resposta QRCodeStatus status=200 body={invoiceId:42,status:"paid"}
INFO TicketService: ProcessPayment invoiceId=42 → Paid — emitidos 3 tickets
```

## Checklist de validação

- [ ] POST /tickets/qrcode (Random) devolve `brCode` + `brCodeBase64` em < 3s (SC-001).
- [ ] GET status retorna `Pending` antes do pagamento.
- [ ] GET status após pagamento emite tickets e devolve `tickets[]`.
- [ ] GET status subsequente retorna mesma lista sem duplicar (SC-003).
- [ ] Modo UserPicks cria `NumberReservation`s vinculadas ao `invoiceId`.
- [ ] QR Code expirado libera reservas (via lazy) e devolve `Expired`.
- [ ] Lottery fechada pós-pagamento devolve `Paid + refundHint` sem emitir tickets (SC-005).
- [ ] NAuth incompleto → 400 com campos faltantes.
- [ ] `invoiceId` inexistente → `Unknown`.
- [ ] Logs do ProxyPay visíveis no container.
- [ ] Nenhuma referência a `WebhooksController` de pagamento ou `ProxyPayWebhookHmacFilter` no build (SC-006).
