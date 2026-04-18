# Implementation Plan: QA Test Suite (Bruno + Unit + API)

**Branch**: `002-qa-test-suite` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-qa-test-suite/spec.md`

## Summary

Entrega de três artefatos de qualidade coexistentes no repositório:

1. **Bruno collection** versionada em `/bruno/` cobrindo todos os controllers da `Fortuno.API`, com ambientes `local/dev/prod`, captura automática do token NAuth via script de pós-resposta e payloads compatíveis com os validators FluentValidation atuais.
2. **`Fortuno.Tests`** — projeto xUnit + FluentAssertions + Moq que mira Domain Services, Validators e AppServices, alcançando ≥ 80% de cobertura agregada (ponderada por linhas) sobre `Domain + Application + Infra`, com gate no CI via Coverlet.
3. **`Fortuno.ApiTests`** — projeto xUnit + Flurl.Http + FluentAssertions que autentica via NAuth (tenant lido de env, default `"fortuna"`), valida o ciclo de vida da Lottery (Draft → Publish/Close/Cancel + transições inválidas) e os endpoints públicos `[AllowAnonymous]` de consulta. Raffle / Purchase / Webhook ProxyPay ficam fora dos ApiTests nesta entrega (adiados até existir fluxo simulado de pagamento).

Abordagem técnica: preset `dotnet-test-api` (xUnit + Flurl + FluentAssertions + fixture `IAsyncLifetime` compartilhada) e preset `dotnet-test` (mirroring de pastas por camada). Usuário e `StoreId` de teste são **pré-provisionados** no par NAuth+ProxyPay e passados por env var — a fixture valida ownership na fase de setup e falha fast em caso de inconsistência. CI: gate de cobertura 80% em GitHub Actions bloqueia build em regressão.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (conforme constituição, Princípio II)
**Primary Dependencies (test)**: xUnit `2.9.x`, FluentAssertions `6.x`, Moq `4.x`, Flurl.Http `4.x`, Microsoft.NET.Test.Sdk `17.x`, coverlet.collector `6.x`
**Primary Dependencies (under test, herdadas)**: ASP.NET Core 8, EF Core 9.x, Npgsql, FluentValidation, NAuth 0.5.x, zTools, Swashbuckle 8.x
**Storage**: PostgreSQL (Fortuno) e ProxyPay (externo, para dados de Store). `Fortuno.ApiTests` **não** acessa o banco diretamente — somente chamadas HTTP contra `Fortuno.API` rodando.
**Testing**: xUnit em ambos os projetos; Coverlet para cobertura (formato Cobertura). Execução: `dotnet test`.
**Target Platform**: Windows 11 (dev) + Linux (CI GitHub Actions). Bruno collection abre em qualquer SO.
**Project Type**: Web service (API) + artefatos de qualidade. Nenhum frontend/mobile.
**Performance Goals**: SC-001 < 2 min (unit); SC-003 < 3 min (API); SC-004 ≤ 10 min de onboarding via Bruno.
**Constraints**:
- Sem Docker local (constituição, Princípio II).
- Nenhum secret versionado (constituição, Princípio V + SC-005).
- ApiTests não podem depender de endpoint Fortuno ainda inexistente (ex.: criação de Store) — ProxyPay é o dono das Stores.
- Coverage aggregation ponderada por linhas, não por projeto isolado.
**Scale/Scope**: 12 controllers, ~10 Domain Services, 8 Validators, ~30–40 requests na Bruno collection, ~8 cenários na suite de API, ~120–160 testes unitários estimados.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra `.specify/memory/constitution.md` v1.0.0:

| Princípio | Aplicabilidade | Status | Observações |
|---|---|---|---|
| I. Skills Obrigatórias (`dotnet-architecture`) | **Parcial** | ✓ PASS | Esta feature não cria entidades, services, repositories, DTOs ou migrations de produção; é suite de testes + collection Bruno. A skill correspondente é `dotnet-test` (unit) e `dotnet-test-api` (integration), acionadas pelo agente `qa-developer` (FR-022). |
| II. Stack Tecnológica Fixa | **Parcial** | ✓ PASS | Não introduz ORM alternativo nem Docker. Dependências de teste (xUnit, Flurl, Moq, FluentAssertions, Coverlet) estão fora da tabela de stack de produção — são ferramentais de teste padrão do ecossistema .NET. |
| III. Convenções de Código .NET | **Total** | ✓ PASS | Testes seguirão file-scoped namespaces, PascalCase para classes/métodos, `_camelCase` para campos privados. Nenhum DTO novo; se helpers de teste expuserem serialização, `[JsonPropertyName("camelCase")]` será aplicado. |
| IV. Convenções de Banco (PostgreSQL) | **N/A** | ✓ PASS | Nenhuma alteração de schema, migration ou tabela. ApiTests não acessam DB diretamente. |
| V. Autenticação e Segurança | **Total** | ✓ PASS | ApiTests autenticam via NAuth Basic token (`Authorization: Basic {token}`), idêntico ao padrão de produção. Endpoints públicos `[AllowAnonymous]` são exercitados sem header — consistente com o design atual. Nenhum secret em arquivos versionados (FR-006, FR-019, SC-005). |

**Checklist para novos contribuidores** (`.specify/memory/constitution.md` §Fluxo de Desenvolvimento):

- [x] Skill `dotnet-architecture` — N/A (sem entidades backend; usada indiretamente só se Store precisar ser exposta, cenário descartado pelo research).
- [x] snake_case em PostgreSQL — N/A (sem schema change).
- [x] `[Authorize]` em controllers com dados sensíveis — N/A (sem novos controllers).
- [x] `[JsonPropertyName("camelCase")]` em DTOs — N/A (sem DTOs novos).
- [x] Nenhum ORM alternativo — confirmado.
- [x] Nenhum `docker` / `docker compose` local — confirmado.

**Resultado do Gate**: ✓ **PASS**. Sem violações; Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/002-qa-test-suite/
├── plan.md                          # Este arquivo (/speckit.plan)
├── research.md                      # Phase 0 — decisões e alternativas
├── data-model.md                    # Phase 1 — entidades exercitadas + estados
├── contracts/                       # Phase 1 — contratos de endpoint por área
│   ├── auth-nauth.md
│   ├── lottery-lifecycle.md
│   └── bruno-collection-layout.md
├── quickstart.md                    # Phase 1 — como rodar cada suite e Bruno
├── checklists/
│   └── requirements.md              # /speckit.specify — completo
└── spec.md                          # /speckit.specify — spec canônica
```

### Source Code (repository root)

Estrutura resultante (adições em **negrito**, existentes em itálico):

```text
<repo>/
├── _Fortuno.API/_                                 (existente)
├── _Fortuno.Application/_                         (existente)
├── _Fortuno.Domain/_                              (existente)
├── _Fortuno.DTO/_                                 (existente)
├── _Fortuno.Infra/_                               (existente)
├── _Fortuno.Infra.Interfaces/_                    (existente)
│
├── **Fortuno.Tests/**                             (novo — unit tests)
│   ├── Fortuno.Tests.csproj
│   ├── Domain/
│   │   ├── Services/
│   │   │   ├── LotteryServiceTests.cs
│   │   │   ├── RaffleServiceTests.cs
│   │   │   ├── TicketServiceTests.cs
│   │   │   ├── PurchaseServiceTests.cs
│   │   │   ├── RaffleAwardServiceTests.cs
│   │   │   ├── LotteryComboServiceTests.cs
│   │   │   ├── LotteryImageServiceTests.cs
│   │   │   ├── RefundServiceTests.cs
│   │   │   ├── ReferralServiceTests.cs
│   │   │   ├── SlugServiceTests.cs
│   │   │   ├── NumberCompositionServiceTests.cs
│   │   │   └── StoreOwnershipGuardTests.cs
│   │   └── _Helpers/AutoMocker, Fixtures_
│   ├── Application/
│   │   └── Validations/
│   │       ├── LotteryInsertInfoValidatorTests.cs
│   │       ├── LotteryImageInsertInfoValidatorTests.cs
│   │       ├── LotteryCancelRequestValidatorTests.cs
│   │       ├── PurchasePreviewRequestValidatorTests.cs
│   │       ├── PurchaseConfirmRequestValidatorTests.cs
│   │       ├── LotteryComboInsertInfoValidatorTests.cs
│   │       ├── RaffleCancelRequestValidatorTests.cs
│   │       └── RefundStatusChangeRequestValidatorTests.cs
│   └── Infra/
│       └── AppServices/
│           ├── ProxyPayAppServiceTests.cs        # parser + HMAC
│           ├── NAuthAppServiceTests.cs
│           └── ZToolsAppServiceTests.cs
│
├── **Fortuno.ApiTests/**                          (novo — integration tests)
│   ├── Fortuno.ApiTests.csproj
│   ├── appsettings.Tests.example.json
│   ├── .gitignore                                  # exclui appsettings.Tests.json
│   ├── _Fixtures/
│   │   ├── ApiSessionFixture.cs                   # IAsyncLifetime — login NAuth + valida Store
│   │   ├── TestSettings.cs                        # binding env → config
│   │   └── UniqueId.cs                            # slug/nome sufixado por Guid
│   ├── Lotteries/
│   │   ├── LotteryLifecycleTests.cs               # Cenários US1 #2–#6
│   │   └── LotteryPublicQueryTests.cs             # Cenários US1 #7 (AllowAnonymous)
│   └── _Smoke/
│       └── AuthenticationSmokeTests.cs            # Cenário US1 #1
│
├── **bruno/**                                     (novo — collection)
│   ├── bruno.json
│   ├── environments/
│   │   ├── local.bru
│   │   ├── dev.bru
│   │   └── prod.bru
│   ├── _Auth/
│   │   └── login.bru                              # POST /login (NAuth) + script pós-resposta
│   ├── Lotteries/
│   │   ├── create.bru
│   │   ├── get-by-id.bru
│   │   ├── get-by-slug.bru
│   │   ├── list-by-store.bru
│   │   ├── update.bru
│   │   ├── publish.bru
│   │   ├── close.bru
│   │   └── cancel.bru
│   ├── LotteryCombos/            ├── LotteryImages/
│   ├── Raffles/                  ├── RaffleAwards/
│   ├── Tickets/                  ├── Purchases/
│   ├── Referrals/                ├── Commissions/
│   ├── Refunds/                  └── Webhooks/
│
├── _Fortuno.sln_                                   (atualizada com os 2 csproj)
├── _.github/workflows/_
│   └── **coverage-check.yml** (novo)              # gate de 80% via Coverlet+ReportGenerator
└── _.gitignore_                                    (adicionado: appsettings.Tests.json, bruno/**/environments/*.secret.bru)
```

**Structure Decision**: layout *multi-project* (dois projetos de teste irmãos dos projetos de produção), consistente com o preset `dotnet-test-api` e já antecipado pela constituição (Clean Architecture). Nenhum novo projeto de domínio ou aplicação; nenhuma alteração em `Fortuno.API`, `Fortuno.Application`, `Fortuno.Domain`, `Fortuno.DTO`, `Fortuno.Infra`, `Fortuno.Infra.Interfaces`. A pasta `bruno/` vive na raiz por convenção do Bruno (collection = root dir).

## Post-Design Constitution Re-check

Pós-Phase 1 (research, data-model, contracts, quickstart), nenhuma nova violação surge:

- Nenhum endpoint novo foi adicionado à `Fortuno.API` (evita ativar dotnet-architecture).
- Nenhum mock de banco que contorne `NpgsqlDataSource`; unit tests usam Moq sobre interfaces (`IRepository<T>`, `IUserClient`, `IProxyPayAppService`).
- ApiTests consomem o NAuth real (Princípio V) e falham fast se segredos estiverem ausentes.

**Resultado**: ✓ **PASS** (pós-design).

## Complexity Tracking

> Não preenchido — sem violações de constituição.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
