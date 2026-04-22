# Implementation Plan: Compra de Ticket via QR Code PIX (sem webhook, sem preview)

**Branch**: `003-ticket-qrcode-purchase` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-ticket-qrcode-purchase/spec.md`

## Summary

Refatoração do fluxo de compra de tickets: (a) rotas saem do `PurchasesController` e entram no `TicketController`; (b) remoção do endpoint de preview (migra para o frontend); (c) integração do ProxyPay migra do par "criar invoice + webhook" para o par "`POST /payment/qrcode` + polling em `GET /payment/qrcode/status/{invoiceId}`"; (d) `TicketService.ProcessPayment(invoiceId)` passa a ser o único ponto de emissão de tickets, disparado pelo polling; (e) remoção do `WebhooksController` de pagamento e de `PurchaseService.ProcessPaidWebhookAsync`. Introdução de uma nova entidade `TicketOrder` para carregar o contexto da compra (userId, mode, pickedNumbers, referralCode, referralPercentAtPurchase) entre a criação do QR Code e a confirmação do pagamento — substituindo a metadata que hoje viaja pelo webhook.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (Constituição Fortuno §II)
**Primary Dependencies**: ASP.NET Core 8 (MVC Controllers), Entity Framework Core 9.x + Npgsql, NAuth ACL (`IUserClient`), zTools (não alterado nesta feature), FluentValidation, Swashbuckle, HotChocolate (não alterado), Flurl.Http + xUnit + FluentAssertions (ApiTests)
**Storage**: PostgreSQL (tabela nova `fortuna_ticket_orders`; tabelas existentes `fortuna_number_reservations`, `fortuna_tickets`, `fortuna_invoice_referrers` permanecem, webhook_events **DEPRECIA**)
**Testing**: xUnit (`Fortuno.Tests` unit + `Fortuno.ApiTests` integração HTTP) — coverlet.runsettings já configurado
**Target Platform**: Linux container (Docker), ambiente `Development`/`Docker`/`Production` (ASPNETCORE_ENVIRONMENT)
**Project Type**: Web service (Clean Architecture — camadas DTO / Domain / Infra.Interfaces / Infra / Application / API / GraphQL)
**Performance Goals**: QR Code devolvido ao comprador em < 3s (SC-001); tickets visíveis ≤ 2s após primeira consulta pós-pagamento (SC-002); idempotência 100% sob polling concorrente (SC-003)
**Constraints**: Sem webhook (FR-017); sem preview HTTP (FR-018); sem job em background (FR-024 — liberação de reservas é lazy); header `X-Tenant-Id` em toda chamada ao ProxyPay (FR-006); `customer` vem do NAuth (FR-010)
**Scale/Scope**: ~10 endpoints alterados/removidos/criados; 1 tabela nova + 1 tabela depreciada; alterações em `PurchaseService` (→ `TicketService.ProcessPayment` + `TicketService.CreateQRCodeAsync` + `TicketService.CheckQRCodeStatusAsync`), `ProxyPayAppService` (2 métodos novos), `IProxyPayAppService` (contrato atualizado). Testes impactados: `PurchaseServiceTests`, `ProxyPayAppServiceTests`, `TicketServiceTests` + nova `TicketPurchaseFlowTests` em `Fortuno.ApiTests`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verificação item a item contra a Constituição Fortuno v1.0.0:

### I. Skills Obrigatórias (NÃO NEGOCIÁVEL)

- [x] `dotnet-architecture` será invocada para a nova entidade `TicketOrder` (DTO, Domain Model, Infra.Interfaces repository, Infra repository, DI registration) e para a migration correspondente. `dotnet-fluent-validation` será invocada para o validator do novo `TicketOrderRequest`.
- [x] Nenhuma reimplementação manual de padrões cobertos pela skill — `IRepository<T>`, DI centralizado em `Fortuno.Application/Startup.cs`, DTOs em `Fortuno.DTO` com `[JsonPropertyName("camelCase")]`.

### II. Stack Tecnológica Fixa

- [x] Sem novos ORMs, sem novas dependências de runtime — apenas EF Core + Npgsql (já em uso).
- [x] Sem execução de `docker` / `docker compose` no ambiente local — build/test via `dotnet build` e `dotnet test`; migrações via `dotnet ef migrations add` / `dotnet ef database update`.

### III. Convenções de Código .NET

- [x] Nomes PascalCase para classes, interfaces, métodos, propriedades; `_camelCase` para campos privados.
- [x] DTOs em `Fortuno.DTO/Ticket/` e `Fortuno.DTO/Payment/` com `[JsonPropertyName("camelCase")]` em todas as propriedades; cada DTO em arquivo próprio (conforme refactor recente de separação de classes).

### IV. Convenções de Banco de Dados (PostgreSQL)

- [x] Nova tabela `fortuna_ticket_orders` (snake_case, plural).
- [x] PK `ticket_order_id bigint`, constraint `fortuna_ticket_orders_pkey`.
- [x] FKs com `ClientSetNull`: `fk_ticket_order_lottery` (→ `fortuna_lotteries`), `fk_purchase_intent_user` (lógica; `user_id` é do NAuth, sem FK física).
- [x] Timestamps `timestamp without time zone` (com `Npgsql.EnableLegacyTimestampBehavior` já habilitado em `Program.cs`).
- [x] Strings com `MaxLength` explícito (`referral_code varchar(8)`, `picked_numbers varchar(4000)` — JSON serializado).
- [x] Status em `integer` (`status int NOT NULL DEFAULT 1` — Pending).

### V. Autenticação e Segurança (NÃO NEGOCIÁVEL)

- [x] Endpoints `POST /tickets/qrcode` e `GET /tickets/qrcode/{invoiceId}/status` ficam com `[Authorize]` em `TicketController`.
- [x] Header `Authorization: Basic {token}` via `NAuthHandler` — inalterado; `CheckQRCodeStatus` apenas exige autenticação válida (FR-004 explicita ausência de checagem de posse).
- [x] CORS `AllowAnyOrigin` apenas em `Development` — inalterado.
- [x] Chamadas ao ProxyPay continuam usando `X-Tenant-Id`; `POST /payment/qrcode` recebe `clientId` via `ProxyPaySettings` (não exposto em logs).

### Checklist do Contribuidor

- [x] Skill `dotnet-architecture` para `TicketOrder`.
- [x] snake_case em `fortuna_ticket_orders` + colunas.
- [x] `[Authorize]` em `TicketController` (endpoints novos).
- [x] `[JsonPropertyName("camelCase")]` nos novos DTOs.
- [x] Sem ORMs alternativos.
- [x] Sem `docker` local.

**Resultado do Gate**: PASS. Nenhuma violação. Seção **Complexity Tracking** fica vazia.

## Project Structure

### Documentation (this feature)

```text
specs/003-ticket-qrcode-purchase/
├── plan.md              # Este arquivo
├── research.md          # Phase 0 (decisões técnicas: vocabulário ProxyPay, idempotência, etc.)
├── data-model.md        # Phase 1 (TicketOrder + mudanças em entidades existentes)
├── quickstart.md        # Phase 1 (fluxo end-to-end: como testar manualmente)
├── contracts/           # Phase 1 (contrato dos 2 endpoints internos + 2 endpoints ProxyPay consumidos)
│   ├── ticket-purchase.md
│   ├── ticket-qrcode-status.md
│   └── proxypay-payment-qrcode.md
└── tasks.md             # Phase 2 (gerado por /speckit.tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
Fortuno.DTO/
├── Ticket/
│   ├── TicketOrderRequest.cs          # NOVO — body do POST /tickets/qrcode
│   ├── TicketQRCodeInfo.cs                # NOVO — response do POST /tickets/qrcode (brCode, brCodeBase64, expiredAt, invoiceId, invoiceNumber)
│   ├── TicketQRCodeStatusInfo.cs          # NOVO — response do GET /tickets/qrcode/{invoiceId}/status (status + opcionalmente brCode/tickets/refundHint)
│   ├── TicketOrderStatus.cs            # NOVO — enum { Pending, Paid, Expired, Cancelled, Unknown }
│   ├── TicketInfo.cs                      # existente, sem alteração
│   └── TicketSearchQuery.cs               # existente, sem alteração
├── ProxyPay/                              # existente — adicionar DTOs novos abaixo
│   ├── ProxyPayQRCodeRequest.cs           # NOVO — body do POST /payment/qrcode (clientId, customer, items)
│   ├── ProxyPayQRCodeResponse.cs          # NOVO — response
│   ├── ProxyPayQRCodeStatusResponse.cs    # NOVO — response do GET /payment/qrcode/status/{invoiceId}
│   ├── ProxyPayCustomer.cs                # NOVO
│   ├── ProxyPayItem.cs                    # NOVO
│   ├── ProxyPayStoreInfo.cs               # existente (sem alteração)
│   ├── ProxyPayInvoiceInfo.cs             # DEPRECIADO nesta feature (manter para compat de outros usos; remover se não restar consumidor)
│   └── ProxyPayCreateInvoiceRequest.cs    # DEPRECIADO (mesmo tratamento)
└── Purchase/                              # 4 arquivos REMOVIDOS nesta feature
    ├── ~~PurchasePreviewRequest.cs~~
    ├── ~~PurchasePreviewInfo.cs~~
    ├── ~~PurchaseConfirmRequest.cs~~
    └── ~~PurchaseConfirmResponse.cs~~

Fortuno.Domain/
├── Models/
│   └── TicketOrder.cs                  # NOVO — entidade
├── Interfaces/
│   ├── ITicketService.cs                  # amplia: CreateQRCodeAsync, CheckQRCodeStatusAsync, ProcessPaymentAsync
│   └── IPurchaseService.cs                # REMOVIDO (toda responsabilidade migra para TicketService)
├── Services/
│   ├── TicketService.cs                   # EXPANDE com os 3 métodos acima + DrawRandomAvailableNumbersAsync (migrado)
│   └── PurchaseService.cs                 # REMOVIDO
└── Enums/
    └── TicketOrderStatus.cs            # NOVO — { Pending=1, Paid=2, Expired=3, Cancelled=4 }

Fortuno.Infra.Interfaces/
├── Repository/
│   └── ITicketOrderRepository.cs       # NOVO
└── AppServices/
    └── IProxyPayAppService.cs             # ATUALIZA: métodos novos CreateQRCodeAsync, GetQRCodeStatusAsync; métodos antigos CreateInvoiceAsync/GetInvoiceAsync removidos

Fortuno.Infra/
├── Repository/
│   └── TicketOrderRepository.cs        # NOVO
├── Context/
│   └── FortunoContext.cs                  # ADICIONA DbSet<TicketOrder> + Fluent API config
├── Migrations/
│   └── {timestamp}_AddTicketOrders.cs  # NOVO (gerado via dotnet ef)
└── AppServices/
    └── ProxyPayAppService.cs              # REESCREVE 2 métodos (POST /payment/qrcode e GET /payment/qrcode/status/{invoiceId})

Fortuno.Application/
└── Startup.cs                             # registra ITicketOrderRepository → TicketOrderRepository; remove IPurchaseService; mantém IProxyPayAppService; remove WebhookEventRepo se nenhum outro caminho consumir

Fortuno.API/
├── Controllers/
│   ├── TicketsController.cs               # EXPANDE com POST /tickets/qrcode e GET /tickets/qrcode/{invoiceId}/status
│   ├── ~~PurchasesController.cs~~          # REMOVIDO
│   └── WebhooksController.cs              # REMOVIDO (ou despe a rota de pagamento; remove ProxyPayWebhookHmacFilter)
├── Filters/
│   └── ~~ProxyPayWebhookHmacFilter.cs~~    # REMOVIDO
├── Validators/
│   ├── TicketOrderRequestValidator.cs  # NOVO (via skill dotnet-fluent-validation)
│   ├── ~~PurchasePreviewRequestValidator.cs~~  # REMOVIDO
│   └── ~~PurchaseConfirmRequestValidator.cs~~  # REMOVIDO
└── Program.cs                             # remove AddScoped<ProxyPayWebhookHmacFilter>; mantém tudo mais

Fortuno.Tests/
├── Domain/Services/
│   ├── TicketServiceTests.cs              # EXPANDE: CreateQRCodeAsync_* (6 cenários), CheckQRCodeStatusAsync_* (8), ProcessPaymentAsync_* (10 — idempotência, modos, edge cases)
│   ├── ~~PurchaseServiceTests.cs~~         # REMOVIDO
│   └── ~~PurchaseServiceExtendedTests.cs~~ # REMOVIDO
├── Infra/AppServices/
│   └── ProxyPayAppServiceTests.cs         # REESCREVE GetStore e adiciona CreateQRCode/GetQRCodeStatus
└── Application/Validations/
    ├── TicketOrderRequestValidatorTests.cs  # NOVO
    └── ~~PurchasePreviewRequestValidatorTests.cs~~  # REMOVIDO (similar para ConfirmRequestValidatorTests)

Fortuno.ApiTests/
└── Tickets/
    └── TicketPurchaseFlowTests.cs         # NOVO — end-to-end contra API rodando: cria Lottery em Open, POST /tickets/qrcode (Random e UserPicks), GET status inicial (Pending), mock/injection de estado pago via flag de teste OU teste de polling idempotente após pagamento manual

bruno/
├── Tickets/
│   ├── qrcode-create.bru                  # NOVO
│   └── qrcode-status.bru                  # NOVO
├── ~~Purchases/~~                         # REMOVIDO (preview.bru e confirm.bru)
└── ~~Webhooks/proxypay-invoice-paid.bru~~ # REMOVIDO
```

**Structure Decision**: Manter a Clean Architecture existente (7 projetos + API + Tests + ApiTests). A feature consiste majoritariamente de **refactor** (mover responsabilidades de `PurchaseService`→`TicketService`, eliminar `WebhooksController`/filter) + **expansão** de 2 camadas (`TicketOrder` novo end-to-end). Nenhum projeto novo é criado.

## Complexity Tracking

> Preencher SOMENTE se houver violações da Constituição para justificar.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(vazio)_ | _(nenhuma violação)_ | _(nenhuma)_ |

---

## Phase 0 → Phase 1 — Execução

Artefatos gerados em ordem:

1. **`research.md`** — resolve pontos técnicos deferidos da sessão de clarify:
   - Vocabulário exato de status do ProxyPay em `GET /payment/qrcode/status/{invoiceId}` (mapeamento → enum interno)
   - Estratégia de idempotência de `ProcessPaymentAsync` sob concorrência (índice único em `purchase_intents.invoice_id` + update condicional por status)
   - Estratégia de mapeamento `items[]` → payload ProxyPay (linha única com `quantity`/`unitPrice`/`discount` ou N linhas)
   - Tratamento do `clientId` por tenant/Store (via `ProxyPaySettings`)
2. **`data-model.md`** — `TicketOrder` (colunas, constraints, índices, máquina de estado, relacionamentos com `NumberReservation` / `Ticket` / `InvoiceReferrer`)
3. **`contracts/ticket-purchase.md`** — `POST /tickets/qrcode` (request body, response, códigos HTTP)
4. **`contracts/ticket-qrcode-status.md`** — `GET /tickets/qrcode/{invoiceId}/status` (request, response por estado, idempotência)
5. **`contracts/proxypay-payment-qrcode.md`** — `POST /payment/qrcode` e `GET /payment/qrcode/status/{invoiceId}` (shapes reais que o `ProxyPayAppService` consome)
6. **`quickstart.md`** — passo a passo para validar manualmente o fluxo em ambiente local
7. **Agent context update** — roda `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude` ao final para persistir a tech adicionada ao `CLAUDE.md`.

## Re-evaluation (após Phase 1)

A re-avaliação do Constitution Check após o design (Phase 1) será realizada ao final dos artefatos gerados. Expectativa: PASS mantido — nenhuma decisão de design introduz tech nova, endpoint desprotegido, ou tabela fora do snake_case.
