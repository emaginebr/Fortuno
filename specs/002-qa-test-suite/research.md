# Phase 0 — Research

**Feature**: 002-qa-test-suite
**Date**: 2026-04-18

Esta fase resolve os pontos ainda abertos após `/speckit.clarify` e registra as decisões que sustentam o plan. Cada seção segue o formato *Decision / Rationale / Alternatives considered*.

---

## R-001 — Provisionamento de Store (achado crítico)

**Pergunta**: Como a fixture dos ApiTests obtém um `StoreId` válido para o `POST /api/lotteries`?

### v2 (atual) — descoberta via GraphQL do ProxyPay

**Decision**: A fixture resolve o `StoreId` em tempo de execução consultando `POST {proxyPayUrl}/graphql` com a query `{ myStore { storeId } }`, autenticada com o token NAuth emitido no login. **Nenhuma variável `StoreId` é configurada externamente.**

**Rationale**: O ProxyPay expõe a query GraphQL `myStore` que retorna a Store do usuário autenticado (escopo implícito pelo token + header `X-Tenant-Id`). Isso elimina o acoplamento a `FORTUNO_TEST_STORE_ID` e garante que a suite sempre opere sobre a Store real do usuário configurado, sem risco de ID desatualizado ou vazado em configuração.

A fixture faz:

1. Ler `FORTUNO_TEST_PROXYPAY_URL` + credenciais NAuth (`FORTUNO_TEST_NAUTH_*`).
2. Logar no NAuth → obtém token.
3. `POST {proxyPayUrl}/graphql` com `Authorization: Basic {token}` + `X-Tenant-Id: {tenant}` + body `{ "query": "{ myStore { storeId } }" }`.
4. Se a resposta vier com `errors` não-vazio ou `data.myStore == null`, falhar com mensagem acionável ("usuário de teste precisa ter uma Store associada no ProxyPay").
5. Logar o `storeId` descoberto para rastreabilidade.

**Env vars obrigatórias** (atualizadas): `FORTUNO_TEST_API_BASE_URL`, `FORTUNO_TEST_NAUTH_URL`, `FORTUNO_TEST_NAUTH_TENANT`, `FORTUNO_TEST_NAUTH_USER`, `FORTUNO_TEST_NAUTH_PASSWORD`, `FORTUNO_TEST_PROXYPAY_URL`. ~~`FORTUNO_TEST_STORE_ID`~~ **removida**.

**Alternatives considered (nesta versão)**:

- **Manter `FORTUNO_TEST_STORE_ID` como fallback opcional**: rejeitado — duas formas de configurar a mesma coisa aumenta a superfície de bug. GraphQL sempre disponível resolve sozinho.
- **Fazer a query uma vez por teste**: rejeitado — overhead. Resolução no `InitializeAsync` da fixture (uma vez por execução) é suficiente.

**Implementação**: `Fortuno.ApiTests/_Fixtures/ProxyPayStoreResolver.cs` encapsula a query. `ApiSessionFixture.InitializeAsync` chama o resolver após login.

### v1 (obsoleta) — pré-provisionado via env var

**Decision original**: `StoreId` pré-provisionado no ProxyPay e lido de `FORTUNO_TEST_STORE_ID`. A fixture não criava Stores.

**Rationale original**: Na época, não se conhecia o endpoint GraphQL `myStore` do ProxyPay. As opções consideradas (criar Store via API Fortuno, seed direto no DB, criar via API admin do ProxyPay, mockar `IProxyPayAppService`) eram todas inviáveis. A única opção era reutilizar Store existente via configuração externa.

**Motivo da substituição**: descoberta do endpoint `{ myStore { storeId } }` no GraphQL do ProxyPay tornou a resolução automática viável, eliminando o acoplamento de configuração.

**Impacto no spec**: A-005, FR-023 e Clarifications Q4 foram atualizados para refletir a descoberta dinâmica.

---

## R-002 — Endpoint de login NAuth

**Pergunta**: Qual é a forma da chamada de login do NAuth para a fixture e para a Bruno collection?

**Decision**: `POST {nauthUrl}/auth/login` com body JSON `{ "tenant": "{tenant}", "user": "{user}", "password": "{password}" }`, retornando um objeto que contém o token a ser usado como `Authorization: Basic {token}`.

**Rationale**: A constituição (Princípio V) e a skill `nauth-guide` do repositório estabelecem o uso de **Basic Authentication via NAuth** com header `Authorization: Basic {token}` — ou seja, o token obtido no login **é o próprio valor Basic** (já codificado/embalado pelo NAuth). A suite e a collection nunca montam Basic localmente a partir de `user:password`; recebem o token pronto e o usam.

A fixture centraliza login em `ApiSessionFixture : IAsyncLifetime` (preset `dotnet-test-api`). Uma única chamada por execução. Token cacheado na instância Flurl.

**Alternatives considered**:

- **Chamar NAuth a cada teste**: rejeitado — overhead, risco de rate limit, viola o preset `dotnet-test-api`.
- **Mockar NAuth via WireMock**: rejeitado — o objetivo declarado é exercitar autenticação real contra NAuth (FR-014).

**Nota operacional**: A URL exata do endpoint (`/auth/login` vs `/login` vs outro) deve ser confirmada contra a versão do pacote `NAuth 0.5.x` usada em `Fortuno.API/Program.cs` durante a implementação — se divergir, ajustar apenas a constante no `TestSettings`/`ApiSessionFixture`.

---

## R-003 — Excludes de cobertura (Coverlet)

**Pergunta**: Como aplicar FR-011 (excluir `Program.cs`, `Startup.cs`, `DbContext`, migrations, Settings sem lógica)?

**Decision**: Configurar Coverlet via arquivo `coverlet.runsettings` na raiz do `Fortuno.Tests`:

```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[Fortuno.API]Fortuno.API.Program,[Fortuno.Application]Fortuno.Application.Startup,[*]Fortuno.Infra.Context.*,[*]*.Migrations.*,[*]*Settings</Exclude>
          <ExcludeByFile>**/Migrations/*.cs,**/Program.cs,**/Startup.cs</ExcludeByFile>
          <SingleHit>false</SingleHit>
          <UseSourceLink>false</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

Execução: `dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`. ReportGenerator consolida e extrai a métrica de linhas do conjunto `Fortuno.Domain + Fortuno.Application + Fortuno.Infra` (via `assemblyfilters: +Fortuno.Domain;+Fortuno.Application;+Fortuno.Infra`).

**Rationale**: formato Cobertura é padrão consumido por GitHub Actions e pela maioria das ferramentas. Exclude de `*Settings` é uma aproximação segura — POCOs de settings não têm lógica. Exclude por caminho captura migrations regeneráveis.

**Alternatives considered**:

- **Exclude via atributo `[ExcludeFromCodeCoverage]` no código**: rejeitado — pede modificação dos projetos de produção; a constituição desencoraja mudanças de produção para atender testes.
- **Coverage por projeto isolado**: rejeitado por clarify (Q3 → Option A).

---

## R-004 — Fixture compartilhada com Flurl.Http

**Pergunta**: Como implementar autenticação uma única vez por execução (FR-014)?

**Decision**: `ApiSessionFixture : IAsyncLifetime` registrada como `ICollectionFixture` (xUnit). Todas as classes de teste pertencentes ao collection `[Collection("api")]` reutilizam a mesma instância de `FlurlClient` com cabeçalho `Authorization` pré-configurado.

```csharp
public sealed class ApiSessionFixture : IAsyncLifetime
{
    public FlurlClient Client { get; private set; } = default!;
    public long StoreId { get; private set; }

    public async Task InitializeAsync()
    {
        var settings = TestSettings.FromEnvironment();               // fail-fast se faltam env vars
        var token = await NAuthLogin(settings);                      // POST /auth/login
        Client = new FlurlClient(settings.ApiBaseUrl)
            .WithHeader("Authorization", $"Basic {token}");
        StoreId = await ValidateStoreOwnership(Client, settings);    // R-001
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

**Rationale**: `ICollectionFixture` é o mecanismo canônico em xUnit para estado compartilhado. `IAsyncLifetime` permite setup `async` (chamada HTTP para NAuth). Flurl.Http 4.x permite compartilhar `FlurlClient` com segurança entre threads.

**Alternatives considered**:

- **`IClassFixture`**: rejeitado — faria login uma vez *por classe* de teste, violando FR-014.
- **Singleton estático**: rejeitado — dificulta debugging e não coopera com descoberta de testes.

---

## R-005 — Bruno: captura de token via pós-resposta

**Pergunta**: Como a request de login armazena o `accessToken` para demais requests (FR-004)?

**Decision**: Usar o script `post-response` embutido em Bruno (`.bru` texto) com `bru.setVar("accessToken", res.body.token)`. Demais requests referenciam `{{accessToken}}` no header. Exemplo do arquivo `_Auth/login.bru`:

```text
meta {
  name: Login
  type: http
  seq: 1
}

post {
  url: {{nauthUrl}}/auth/login
  body: json
  auth: none
}

body:json {
  {
    "tenant": "{{nauthTenant}}",
    "user": "{{nauthUser}}",
    "password": "{{nauthPassword}}"
  }
}

script:post-response {
  bru.setVar("accessToken", res.body.token);
}
```

Todas as demais requests declaram:

```text
headers {
  Authorization: Basic {{accessToken}}
}
```

**Rationale**: é o idioma oficial do Bruno para captura de estado. Arquivos `.bru` são texto versionado (revisáveis em PR — Acceptance Scenario US3.4).

**Alternatives considered**:

- **Pre-request script em cada request**: rejeitado — duplicação, difícil de manter.
- **Variável de ambiente injetada manualmente**: rejeitado — quebra o acceptance scenario US3.3.

---

## R-006 — Gate de cobertura no GitHub Actions

**Pergunta**: Como FR-020 e SC-007 são implementados em CI?

**Decision**: Novo workflow `.github/workflows/coverage-check.yml` com 3 steps:

1. `dotnet test Fortuno.Tests --settings coverlet.runsettings --collect:"XPlat Code Coverage"`.
2. `reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:TextSummary -assemblyfilters:"+Fortuno.Domain;+Fortuno.Application;+Fortuno.Infra"`.
3. Shell step que lê `coverage-report/Summary.txt`, extrai o percentual de *Line coverage*, compara com `80` e falha (`exit 1`) se menor.

Trigger: `on: [pull_request, push]` para `main` e `002-*`.

ApiTests **não** rodam neste workflow — por FR-021 são manuais / job sob demanda com segredos.

**Rationale**: `ReportGenerator` + comparação de shell é suficiente, sem dependência de ferramenta SaaS de cobertura. Mantém o gate no repositório, sem custo adicional.

**Alternatives considered**:

- **Codecov / Coveralls**: rejeitado — introduz dependência externa e segredos extras; a equipe pode migrar depois se quiser dashboards.
- **Coverlet MSBuild com `/p:Threshold=80`**: rejeitado — threshold é por assembly, não agregado, e não permite excludes por arquivo da forma como precisamos.

---

## R-007 — Idempotência e dados residuais

**Pergunta**: Como garantir execuções consecutivas sem interferência (FR-017)?

**Decision**: Todo identificador gerado pela suite recebe sufixo `-{yyyyMMddHHmmss}-{guid8}`, centralizado em `UniqueId.New(prefix)`. Exemplo: `"test-lottery-20260418153042-a3f1c900"`. Sem limpeza agressiva; o DB acumula Lotteries órfãs de teste, o que é aceitável em ambiente de teste e deliberadamente não é responsabilidade desta feature.

**Rationale**: `UniqueId` é a solução idiomática em testes de integração quando DB cleanup completo não é garantido. Simpler é melhor.

**Alternatives considered**:

- **Transaction rollback no fim de cada teste**: rejeitado — o banco é o da API real, não um in-memory; rollback via SQL direto reabre o problema de acesso a DB que queremos evitar.
- **Endpoint de limpeza**: rejeitado — adiciona superfície.

---

## R-008 — Mapeamento de responsabilidade do `qa-developer`

**Pergunta**: Como FR-022 se materializa no workflow da equipe?

**Decision**: A próxima etapa (`/speckit.tasks`) deve prefixar a instrução de cada task com o nome do agente responsável (`qa-developer`). Tasks que tocam a `Fortuno.API` (ex.: alterar `appsettings.json` para introduzir seção de NAuth de teste) também passam pelo `qa-developer`, pois são ajustes de configuração associados ao preset de teste — **sem** alteração de lógica de negócio.

**Rationale**: consolida a responsabilidade do agente na camada que ele domina (testes), evita spray de agentes.

**Alternatives considered**:

- **Delegar ajustes de config ao `dotnet-senior-developer`**: rejeitado — viola a fronteira declarada em FR-022.

---

## Resumo das decisões

| ID | Área | Decisão |
|---|---|---|
| R-001 | Store provisioning | v2: descoberta via `{ myStore { storeId } }` no GraphQL do ProxyPay; `FORTUNO_TEST_PROXYPAY_URL` env var substitui `FORTUNO_TEST_STORE_ID` |
| R-002 | NAuth login | `POST {nauthUrl}/auth/login` JSON; header `Authorization: Basic {token}` |
| R-003 | Coverage excludes | `coverlet.runsettings` com excludes por assembly e por caminho |
| R-004 | Shared fixture | `ApiSessionFixture : IAsyncLifetime` + `ICollectionFixture` |
| R-005 | Bruno token capture | Script `post-response` + `bru.setVar("accessToken", ...)` |
| R-006 | CI coverage gate | GitHub Actions + Coverlet + ReportGenerator + shell threshold |
| R-007 | Idempotência | Sufixo `yyyyMMddHHmmss-guid8` em todos os identificadores |
| R-008 | Agente responsável | `qa-developer` também para ajustes de config ligados a teste |

Nenhum `NEEDS CLARIFICATION` remanescente.
