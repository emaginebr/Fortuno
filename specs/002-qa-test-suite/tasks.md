---
description: "Task list for feature 002-qa-test-suite"
---

# Tasks: QA Test Suite (Bruno + Unit + API)

**Input**: Design documents from `/specs/002-qa-test-suite/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md
**Assigned agent (FR-022)**: `qa-developer`

**Tests are the feature**: Os "tests" aqui **são o próprio entregável**. Não há "TDD antes da implementação" — cada task escreve ou configura um artefato de teste / collection.

**Organization**: Tarefas agrupadas por user story para habilitar entrega incremental. US1 (ApiTests) e US2 (Unit) são os pilares automatizados; US3 (Bruno) é artefato humano de onboarding.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Paralelizável (arquivo diferente, sem dependência pendente).
- **[Story]**: Label de user story apenas nas fases US1/US2/US3; Setup, Foundational e Polish **não** levam label.

## Path Conventions

- **Raiz**: `C:\repos\Fortuno\Fortuno\`
- `Fortuno.Tests/` (novo) — unit tests
- `Fortuno.ApiTests/` (novo) — integration tests
- `bruno/` (novo) — Bruno collection
- `.github/workflows/` — CI
- Projetos existentes (`Fortuno.API`, `Fortuno.Application`, `Fortuno.Domain`, `Fortuno.DTO`, `Fortuno.Infra`, `Fortuno.Infra.Interfaces`) **NÃO** são modificados por esta feature.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: criar os dois projetos de teste, registrá-los na solution, inicializar a pasta `bruno/` e ajustar `.gitignore`.

- [X] T001 Criar projeto xUnit `Fortuno.Tests` via `dotnet new xunit -n Fortuno.Tests -f net8.0` na raiz do repo e adicioná-lo à `Fortuno.sln` com `dotnet sln add Fortuno.Tests/Fortuno.Tests.csproj`
- [X] T002 [P] Criar projeto xUnit `Fortuno.ApiTests` via `dotnet new xunit -n Fortuno.ApiTests -f net8.0` na raiz do repo e adicioná-lo à `Fortuno.sln` com `dotnet sln add Fortuno.ApiTests/Fortuno.ApiTests.csproj`
- [X] T003 [P] Adicionar pacotes NuGet ao `Fortuno.Tests/Fortuno.Tests.csproj`: `FluentAssertions` (6.x), `Moq` (4.x), `coverlet.collector` (6.x) — `Microsoft.NET.Test.Sdk` e `xunit` já vêm do template
- [X] T004 [P] Adicionar pacotes NuGet ao `Fortuno.ApiTests/Fortuno.ApiTests.csproj`: `Flurl.Http` (4.x), `FluentAssertions` (6.x), `Microsoft.Extensions.Configuration.Json`, `Microsoft.Extensions.Configuration.EnvironmentVariables`
- [X] T005 [P] Adicionar referências de projeto ao `Fortuno.Tests/Fortuno.Tests.csproj`: `Fortuno.Domain`, `Fortuno.Application`, `Fortuno.Infra`, `Fortuno.Infra.Interfaces`, `Fortuno.DTO`
- [X] T006 [P] Adicionar referência de projeto ao `Fortuno.ApiTests/Fortuno.ApiTests.csproj`: `Fortuno.DTO` (somente — ApiTests chama via HTTP, não usa domínio)
- [X] T007 [P] Inicializar pasta `bruno/` na raiz com `bruno/bruno.json` (nome "Fortuno API", versão "1") e `bruno/environments/local.example.bru`, `dev.example.bru`, `prod.example.bru` (templates sem segredos, conforme `contracts/bruno-collection-layout.md`)
- [X] T008 Atualizar `.gitignore` da raiz para excluir: `Fortuno.ApiTests/appsettings.Tests.json`, `bruno/environments/local.bru`, `bruno/environments/dev.bru`, `bruno/environments/prod.bru` (preservar apenas `*.example.bru`)

**Checkpoint Setup**: projetos criados, compilam (`dotnet build Fortuno.sln`), `bruno/` existe, `.gitignore` atualizado.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: helpers e configuração que toda US1 (e algumas partes da US2) consomem.

**⚠️ CRITICAL**: US1 não pode começar sem T009–T012. US2 não pode começar sem T013.

- [X] T009 Criar `Fortuno.ApiTests/appsettings.Tests.example.json` com chaves `FortunoTests:ApiBaseUrl`, `FortunoTests:NAuthUrl`, `FortunoTests:NAuthTenant` (default `"fortuna"`), `FortunoTests:NAuthUser`, `FortunoTests:NAuthPassword`, `FortunoTests:ProxyPayUrl` — todos com placeholders vazios (referência: `contracts/auth-nauth.md`). **R-001 v2**: `StoreId` removido; Store é descoberta em runtime.
- [X] T010 Criar `Fortuno.ApiTests/_Fixtures/TestSettings.cs` como `sealed record TestSettings(string ApiBaseUrl, string NAuthUrl, string NAuthTenant, string NAuthUser, string NAuthPassword, string ProxyPayUrl)` com método estático `FromEnvironment()` que lê env vars `FORTUNO_TEST_*` ou `appsettings.Tests.json` e lança `InvalidOperationException` com mensagem acionável se algum campo vier vazio (FR-018). **R-001 v2**: `StoreId` removido; descoberto em runtime via `ProxyPayStoreResolver`.
- [X] T011 [P] Criar `Fortuno.ApiTests/_Fixtures/UniqueId.cs` com método `UniqueId.New(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("n").Substring(0, 8)}"` (FR-017, R-007)
- [X] T012 Criar `Fortuno.ApiTests/_Fixtures/ApiSessionFixture.cs` implementando `IAsyncLifetime` conforme R-004: em `InitializeAsync()` lê `TestSettings`, faz `POST {NAuthUrl}/auth/login` com body `{tenant,user,password}`, captura `token`, instancia `FlurlClient(ApiBaseUrl).WithHeader("Authorization", $"Basic {token}")`, e valida ownership chamando `GET /lotteries/store/{StoreId}` (200 esperado; 4xx → lançar `InvalidOperationException` "Usuário não é dono da Store {id}"). Expor `Client` e `StoreId` como propriedades públicas. Registrar como `[CollectionDefinition("api")]`
- [X] T013 Criar `Fortuno.Tests/coverlet.runsettings` conforme R-003 com excludes por assembly (`[*]*Settings`, `[*]Fortuno.Infra.Context.*`, `[*]*.Migrations.*`) e por arquivo (`**/Program.cs`, `**/Startup.cs`, `**/Migrations/*.cs`), formato Cobertura

**Checkpoint Foundational**: fixture compila; rodar `dotnet build Fortuno.sln` sem erro. Sem env vars definidas, a fixture falha fast com mensagem clara (testado invocando `TestSettings.FromEnvironment()` num teste sentinel manual — opcional).

---

## Phase 3: User Story 1 — Suite de testes de API para o ciclo de vida da Lottery (Priority: P1) 🎯 MVP

**Goal**: exercitar ponta-a-ponta o ciclo de vida da Lottery (criação, publish, close, cancel + transição inválida) e os endpoints `[AllowAnonymous]` de consulta, autenticando via NAuth.

**Independent Test**: com API + NAuth rodando e env vars configuradas, `dotnet test Fortuno.ApiTests` passa 100% dos 8 cenários em < 3 minutos (SC-003).

### Implementation for User Story 1

- [X] T014 [P] [US1] Criar `Fortuno.ApiTests/_Smoke/AuthenticationSmokeTests.cs` cobrindo Cenário US1 #1: classe `[Collection("api")]`, injeta `ApiSessionFixture`, teste `Login_ShouldObtainTokenAndAuthorizeAuthenticatedCall` que verifica que `fixture.Client` responde 200 em `GET /lotteries/store/{StoreId}` (prova que o token injetado pela fixture está ativo)
- [X] T015 [US1] Criar `Fortuno.ApiTests/Lotteries/LotteryLifecycleTests.cs` cobrindo Cenários US1 #2, #3, #4, #5, #6, #8. Classe `[Collection("api")]` com testes:
  - `Create_ShouldReturnLotteryInDraftStatus` — POST `/lotteries` com `LotteryInsertInfo` usando `UniqueId.New("qa-lottery")` e `fixture.StoreId`; asserta 201 e `status == "Draft"`
  - `Publish_FromDraft_ShouldTransitionToOpen` — cria + publish + GET; asserta `status == "Open"`
  - `Close_FromOpen_ShouldTransitionToClosed` — cria + publish + close + GET; asserta `status == "Closed"`
  - `Cancel_FromDraft_ShouldTransitionToCancelled` — cria + cancel (body com `reason`) + GET; asserta `status == "Cancelled"`
  - `Publish_OnCancelledLottery_ShouldReturn4xx` — cria + cancel + publish; asserta status >= 400 e que GET posterior mantém `status == "Cancelled"` (SC-007)
  - `SuiteIsIdempotent_WhenRunTwice` — dois ciclos create+publish+close back-to-back usando `UniqueId` distintos; ambos sucesso
- [X] T016 [P] [US1] Criar `Fortuno.ApiTests/Lotteries/LotteryPublicQueryTests.cs` cobrindo Cenário US1 #7: classe `[Collection("api")]` com testes:
  - `GetById_WithoutAuth_ShouldReturn200` — cria uma Lottery com cliente autenticado, depois faz `new FlurlRequest(…).GetAsync()` (sem header `Authorization`) em `/lotteries/{id}`; asserta 200 e payload com `lotteryId` igual
  - `GetBySlug_WithoutAuth_ShouldReturn200` — idem com `/lotteries/slug/{slug}`

**Checkpoint US1**: `dotnet test Fortuno.ApiTests` em ambiente preparado conclui < 3 min, 100% verde. User Story 1 é entregável/demonstrável como MVP.

---

## Phase 4: User Story 2 — Testes unitários cobrindo ≥ 80% (Priority: P2)

**Goal**: suite xUnit determinística para Domain Services, Validators e AppServices, com cobertura agregada ≥ 80% sobre `Domain + Application + Infra`.

**Independent Test**: `dotnet test Fortuno.Tests --settings Fortuno.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"` conclui < 2 min com 100% verde (SC-001) e relatório Cobertura acusa ≥ 80% (SC-002).

### Domain Services (12 tarefas — todas paralelas)

- [X] T017 [P] [US2] Criar `Fortuno.Tests/Domain/Services/LotteryServiceTests.cs` cobrindo `CreateAsync`, `PublishAsync`, `CloseAsync`, `CancelAsync`, `GetByIdAsync`, `ListByStoreAsync`; cenários felizes + falha por transição inválida (publish em Cancelled, close em Draft) e input inválido (storeId inexistente via Moq de `StoreOwnershipGuard`)
- [X] T018 [P] [US2] Criar `Fortuno.Tests/Domain/Services/RaffleServiceTests.cs` cobrindo `CreateAsync` (exige Lottery em Open), `PreviewWinnersAsync` (N vencedores = N `RaffleAward`), `ConfirmWinnersAsync` (idempotente), `CloseAsync` (bloqueia sem winners). Mock de `IRepository<Lottery>`, `IRepository<Ticket>`, `IRepository<Raffle>`
- [X] T019 [P] [US2] Criar `Fortuno.Tests/Domain/Services/TicketServiceTests.cs`: consultas por usuário, por lottery; filtro de datas; obter por id
- [X] T020 [P] [US2] Criar `Fortuno.Tests/Domain/Services/PurchaseServiceTests.cs`: `PreviewAsync` (reserva de números, combos aplicados), `ConfirmAsync` (gera Tickets), erros (Lottery fechada, números indisponíveis)
- [X] T021 [P] [US2] Criar `Fortuno.Tests/Domain/Services/RaffleAwardServiceTests.cs`: create/update/delete, validação de `Position` único por Raffle
- [X] T022 [P] [US2] Criar `Fortuno.Tests/Domain/Services/LotteryComboServiceTests.cs`: create/update/delete, rejeita quantidade ≤ 0 ou desconto fora de [0,100]
- [X] T023 [P] [US2] Criar `Fortuno.Tests/Domain/Services/LotteryImageServiceTests.cs`: create/update/delete, URL obrigatória
- [X] T024 [P] [US2] Criar `Fortuno.Tests/Domain/Services/RefundServiceTests.cs`: listagem de pendentes, `MarkRefundedAsync` transita estado do Ticket
- [X] T025 [P] [US2] Criar `Fortuno.Tests/Domain/Services/ReferralServiceTests.cs`: `GetByUser`, `GetCode`
- [X] T026 [P] [US2] Criar `Fortuno.Tests/Domain/Services/SlugServiceTests.cs`: slug único por Store, acentos removidos, colisão resolvida com sufixo
- [X] T027 [P] [US2] Criar `Fortuno.Tests/Domain/Services/NumberCompositionServiceTests.cs`: composição Int64 + Composed3..Composed8, rejeição de formato inválido
- [X] T028 [P] [US2] Criar `Fortuno.Tests/Domain/Services/StoreOwnershipGuardTests.cs`: `IsOwnerAsync` true/false via mock de `IProxyPayAppService`; `EnsureOwnershipAsync` lança em caso negativo

### Validators (8 tarefas — todas paralelas)

- [X] T029 [P] [US2] Criar `Fortuno.Tests/Application/Validations/LotteryInsertInfoValidatorTests.cs` com DTO válido + uma asserção negativa por regra: `Name` vazio, `TicketPrice` ≤ 0, `StoreId` ≤ 0, `NumberType` inválido
- [X] T030 [P] [US2] Criar `Fortuno.Tests/Application/Validations/LotteryImageInsertInfoValidatorTests.cs` com DTO válido + negativas: `LotteryId` ≤ 0, `ImageUrl` vazio, `ImageUrl` malformado
- [X] T031 [P] [US2] Criar `Fortuno.Tests/Application/Validations/LotteryCancelRequestValidatorTests.cs` com DTO válido + `Reason` vazio
- [X] T032 [P] [US2] Criar `Fortuno.Tests/Application/Validations/PurchasePreviewRequestValidatorTests.cs` com DTO válido + `LotteryId` ≤ 0, `Quantity` ≤ 0, `AssignmentMode` inválido, `TicketNumbers` exigido em `UserPicks`
- [X] T033 [P] [US2] Criar `Fortuno.Tests/Application/Validations/PurchaseConfirmRequestValidatorTests.cs` com DTO válido + negativas análogas a T032 + `ReservationId` vazio
- [X] T034 [P] [US2] Criar `Fortuno.Tests/Application/Validations/LotteryComboInsertInfoValidatorTests.cs` com DTO válido + `LotteryId` ≤ 0, `Quantity` ≤ 0, `DiscountPercent` fora de [0,100]
- [X] T035 [P] [US2] Criar `Fortuno.Tests/Application/Validations/RaffleCancelRequestValidatorTests.cs` com DTO válido + `Reason` vazio
- [X] T036 [P] [US2] Criar `Fortuno.Tests/Application/Validations/RefundStatusChangeRequestValidatorTests.cs` com DTO válido + `TicketId` ≤ 0, `NewState` inválido

### AppServices (3 tarefas — todas paralelas)

- [X] T037 [P] [US2] Criar `Fortuno.Tests/Infra/AppServices/ProxyPayAppServiceTests.cs` focando parser do webhook + verificação de assinatura HMAC (uso de `HttpClient` mockado via `HttpMessageHandler` fake); casos: HMAC válido aceita, HMAC inválido rejeita, payload malformado rejeita
- [X] T038 [P] [US2] Criar `Fortuno.Tests/Infra/AppServices/NAuthAppServiceTests.cs` validando a interação com `IUserClient` via Moq (não chama NAuth real); caminhos felizes + erro
- [X] T039 [P] [US2] Criar `Fortuno.Tests/Infra/AppServices/ZToolsAppServiceTests.cs` para upload S3 e utilidades de slug; mock de dependências externas

### Coverage gate validation

- [X] T040 [US2] Rodar `dotnet test Fortuno.Tests --settings Fortuno.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"` + `reportgenerator` localmente (ver `quickstart.md` §1) e confirmar que `Summary.txt` acusa `Line coverage >= 80.0%` sobre assemblies `Fortuno.Domain;Fortuno.Application;Fortuno.Infra`. Se abaixo, adicionar testes adicionais até bater o piso (SC-002). Commit só aprovado se verde. **Resultado**: Line coverage = 94% (Fortuno.Domain 93.4%, Fortuno.Infra 100%). 199 unit tests aprovados em < 2s.

**Checkpoint US2**: unit tests passam em < 2 min, cobertura ≥ 80% confirmada. US2 entregável independente.

---

## Phase 5: User Story 3 — Bruno collection (Priority: P3)

**Goal**: coleção Bruno versionada em `/bruno/` cobrindo todos os 12 controllers, com login automático e ambientes configuráveis.

**Independent Test**: onboarding de novo integrante em ≤ 10 min executando login + create + publish + get-by-id (SC-004).

### Auth (1 tarefa)

- [X] T041 [US3] Criar `bruno/_Auth/login.bru` conforme R-005: `post {{nauthUrl}}/auth/login` com body JSON (`tenant`, `user`, `password`), `script:post-response` capturando `res.body.token` via `bru.setVar("accessToken", ...)`. Template `.example` sem credenciais reais

### Domain folders (12 tarefas paralelas — uma por controller)

- [X] T042 [P] [US3] Criar `bruno/Lotteries/` com 8 requests: `create.bru`, `get-by-id.bru`, `get-by-slug.bru`, `list-by-store.bru`, `update.bru`, `publish.bru`, `close.bru`, `cancel.bru` — com headers `Authorization: Basic {{accessToken}}` exceto em `get-by-id` e `get-by-slug` (`[AllowAnonymous]`); payloads válidos por validator (ver `contracts/lottery-lifecycle.md`)
- [X] T043 [P] [US3] Criar `bruno/LotteryCombos/` com 4 requests: `create.bru`, `update.bru`, `delete.bru`, `list-by-lottery.bru`
- [X] T044 [P] [US3] Criar `bruno/LotteryImages/` com 4 requests: `create.bru`, `update.bru`, `delete.bru`, `list-by-lottery.bru`
- [X] T045 [P] [US3] Criar `bruno/Raffles/` com 6 requests: `create.bru`, `get-by-id.bru` (AllowAnonymous), `list-by-lottery.bru` (AllowAnonymous), `preview-winners.bru`, `confirm-winners.bru`, `close.bru` — script pós-resposta em `create.bru` captura `raffleId`
- [X] T046 [P] [US3] Criar `bruno/RaffleAwards/` com 4 requests: `create.bru`, `update.bru`, `delete.bru`, `list-by-raffle.bru`
- [X] T047 [P] [US3] Criar `bruno/Tickets/` com 2 requests: `list-mine.bru`, `get-by-id.bru`
- [X] T048 [P] [US3] Criar `bruno/Purchases/` com 2 requests: `preview.bru`, `confirm.bru`
- [X] T049 [P] [US3] Criar `bruno/Referrals/` com 2 requests: `get-me.bru`, `get-code.bru`
- [X] T050 [P] [US3] Criar `bruno/Commissions/` com 1 request: `list-by-lottery.bru`
- [X] T051 [P] [US3] Criar `bruno/Refunds/` com 2 requests: `list-pending.bru`, `mark-refunded.bru`
- [X] T052 [P] [US3] Criar `bruno/Webhooks/` com 1 request: `proxypay-invoice-paid.bru` (AllowAnonymous, inclui header e body assinados HMAC conforme implementação atual)

### Onboarding validation

- [ ] T053 [US3] Validar SC-004 executando pela UI do Bruno: abrir `bruno/`, configurar `environments/local.bru` com ambiente local real, executar `_Auth/login` → `Lotteries/create` → `Lotteries/publish` → `Lotteries/get-by-id` em ordem; anotar tempo total em um comentário no PR (meta ≤ 10 min) — **Pendente validação manual**: requer API + NAuth rodando e credenciais reais; humano deve rodar e anotar tempo no PR

**Checkpoint US3**: collection carrega sem erros, login captura token, fluxo de onboarding em ≤ 10 min.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T054 [P] Criar `.github/workflows/coverage-check.yml` conforme R-006: trigger `push`/`pull_request` em `main` e `002-*`; steps: checkout, `actions/setup-dotnet@v4` (8.0.x), `dotnet restore`, `dotnet test Fortuno.Tests --settings Fortuno.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`, instalar `dotnet-reportgenerator-globaltool`, gerar `TextSummary`, shell step que extrai `Line coverage` e `exit 1` se menor que 80 (FR-020, SC-002). Workflow **NÃO** roda `Fortuno.ApiTests` (FR-021)
- [X] T055 [P] Executar varredura textual no repositório para SC-005: `rg -n "(password|JWT|ConnectionString)" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!.git/**"` e confirmar que nenhum match revela credenciais reais (todos os matches devem ser em templates `.example`, logs de migrações ou strings de log sem valor sensível). **Resultado**: 2 matches, ambos em skill docs (`add-react-i18n/SKILL.md`, `dotnet-test-api/SKILL.md`) com placeholders `<secret>`; nenhuma credencial real encontrada.
- [ ] T056 Executar `quickstart.md` §1 (unit), §2 (API) e §3 (Bruno) contra ambiente local e confirmar que todas as SC (001–007) são satisfeitas. Registrar evidências (screenshots ou logs) no PR — **Pendente validação manual**: requer API + NAuth rodando e env vars preenchidas
- [X] T057 Marcar checklists `specs/002-qa-test-suite/checklists/requirements.md` como revisados pós-implementação (todos os critérios já estão ✓ após `/speckit.clarify`; revalidar que nada regrediu com a implementação real)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências; pode iniciar imediatamente.
- **Foundational (Phase 2)**: depende de Setup. Bloqueia US1 (T009–T012) e US2 (T013).
- **US1 (Phase 3)**: depende de T009–T012.
- **US2 (Phase 4)**: depende de T013 (e T003+T005 de Setup).
- **US3 (Phase 5)**: depende apenas de T007 e T008 (Setup); **independente** de US1/US2.
- **Polish (Phase 6)**: T054 depende de US2 (precisa dos testes existindo para rodar o CI). T055/T056/T057 dependem do conjunto das 3 user stories.

### User Story Dependencies

- **US1 (P1)**: independente de US2 e US3.
- **US2 (P2)**: independente de US1 e US3.
- **US3 (P3)**: independente das outras.

As três podem ser executadas **em paralelo** por desenvolvedores distintos após Foundational.

### Within Each Story

- US1: T014, T015 e T016 são independentes entre si (classes distintas); T015 não é `[P]` apenas porque é o maior escopo e representa o caminho crítico — mas pode ser paralelizado se a equipe preferir.
- US2: **todas** as 23 tarefas de test class (T017–T039) são `[P]`; T040 depende de todas elas.
- US3: T041 primeiro (para popular `accessToken` na UI manual); T042–T052 são `[P]`; T053 depende do conjunto.

---

## Parallel Example: User Story 2

```text
# Após T013 completo, lançar em paralelo (23 tasks):
T017..T028  (12 Domain Service test classes)
T029..T036  (8 Validator test classes)
T037..T039  (3 AppService test classes)

# Cada task escreve um arquivo distinto — sem conflito.
# T040 aguarda o conjunto e roda a métrica de cobertura.
```

## Parallel Example: User Story 3

```text
# Após T007, T008 e T041 completos:
T042..T052  (11 pastas de domínio, cada uma isolada)

# T053 aguarda e faz a validação manual.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Setup (T001–T008).
2. Foundational (T009–T012).
3. US1 (T014–T016).
4. **STOP & VALIDATE**: ambiente preparado (env vars + API + NAuth), rodar `dotnet test Fortuno.ApiTests`. Se 100% verde em < 3 min, demonstrar.

### Incremental Delivery

1. MVP (acima).
2. US2: unit tests + coverage gate → merge PR → CI bloqueia regressões.
3. US3: Bruno collection → merge PR → onboarding fluido.
4. Polish → CI workflow + scan de segredos + validação full quickstart.

### Parallel Team Strategy

Após Foundational completo:

- Dev A (owner do qa-developer): US1 + US2 (ambos são .NET; mesmo contexto mental).
- Dev B (pode ser outro dev ou mesmo QA): US3 (Bruno é independente, pode escrever ao mesmo tempo).

O agente `qa-developer` (FR-022) é responsável pelo conjunto; delegação a humanos fica a critério do time.

---

## Notes

- `[P]` implica arquivo distinto e sem dependência pendente — verificar cada task antes de paralelizar.
- Todas as tarefas de US1/US2/US3 carregam label `[US1]`/`[US2]`/`[US3]` para rastreabilidade; Setup, Foundational e Polish **não** levam label (convenção do template).
- `T040` e `T053` são checkpoints qualitativos — não são código, mas validações que bloqueiam o PR correspondente.
- A implementação **não** altera arquivos em `Fortuno.API`, `Fortuno.Application`, `Fortuno.Domain`, `Fortuno.DTO`, `Fortuno.Infra`, `Fortuno.Infra.Interfaces`. Qualquer tentativa de modificar produção indica que uma premissa da spec mudou — parar e revisitar.
- Commits frequentes recomendados: 1 commit por task ou por grupo de tasks irmãs (ex.: "US2: validators suite").
