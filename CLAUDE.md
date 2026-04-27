# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Convenções e princípios de força obrigatória estão em `.specify/memory/constitution.md` (Constituição Fortuno v1.0.0). Em caso de conflito, **a constituição prevalece**.

## Stack & Topologia

Backend único em **.NET 8 / C# 12** seguindo Clean Architecture, organizado em projetos `Fortuno.*` na raiz da solution `Fortuno.sln`:

```
Fortuno.API           ASP.NET Core (Controllers + GraphQL endpoint, DI bootstrap, Swagger)
Fortuno.GraphQL       HotChocolate — Query/Types (projections, filtering, sorting)
Fortuno.Application   Composição da DI (Startup.AddFortuno) — único lugar onde camadas são amarradas
Fortuno.Domain        Models (POCOs), Interfaces, Services, Enums — sem dependência de Infra
Fortuno.Infra         FortunoContext (EF Core 9 + Npgsql), Repositories, AppServices, Migrations
Fortuno.Infra.Interfaces  Contratos genéricos de IRepository<T>/IAppService consumidos pelo Domain
Fortuno.DTO           Contratos de I/O (Info / InsertInfo / UpdateInfo / Result), IOptions settings, Common (ApiResponse, UserContextExtensions)
Fortuno.Tests         Testes unitários xUnit + FluentAssertions + Moq (gate de cobertura ≥80% em CI)
Fortuno.ApiTests      Testes de integração HTTP via Flurl.Http (sem acesso direto a banco)
```

Fluxo de dependência: `API/GraphQL → Application → {Domain, Infra, DTO}; Domain → Infra.Interfaces, DTO; Infra → Domain, Infra.Interfaces`. **Nunca** referenciar `Fortuno.Infra` a partir de `Fortuno.Domain`.

Persistência: PostgreSQL com `Npgsql.EnableLegacyTimestampBehavior=true` (schema usa `timestamp without time zone` — ver `Program.cs:14`). Tabelas seguem o prefixo `fortuna_*` em `snake_case`. FKs nomeadas `fk_{pai}_{filho}`, `OnDelete(ClientSetNull)` por padrão.

Auth: NAuth Basic Token (`Authorization: Basic {token}` + header `X-Tenant-Id`). Toda rota com dado sensível **deve** ter `[Authorize]`. O `userId` é lido via `User.GetCurrentUserId()` (extension em `Fortuno.DTO.Common.UserContextExtensions`). O middleware `EnsureUserReferrerMiddleware` garante que todo usuário autenticado tenha um `UserReferrer` na primeira requisição.

Integrações externas (HTTP, sem persistência local): **NAuth** (usuários, ACL), **ProxyPay** (Stores, pagamentos PIX, webhooks via tabela `fortuna_ticket_orders`), **zTools** (upload S3, e-mail, slugs). Lacunas conhecidas dos parceiros estão em `docs/EXTERNAL_DEPS_INSTRUCTIONS.md`.

Resposta padrão de erro/sucesso usa `ApiResponse` com chaves portuguesas (`sucesso`, `mensagem`, `erros`); JSON do body em `camelCase` (configurado em `Program.cs`). DTOs **devem** anotar `[JsonPropertyName("camelCase")]` em todas as propriedades.

## Comandos

Restore/build/run da API:

```bash
dotnet restore Fortuno.sln
dotnet build Fortuno.sln -c Debug
dotnet run --project Fortuno.API
# Swagger disponível em https://localhost:{port}/swagger no env Development
# GraphQL em /graphql (Banana Cake Pop em dev)
```

Testes unitários (todo o `Fortuno.Tests`):

```bash
dotnet test Fortuno.Tests/Fortuno.Tests.csproj
```

Rodar **um único** teste/classe (xUnit usa filter `FullyQualifiedName`):

```bash
dotnet test Fortuno.Tests/Fortuno.Tests.csproj --filter "FullyQualifiedName~LotteryServiceTest"
dotnet test Fortuno.Tests/Fortuno.Tests.csproj --filter "FullyQualifiedName=Fortuno.Tests.Domain.Services.LotteryServiceTest.Create_ShouldReturnInfo"
```

Cobertura local (mesmo runsettings que CI usa, gate de 80% em `Fortuno.Domain` + `Application` + `Infra`, excluindo Migrations/Repository/Settings):

```bash
dotnet test Fortuno.Tests/Fortuno.Tests.csproj \
  --settings Fortuno.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage-raw
reportgenerator -reports:"./coverage-raw/**/coverage.cobertura.xml" -targetdir:./coverage-report -reporttypes:"TextSummary;HtmlSummary"
```

API tests (HTTP end-to-end contra uma API rodando — **não** sobe a API automaticamente, **não** acessa o banco):

```bash
# 1. Subir Fortuno.API localmente em outro terminal
# 2. Copiar Fortuno.ApiTests/appsettings.Tests.example.json -> appsettings.Tests.json
#    OU exportar FORTUNO_TEST_API_BASE_URL / FORTUNO_TEST_NAUTH_URL / NAUTH_TENANT / NAUTH_USER / NAUTH_PASSWORD / PROXYPAY_URL
dotnet test Fortuno.ApiTests/Fortuno.ApiTests.csproj
```

Migrations EF Core (executadas a partir da raiz; o startup do projeto API instancia o `FortunoContext`):

```bash
dotnet ef migrations add <NomeDaMigration> --project Fortuno.Infra --startup-project Fortuno.API
dotnet ef database update                    --project Fortuno.Infra --startup-project Fortuno.API
dotnet ef migrations remove                  --project Fortuno.Infra --startup-project Fortuno.API
```

Coleção HTTP manual: `bruno/` (Bruno collection — Lotteries, Tickets, Refunds, etc.).

## Restrições não negociáveis (do constitution)

- **Nunca** introduzir ORMs alternativos (Dapper, NHibernate, ...). EF Core 9.x é o único ORM permitido.
- **Nunca** executar `docker` ou `docker compose` no ambiente local — Docker não está acessível aqui (deploy de produção é via `.github/workflows/deploy-prod.yml` por SSH no servidor de prod, e somente lá).
- Schema PostgreSQL: tabelas/colunas em `snake_case`, PK `{entidade}_id bigint identity`, timestamps `timestamp without time zone`, `OnDelete(ClientSetNull)` (Cascade só onde já existe documentado em migrations específicas).
- DTOs com `[JsonPropertyName("camelCase")]`; respostas usam `ApiResponse` com chaves PT (`sucesso`, `mensagem`, `erros`).
- Controllers envolvem chamada de service em try/catch e devolvem `500` com `ex.Message` em caso de exceção não esperada (handler global em `Program.cs` é o fallback).
- `Cors.AllowAnyOrigin=true` **somente** em Development.
- DI **sempre** centralizada em `Fortuno.Application/Startup.cs`. Não registrar serviços direto em `Program.cs`.

## Skills (workflow obrigatório)

Criação/modificação de entidades, services, repositories, DTOs, migrations e DI **deve** invocar a skill `dotnet-architecture` (em `.claude/skills/dotnet-architecture/`) antes de produzir código. Para testes, usar `dotnet-test` (unitários em `Fortuno.Tests`) ou `dotnet-test-api` (integração em `Fortuno.ApiTests`). Outras skills locais relevantes: `dotnet-fluent-validation`, `dotnet-graphql`, `dotnet-multi-tenant`, `nauth-guide`, `ztools-guide`, `docker-compose-config`, `deploy-prod`.

## Spec-Kit

O fluxo de planejamento usa Spec-Kit (`/speckit.specify` → `/speckit.clarify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`). Specs vivem em `specs/{NNN-feature}/`. A constituição em `.specify/memory/constitution.md` é a fonte de verdade para revisões de PR — desvios precisam ser justificados em "Complexity Tracking" do plano.

A seção "Active Technologies" / "Recent Changes" abaixo é regenerada por `/speckit.plan` a partir dos planos. Não editar manualmente; conteúdo persistente para Claude vai entre os marcadores `MANUAL ADDITIONS`.

## Active Technologies
- C# 12 / .NET 8.0 (conforme constituição, Princípio II) (002-qa-test-suite)
- PostgreSQL (Fortuno) e ProxyPay (externo, para dados de Store). `Fortuno.ApiTests` **não** acessa o banco diretamente — somente chamadas HTTP contra `Fortuno.API` rodando. (002-qa-test-suite)
- C# 12 / .NET 8.0 (Constituição Fortuno §II) + ASP.NET Core 8 (MVC Controllers), Entity Framework Core 9.x + Npgsql, NAuth ACL (`IUserClient`), zTools (não alterado nesta feature), FluentValidation, Swashbuckle, HotChocolate (não alterado), Flurl.Http + xUnit + FluentAssertions (ApiTests) (003-ticket-qrcode-purchase)
- PostgreSQL (tabela nova `fortuna_ticket_orders`; tabelas existentes `fortuna_number_reservations`, `fortuna_tickets`, `fortuna_invoice_referrers` permanecem, webhook_events **DEPRECIA**) (003-ticket-qrcode-purchase)
- C# 12 / .NET 8.0 (001-lottery-saas)

## Recent Changes
- 003-ticket-qrcode-purchase: Added C# 12 / .NET 8.0 (Constituição Fortuno §II) + ASP.NET Core 8 (MVC Controllers), Entity Framework Core 9.x + Npgsql, NAuth ACL (`IUserClient`), zTools (não alterado nesta feature), FluentValidation, Swashbuckle, HotChocolate (não alterado), Flurl.Http + xUnit + FluentAssertions (ApiTests)
- 002-qa-test-suite: Added C# 12 / .NET 8.0 (conforme constituição, Princípio II)
- 001-lottery-saas: Added C# 12 / .NET 8.0

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
