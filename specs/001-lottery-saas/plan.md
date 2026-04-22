# Implementation Plan: Plataforma SaaS de Sorteios Online (Fortuno)

**Branch**: `001-lottery-saas` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-lottery-saas/spec.md`

## Summary

Plataforma SaaS Fortuno para criação, venda e sorteio de loterias online.
Integra-se a três microserviços externos:

- **NAuth** (identidade, tenant `fortuna`) via NuGet NAuth + Basic auth.
- **ProxyPay** (Stores + cobrança PIX, tenant `fortuna`) via HTTP (sem NuGet);
  confirmação de pagamento por webhook idempotente.
- **zTools** (upload S3, IA, geração de PDF a partir de Markdown) via NuGet zTools.

O Fortuno é **single-tenant interno** — todas as tabelas criadas por ele usam
prefixo `fortuna_`. A API expõe REST (Swashbuckle) para mutações e
HotChocolate GraphQL para consultas. O sistema **não movimenta valores
financeiros**: comissões de indicação e estornos são apenas calculados/
exibidos e liquidados off-platform pelo dono da Lottery.

Arquitetura segue **Clean Architecture** conforme skill `dotnet-architecture`
da constituição, com 6 projetos backend + 1 GraphQL + 1 test.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0
**Primary Dependencies**:
- Entity Framework Core 9.x (Npgsql.EntityFrameworkCore.PostgreSQL)
- HotChocolate.AspNetCore (GraphQL, via skill `dotnet-graphql`)
- NAuth (pacote NuGet, última versão) — autenticação Basic + `IUserClient`
- zTools (pacote NuGet, última versão) — upload S3, IA, slugs, PDF/Markdown
- Swashbuckle.AspNetCore 8.x — Swagger/OpenAPI para os endpoints REST
- FluentValidation (via skill `dotnet-fluent-validation`) — validação de DTOs
- xUnit + FluentAssertions (via skill `dotnet-test`) — testes unitários
- Markdig — conversão Markdown → HTML para geração de PDF

**Storage**: PostgreSQL; todas as tabelas com prefixo `fortuna_` (ex.:
`fortuna_lotteries`, `fortuna_tickets`, `fortuna_user_referrers`,
`fortuna_invoice_referrers`, `fortuna_refund_logs`).

**Testing**: xUnit com projeto `Fortuno.Tests` espelhando a estrutura de
pastas das camadas (Domain, Application, Infra, API). Testes de integração
usam banco PostgreSQL real por instrução da constituição (sem mock de DB
na camada de integração).

**Target Platform**:
- Ambientes lógicos: `Development` (local Windows), `Docker` (container de
  homologação), `Production` (Linux server). Configurado via skill
  `dotnet-env`.
- Runtime: ASP.NET Core 8 Kestrel.

**Project Type**: Web service (API REST + GraphQL) + integrações HTTP com
3 microserviços. Sem frontend neste escopo.

**Performance Goals** (derivados dos Success Criteria da spec):
- SC-003: Tickets aparecem em "Meus Tickets" em até 30s após confirmação PIX.
- SC-007: GraphQL `listLotteries(status: Open)` p95 ≤ 1s com 1k loterias.
- SC-008: Registrar até 100 RaffleWinners em ≤ 5s após confirmação da prévia.
- SC-009/010: Painéis de comissão refletem mudanças em ≤ 30s (cálculo em
  tempo real, sem persistência da comissão).

**Constraints**:
- Docker **não executar** localmente (constituição §1).
- Sistema **não movimenta valores** — FR-R10, FR-033b, SC-011.
- Webhook do ProxyPay DEVE ser idempotente por `InvoiceId` (FR-029b).
- Commit-free para alterar ProxyPay, NAuth e zTools (skills separadas).
- Todos os DTOs com `[JsonPropertyName("camelCase")]` (constituição §2).
- Foreign keys com `DeleteBehavior.ClientSetNull` (constituição §3).

**Scale/Scope**:
- Backend único.
- Múltiplas lojas, múltiplas loterias (uma `Lottery` = N Raffles = 1 pool
  compartilhado de Tickets).
- Pool máximo projetado: int64 ou composição de até 8 dígitos (10^16 casos).

## Constitution Check

*GATE: deve passar antes da Phase 0 e após Phase 1.*

| # | Princípio | Status | Comentário |
|---|-----------|--------|------------|
| I | Skills obrigatórias | ✅ | `dotnet-architecture` invocada no plano; `dotnet-env`, `dotnet-graphql`, `nauth-guide`, `ztools-guide`, `dotnet-fluent-validation`, `dotnet-test` serão invocadas na fase de execução das tarefas correspondentes. |
| II | Stack fixa | ✅ | .NET 8, EF Core 9, PostgreSQL, NAuth, zTools, Swashbuckle 8 — **nenhum** ORM alternativo; **nenhum** comando Docker local. |
| III | Convenções de Código .NET | ✅ | PascalCase, file-scoped namespaces, `_camelCase`, `[JsonPropertyName("camelCase")]` em todos os DTOs. |
| IV | Convenções de Banco PostgreSQL | ✅ | Prefixo `fortuna_`, snake_case, `{entidade}_id bigint identity`, FK `fk_{pai}_{filho}`, `ClientSetNull`, `timestamp without time zone`, `varchar(MaxLength)`. |
| V | Autenticação NAuth | ✅ | `NAuthHandler` no DI, Basic auth, `[Authorize]` em controllers sensíveis, `FR-045a` restringe escrita ao proprietário da Store. |
| Restrições adicionais | Env vars obrigatórias | ✅ | `ConnectionStrings__FortunoContext`, `ASPNETCORE_ENVIRONMENT` configuradas via `dotnet-env`. |
| Tratamento de Erros | try/catch 500 | ✅ | Padrão aplicado em todos os controllers REST. GraphQL usa logging via skill `dotnet-graphql`. |

**Gates adicionais derivados dos princípios**:

- [x] Nenhuma entidade backend nova é criada sem invocar `dotnet-architecture`.
- [x] Tabelas e colunas seguem `snake_case` com prefixo `fortuna_`.
- [x] Controllers com dados sensíveis possuem `[Authorize]`; escrita
      restrita ao proprietário da Store.
- [x] Nenhum chamada de pagamento/estorno disparada pelo sistema (SC-011).

**Resultado**: PASS — sem violações. Seção "Complexity Tracking" fica vazia.

## Project Structure

### Documentation (this feature)

```text
specs/001-lottery-saas/
├── plan.md              # Este arquivo
├── research.md          # Phase 0 (decisões e alternativas avaliadas)
├── data-model.md        # Phase 1 (entidades, campos, relacionamentos, estados)
├── quickstart.md        # Phase 1 (setup local + execução end-to-end)
├── contracts/           # Phase 1 (REST OpenAPI + GraphQL SDL + Webhook)
│   ├── rest-openapi.yaml
│   ├── graphql.schema.graphql
│   └── webhook-proxypay.md
├── checklists/
│   └── requirements.md  # gerado no /speckit.specify
└── tasks.md             # gerado no /speckit.tasks (fase seguinte)
```

### Source Code (repository root)

Estrutura de 8 projetos em uma única solution `Fortuno.sln`, seguindo
o layout canônico da skill `dotnet-architecture`:

```text
Fortuno.sln
src/
├── Fortuno.DTO/                        # Data contracts, IOptions
│   ├── Lottery/
│   │   ├── LotteryInfo.cs
│   │   ├── LotteryInsertInfo.cs
│   │   └── LotteryUpdateInfo.cs
│   ├── LotteryImage/
│   ├── LotteryCombo/
│   ├── Ticket/
│   ├── Raffle/
│   ├── RaffleAward/
│   ├── RaffleWinner/
│   ├── UserReferrer/
│   ├── InvoiceReferrer/
│   ├── ReferralEarning/
│   ├── RefundLog/
│   ├── Purchase/                       # PurchaseRequestInfo, PurchasePreviewInfo
│   ├── Webhook/                        # ProxyPayWebhookPayload
│   └── Settings/                       # FortunoSettings, NAuthSettings, ProxyPaySettings, ZToolsSettings
│
├── Fortuno.Infra.Interfaces/           # Repository + AppService contracts (generics)
│   ├── Repository/
│   │   ├── ILotteryRepository.cs
│   │   ├── ITicketRepository.cs
│   │   └── ... (1 interface por entidade local)
│   └── AppServices/
│       ├── INAuthAppService.cs         # Wrapper sobre IUserClient
│       ├── IProxyPayAppService.cs      # Stores, Invoices, Webhook verify
│       └── IZToolsAppService.cs        # Upload S3, PDF, slug
│
├── Fortuno.Domain/                     # Models, Services, Enums, Interfaces
│   ├── Models/
│   │   ├── Lottery.cs
│   │   ├── LotteryImage.cs
│   │   ├── LotteryCombo.cs
│   │   ├── Ticket.cs
│   │   ├── Raffle.cs
│   │   ├── RaffleAward.cs
│   │   ├── RaffleWinner.cs
│   │   ├── UserReferrer.cs
│   │   ├── InvoiceReferrer.cs
│   │   └── RefundLog.cs
│   ├── Enums/
│   │   ├── LotteryStatus.cs            # Draft, Open, Closed, Cancelled
│   │   ├── RaffleStatus.cs             # Open, Closed, Cancelled
│   │   ├── TicketRefundState.cs        # None, PendingRefund, Refunded
│   │   ├── NumberType.cs               # Int64, Composed3, ... Composed8
│   │   └── TicketOrderMode.cs   # Random, UserPicks
│   ├── Interfaces/
│   │   ├── ILotteryService.cs
│   │   ├── IPurchaseService.cs
│   │   ├── IRaffleService.cs
│   │   ├── IReferralService.cs
│   │   ├── IRefundService.cs
│   │   └── INumberCompositionService.cs
│   └── Services/
│       ├── LotteryService.cs
│       ├── PurchaseService.cs
│       ├── RaffleService.cs
│       ├── ReferralService.cs          # Cálculo em tempo real
│       ├── RefundService.cs            # Gestão de status
│       ├── NumberCompositionService.cs # int64 ↔ componentes; cálculo de possibilidades
│       └── SlugService.cs
│
├── Fortuno.Infra/                      # DbContext, Repositories, AppServices
│   ├── Context/
│   │   └── FortunoContext.cs
│   ├── Migrations/
│   ├── Repository/
│   │   ├── LotteryRepository.cs
│   │   └── ... (1 por interface)
│   └── AppServices/
│       ├── NAuthAppService.cs
│       ├── ProxyPayAppService.cs
│       └── ZToolsAppService.cs
│
├── Fortuno.Application/                # Startup (DI centralizado), cross-cutting
│   └── Startup.cs
│
├── Fortuno.GraphQL/                    # Schema HotChocolate (via dotnet-graphql)
│   ├── Queries/
│   │   ├── LotteryQueries.cs
│   │   ├── RaffleQueries.cs
│   │   └── TicketQueries.cs
│   ├── TypeExtensions/
│   │   ├── LotteryTypeExtensions.cs    # Campos computados (ex.: availableTickets)
│   │   └── RaffleTypeExtensions.cs
│   └── Startup.cs                      # AddGraphQLServer()
│
└── Fortuno.API/                        # ASP.NET Core Web API (presentation)
    ├── Program.cs
    ├── Controllers/
    │   ├── LotteriesController.cs
    │   ├── LotteryImagesController.cs
    │   ├── LotteryCombosController.cs
    │   ├── RafflesController.cs
    │   ├── RaffleAwardsController.cs
    │   ├── RaffleWinnersController.cs
    │   ├── PurchasesController.cs      # Preview + confirm + UserPicks reserve
    │   ├── TicketsController.cs        # Meus tickets + pesquisa
    │   ├── ReferralsController.cs      # Painel do indicador
    │   ├── CommissionsController.cs    # Painel do dono da Lottery
    │   ├── RefundsController.cs        # UI de status refund
    │   └── WebhooksController.cs       # /webhooks/proxypay (FR-029a)
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── appsettings.Docker.json
    └── appsettings.Production.json

tests/
└── Fortuno.Tests/                      # via skill dotnet-test
    ├── Domain/
    │   ├── Services/
    │   │   ├── NumberCompositionServiceTests.cs
    │   │   ├── PurchaseServiceTests.cs
    │   │   ├── ReferralServiceTests.cs
    │   │   └── RaffleServiceTests.cs
    │   └── Models/
    ├── Application/
    ├── Infra/
    │   └── Repository/
    └── API/
        └── Controllers/

.github/workflows/                      # via skill deploy-prod (fase futura)
docker-compose.yml                      # via skill dotnet-env (não executado local)
docker-compose.prod.yml
.env.example
.env.prod.example
```

**Structure Decision**: projeto único backend em Clean Architecture .NET
(Option 1 do template, adaptada para a skill `dotnet-architecture`). Sem
frontend nesta iteração; a API serve REST (operações) + GraphQL
(consultas) diretamente.

## Complexity Tracking

Nenhuma violação de constituição a justificar. Seção intencionalmente vazia.
