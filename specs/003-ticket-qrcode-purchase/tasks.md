---

description: "Task list para feature 003-ticket-qrcode-purchase"
---

# Tasks: Compra de Ticket via QR Code PIX (sem webhook, sem preview)

**Input**: Design documents from `/specs/003-ticket-qrcode-purchase/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Inclusos (SC-007 exige suíte automatizada; skills `dotnet-test` e `dotnet-test-api` obrigatórias para criar/manter `Fortuno.Tests` e `Fortuno.ApiTests`).

**Organization**: Tasks agrupadas por User Story (US1/US2/US3) conforme `spec.md`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências bloqueantes)
- **[Story]**: Mapeia para US1/US2/US3 de `spec.md`
- Caminhos absolutos ou relativos à raiz do repo (`C:/repos/Fortuno/Fortuno/`)

## Path Conventions

Clean Architecture existente:

- `Fortuno.DTO/` — DTOs (1 classe por arquivo)
- `Fortuno.Domain/Models/`, `Fortuno.Domain/Services/`, `Fortuno.Domain/Interfaces/`, `Fortuno.Domain/Enums/`
- `Fortuno.Infra.Interfaces/Repository/`, `Fortuno.Infra.Interfaces/AppServices/`
- `Fortuno.Infra/Context/`, `Fortuno.Infra/Repository/`, `Fortuno.Infra/AppServices/`, `Fortuno.Infra/Migrations/`
- `Fortuno.Application/Startup.cs`
- `Fortuno.API/Controllers/`, `Fortuno.API/Validators/`, `Fortuno.API/Program.cs`
- `Fortuno.Tests/Domain/Services/`, `Fortuno.Tests/Infra/AppServices/`, `Fortuno.Tests/Application/Validations/`
- `Fortuno.ApiTests/Tickets/`
- `bruno/Tickets/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Nenhuma infra nova — a Clean Architecture já existe, só precisa confirmar branch e dependências.

- [X] T001 Garantir branch `003-ticket-qrcode-purchase` ativa e `dotnet build Fortuno.sln` limpo antes de iniciar
- [X] T002 Confirmar que o `CLAUDE.md` já foi atualizado pelo `/speckit.plan` (linha `003-ticket-qrcode-purchase` presente em `C:/repos/Fortuno/Fortuno/CLAUDE.md`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entidade `TicketOrder` + cache `StoreClientId` em `Lottery` + DTOs ProxyPay novos — pré-requisitos de **todas** as 3 user stories.

**⚠️ CRITICAL**: US1 não pode começar sem T003–T018.

### DTOs (skill `dotnet-architecture` aplicada; 1 classe por arquivo — §III)

- [X] T003 [P] Criar `Fortuno.DTO/Ticket/TicketOrderStatus.cs` com enum `{ Pending=1, Paid=2, Expired=3, Cancelled=4, Unknown=5 }`
- [X] T004 [P] Criar `Fortuno.DTO/Ticket/TicketOrderRequest.cs` com propriedades `LotteryId`, `Quantity`, `Mode` (usa `TicketOrderMode`), `PickedNumbers` (`List<long>?`), `ReferralCode` (`string?`) — todas com `[JsonPropertyName("camelCase")]`
- [X] T005 [P] Criar `Fortuno.DTO/Ticket/TicketQRCodeInfo.cs` (response do POST /tickets/qrcode) com `InvoiceId`, `InvoiceNumber`, `BrCode`, `BrCodeBase64`, `ExpiredAt`
- [X] T006 [P] Criar `Fortuno.DTO/Ticket/TicketQRCodeStatusInfo.cs` (response do GET status) com `Status` (enum), `InvoiceId`, `InvoiceNumber`, `ExpiredAt?`, `BrCode?`, `BrCodeBase64?`, `Tickets?` (`List<TicketInfo>?`), `RefundHint?`
- [X] T007 [P] Criar `Fortuno.DTO/ProxyPay/ProxyPayCustomer.cs` com `Name`, `Email`, `DocumentId`, `Cellphone`
- [X] T008 [P] Criar `Fortuno.DTO/ProxyPay/ProxyPayItem.cs` com `Id`, `Description`, `Quantity`, `UnitPrice`, `Discount`
- [X] T009 [P] Criar `Fortuno.DTO/ProxyPay/ProxyPayQRCodeRequest.cs` com `ClientId`, `Customer`, `Items`
- [X] T010 [P] Criar `Fortuno.DTO/ProxyPay/ProxyPayQRCodeResponse.cs` com `InvoiceId`, `InvoiceNumber`, `BrCode`, `BrCodeBase64`, `ExpiredAt`
- [X] T011 [P] Criar `Fortuno.DTO/ProxyPay/ProxyPayQRCodeStatusResponse.cs` com `InvoiceId`, `Status` (string), `PaidAt?`
- [X] T012 [P] Atualizar `Fortuno.DTO/ProxyPay/ProxyPayStoreInfo.cs` adicionando propriedade `ClientId string`

### Domain

- [X] T013 [P] Criar `Fortuno.Domain/Enums/TicketOrderStatus.cs` com `{ Pending=1, Paid=2, Expired=3, Cancelled=4 }`
- [X] T014 [P] Criar `Fortuno.Domain/Models/TicketOrder.cs` conforme `data-model.md` §1 (propriedades + `public Lottery Lottery { get; set; }`)
- [X] T015 [P] Ampliar `Fortuno.Domain/Models/Lottery.cs` adicionando `public string? StoreClientId { get; set; }`

### Infra.Interfaces

- [X] T016 [P] Criar `Fortuno.Infra.Interfaces/Repository/ITicketOrderRepository.cs` herdando de `IRepository<TicketOrder>` com métodos `GetByInvoiceIdAsync(long)`, `TryMarkPaidAsync(long)`, `TryMarkExpiredAsync(long)`, `TryMarkCancelledAsync(long)`
- [X] T017 Atualizar `Fortuno.Infra.Interfaces/AppServices/IProxyPayAppService.cs` — remover `CreateInvoiceAsync` e `GetInvoiceAsync`; adicionar `Task<ProxyPayQRCodeResponse> CreateQRCodeAsync(ProxyPayQRCodeRequest request)` e `Task<ProxyPayQRCodeStatusResponse?> GetQRCodeStatusAsync(long invoiceId)`

### Infra: Context + Repository + Migration

- [X] T018 Atualizar `Fortuno.Infra/Context/FortunoContext.cs`: adicionar `DbSet<TicketOrder> TicketOrders` + Fluent API config completa (ver `data-model.md`); adicionar `e.Property(x => x.StoreClientId).HasColumnName("store_client_id").HasColumnType("varchar(64)");` no `modelBuilder.Entity<Lottery>` existente
- [X] T019 Criar `Fortuno.Infra/Repository/TicketOrderRepository.cs` implementando `ITicketOrderRepository`. `TryMarkPaidAsync` executa `UPDATE fortuna_ticket_orders SET status = 2, updated_at = now() WHERE ticket_order_id = @id AND status = 1` via `_context.Database.ExecuteSqlInterpolatedAsync` e retorna rows afetadas (R-002 idempotência)
- [X] T020 Gerar migration via skill `dotnet-architecture`: `dotnet ef migrations add AddTicketOrders --project Fortuno.Infra --startup-project Fortuno.API` — migration deve criar `fortuna_ticket_orders` com PK, índice único em `invoice_id`, índices em `user_id` e `lottery_id`, FK `fk_ticket_order_lottery` com `ClientSetNull`; e adicionar coluna `store_client_id varchar(64) NULL` em `fortuna_lotteries`; verificar também `CREATE INDEX IF NOT EXISTS ix_tickets_invoice_id ON fortuna_tickets (invoice_id)` (R-002)

### Application (DI)

- [X] T021 Atualizar `Fortuno.Application/Startup.cs`: registrar `services.AddScoped<ITicketOrderRepository<TicketOrder>, TicketOrderRepository>();` (após `_webhookEventRepo`); verificar se a assinatura `IRepository<TicketOrder>` casa com o repo

**Checkpoint Foundational**: `dotnet build Fortuno.sln` 0 erros; migration aplicada em ambiente local (`dotnet ef database update --project Fortuno.Infra --startup-project Fortuno.API`); banco tem tabela `fortuna_ticket_orders` e coluna `store_client_id`.

---

## Phase 3: User Story 1 — Comprador recebe QR Code PIX (Priority: P1) 🎯 MVP

**Goal**: `POST /tickets/qrcode` valida, chama ProxyPay, persiste `TicketOrder` e devolve o QR Code.

**Independent Test**: Criar Lottery em `Open` com imagem/raffle/award + `store_client_id` populado; usuário NAuth com `name/email/documentId/cellphone` completos; POST no endpoint devolve 201 com `brCode`/`brCodeBase64`/`expiredAt`/`invoiceId`.

### Tests for User Story 1 (skill `dotnet-test` e `dotnet-test-api`)

- [ ] T022 [P] [US1] Criar `Fortuno.Tests/Application/Validations/TicketOrderRequestValidatorTests.cs` cobrindo: DTO válido (Random e UserPicks), `LotteryId <= 0`, `Quantity <= 0`, `Mode` inválido, `UserPicks` sem `PickedNumbers`, `UserPicks` com `PickedNumbers.Count != Quantity`, `ReferralCode` > 8 chars, `ReferralCode` com chars fora de `[A-Z0-9]`
- [X] T023 [P] [US1] Ampliar `Fortuno.Tests/Infra/AppServices/ProxyPayAppServiceTests.cs` com cenários `CreateQRCodeAsync_*`: happy path (mock devolve response completa), 4xx (propaga `InvalidOperationException` com mensagem), 5xx (propaga "ProxyPay indisponível"); verifica via `HttpMessageHandler` mock que a request tem `X-Tenant-Id` e body no shape correto
- [ ] T024 [P] [US1] Criar `Fortuno.Tests/Domain/Services/TicketServiceTests.cs` (ou ampliar se existir) com cenários `CreateQRCodeAsync_*`: happy Random, happy UserPicks, Lottery não Open → `InvalidOperationException`, quantity fora de TicketMin/TicketMax, NAuth sem `DocumentId`/`Phone` → exception com mensagem acionável, Lottery sem `StoreClientId` → exception, ProxyPay falha → exception e reservas revertidas
- [X] T025 [US1] Criar `Fortuno.ApiTests/Tickets/TicketPurchaseFlowTests.cs` (skill `dotnet-test-api`) com teste `Create_Random_ShouldReturnQRCodeInfo` que: cria Lottery em Open via helper end-to-end (imagem + raffle + award), POST `/tickets/qrcode` com mode Random, asserta status 201 + campos presentes + `expiredAt` no futuro; captura body em `FlurlHttpException` para diagnóstico

### Implementation for User Story 1

- [X] T026 [P] [US1] Criar `Fortuno.API/Validators/TicketOrderRequestValidator.cs` via skill `dotnet-fluent-validation` conforme `contracts/ticket-purchase.md` (regras por tipo de campo)
- [X] T027 [US1] Atualizar `Fortuno.Infra/AppServices/ProxyPayAppService.cs`: alterar query `myStore` para `{ myStore { storeId userId clientId name } }` e `StoreDto` para incluir `ClientId`; mapear `ProxyPayStoreInfo.ClientId = store.ClientId`; logar valor no `Information` (tratar como dado não-sensível — é identificador do tenant na Store)
- [X] T028 [US1] Implementar `Fortuno.Infra/AppServices/ProxyPayAppService.CreateQRCodeAsync`: POST relativo `"payment/qrcode"` (sem `/` inicial, confiando na `BaseAddress` com trailing slash), forwarda Authorization + adiciona X-Tenant-Id explícito, log `Information` antes/depois com body cru, tolera 4xx/5xx conforme `contracts/proxypay-payment-qrcode.md`
- [X] T029 [US1] Atualizar `Fortuno.Domain/Services/LotteryService.CreateAsync`: após validar dto e antes de salvar, chamar `_proxyPay.GetStoreAsync(dto.StoreId)` (já chamado por `_ownership.EnsureOwnershipAsync`; reutilizar ou fazer uma segunda chamada); popular `entity.StoreClientId = store.ClientId`
- [X] T030 [US1] Ampliar `Fortuno.Domain/Interfaces/ITicketService.cs` adicionando `Task<TicketQRCodeInfo> CreateQRCodeAsync(long currentUserId, TicketOrderRequest request)`
- [X] T031 [US1] Implementar `Fortuno.Domain/Services/TicketService.CreateQRCodeAsync` conforme `contracts/ticket-purchase.md` sequência: (a) valida Lottery `Open` + `StoreClientId` preenchido + `TicketMin/TicketMax`; (b) consulta NAuth via `INAuthAppService.GetCurrentAsync()` e valida 4 campos obrigatórios (`InvalidOperationException` com lista de faltantes se alguém faltar); (c) calcula `totalAmount` com desconto de combo via `_comboRepo.FindMatchingComboAsync`; (d) em UserPicks: valida números (reutilizar `NumberCompositionService` + `AreNumbersAvailableAsync`) e cria `NumberReservation`s com TTL provisional = 20min; (e) chama `ProxyPayAppService.CreateQRCodeAsync`; (f) em UserPicks: atualiza reservations com `InvoiceId` + ajusta `ExpiresAt` para `response.ExpiredAt`; (g) grava `TicketOrder` (status Pending, `referralPercentAtPurchase = lottery.ReferralPercent`, `pickedNumbersJson = JsonSerializer.Serialize(pickedNumbers)` se UserPicks); (h) retorna `TicketQRCodeInfo` mapeado da response
- [X] T032 [US1] Adicionar endpoint `POST /tickets/qrcode` em `Fortuno.API/Controllers/TicketsController.cs`: `[Authorize]`, recebe `TicketOrderRequest`, chama `_tickets.CreateQRCodeAsync(User.GetCurrentUserId(), dto)`; try/catch para `InvalidOperationException` → BadRequest, `UnauthorizedAccessException` → 403, `Exception` → 500 (conforme §6 da Constituição)
- [X] T033 [US1] Registrar `FluentValidation` do `TicketOrderRequestValidator` em `Fortuno.API/Program.cs` (já é automático via assembly scan — verificar e ajustar se necessário)
- [X] T034 [P] [US1] Criar request Bruno `bruno/Tickets/qrcode-create.bru` com body exemplo para ambos os modos (Random e UserPicks); script pós-resposta captura `invoiceId` em `bru.setVar("invoiceId", res.body.invoiceId)`

**Checkpoint US1**: `dotnet test Fortuno.Tests` + `dotnet test Fortuno.ApiTests` passam; Bruno consegue criar QR Code contra ambiente local; `SELECT * FROM fortuna_ticket_orders` mostra linha com status=1.

---

## Phase 4: User Story 2 — Polling de status + emissão de tickets (Priority: P1)

**Goal**: `GET /tickets/qrcode/{invoiceId}/status` consulta ProxyPay, traduz, e quando Paid (primeira vez) chama `ProcessPaymentAsync` — idempotente.

**Independent Test**: Após US1, cria QR Code; antes de pagar, GET status → Pending; paga o PIX; GET status → Paid com tickets[]; GET status de novo → mesmos tickets (idempotente).

### Tests for User Story 2

- [X] T035 [P] [US2] Ampliar `Fortuno.Tests/Infra/AppServices/ProxyPayAppServiceTests.cs` com cenários `GetQRCodeStatusAsync_*`: status "pending"→Pending, "paid"→Paid (case-insensitive, teste com "PAID"), "expired"→Expired, "cancelled"→Cancelled, status desconhecido→Unknown, HTTP 404→null/Unknown, HTTP 5xx→null/Unknown (sem exception — R-001)
- [ ] T036 [P] [US2] Ampliar `Fortuno.Tests/Domain/Services/TicketServiceTests.cs` com cenários `CheckQRCodeStatusAsync_*` (8 cenários): Pending devolve brCode+brCodeBase64; Paid primeira vez emite tickets e InvoiceReferrer; Paid segunda vez devolve mesmos ticketIds sem duplicar; Paid mas Lottery fechada devolve refundHint sem tickets; Paid em UserPicks com reservas expiradas devolve refundHint; Paid em Random com pool insuficiente devolve refundHint; Expired retorna shape mínimo + marca intent como Expired (condicional); invoiceId inexistente retorna Unknown
- [ ] T037 [P] [US2] Ampliar `Fortuno.Tests/Domain/Services/TicketServiceTests.cs` com cenários `ProcessPaymentAsync_*` cobrindo concorrência: simular dupla chamada com `TryMarkPaidAsync` retornando 1 na primeira e 0 na segunda → segunda retorna `AlreadyProcessed` sem inserir tickets; reconciliação quando intent.Status=Paid mas 0 tickets por invoiceId → re-emite
- [ ] T038 [US2] Ampliar `Fortuno.ApiTests/Tickets/TicketPurchaseFlowTests.cs` com teste `Status_BeforePayment_ShouldReturnPending` e `Status_UnknownInvoice_ShouldReturnUnknown` (idempotência pós-pagamento depende de pagamento manual no devmode do ProxyPay — deixar como teste opcional/integração futura)

### Implementation for User Story 2

- [X] T039 [US2] Implementar `Fortuno.Infra/AppServices/ProxyPayAppService.GetQRCodeStatusAsync`: GET relativo `"payment/qrcode/status/{invoiceId}"`, forwarda Authorization + adiciona X-Tenant-Id, log antes/depois; retorna `ProxyPayQRCodeStatusResponse?` (null em 404/5xx); deserialização case-insensitive
- [X] T040 [US2] Ampliar `Fortuno.Domain/Interfaces/ITicketService.cs` adicionando `Task<TicketQRCodeStatusInfo> CheckQRCodeStatusAsync(long invoiceId)` e método interno `Task<ProcessPaymentResult> ProcessPaymentAsync(TicketOrder intent)` (ou deixar `ProcessPaymentAsync` como privado no serviço)
- [X] T041 [US2] Implementar `Fortuno.Domain/Services/TicketService.CheckQRCodeStatusAsync` conforme `contracts/ticket-qrcode-status.md`: (a) chama `_proxyPay.GetQRCodeStatusAsync(invoiceId)` e mapeia string→`TicketOrderStatus`; (b) `_intentRepo.GetByInvoiceIdAsync(invoiceId)` — se null, retorna `{ Status: Unknown, InvoiceId }`; (c) switch por estado: Paid→`ProcessPaymentAsync`; Expired→`TryMarkExpiredAsync`; Cancelled→`TryMarkCancelledAsync`; Pending→devolve shape com brCode/brCodeBase64; Unknown→devolve shape mínimo; (d) monta response de acordo com resultado
- [X] T042 [US2] Implementar `Fortuno.Domain/Services/TicketService.ProcessPaymentAsync(intent)` conforme R-002: (a) early return se `intent.Status == Paid && tickets já existem` → reconciliação não necessária, retorna resultado completo; (b) valida Lottery `Open` → senão retorna `RefundManual` com motivo "Lottery fechada"; (c) em UserPicks: carrega reservas do invoiceId, valida que count==quantity e não expiradas → senão `RefundManual` com motivo "reservas"; em Random: chama `DrawRandomAvailableNumbersAsync` (migrado de `PurchaseService`) → se count<quantity, `RefundManual` com motivo "pool"; (d) `TryMarkPaidAsync` — se 0 rows: concorrência, outra thread assumiu → retorna estado atual; (e) BEGIN TX → inserir tickets em batch → inserir `InvoiceReferrer` se `ReferralCode` presente e não self → COMMIT; (f) retorna `{ Status: Paid, Tickets: [...] }`
- [X] T043 [US2] Migrar `DrawRandomAvailableNumbersAsync` de `Fortuno.Domain/Services/PurchaseService.cs` para `TicketService.cs` (cópia literal + filtro por `ExpiresAt > now` nas reservas — FR-024); garantir `Shuffle` também migrado como helper privado
- [X] T044 [US2] Adicionar endpoint `GET /tickets/qrcode/{invoiceId:long}/status` em `TicketsController`: `[Authorize]`, chama `_tickets.CheckQRCodeStatusAsync(invoiceId)`; tudo em try/catch devolvendo 500 no pior caso (status Unknown é 200)
- [X] T045 [P] [US2] Criar request Bruno `bruno/Tickets/qrcode-status.bru` usando `{{invoiceId}}` capturado no qrcode-create

**Checkpoint US2**: Polling funciona ponta-a-ponta; pagamento manual em devmode emite tickets idempotentemente; `fortuna_ticket_orders.status` transita 1→2; `fortuna_tickets` ganha linhas com `invoice_id`.

---

## Phase 5: User Story 3 — Remoção de Webhook e Preview (Priority: P2)

**Goal**: O código deixa de conter rotas de webhook de pagamento e endpoints/DTOs de preview de compra.

**Independent Test**: Grep no repositório não encontra `ProcessPaidWebhookAsync`, `ProxyPayWebhookHmacFilter`, `PurchasesController`, `PurchasePreviewRequest`, `PurchaseConfirmRequest`; `dotnet build` e testes continuam passando.

### Remoção (order matters — remover consumers antes dos contratos)

- [X] T046 [US3] Remover `Fortuno.API/Controllers/PurchasesController.cs`
- [X] T047 [US3] Remover `Fortuno.API/Controllers/WebhooksController.cs` (ou remover apenas a rota `/webhooks/proxypay/invoice-paid` se o controller tiver outras finalidades — conferir; pela estrutura atual, controller inteiro sai)
- [X] T048 [US3] Remover `Fortuno.API/Filters/ProxyPayWebhookHmacFilter.cs`
- [X] T049 [US3] Remover linha `builder.Services.AddScoped<ProxyPayWebhookHmacFilter>();` de `Fortuno.API/Program.cs`
- [X] T050 [US3] Remover `Fortuno.API/Validators/PurchasePreviewRequestValidator.cs` e `Fortuno.API/Validators/PurchaseConfirmRequestValidator.cs`
- [X] T051 [US3] Remover `Fortuno.Tests/Application/Validations/PurchasePreviewRequestValidatorTests.cs` e correspondente de Confirm (se existir)
- [X] T052 [US3] Remover `Fortuno.Domain/Services/PurchaseService.cs` e `Fortuno.Domain/Interfaces/IPurchaseService.cs`
- [X] T053 [US3] Remover `Fortuno.Tests/Domain/Services/PurchaseServiceTests.cs` e `Fortuno.Tests/Domain/Services/PurchaseServiceExtendedTests.cs`
- [X] T054 [US3] Remover registro `services.AddScoped<IPurchaseService, PurchaseService>();` de `Fortuno.Application/Startup.cs`
- [X] T055 [US3] Remover DTOs de preview/confirm de `Fortuno.DTO/Purchase/`: `PurchasePreviewRequest.cs`, `PurchasePreviewInfo.cs`, `PurchaseConfirmRequest.cs`, `PurchaseConfirmResponse.cs`; remover pasta `Fortuno.DTO/Purchase/` se ficar vazia
- [X] T056 [US3] Remover de `Fortuno.Infra.Interfaces/AppServices/IProxyPayAppService.cs` as referências antigas (`CreateInvoiceAsync`, `GetInvoiceAsync`) — já feito no T017; confirmar que não sobra nada
- [X] T057 [US3] Remover `Fortuno.DTO/ProxyPay/ProxyPayCreateInvoiceRequest.cs` e `Fortuno.DTO/ProxyPay/ProxyPayInvoiceInfo.cs` (não há mais consumer)
- [X] T058 [US3] Remover `Fortuno.Infra/AppServices/ProxyPayAppService.CreateInvoiceAsync` e `GetInvoiceAsync` (corpo dos métodos) de `Fortuno.Infra/AppServices/ProxyPayAppService.cs`
- [X] T059 [US3] Remover Bruno requests obsoletos: `bruno/Purchases/preview.bru`, `bruno/Purchases/confirm.bru`, pasta `bruno/Purchases/`, `bruno/Webhooks/proxypay-invoice-paid.bru`, pasta `bruno/Webhooks/`

**Checkpoint US3**: `dotnet build Fortuno.sln` 0 erros; `grep -r "PurchaseService\|ProcessPaidWebhookAsync\|ProxyPayWebhookHmacFilter" Fortuno.*` vazio (exceto specs antigas); suíte de testes verde.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: docs + quickstart + cleanup final.

- [ ] T060 [P] Atualizar `specs/001-lottery-saas/quickstart.md` e `specs/001-lottery-saas/contracts/rest-openapi.yaml` removendo referências a `/purchases/*` e `/webhooks/proxypay` **OU** marcar como deprecated (referência histórica — decisão por escopo)
- [ ] T061 [P] Atualizar `docs/EXTERNAL_DEPS_INSTRUCTIONS.md` substituindo referências a `POST /purchases/confirm` no fluxo de webhook por descrição do fluxo de polling QR Code
- [ ] T062 Executar `quickstart.md` desta feature (`specs/003-ticket-qrcode-purchase/quickstart.md`) contra ambiente local e marcar o checklist — registrar evidências no PR
- [ ] T063 [P] Rodar `dotnet format Fortuno.sln` para normalizar imports/espaçamento e confirmar 0 warnings
- [ ] T064 Atualizar `CHANGELOG.md` (se existir) ou comentar no PR o diff resumido: "POST /tickets/qrcode + GET /tickets/qrcode/{id}/status substituem /purchases/preview + /purchases/confirm + webhook ProxyPay"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: sem dependência; roda antes de tudo
- **Phase 2 (Foundational)**: depende de Phase 1; **bloqueia** US1, US2, US3
- **Phase 3 (US1)**: depende de Phase 2
- **Phase 4 (US2)**: depende de Phase 2 **e** de parte da US1 (precisa do `TicketOrder` criado pelo CreateQRCodeAsync para testar ponta-a-ponta; tecnicamente os métodos de `ITicketService` podem ser implementados em paralelo, mas os testes de integração de US2 exigem US1)
- **Phase 5 (US3)**: depende **exclusivamente** da Phase 2 e da US1 (US3 remove os caminhos substituídos; pode ser feita em paralelo com US2 se o dev tiver certeza de que US2 não vai reintroduzir uma dependência)
- **Phase 6 (Polish)**: depende de US1 + US2 + US3

### Within US1

- T022–T025 (tests) podem rodar em paralelo entre si (arquivos distintos)
- T026 (validator) paralelo com testes
- T027 (ProxyPayAppService.GetStoreAsync update) é pré-requisito de T029 (LotteryService)
- T028 (CreateQRCodeAsync no ProxyPayAppService) é pré-requisito de T031 (TicketService.CreateQRCodeAsync)
- T030 (interface) antes de T031 (implementação)
- T031 antes de T032 (controller endpoint)
- T034 (Bruno) pode rodar a qualquer momento após T032

### Within US2

- T035, T036, T037 (tests) paralelos entre si
- T039 (ProxyPayAppService.GetQRCodeStatusAsync) pré-requisito de T041 (TicketService.CheckQRCodeStatusAsync)
- T040 (interface) antes de T041/T042
- T043 (migração Draw*/Shuffle) pode ser feito em paralelo com T041 — arquivos diferentes, mas Migration antes de chamar
- T044 (controller endpoint) depende de T041
- T045 (Bruno) após T044

### Parallel Opportunities

- Phase 2: T003–T015 todos `[P]` (arquivos DTO/Model/Enum diferentes) — pode-se acelerar foundation com várias instâncias de skill `dotnet-architecture`
- Phase 3–4: tests `[P]` entre si; validator `[P]`; Bruno `[P]`
- Phase 5: removals `[P]` entre arquivos distintos; **cuidado** com Program.cs (T049) + Startup.cs (T054) — mesmo arquivo? não, arquivos diferentes, então pode `[P]`

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all DTOs in parallel via dotnet-architecture:
Task T003 [P]: TicketOrderStatus.cs
Task T004 [P]: TicketOrderRequest.cs
Task T005 [P]: TicketQRCodeInfo.cs
Task T006 [P]: TicketQRCodeStatusInfo.cs
Task T007 [P]: ProxyPayCustomer.cs
Task T008 [P]: ProxyPayItem.cs
Task T009 [P]: ProxyPayQRCodeRequest.cs
Task T010 [P]: ProxyPayQRCodeResponse.cs
Task T011 [P]: ProxyPayQRCodeStatusResponse.cs
Task T012 [P]: ProxyPayStoreInfo.cs (add ClientId)
Task T013 [P]: TicketOrderStatus.cs (enum)
Task T014 [P]: TicketOrder.cs (model)
Task T015 [P]: Lottery.cs (add StoreClientId)
```

T016–T018 sequenciais (Infra.Interfaces depende dos Models; Context depende das Interfaces).

## Parallel Example: US1 Tests

```bash
Task T022 [P] [US1]: TicketOrderRequestValidatorTests.cs
Task T023 [P] [US1]: ProxyPayAppServiceTests.CreateQRCodeAsync
Task T024 [P] [US1]: TicketServiceTests.CreateQRCodeAsync_*
```

---

## Implementation Strategy

### MVP First (US1 — P1)

1. Phase 1 (Setup) — 2 tasks triviais
2. Phase 2 (Foundational) — 19 tasks; executar DTOs em paralelo, migration por último
3. Phase 3 (US1) — `POST /tickets/qrcode` funcional
4. **STOP + VALIDATE**: criar QR Code via Bruno e verificar `fortuna_ticket_orders`

Neste ponto já temos entrega parcial: o front pode criar cobrança; a **confirmação** fica para US2.

### Incremental Delivery

1. Phase 1+2 → foundation pronta
2. US1 → pode demonstrar criação de QR Code (sem confirmação automática)
3. US2 → polling funcional → MVP completo do fluxo de compra
4. US3 → cleanup (pode ir no mesmo PR da US2 ou em PR separado de refactor)
5. Phase 6 → docs + validação

### Parallel Team Strategy

- Dev A: Phase 2 inteira (foundation)
- Após Phase 2:
  - Dev A: US1 (tests + implementação)
  - Dev B: US2 (tests + implementação — usa mocks no TicketService enquanto US1 não existe)
  - Dev C: US3 (remoção) em paralelo — não depende de US1/US2 funcionalmente
- Integração final antes do merge

---

## Notes

- `[P]` tasks = arquivos diferentes, sem dependência bloqueante
- `[Story]` mapeia cada task à User Story para rastreabilidade
- Cada user story é completável e testável independentemente (exceto US2 dependendo de US1 para E2E)
- Testes unitários devem falhar antes da implementação (TDD-friendly)
- Commit após cada task ou grupo lógico coerente
- **Nunca** reintroduzir `ProxyPayWebhookHmacFilter` ou `PurchaseService` após a Phase 5
- Se alguma decisão técnica de `research.md` for invalidada empiricamente (ex.: status ProxyPay devolver vocabulário diferente), corrigir apenas o mapeamento em `ProxyPayAppService.GetQRCodeStatusAsync` — não refazer a arquitetura
