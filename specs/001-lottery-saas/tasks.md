---

description: "Task list for 001-lottery-saas — Plataforma SaaS de Sorteios Online (Fortuno)"
---

# Tasks: Plataforma SaaS de Sorteios Online (Fortuno)

**Input**: Design documents from `/specs/001-lottery-saas/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Testes unitários/integração NÃO foram requisitados explicitamente na
spec. A fase 10 inclui tarefas **opcionais** de testes via skill `dotnet-test`,
concentrando-se em lógica de alto risco (composição de número, concorrência de
pool, idempotência de webhook).

**Organization**: Tarefas agrupadas por User Story (US1..US6) conforme a spec.
Convenção de caminhos segue a estrutura de raiz do repo em `plan.md`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos distintos, sem dependência pendente).
- **[Story]**: US1..US6 quando aplicável.
- Caminhos absolutos partem da raiz do repo `C:\repos\Fortuno\Fortuno\`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Criar a solução .NET 8, os 8 projetos com dependências corretas
segundo a skill `dotnet-architecture`, e instalar pacotes NuGet base.

- [X] T001 Criar `Fortuno.sln` na raiz do repo via `dotnet new sln -n Fortuno`.
- [X] T002 [P] Criar projeto classlib `Fortuno.DTO/Fortuno.DTO.csproj` (.NET 8).
- [X] T003 [P] Criar projeto classlib `Fortuno.Infra.Interfaces/Fortuno.Infra.Interfaces.csproj` (.NET 8).
- [X] T004 [P] Criar projeto classlib `Fortuno.Domain/Fortuno.Domain.csproj` (.NET 8).
- [X] T005 [P] Criar projeto classlib `Fortuno.Infra/Fortuno.Infra.csproj` (.NET 8).
- [X] T006 [P] Criar projeto classlib `Fortuno.Application/Fortuno.Application.csproj` (.NET 8).
- [X] T007 [P] Criar projeto classlib `Fortuno.GraphQL/Fortuno.GraphQL.csproj` (.NET 8).
- [X] T008 Criar projeto webapi `Fortuno.API/Fortuno.API.csproj` (.NET 8).
- [X] T009 Adicionar todos os projetos à solução (`dotnet sln add src/**/*.csproj`).
- [X] T010 Configurar referências entre projetos conforme `plan.md` (DTO ← Infra.Interfaces ← Domain ← Infra ← Application ← GraphQL ← API; Domain também referencia DTO; Infra referencia Domain + Infra.Interfaces).
- [X] T011 [P] Instalar NuGets no `Fortuno.Infra`: `Microsoft.EntityFrameworkCore` 9.x, `Npgsql.EntityFrameworkCore.PostgreSQL` 9.x, `Microsoft.EntityFrameworkCore.Design` 9.x.
- [X] T012 [P] Instalar NuGets no `Fortuno.API`: `Swashbuckle.AspNetCore` 8.x, `FluentValidation.AspNetCore` (última versão), `NAuth` (última versão), `zTools` (última versão).
- [X] T013 [P] Instalar NuGets no `Fortuno.GraphQL`: `HotChocolate.AspNetCore` (última versão), `HotChocolate.Data.EntityFramework`.
- [X] T014 [P] Instalar NuGet `Markdig` no `Fortuno.Infra` (conversão Markdown → HTML para PDF via zTools).
- [X] T015 [P] Criar arquivos `.editorconfig` e `Directory.Build.props` na raiz com convenções da constituição (file-scoped namespaces, nullable enabled, warnings as errors opcional).
- [X] T016 Configurar `appsettings.json`, `appsettings.Development.json`, `appsettings.Docker.json`, `appsettings.Production.json` em `Fortuno.API/` via skill `dotnet-env` (3 ambientes).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura compartilhada que **todas** as US dependem:
DbContext, migration inicial, integrações externas, autenticação, webhook
idempotency, enums, erros, DI.

**⚠️ CRITICAL**: Nenhuma US pode começar antes deste bloco concluir.

- [X] T017 [P] Criar enums em `Fortuno.Domain/Enums/` (`LotteryStatus`, `RaffleStatus`, `TicketRefundState`, `NumberType`, `TicketOrderMode`) conforme `data-model.md`.
- [X] T018 [P] Criar classes de settings em `Fortuno.DTO/Settings/` (`FortunoSettings`, `NAuthSettings`, `ProxyPaySettings`, `ZToolsSettings`).
- [X] T019 [P] Criar DTO base de resposta padronizada `Fortuno.DTO/Common/ApiResponse.cs` com chaves `sucesso`, `mensagem`, `erros` (constituição).
- [X] T020 Criar `Fortuno.Infra/Context/FortunoContext.cs` com `DbSet` vazios e `OnModelCreating` vazio (esqueleto); string de conexão injetada via `IOptions<FortunoSettings>`.
- [X] T021 Criar todos os Models em `Fortuno.Domain/Models/` (Lottery, LotteryImage, LotteryCombo, Ticket, Raffle, RaffleAward, RaffleWinner, UserReferrer, InvoiceReferrer, RefundLog, NumberReservation, WebhookEvent) conforme `data-model.md`. Usar tipos primitivos; sem atributos EF.
- [X] T022 [P] Criar interfaces de repositório genéricas em `Fortuno.Infra.Interfaces/Repository/` (`ILotteryRepository<TModel>`, `ITicketRepository<TModel>`, `IRaffleRepository<TModel>`, `IRaffleAwardRepository<TModel>`, `IRaffleWinnerRepository<TModel>`, `ILotteryImageRepository<TModel>`, `ILotteryComboRepository<TModel>`, `IUserReferrerRepository<TModel>`, `IInvoiceReferrerRepository<TModel>`, `IRefundLogRepository<TModel>`, `INumberReservationRepository<TModel>`, `IWebhookEventRepository<TModel>`).
- [X] T023 [P] Criar interfaces de AppService em `Fortuno.Infra.Interfaces/AppServices/` (`INAuthAppService`, `IProxyPayAppService`, `IZToolsAppService`).
- [X] T024 Configurar Fluent API para todas as entidades em `FortunoContext.OnModelCreating` — respeitando prefixo `fortuna_`, snake_case, `bigint identity`, `ClientSetNull`, `timestamp without time zone`, `varchar(MaxLength)`, UNIQUE constraints e índices listados em `data-model.md`.
- [X] T025 Implementar todos os repositórios em `Fortuno.Infra/Repository/` (um por interface), derivando de um padrão genérico com `AsNoTracking` em leituras.
- [X] T026 Implementar `Fortuno.Infra/AppServices/NAuthAppService.cs` encapsulando `IUserClient` do NAuth (tenant `fortuna`); expor `GetByIdAsync`, `GetByIdsAsync`, `GetCurrentAsync`.
- [X] T027 Implementar `Fortuno.Infra/AppServices/ProxyPayAppService.cs` com `HttpClient` tipado, Basic auth, header tenant `fortuna`; métodos `GetStoreAsync(storeId)`, `CreateInvoiceAsync(...)`, `GetInvoiceAsync(id)` conforme `research.md §3`.
- [X] T028 Implementar `Fortuno.Infra/AppServices/ZToolsAppService.cs` via skill `ztools-guide`, expondo `UploadImageAsync`, `UploadFileAsync`, `GeneratePdfFromMarkdownAsync`, `GenerateSlug`.
- [X] T029 Implementar `Fortuno.Application/Startup.cs` (classe estática) centralizando DI: `AddDbContext<FortunoContext>`, todos os repositórios `Scoped`, AppServices (`AddHttpClient` para ProxyPay; `AddNAuth`; `AddZTools`), e preparando slots para Services de Domain.
- [X] T030 Configurar `Fortuno.API/Program.cs`: `ConfigureServices` via `Startup`, NAuth `AddAuthentication().AddNAuth()`, Swashbuckle, CORS (`AllowAnyOrigin` apenas em Development), exception middleware (try/catch 500 padronizado via constituição).
- [X] T031 Criar primeira migration `dotnet ef migrations add InitialSchema --project Fortuno.Infra --startup-project Fortuno.API` gerando todas as tabelas `fortuna_*`. Revisar SQL gerado.
- [X] T032 [P] Criar `Fortuno.Domain/Services/NumberCompositionService.cs` implementando `INumberCompositionService`: compose/decompose int64↔componentes e `CountPossibilities(NumberType, min, max)`. Base para FR-010..013.
- [X] T033 [P] Criar `Fortuno.Domain/Services/SlugService.cs` implementando geração de slug via zTools + verificação de unicidade global no repositório (FR-005/006).
- [X] T034 Middleware de autorização: helper `IStoreOwnershipGuard` que, dado `storeId` + `currentUserId`, valida ownership via `ProxyPayAppService.GetStoreAsync`. Usado em FR-045a.
- [X] T035 Middleware de webhook: filtro `ProxyPayWebhookHmacFilter` em `Fortuno.API/Filters/` que valida `X-ProxyPay-Signature` antes de chegar ao controller (FR-029b).
- [X] T036 Registrar DI de todos os Domain Services no `Startup.cs` (a serem adicionados conforme cada US; este task prepara o slot).

**Checkpoint**: Foundation pronta — US podem começar em paralelo.

---

## Phase 3: User Story 1 - Criador publica primeira loteria (P1) 🎯 MVP

**Goal**: Empreendedor cadastrado consegue criar Store no ProxyPay, criar uma
Lottery em Draft com imagem, raffle e prêmio, e publicar (Draft → Open).

**Independent Test**: criar conta nova, completar cadastro, criar Store,
`POST /lotteries` → `POST /lottery-images` → `POST /raffles` →
`POST /raffle-awards` → `POST /lotteries/{id}/publish` e verificar
status `Open`.

### Implementation for User Story 1

- [X] T037 [P] [US1] Criar DTOs de Lottery em `Fortuno.DTO/Lottery/` (`LotteryInsertInfo`, `LotteryUpdateInfo`, `LotteryInfo`, `LotteryCancelRequest`) com `[JsonPropertyName("camelCase")]` em todas as propriedades.
- [X] T038 [P] [US1] Criar DTOs de LotteryImage em `Fortuno.DTO/LotteryImage/` (`LotteryImageInsertInfo`, `LotteryImageInfo`).
- [X] T039 [P] [US1] Criar DTOs de Raffle/RaffleAward em `Fortuno.DTO/Raffle/` e `Fortuno.DTO/RaffleAward/`.
- [X] T040 [US1] Criar `ILotteryService` em `Fortuno.Domain/Interfaces/` com `CreateAsync`, `UpdateAsync`, `GetByIdAsync`, `GetBySlugAsync`, `ListByStoreAsync`, `PublishAsync`, `CloseAsync`, `CancelAsync`, `CalculatePossibilitiesAsync`.
- [X] T041 [US1] Implementar `LotteryService` em `Fortuno.Domain/Services/LotteryService.cs`: geração de slug via `SlugService`, validações de status, cancelamento com motivo ≥20 chars (FR-033a1), integração com `IStoreOwnershipGuard` (FR-045a).
- [X] T042 [US1] Implementar validador FluentValidation `LotteryInsertInfoValidator` em `Fortuno.API/Validators/` via skill `dotnet-fluent-validation` (campos da tabela `data-model.md §fortuna_lotteries`).
- [X] T043 [US1] Implementar validador `LotteryCancelRequestValidator` exigindo `reason` com length ≥ 20.
- [X] T044 [P] [US1] Criar `ILotteryImageService` + `LotteryImageService` em Domain, com bloqueio de CRUD fora de `Draft` (FR-018).
- [X] T045 [P] [US1] Criar `IRaffleService` (somente Create/List/Get por enquanto; Cancel e Close ficam em US4/US3) + implementação em `Fortuno.Domain/Services/RaffleService.cs`.
- [X] T046 [P] [US1] Criar `IRaffleAwardService` + implementação básica (create/list) em Domain.
- [X] T047 [US1] Implementar `Fortuno.API/Controllers/LotteriesController.cs` com rotas: `POST /lotteries`, `PUT /lotteries/{id}`, `POST /lotteries/{id}/publish`, `POST /lotteries/{id}/close`, `POST /lotteries/{id}/cancel`. Todos com `[Authorize]` e checagem de ownership.
- [X] T048 [US1] Implementar `GET /lotteries/{id}/rules.pdf` e `GET /lotteries/{id}/privacy-policy.pdf` em `LotteriesController` gerando PDF via `IZToolsAppService.GeneratePdfFromMarkdownAsync` a partir do Markdown persistido.
- [X] T049 [US1] Implementar `Fortuno.API/Controllers/LotteryImagesController.cs` com `POST /lottery-images` (upload via zTools) e `DELETE /lottery-images/{id}` (bloqueado fora de Draft).
- [X] T050 [US1] Implementar `Fortuno.API/Controllers/RafflesController.cs` com `POST /raffles` (criar em Lottery Draft).
- [X] T051 [US1] Implementar `Fortuno.API/Controllers/RaffleAwardsController.cs` com `POST /raffle-awards` (criar em Lottery Draft).
- [X] T052 [US1] Implementar `LotteryService.PublishAsync`: validar existência de ≥1 LotteryImage, ≥1 Raffle, ≥1 RaffleAward; para tipos compostos validar `ticket_num_end > ticket_num_ini`; transicionar para `Open` (FR-016).
- [X] T053 [US1] Implementar `LotteryService.CalculatePossibilitiesAsync` consumindo `INumberCompositionService` (FR-013); expor via rota `GET /lotteries/{id}/possibilities`.
- [X] T054 [US1] Registrar todos os novos services de US1 no `Application/Startup.cs`.
- [X] T055 [US1] Adicionar ao Program.cs o middleware que auto-cria `UserReferrer` no primeiro acesso autenticado (FR-R02) — pré-requisito para painéis aparecerem vazios em vez de 404.

**Checkpoint**: US1 funcional — criador publica Lottery `Open` (MVP entregável).

---

## Phase 4: User Story 2 - Comprador adquire tickets via PIX (P1)

**Goal**: Visitante/cadastrado compra N tickets (Random ou UserPicks),
opcionalmente com código de indicação, gera Invoice no ProxyPay, paga PIX,
e recebe tickets imediatamente após webhook.

**Independent Test**: dada uma Lottery `Open`, fluxo completo
`preview → confirm → simular webhook` e verificar tickets em
`GET /tickets/mine`.

### Implementation for User Story 2

- [X] T056 [P] [US2] Criar DTOs de compra em `Fortuno.DTO/Purchase/` (`PurchasePreviewRequest`, `PurchasePreviewInfo`, `PurchaseConfirmRequest`, `PurchaseConfirmResponse`).
- [X] T057 [P] [US2] Criar DTOs de Ticket em `Fortuno.DTO/Ticket/` (`TicketInfo`, `TicketSearchQuery`).
- [X] T058 [P] [US2] Criar DTO de webhook em `Fortuno.DTO/Webhook/ProxyPayWebhookPayload.cs` conforme `contracts/webhook-proxypay.md`.
- [X] T059 [P] [US2] Criar DTOs de combo em `Fortuno.DTO/LotteryCombo/` (`LotteryComboInsertInfo`, `LotteryComboInfo`) — usado no preview para mostrar desconto aplicável.
- [X] T060 [US2] Criar `IPurchaseService` em Domain com métodos `PreviewAsync(PurchasePreviewRequest)`, `ConfirmAsync(PurchaseConfirmRequest)`, `ProcessPaidWebhookAsync(ProxyPayWebhookPayload)`.
- [X] T061 [US2] Implementar `PurchaseService.PreviewAsync` em `Fortuno.Domain/Services/PurchaseService.cs`: valida `ticket_min/max`, calcula desconto de combo, calcula `availableTickets` (FR-026/027/025), valida código de indicação quando presente (FR-R03/R04).
- [X] T062 [US2] Implementar `PurchaseService.ConfirmAsync`: chama `ProxyPayAppService.CreateInvoiceAsync` passando `metadata.fortunoPurchaseId`; em modo `UserPicks` insere reservas em `fortuna_number_reservations` com `expires_at = now() + 15min` (FR-030c).
- [X] T063 [US2] Implementar `PurchaseService.ProcessPaidWebhookAsync` com idempotência via `fortuna_webhook_events` UNIQUE; chamada em transação `SERIALIZABLE` para emitir tickets:
  - Modo `Random`: sortear N números do pool não vendido.
  - Modo `UserPicks`: promover reservas → tickets.
  - Em caso de reserva expirada, marcar invoice como `PendingRefund` sem emitir tickets.
  - Persistir `InvoiceReferrer` quando `referralCode` presente.
- [X] T064 [US2] Implementar `Fortuno.API/Controllers/PurchasesController.cs` com rotas `POST /purchases/preview` (não autenticada se comprador ainda não criou conta — middleware permite; mas preenche `ReferrerUserId` só se `referralCode` válido) e `POST /purchases/confirm` (requer autenticação simples NAuth).
- [X] T065 [US2] Implementar `Fortuno.API/Controllers/WebhooksController.cs` com `POST /webhooks/proxypay/invoice-paid`, protegido pelo `ProxyPayWebhookHmacFilter` (T035); delega ao `PurchaseService.ProcessPaidWebhookAsync`.
- [X] T066 [US2] Criar `ITicketService` + `TicketService` em Domain com `ListForUserAsync(userId, filter)` e `GetByIdAsync` (para listar/pesquisar "Meus Tickets", FR-033).
- [X] T067 [US2] Implementar `Fortuno.API/Controllers/TicketsController.cs` com `GET /tickets/mine` (filtros `lotteryId`, `number`, `date`).
- [X] T068 [US2] Implementar validador `PurchaseRequestValidator` (tanto preview quanto confirm): `quantity ≥ 1` e dentro de `[ticket_min, ticket_max]` quando > 0; `mode ∈ {Random, UserPicks}`; para `UserPicks` validar `pickedNumbers.Count == quantity` e cada número válido no pool/faixa composta via `INumberCompositionService`.
- [X] T069 [US2] Validar auto-indicação (FR-R04) no `PurchaseService.PreviewAsync` e `.ConfirmAsync`: comparar `referralCode` com o código do `currentUserId`; rejeitar.
- [X] T070 [US2] Implementar `IUserReferrerService.GetOrCreateCodeForUserAsync` em Domain gerando código de 8 chars (alfabeto A-Z sem I/O + 2-9) com retry em colisão (FR-R02/R02a).
- [X] T071 [US2] Registrar novos services de US2 no `Application/Startup.cs`.
- [X] T072 [US2] Adicionar rota `GET /lotteries/{id}/possibilities` em `LotteriesController` já prevista em T053, garantir que retorna também `availableTickets` (usado pela UI antes de abrir compra).
- [X] T073 [US2] Teste manual smoke via `curl`/Postman do fluxo quickstart §4.6–4.9; ajustar rotas/response se divergir de `contracts/rest-openapi.yaml`.

**Checkpoint**: US2 funcional — ciclo de compra PIX completo com tickets emitidos.

---

## Phase 5: User Story 3 - Dono realiza sorteio (P2)

**Goal**: Dono informa números ganhadores, revisa prévia, confirma registro;
pode repetir até fechar o Raffle. Flag `IncludePreviousWinners` respeitado.

**Independent Test**: Lottery `Open` com tickets vendidos → `POST
/raffles/{id}/winners/preview` → `POST .../confirm` → `POST .../close` →
tentativa posterior de alteração retorna 400.

### Implementation for User Story 3

- [X] T074 [P] [US3] Criar DTOs em `Fortuno.DTO/Raffle/` (`RaffleWinnersPreviewRequest`, `RaffleWinnerPreviewRow`).
- [X] T075 [P] [US3] Criar DTOs em `Fortuno.DTO/RaffleWinner/` (`RaffleWinnerInfo`).
- [X] T076 [US3] Expandir `IRaffleService` com `PreviewWinnersAsync`, `ConfirmWinnersAsync`, `CloseAsync`.
- [X] T077 [US3] Implementar `RaffleService.PreviewWinnersAsync` em `Fortuno.Domain/Services/RaffleService.cs`: para cada `winningNumber` faz lookup no `TicketRepository`; aplica regra `IncludePreviousWinners` (FR-034a/b); retorna linhas com status (matched/sem-ganhador/excluded-by-flag); busca `User` via `NAuthAppService` e mascara CPF.
- [X] T078 [US3] Implementar `RaffleService.ConfirmWinnersAsync`: persiste `RaffleWinner` por posição (denormaliza `position` e `prizeText` do Award); bloqueia se Raffle já `Closed` (FR-042).
- [X] T079 [US3] Implementar `RaffleService.CloseAsync`: transição `Open → Closed` torna ganhadores imutáveis.
- [X] T080 [US3] Implementar rotas em `RafflesController`: `POST /raffles/{id}/winners/preview`, `POST /raffles/{id}/winners/confirm`, `POST /raffles/{id}/close`. `[Authorize]` + ownership check.
- [X] T081 [US3] Validar que cada posição de um mesmo Raffle aceite ticket distinto (constraint do banco + validação em service).
- [X] T082 [US3] Alertar prévia-vazia quando `IncludePreviousWinners = false` e pool elegível estiver vazio (edge case da spec).
- [X] T083 [US3] Registrar services atualizados no `Application/Startup.cs`.

**Checkpoint**: US3 funcional — sorteio completo e registro imutável.

---

## Phase 6: User Story 4 - Gestão de imagens, combos, raffles e prêmios (P2)

**Goal**: CRUD completo de LotteryImage, LotteryCombo, Raffle, RaffleAward
enquanto Lottery em `Draft`, incluindo cancelamento de Raffle com
redistribuição obrigatória.

**Independent Test**: Lottery `Draft`, criar/editar/deletar cada entidade;
em Lottery `Open` confirmar 400; `POST /raffles/{id}/cancel` com
redistribuição válida muda status.

### Implementation for User Story 4

- [X] T084 [P] [US4] Criar DTO de cancelamento de Raffle em `Fortuno.DTO/Raffle/RaffleCancelRequest.cs` com lista `redistributions[]`.
- [X] T085 [US4] Expandir `ILotteryImageService` com `UpdateAsync`, `DeleteAsync`, `ListByLotteryAsync`, `ReorderAsync`; implementar bloqueio fora de Draft (FR-018).
- [X] T086 [US4] Criar `ILotteryComboService` + `LotteryComboService` com CRUD, incluindo validação de **sobreposição de faixas** (FR-024) — método `ValidateNoOverlapAsync` que consulta combos existentes da Lottery.
- [X] T087 [US4] Expandir `IRaffleService` com `UpdateAsync`, `DeleteAsync` (apenas Draft, FR-037) e `CancelAsync` com redistribuição obrigatória de awards (FR-042a/b/c).
- [X] T088 [US4] Expandir `IRaffleAwardService` com `UpdateAsync`, `DeleteAsync`, `ListByRaffleAsync`, `ReassignToRaffleAsync` (usado pela redistribuição).
- [X] T089 [US4] Implementar `Fortuno.API/Controllers/LotteryCombosController.cs` com `POST`, `PUT /{id}`, `DELETE /{id}`, `GET /{lotteryId}`.
- [X] T090 [US4] Adicionar rotas em `RafflesController`: `PUT /raffles/{id}`, `DELETE /raffles/{id}`, `POST /raffles/{id}/cancel` (recebe `RaffleCancelRequest`).
- [X] T091 [US4] Adicionar rotas em `RaffleAwardsController`: `PUT /raffle-awards/{id}`, `DELETE /raffle-awards/{id}`, `GET /raffle-awards?raffleId=...`.
- [X] T092 [US4] Expandir `LotteryImagesController` com `PUT /lottery-images/{id}` (editar descrição/ordem).
- [X] T093 [US4] Implementar validadores FluentValidation para `LotteryComboInsertInfo` (com check de sobreposição), `RaffleInsertInfo`, `RaffleAwardInsertInfo`, `RaffleCancelRequest` (deve cobrir 100% dos awards órfãos).
- [X] T094 [US4] Refund flow (parte de cancelamento de Lottery): criar `IRefundService` + `RefundService` com `ListPendingByLotteryAsync`, `MarkRefundedAsync(ticketIds, externalReference)` gerando `RefundLog` (FR-033a/b/c).
- [X] T095 [US4] Implementar `Fortuno.API/Controllers/RefundsController.cs` com `GET /refunds/pending/{lotteryId}` e `POST /refunds/mark-refunded`. Sem qualquer chamada de pagamento (FR-033b).
- [X] T096 [US4] No `LotteryService.CancelAsync` (já existe em US1), garantir marcação de todos os Tickets ativos como `PendingRefund` (FR-033a).
- [X] T097 [US4] Registrar todos os novos services de US4 no `Application/Startup.cs`.

**Checkpoint**: US4 funcional — gestão completa e estornos gerenciais operacionais.

---

## Phase 7: User Story 5 - Sistema de indicação e painéis de comissão (P2)

**Goal**: Usuários têm código único; comissão calculada em tempo real; dois
painéis (indicador + dono da Lottery) com cálculos agregados. Sistema NÃO
movimenta valores.

**Independent Test**: criar indicador e comprador; comprador faz compra com
código; ambos os painéis exibem valores corretos; marcar um ticket como
`Refunded` reduz proporcionalmente na próxima leitura.

### Implementation for User Story 5

- [X] T098 [P] [US5] Criar DTOs em `Fortuno.DTO/Referrer/` (`ReferrerEarningsPanel`, `ReferrerLotteryBreakdown`) e `Fortuno.DTO/Commission/` (`LotteryCommissionsPanel`, `ReferrerCommission`).
- [X] T099 [US5] Criar `IReferralService` em Domain com `GetOrCreateCodeForUserAsync(userId)`, `ValidateCodeAsync(code, buyerUserId)` (rejeita auto-indicação e códigos inexistentes), `RegisterInvoiceReferrerAsync(invoiceId, referrerUserId, lotteryId, percentSnapshot)`, `GetEarningsForReferrerAsync(userId)`, `GetPayablesForLotteryAsync(lotteryId, currentUserId)`.
- [X] T100 [US5] Implementar `ReferralService.GetEarningsForReferrerAsync` com query agregada única sobre `fortuna_invoice_referrers JOIN fortuna_tickets` agrupando por Invoice + Lottery, filtrando `refund_state != Refunded`, aplicando `Lottery.referral_percent` vigente em tempo real (FR-R06/R07).
- [X] T101 [US5] Implementar `ReferralService.GetPayablesForLotteryAsync` — mesma lógica mas pivotada por indicador para o painel do dono; exige que `currentUserId == Lottery.Store.OwnerUserId` (FR-R09 + FR-045a).
- [X] T102 [US5] No `PurchaseService` (T061/T063), chamar `ReferralService.ValidateCodeAsync` no preview/confirm e `RegisterInvoiceReferrerAsync` dentro do webhook.
- [X] T103 [US5] Garantir que `ReferralService` nunca chame `ProxyPay` para movimentar valores (FR-R10; SC-011).
- [X] T104 [P] [US5] Implementar `Fortuno.API/Controllers/ReferralsController.cs` com `GET /referrals/me` (painel do indicador autenticado) e `GET /referrals/code/me` (retorna o código gerado/criado lazy).
- [X] T105 [P] [US5] Implementar `Fortuno.API/Controllers/CommissionsController.cs` com `GET /commissions/lottery/{lotteryId}` (painel do dono) — apenas leitura, sem botão de pagar.
- [X] T106 [US5] Registrar `IReferralService` no `Application/Startup.cs`.
- [X] T107 [US5] Edge case "indicador órfão": quando `NAuthAppService.GetByIdAsync` retornar 404, o painel exibe `referrerName = null` e a UI marca "indicador indisponível" (FR-R04/R05).

**Checkpoint**: US5 funcional — indicações e painéis ativos, sem movimentação financeira.

---

## Phase 8: User Story 6 - Consulta pública via GraphQL (P3)

**Goal**: Clientes consultam Lottery/Raffle/Ticket/RaffleWinner via GraphQL
com filtros, projeção sobre EF Core, campos computados via type extensions.

**Independent Test**: query `lotteries(status: OPEN)` retorna apenas abertas;
`lotteryBySlug("rifa-x")` retorna relacionamentos; `myTickets` retorna apenas
tickets do usuário autenticado; CPF de RaffleWinner sempre mascarado.

### Implementation for User Story 6

- [X] T108 [P] [US6] Criar scalars `Long`, `Decimal`, `DateTime` em `Fortuno.GraphQL/Scalars/` via HotChocolate.
- [X] T109 [P] [US6] Criar enums GraphQL em `Fortuno.GraphQL/Enums/` espelhando Domain (`LotteryStatus`, `RaffleStatus`, `TicketRefundState`, `NumberType`) — tradução para SCREAMING_SNAKE via `[GraphQLName]`.
- [X] T110 [US6] Criar tipos GraphQL em `Fortuno.GraphQL/Types/` para cada entidade (Lottery, LotteryImage, LotteryCombo, Raffle, RaffleAward, RaffleWinner, Ticket) via skill `dotnet-graphql`.
- [X] T111 [US6] Criar queries em `Fortuno.GraphQL/Queries/`: `LotteryQueries`, `RaffleQueries`, `TicketQueries` com `[UseProjection]`, `[UseFiltering]`, `[UseSorting]` retornando `IQueryable<T>` direto do `FortunoContext`.
- [X] T112 [US6] Implementar `LotteryQueries.LotteryBySlug(string slug)` retornando `Lottery?`.
- [X] T113 [US6] Implementar `TicketQueries.MyTickets` restringindo via `[Authorize]` + filtro por `ClaimsPrincipal.UserId`.
- [X] T114 [US6] Criar type extensions em `Fortuno.GraphQL/TypeExtensions/`:
  - `LotteryTypeExtensions`: `availableTickets`, `soldTickets`, `totalPossibilities` (via `INumberCompositionService` + query agregada).
  - `RaffleWinnerTypeExtensions`: `userName`, `userCpfMasked` (via `NAuthAppService.GetByIdAsync` + mascaramento; **nunca** expor CPF cru).
  - `TicketTypeExtensions`: `winnings` (lista de `RaffleWinner` onde `ticket_id == this.ticket_id`).
- [X] T115 [US6] Criar queries de painel: `ReferralQueries.MyReferrerSummary` e `CommissionQueries.CommissionsByLottery(id)` que reusam `IReferralService`.
- [X] T116 [US6] Em `Fortuno.GraphQL/Startup.cs`, método estático `AddFortunoGraphQL(services)`: `.AddGraphQLServer().AddQueryType<Query>().AddTypeExtension<...>().AddProjections().AddFiltering().AddSorting().AddAuthorization()`.
- [X] T117 [US6] No `Fortuno.API/Program.cs`, registrar `AddFortunoGraphQL` e mapear endpoint `/graphql`.
- [X] T118 [US6] Verificar que CPF NÃO é exposto em nenhum tipo de leitura pública (FR-044); adicionar `[GraphQLIgnore]` em qualquer propriedade que possa vazar o campo cru.
- [X] T119 [US6] Smoke test manual dos 3 acceptance scenarios de US6 via Banana Cake Pop em `/graphql`.

**Checkpoint**: US6 funcional — GraphQL público com segurança.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T120 [P] Atualizar/completar Swagger (Swashbuckle) com XML-doc de todos os controllers; gerar `.xml` de cada projeto e incluir em `AddSwaggerGen(c => c.IncludeXmlComments(...))`.
- [X] T121 [P] Implementar middleware de erro global que capture exceções não tratadas e retorne `ApiResponse` com chaves portuguesas (constituição §6).
- [ ] T122 [P] Configurar logging estruturado (`ILogger<T>`) com `X-Request-Id` propagado via `LogContext` (research §11).
- [X] T123 [P] Rodar `dotnet format` em toda a solução e garantir aderência ao `.editorconfig` (constituição §2).
- [X] T124 [P] Revisar `appsettings.*.json` por ambiente via skill `dotnet-env`: garantir que `Development` use CORS `AllowAnyOrigin` e `Production` não.
- [ ] T125 Adicionar documento `docs/DEPLOY.md` via skill `deploy-prod` (fase futura; criar apenas stub indicando dependência da skill).
- [ ] T126 Rodar `quickstart.md` end-to-end em ambiente local (exceto pagamento PIX real — simular webhook com payload de teste assinado).
- [ ] T127 [P] **(Opcional)** Criar projeto `tests/Fortuno.Tests` via skill `dotnet-test` com cobertura mínima: `NumberCompositionServiceTests`, `PurchaseServiceTests` (modos Random/UserPicks com reserva expirada), `RaffleServiceTests` (flag `IncludePreviousWinners`), `ReferralServiceTests` (cálculo em tempo real + tickets estornados), `WebhooksControllerTests` (idempotência).
- [ ] T128 [P] **(Opcional)** Adicionar GitHub Actions CI para build + test via skill `deploy-prod` (fase futura).
- [X] T129 Revisar checklist da constituição §7 (contribuidor): skill usada, snake_case, `[Authorize]`, `[JsonPropertyName("camelCase")]`, sem ORM alternativo, sem comandos Docker local.
- [X] T130 Documento `docs/EXTERNAL_DEPS_INSTRUCTIONS.md` listando as lacunas observadas em NAuth/ProxyPay/zTools (de `research.md §9`) com instruções do que precisa ser implementado nesses microserviços **sem** o Fortuno modificar os repositórios deles.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: sem dependência — começa imediatamente.
- **Phase 2 (Foundational)**: depende do fim de Phase 1 — bloqueia TODAS as US.
- **Phase 3 (US1)**: depende de Phase 2.
- **Phase 4 (US2)**: depende de Phase 2 (NÃO depende de US1; um mock/seed de Lottery `Open` basta para testar US2 isoladamente, mas integração real exige US1).
- **Phase 5 (US3)**: depende de Phase 2; faz mais sentido depois de US2 (precisa de tickets vendidos).
- **Phase 6 (US4)**: depende de Phase 2 + US1 (precisa de Lottery criada).
- **Phase 7 (US5)**: depende de Phase 2 + US2 (precisa do fluxo de compra para exercitar o vínculo indicador).
- **Phase 8 (US6)**: depende das fases anteriores em que as entidades foram criadas (efetivamente depois de US1..US5).
- **Phase 9 (Polish)**: depende das US que estejam no escopo da entrega.

### User Story Parallelism

- US1 e US2 podem começar **em paralelo** após Phase 2 (dois devs). US1 entrega
  Lottery publicável; US2 pode ser desenvolvido contra fixtures até US1 pronto.
- US3 (sorteio) e US4 (CRUD gestão) podem rodar em paralelo depois de US1.
- US5 pode começar depois de US2 entregar o fluxo de compra.
- US6 normalmente fecha o ciclo (leitura); pode ser desenvolvido em paralelo
  com US5, pois depende apenas das entidades persistidas.

### Within Each User Story

- DTOs → Domain Services → Controllers → Registro de DI → smoke test.
- Testes (quando incluídos) vêm antes da implementação (TDD opcional).
- Registrar DI no `Startup.cs` sempre é a última etapa antes do smoke test.

---

## Parallel Execution Examples

### Phase 1 (Setup)

Tarefas T002–T007 criam projetos distintos e podem rodar em paralelo:

```bash
# 6 tarefas paralelas criando os classlibs
T002: dotnet new classlib -n Fortuno.DTO -o Fortuno.DTO
T003: dotnet new classlib -n Fortuno.Infra.Interfaces -o Fortuno.Infra.Interfaces
T004: dotnet new classlib -n Fortuno.Domain -o Fortuno.Domain
T005: dotnet new classlib -n Fortuno.Infra -o Fortuno.Infra
T006: dotnet new classlib -n Fortuno.Application -o Fortuno.Application
T007: dotnet new classlib -n Fortuno.GraphQL -o Fortuno.GraphQL
```

### Phase 2 (Foundational) — DTOs e interfaces

T017, T018, T019, T022, T023 são arquivos independentes — rodar em paralelo.

### Phase 3 (US1) — DTOs de cada agregado

T037, T038, T039 criam DTOs em pastas distintas — paralelos.

### Phase 7 (US5) — Controllers

T104 e T105 criam controllers distintos — paralelos.

### Phase 8 (US6) — Scalars e enums

T108, T109 são arquivos independentes — paralelos.

---

## Implementation Strategy

### MVP First (User Story 1 + 2)

1. Completar Phase 1 (Setup) e Phase 2 (Foundational).
2. Completar Phase 3 (US1) → Lottery publicável.
3. Completar Phase 4 (US2) → ciclo de compra PIX.
4. **STOP e VALIDATE**: fluxo completo criador→comprador→tickets emitidos.
5. Este é o MVP entregável — já vende ingressos e gera receita.

### Incremental Delivery

- Após MVP (US1+US2): adicionar US3 (sorteio) → plataforma completa em função.
- Adicionar US4 (gestão) → UX mais rica para o dono.
- Adicionar US5 (indicação) → canal viral de aquisição.
- Adicionar US6 (GraphQL) → abrir para integradores/frontends externos.
- Polish: docs, CI, testes adicionais.

### Parallel Team Strategy

Com 3 devs após Phase 2:

- Dev A: US1 → US4 (criação e gestão da Lottery).
- Dev B: US2 → US5 (compra e indicação).
- Dev C: US3 → US6 (sorteio e GraphQL).

---

## Notes

- `[P]` = arquivos distintos, sem dependência pendente.
- `[US#]` vincula task à User Story da spec — rastreabilidade direta.
- Cada US tem checkpoint independente — pode parar e demonstrar.
- Sistema **nunca** movimenta valores: todas as liquidações são off-platform (SC-011).
- Webhook do ProxyPay é **canal único** de confirmação PIX (FR-029a).
- Comissão de indicação é **cálculo em tempo real**, sem tabela de earnings.
- Todas as tabelas têm prefixo `fortuna_`; não executar comandos `docker` local.
