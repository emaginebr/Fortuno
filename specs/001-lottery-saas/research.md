# Research & Decisions — 001-lottery-saas

**Date**: 2026-04-17
**Input**: [spec.md](./spec.md), [plan.md](./plan.md), constituição do projeto

Este documento consolida as decisões técnicas e os padrões adotados antes do
design detalhado. Todos os pontos de `NEEDS CLARIFICATION` do contexto
técnico foram resolvidos.

---

## 1. Arquitetura backend — Clean Architecture (skill `dotnet-architecture`)

- **Decisão**: seguir estritamente o layout da skill: 6 projetos backend
  (`DTO`, `Infra.Interfaces`, `Domain`, `Infra`, `Application`, `API`) + 1
  projeto GraphQL + 1 projeto de testes.
- **Rationale**: constituição §I torna a skill **não-negociável**; garante
  consistência com outros projetos da organização e evita retrabalho.
- **Alternativas consideradas**: Vertical Slice Architecture (rejeitada por
  divergir da constituição); Single-project Minimal API (rejeitada pela
  falta de separação entre Domain e Infra).

---

## 2. Integração com NAuth (skill `nauth-guide`)

- **Decisão**: incluir pacote NuGet NAuth na última versão; registrar
  `NAuthHandler` no DI via `AddNAuth(...)`; usar `IUserClient` para
  operações de usuário (get-by-id, list-by-ids); todas as rotas sensíveis
  decoradas com `[Authorize]`. O tenant Fortuno é **sempre** `"fortuna"`.
- **Rationale**: padrão do ecossistema; autenticação Basic + token; NÃO
  reimplementar usuário/senha localmente.
- **Sem modificação no NAuth**: se a integração expuser uma lacuna
  (ex.: ausência de endpoint `GET /users/by-ids`), o plano registra a
  necessidade em uma seção "Instruções para o NAuth" (ver §9) sem alterar
  o repositório de NAuth.
- **Alternativas consideradas**: ASP.NET Identity local (rejeitada — viola
  a diretiva de reutilizar o microserviço); IdentityServer/Keycloak
  (rejeitadas — fora do stack aprovado).

---

## 3. Integração com ProxyPay (HTTP, sem NuGet)

- **Decisão**: implementar `ProxyPayAppService` em `Fortuno.Infra/AppServices`
  que encapsula os endpoints do ProxyPay via `HttpClient` tipado. Operações:
  - `CreateInvoice(storeId, amount, items, metadata)` — gera cobrança PIX
    retornando `InvoiceId` e QRCode.
  - `GetInvoice(invoiceId)` — consulta estado (usada apenas para
    reconciliação sob demanda pelo dono, não para polling).
  - `GetStore(storeId)` — valida que a Store existe e que o User
    autenticado é o proprietário.
- **Webhook**: o Fortuno expõe `POST /webhooks/proxypay/invoice-paid`
  autenticado por segredo compartilhado (HMAC em header
  `X-ProxyPay-Signature`).
- **Idempotência**: FR-029b — deduplicação por `InvoiceId` usando uma tabela
  `fortuna_webhook_events` (InvoiceId + EventType UNIQUE). Segundo
  recebimento retorna 200 OK sem processar.
- **Tenant**: sempre `"fortuna"`. Como o ProxyPay ainda não tem multi-tenant,
  esta é uma projeção futura — registrar em §9.
- **Rationale**: sem NuGet disponível, `HttpClient` + DTOs próprios é o
  padrão limpo. Webhook é o canal único pelo Q1 da clarificação.
- **Alternativas consideradas**: polling (rejeitada pela latência); fila
  SNS/SQS entre ProxyPay e Fortuno (rejeitada — overkill nesta fase).

---

## 4. Integração com zTools (skill `ztools-guide`)

- **Decisão**: incluir pacote NuGet zTools; registrar serviços via
  `AddZTools(...)` e consumir através de um `ZToolsAppService` wrapper em
  `Fortuno.Infra/AppServices` que expõe apenas o que o Fortuno precisa:
  - Upload de imagens (LotteryImage, RaffleVideo) → S3, retorna URL.
  - Upload de PDFs gerados a partir de Markdown (regras e política de
    privacidade da Lottery) → S3, retorna URL.
  - Geração de slug único para Lottery (uso interno no `SlugService`, com
    verificação de unicidade global via repositório).
  - IA (opcional nesta fase) para sugestão de descrição/regras — escopo
    futuro, não bloqueante.
- **Rationale**: centraliza S3, email, slugs, IA. Alinhado à constituição.
- **Alternativas consideradas**: AWS SDK direto (rejeitada — duplicação);
  MinIO local (rejeitada — Docker proibido no dev).

---

## 5. GraphQL — HotChocolate (skill `dotnet-graphql`)

- **Decisão**: expor um schema único (`Fortuno.GraphQL`) para consulta de
  Lottery, LotteryImage, LotteryCombo, Raffle, RaffleAward, RaffleWinner e
  Ticket do usuário autenticado. Queries retornam `IQueryable<T>` para
  permitir `UseProjection`, `UseFiltering`, `UseSorting` pela otimização do
  HotChocolate. Campos computados (ex.: `availableTickets`, `soldTickets`,
  `referralEarnings`) via **type extensions**. Campos sensíveis
  (ex.: CPF do ganhador) ficam ocultos via `[GraphQLIgnore]` + `TypeExtension`
  que retorna versão mascarada.
- **Rationale**: HotChocolate é o framework indicado pela skill; projeção
  direta sobre EF Core garante SC-007 (p95 ≤ 1s).
- **Alternativas consideradas**: GraphQL.NET (rejeitada — fora da skill);
  REST-only (rejeitada — o usuário pediu GraphQL explicitamente).

---

## 6. Ambientes (skill `dotnet-env`)

- **Decisão**: 3 ambientes lógicos — `Development`, `Docker`, `Production` —
  cada um com `appsettings.{Env}.json` próprio e variáveis obrigatórias
  `ConnectionStrings__FortunoContext` e `ASPNETCORE_ENVIRONMENT`. Arquivo
  `docker-compose.yml` e `docker-compose.prod.yml` gerados pela skill, mas
  **não executados** localmente (constituição). Pipeline de deploy via
  skill `deploy-prod` em fase posterior.
- **Rationale**: atende obrigatoriedade da constituição §5 e a instrução
  explícita do usuário.
- **Alternativas consideradas**: 2 ambientes apenas (rejeitada — usuário
  pediu 3 explicitamente).

---

## 7. Modelo de atribuição de números (`Random` / `UserPicks`)

- **Decisão**: o modo é atributo **da compra**, não da Lottery (Q1 do
  2026-04-16). Implementação:
  - `Random`: no handler do webhook, gera N números aleatórios do pool
    ainda-não-vendidos (composto ou int64 direto) em uma única transação;
    concorrência tratada por `SERIALIZABLE` ou `SELECT ... FOR UPDATE` no
    pool.
  - `UserPicks`: o comprador seleciona os números antes do PIX; uma tabela
    `fortuna_number_reservations` mantém `(LotteryId, Number, UserId,
    ExpiresAt)` com unique constraint em `(LotteryId, Number)` filtrada
    por `ExpiresAt > now()`. TTL **15 minutos** (Q do 2026-04-17).
    Expiração pode ser revisada via job de limpeza ou
    `WHERE expires_at > now()` em cada seleção — o plano usa lazy
    expiration (sem job).
- **Rationale**: evita duplicação e over-sell; atende Q1 e Q4 das
  clarificações.
- **Alternativas consideradas**: bloqueio otimista por `RowVersion` (rejeitada
  — não previne over-sell no Random em alta concorrência).

---

## 8. Cálculo em tempo real da comissão de indicação

- **Decisão**: `ReferralService.GetEarningsForReferrer(userId)` e
  `.GetPayablesForLottery(lotteryId)` são métodos que executam uma única
  query agregada sobre `fortuna_invoice_referrers JOIN fortuna_tickets`
  filtrando `refund_state != 'Refunded'`, agrupando por Invoice e aplicando
  `Lottery.ReferralPercent / 100` no momento da consulta.
- **Rationale**: Q do 2026-04-17 mandou **não persistir**. Volatilidade de
  `ReferralPercent` é aceitável pois o sistema não promete valores.
- **Alternativas consideradas**: materialização em `fortuna_referral_earnings`
  com reversão (rejeitada — explicitamente negada pela clarificação);
  `MATERIALIZED VIEW` do Postgres (rejeitada — volatilidade do percentual
  quebra a premissa de frescor).

---

## 9. Instruções para microserviços externos (sem modificar)

Durante a implementação, se alguma das lacunas abaixo se confirmar, o plano
**NÃO** altera os repositórios `NAuth`, `ProxyPay` ou `zTools` —
registramos a necessidade aqui e pedimos ao usuário as instruções.

**ProxyPay — lacunas conhecidas**:
1. Ausência de tenant ⇒ precisa implementar tenant `"fortuna"` antes que o
   Fortuno entre em produção (mencionado na spec).
2. Endpoint de webhook de confirmação PIX com assinatura HMAC — se ainda
   não existir, solicitar implementação no ProxyPay.
3. Endpoint `GET /stores/{id}` que retorne também o `OwnerUserId` — para
   checagem de autorização (FR-045a).

**NAuth — lacunas prováveis**:
1. `IUserClient.GetByIdsAsync(IEnumerable<long>)` para buscar ganhadores em
   lote na tela de sorteio e para composição dos painéis de indicação.
2. Campo `documentId` (CPF) no UserInfo — precisa confirmar se já existe;
   caso não, solicitar adição.

**zTools — lacunas prováveis**:
1. Método `GeneratePdfFromMarkdown(string, PdfOptions)` na interface
   pública — se não existir, solicitar.

Essas pendências **não bloqueiam** o design — são sinalizadas nas
respectivas contract/tasks e surgem durante a execução.

---

## 10. Estratégia de testes (skill `dotnet-test`)

- **Decisão**: xUnit + FluentAssertions, estrutura espelhando pastas de
  origem. Testes unitários puros para `Domain/Services` (mock de
  repositórios via Moq). Testes de integração para repositórios +
  `FortunoContext` usando PostgreSQL real (Testcontainers é opção, mas como
  Docker está proibido no dev local, o plano usará **um banco dedicado
  local** ou o `Development` DB com `[Fact(Skip=...)]` quando indisponível).
- **Rationale**: alinhado à skill.
- **Alternativas consideradas**: EF InMemory (rejeitada — não valida
  migrations nem SQL específico do Postgres).

---

## 11. Observabilidade mínima

- **Decisão**: `ILogger<T>` em todos os services; correlação via header
  `X-Request-Id` lido pelo middleware e propagado ao `LogContext`. Sem
  APM (Datadog/NewRelic) nesta fase. Métricas futuras pela skill
  `deploy-prod`.
- **Rationale**: deferido no `/speckit.clarify` como planning-level;
  abordagem mínima viável.

---

## 12. Migrations e versionamento de schema

- **Decisão**: uma única migration inicial `InitialSchema` cria todas as
  tabelas com prefixo `fortuna_`. Migrations subsequentes devem ser
  incrementais e nunca editar migrations já aplicadas. Comando:

  ```bash
  dotnet ef migrations add InitialSchema --project Fortuno.Infra --startup-project Fortuno.API
  dotnet ef database update --project Fortuno.Infra --startup-project Fortuno.API
  ```

- **Rationale**: padrão EF Core; compatível com a constituição.
- **Alternativas consideradas**: FluentMigrator (rejeitada — stack fixa
  manda EF Core).

---

## 13. Validação de input (skill `dotnet-fluent-validation`)

- **Decisão**: FluentValidation para cada DTO de mutação
  (`LotteryInsertInfo`, `PurchaseRequestInfo`, etc.). Pipeline integrado
  ao ASP.NET Core e chamado antes da service layer. Erros retornam 400
  com chaves em português (`sucesso`, `mensagem`, `erros`) conforme
  constituição.
- **Rationale**: constituição §I menciona a skill; padrão da organização.

---

## Resumo executivo

Todas as decisões acima seguem a constituição e a spec. Nenhuma requer
aprovação adicional do usuário. Itens pendentes (§9) são instruções para
equipes de microserviços parceiros, **não** mudanças no Fortuno — serão
repassadas ao usuário quando a fase de tasks exigir.
