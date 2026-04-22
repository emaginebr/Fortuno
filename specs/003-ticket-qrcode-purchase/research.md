# Research — Compra de Ticket via QR Code PIX

**Phase**: 0 (pré-design)
**Inputs**: `spec.md` (após `/speckit.clarify`), Constituição v1.0.0
**Goal**: Resolver decisões técnicas deferidas e fixar padrões que o Phase 1 (data-model, contracts) vai assumir sem reinterpretação.

---

## R-001: Vocabulário de status do ProxyPay no `GET /payment/qrcode/status/{invoiceId}`

**Contexto**: O frontend faz polling; o backend traduz a resposta do provedor para um enum interno (`Pending | Paid | Expired | Cancelled | Unknown`). O shape do response do provedor não está nos contratos compartilhados.

**Decision**: Assumir um shape canônico mínimo e protegê-lo com um DTO tolerante a variações:

```json
{
  "invoiceId": 1,
  "status": "pending",
  "paidAt": "2026-04-19T14:20:00.348+00:00"
}
```

Com `status` ∈ `{"pending","paid","expired","cancelled"}` (case-insensitive). Qualquer valor desconhecido → `Unknown`.

**Rationale**:
- É o vocabulário idiomático de integrações PIX similares (AbacatePay, Mercado Pago, Gerencianet) — alto grau de chance de acerto.
- O mapeamento fica centralizado em `ProxyPayAppService.GetQRCodeStatusAsync` — ajuste futuro é uma linha se a resposta real divergir.
- DTO do shape `{ invoiceId, status, paidAt? }` com `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` + fallback em `Unknown` é tolerante a: (a) campo novo; (b) mudança de nomes (`paid_at` vs `paidAt` — resolvemos com `PropertyNameCaseInsensitive = true`).
- `paidAt` é informacional — a **emissão** é disparada pelo `status == Paid`, não pelo timestamp.

**Alternatives considered**:
- Enum strict sem fallback → rejeitado porque qualquer valor novo quebraria o polling.
- Parse via `HttpContent` cru + switch no string → rejeitado por ser menos testável.
- Consultar o provedor para pegar o schema antes da implementação → rejeitado por falta de acesso a docs; será validado empiricamente em ambiente de dev.

**Implementação**:
- `ProxyPayQRCodeStatusResponse` em `Fortuno.DTO/ProxyPay/` com `string? Status`.
- `ProxyPayAppService.GetQRCodeStatusAsync` retorna `TicketOrderStatus` (enum em `Fortuno.DTO/Ticket/`) aplicando `status?.ToLowerInvariant()` e `switch` com `_ => Unknown`.
- Log `Information` do body cru em todo caso (já existe padrão em `ProxyPayAppService` após feature anterior) — facilita ajustar se o vocabulário real divergir.

---

## R-002: Idempotência de `TicketService.ProcessPaymentAsync(invoiceId)` sob concorrência

**Contexto**: FR-012/FR-022. Dois clients fazendo polling simultâneo podem chamar `ProcessPaymentAsync` ao mesmo tempo para o mesmo `invoiceId`. Sem controle, emitiríamos tickets duplicados e dois `InvoiceReferrer`s.

**Decision**: Usar **transição de estado condicional com `UPDATE ... WHERE status = Pending`** na tabela `fortuna_ticket_orders`, com índice único em `invoice_id`.

Fluxo:

```csharp
// 1. Carrega o TicketOrder
var intent = await _intentRepo.GetByInvoiceIdAsync(invoiceId);
if (intent is null) return ProcessResult.IntentNotFound;

// 2. Early-return se já processado (fast path)
if (intent.Status == TicketOrderStatus.Paid) return ProcessResult.AlreadyProcessed;

// 3. Carrega Lottery + valida status Open
var lottery = await _lotteryRepo.GetByIdAsync(intent.LotteryId);
if (lottery is null || lottery.Status != LotteryStatus.Open)
    return ProcessResult.RefundManual; // FR-014

// 4. Seleciona números (UserPicks: reservas; Random: sorteio)
List<long> numbers;
switch (intent.Mode) { ... }

// 5. Atômico: tenta marcar como Paid SE ainda estiver Pending
var rowsAffected = await _intentRepo.TryMarkPaidAsync(intent.TicketOrderId, TicketOrderStatus.Pending);
if (rowsAffected == 0) return ProcessResult.AlreadyProcessed; // perdeu a corrida

// 6. Agora somos o único escritor — insere tickets + InvoiceReferrer em transação
using var tx = await _context.Database.BeginTransactionAsync();
await _ticketRepo.InsertBatchAsync(tickets);
if (intent.ReferralCode is not null) await _invoiceReferrerRepo.InsertAsync(...);
await tx.CommitAsync();
```

O `TryMarkPaidAsync` é um `UPDATE fortuna_ticket_orders SET status = 2 WHERE ticket_order_id = @id AND status = 1` — retorna linhas afetadas. PostgreSQL garante atomicidade; apenas um dos dois polls concorrentes retorna `1`, o outro retorna `0` e segue com `AlreadyProcessed`.

**Rationale**:
- Index único em `invoice_id` (+ PK em `ticket_order_id`) torna o lookup O(1).
- Transição condicional é a primitiva mais barata e portável no PostgreSQL — zero locking explícito, zero job de reconciliação.
- Em caso de falha entre o passo 5 e o commit da transação do passo 6, a próxima chamada verá `status == Paid` **mas sem tickets** — condição anômala que deve ser detectada por consistência: `GetByInvoiceIdAsync` lê também a contagem de tickets por `invoice_id`; se `Paid && ticketCount == 0 && lotteryStillOpen` → retry do passo 6. Mapear essa lógica no helper `ReconcileIfNecessary` antes do early-return do passo 2.
- Alternativa com `SELECT ... FOR UPDATE` funciona mas segura conexão — polling frequente desperdiça recursos.

**Alternatives considered**:
- `Mutex` em memória por `invoiceId` → rejeitado: não funciona entre réplicas (será necessário se escalar horizontalmente).
- Lock distribuído (Redis) → rejeitado: dependência nova, infra nova — viola §II.
- Leitura de `WebhookEvent` (idempotência anterior) → rejeitado: webhook sai (FR-017), a nova idempotência vive no próprio `TicketOrder.Status`.
- Índice único em `fortuna_tickets.invoice_id + ticket_number` como barreira → complementar mas não exclusivo: usar como **defesa secundária** (se por bug lógico alguém tentar inserir, o DB rejeita).

---

## R-003: Shape de `items[]` no `POST /payment/qrcode`

**Contexto**: FR-005/FR-009. O provedor aceita `items[]` com `id`, `description`, `quantity`, `unitPrice`, `discount`. A compra pode ter desconto de combo; precisamos decidir se mandamos 1 linha por ticket ou 1 linha consolidada.

**Decision**: **Uma única linha consolidada** representando o lote.

```json
{
  "items": [
    {
      "id": "LOTTERY-{lotteryId}",
      "description": "Fortuno Lottery #{lotteryId} - {quantity} tickets",
      "quantity": {quantity},
      "unitPrice": {ticketPrice},
      "discount": {comboDiscountValue}
    }
  ]
}
```

Onde `totalAmount = (quantity * unitPrice) - discount`, batendo com o valor calculado pelo backend antes da chamada.

**Rationale**:
- O provedor só precisa do valor total para gerar o PIX; granularidade por ticket não traz benefício (recibo individual de ticket é responsabilidade do Fortuno, não do provedor).
- Mantém o payload compacto mesmo em compras grandes (ex.: 500 tickets → 1 linha, não 500).
- Logs ficam legíveis.

**Alternatives considered**:
- N linhas (1 por ticket) → rejeitado: verboso e sem valor funcional.
- N linhas (1 por faixa de combo) → rejeitado: hoje só existe 1 combo aplicável por compra.

---

## R-004: Fonte do `clientId` enviado ao ProxyPay

**Contexto**: O exemplo do user input usa `clientId: "5b2a4084154d4e88941b76aee1395348"`. Pela GraphQL `myStore`, cada Store tem um `clientId` próprio (tenant do lojista no provedor).

**Decision**: O `clientId` vem da Store dona da Lottery, descoberto em runtime via a query `myStore` do ProxyPay (já implementada em `ProxyPayAppService.GetStoreAsync` para o `StoreOwnershipGuard`).

Expansão: `ProxyPayStoreInfo` ganha a propriedade `ClientId` (string). Guarda-chuva: ao criar o QR Code, `TicketService.CreateQRCodeAsync` chama `_proxyPay.GetStoreAsync(lottery.StoreId)` e usa `store.ClientId` no payload.

**Rationale**:
- `clientId` é por Store, não por instalação do Fortuno — não cabe em `ProxyPaySettings` global.
- Já há uma chamada ao ProxyPay nesse fluxo (`StoreOwnershipGuard` usa `GetStoreAsync` para checar posse da Store por quem é dono). Embora o comprador **não** seja dono da Store, `myStore` retorna as stores **do usuário autenticado**; não serve para resolver `clientId` de Store de terceiro.
- **Correção**: `myStore` só vale quando o sujeito autenticado é o dono. Para o comprador, precisamos de uma rota ProxyPay que devolva **qualquer** Store por `storeId` (público ou read-only). Se o ProxyPay não expõe essa rota, cacheamos o `clientId` em `fortuna_lotteries.store_client_id` ao criar a Lottery (o criador da Lottery é o dono da Store e pode consultar via `myStore` naquele momento).

**Decision revisada**: Adicionar coluna `store_client_id varchar(64)` em `fortuna_lotteries`, populada na criação da Lottery via `myStore` (o criador é o dono). `TicketService.CreateQRCodeAsync` lê do `lottery.StoreClientId` sem nova chamada ao ProxyPay.

**Alternatives considered**:
- Colocar `clientId` em `ProxyPaySettings` global → rejeitado: é por Store, não por app.
- Uma nova tabela `store_settings` que mirror do ProxyPay → rejeitado: overhead para 1 campo.
- Chamar ProxyPay (rota admin) a cada compra → rejeitado: latência extra e depende de rota que pode não existir.

**Implementação**:
- Adicionar `StoreClientId string?` em `Lottery` (nullable para compatibilidade com Lotteries antigas, mas obrigatório em Lotteries criadas nesta feature em diante).
- Na criação da Lottery, `LotteryService.CreateAsync` chama `_proxyPay.GetStoreAsync(dto.StoreId)` (já existe — retorna `ProxyPayStoreInfo`) e popula `StoreClientId = store.ClientId`.
- Se `StoreClientId` vier vazio no momento da compra, retornar 500 com mensagem orientando a recriar a Lottery.

---

## R-005: Migração de dados existentes

**Contexto**: Hoje existem `WebhookEvent`s e potencialmente `NumberReservation`s ativos. Como a feature altera o fluxo, é preciso decidir o que acontece com esses dados.

**Decision**:
- **`WebhookEvent`**: a tabela permanece vazia após o merge (ninguém insere mais). Manter a tabela por 1 release para consulta histórica; remover em feature posterior.
- **`NumberReservation`**: preservada. Reservas ativas (`ExpiresAt > now`) permanecem válidas. Não existe migração de dados porque não havia `TicketOrder`s antes — qualquer reserva sem `InvoiceId` associado é órfã e será liberada pelo TTL natural.
- **Invoices em aberto no ProxyPay no momento do merge**: como não há mais webhook, se o pagamento chegar pós-merge sem `TicketOrder`, o fluxo de polling retorna `Paid` sem `TicketOrder` → `ProcessPaymentAsync` retorna `IntentNotFound` → cai no refund manual. É aceitável porque o volume em dev/homologação é zero e produção ainda não subiu.

**Rationale**: Evitar migração de dados reduz risco do merge. `TicketOrder` é criado a partir daqui; o legado é irrelevante.

---

## R-006: Testes — cobertura mínima

**Contexto**: SC-007 exige cobertura de 5 cenários end-to-end.

**Decision**: Desenho mínimo da suite:

1. **Unit (`TicketServiceTests`)**:
   - `CreateQRCodeAsync_*`: happy path Random, happy path UserPicks, Lottery não Open, quantity fora de limites, NAuth sem campos, provedor falha (4xx/5xx).
   - `CheckQRCodeStatusAsync_*`: Pending, Paid (primeira vez → emite), Paid (segunda vez → idempotente), Expired, Cancelled, Unknown, Paid mas Lottery fechada (refundHint), `invoiceId` inexistente.
   - `ProcessPaymentAsync_*`: happy paths (Random e UserPicks), idempotência (dupla chamada), Lottery saiu de Open, reserva expirada em UserPicks, pool insuficiente em Random, reconciliação (status=Paid mas 0 tickets).

2. **Integration (`Fortuno.ApiTests/Tickets/TicketPurchaseFlowTests`)**:
   - Happy path: cria Lottery em Open com imagem/raffle/award; POST /tickets/qrcode; assert resposta contém `brCode`/`brCodeBase64`/`expiredAt`/`invoiceId`; GET status → Pending.
   - Sem Lottery Open: POST /tickets/qrcode com Lottery Draft → 400.
   - `invoiceId` inexistente em GET status → retorna `Unknown` com 200 (ou 404 se spec preferir; ver `contracts/`).

3. **Contract-like (`ProxyPayAppServiceTests`)**: com `HttpMessageHandler` mock — verifica que a requisição enviada ao ProxyPay tem `X-Tenant-Id`, body no shape correto, parse do response.

**Implementação segue a skill `dotnet-test` + `dotnet-test-api`.**

---

## R-007: Rotas do TicketController

**Contexto**: Após refactor anterior, rotas do Fortuno não levam `/api`. O `TicketsController` já existe com `[Route("tickets")]`.

**Decision**: Rotas finais:

| Método | Rota | Descrição |
|---|---|---|
| POST | `/tickets/qrcode` | Cria a cobrança PIX e devolve QR Code |
| GET | `/tickets/qrcode/{invoiceId}/status` | Consulta status + emite tickets se pago (idempotente) |
| GET | `/tickets/mine` | existente — lista tickets do comprador |
| GET | `/tickets/{ticketId}` | existente — detalhe |

**Rationale**: `tickets/qrcode` agrupa as duas rotas do fluxo de compra sob o sub-path `qrcode`, facilita descoberta no Swagger e no Bruno.

---

## Conclusão do Phase 0

Todos os pontos deferidos da clarify e do user input estão resolvidos ou com estratégia clara para resolver empiricamente durante a implementação. Nenhum `NEEDS CLARIFICATION` remanescente no plan.
