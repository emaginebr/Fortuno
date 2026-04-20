# Feature Specification: QA Test Suite (Bruno + Unit + API)

**Feature Branch**: `002-qa-test-suite`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description:

```text
- Crie uma collection do Bruno encima dos endpoints existentes na pasta /bruno
- Crie testes unitários para cobrir pelomenos 80% do código
- E crie testes de API, fazendo o login usando a API do NAuth e o tenant "Fortuno"
    - Teste todo o processo de criação de Lottery e sorteio
    - Parte de compra do ticket deverá ser feita depois, posteriormente irei criar uma forma de simular o pagamento
- Use o agente "qa-developer"
```

## Clarifications

### Session 2026-04-18

- Q: Tenant exato a usar no login do NAuth → A: As suites leem o tenant de variável de ambiente/config; default atual é `"fortuna"` (valor do `appsettings.json`); o valor nunca é hardcoded no código de teste.
- Q: Como a suite de API obtém tickets para exercitar o sorteio sem passar pelo fluxo de compra? → A: Testes de sorteio (Raffle, preview/confirm winners, close raffle) ficam **adiados** até o fluxo de pagamento simulado existir; nesta entrega, os ApiTests cobrem apenas Lottery (create/publish/close/cancel) e endpoints `[AllowAnonymous]`. Raffle permanece coberto por **testes unitários** apenas.
- Q: Como agregar o piso de 80% de cobertura? → A: Cobertura **agregada ponderada por linhas** do conjunto `Fortuno.Domain + Fortuno.Application + Fortuno.Infra` ≥ 80%, com excludes explícitos para `Program.cs`, `Startup.cs`, EF `DbContext`, migrations e classes de Settings/POCO sem lógica. Sem piso individual por projeto.
- Q: Como provisionar usuário e Store no tenant de teste? → A: O **usuário de teste pré-existe** no tenant (credenciais lidas de env var). No setup da fixture, a suite verifica se o usuário já possui uma Store; se sim, **reutiliza**; se não, **cria uma nova Store**. O mecanismo exato de criação (endpoint HTTP, se existir, ou seed direto no banco compartilhado com a API) fica como decisão de implementação para `/speckit.plan`.
- **Amendment (Phase 0 — `/speckit.plan` Research)**: Descoberto que Stores **não residem no banco do Fortuno** — elas são gerenciadas externamente pelo ProxyPay (a API apenas consome via `ProxyPayAppService.GetStoreAsync`). Não existe endpoint HTTP de criação de Store no Fortuno, e a criação via seed direto no DB é inviável. **Decisão v1 (substituída)**: o `StoreId` do teste **também é pré-provisionado** e lido de variável de ambiente (`FORTUNO_TEST_STORE_ID`); a fixture valida que a Store pertence ao usuário autenticado e **falha fast** caso o guard de ownership recuse.
- **Amendment v2 (Phase 4 implementation)**: O ProxyPay expõe a query GraphQL `{ myStore { storeId } }` que retorna a Store do usuário autenticado (escopo implícito pelo token NAuth + header `X-Tenant-Id`). A fixture passou a descobrir o `StoreId` automaticamente via `POST {proxyPayUrl}/graphql`, eliminando a variável `FORTUNO_TEST_STORE_ID`. Nova env var obrigatória: `FORTUNO_TEST_PROXYPAY_URL`. Se a consulta retornar vazio, a fixture falha fast com mensagem acionável ("usuário de teste precisa ter uma Store associada no ProxyPay"). Ver `research.md` R-001 v2.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Suite de testes de API para o ciclo de vida da Lottery (Priority: P1)

Um desenvolvedor ou pipeline de CI executa a suite de testes externos da API e, em uma única corrida automatizada, o ciclo de vida de uma Lottery é exercitado contra uma instância real da Fortuno.API. O teste se autentica contra o NAuth, cria uma Lottery em `Draft`, exercita transições de estado válidas (publicar, fechar, cancelar) e inválidas (ex.: publicar uma Lottery já cancelada), e valida os endpoints públicos `[AllowAnonymous]` (consulta por id e por slug). Os endpoints de Raffle (sorteio), preview/confirm de vencedores e fechamento de Raffle **não** são exercitados nesta entrega — ficam adiados para quando o fluxo simulado de pagamento existir e permitir a criação real de tickets. Raffle permanece coberto pelos **testes unitários** (User Story 2).

**Why this priority**: É o núcleo do que pode ser validado ponta-a-ponta hoje — o ciclo de vida da Lottery é o pré-requisito de qualquer sorteio, toca autenticação NAuth e toda a camada HTTP/Application/Infra sobre a tabela mais central. Sem esta cobertura, qualquer mudança nos controllers pode regredir silenciosamente.

**Independent Test**: A suite é invocável por um único comando (`dotnet test` no projeto `Fortuno.ApiTests`) contra uma API em execução (local ou ambiente de teste). A aprovação da suite é, por si só, a evidência de que o caminho Lottery (Draft → Open → Closed/Cancelled) e os endpoints públicos de consulta estão íntegros.

**Acceptance Scenarios**:

1. **Given** a Fortuno.API está rodando e o NAuth está acessível, **When** a suite executa, **Then** ela obtém um token de acesso do NAuth usando as credenciais e o tenant configurados, e reutiliza o token em todas as chamadas subsequentes dentro da mesma sessão de teste.
2. **Given** o tester está autenticado, **When** ele cria uma Lottery (`POST /lotteries`) com payload válido, **Then** a Lottery é retornada com `LotteryStatus = Draft`.
3. **Given** uma Lottery em `Draft`, **When** ela é publicada (`POST /lotteries/{id}/publish`), **Then** a Lottery transita para `Open`.
4. **Given** uma Lottery em `Open`, **When** ela é fechada (`POST /lotteries/{id}/close`), **Then** a Lottery transita para `Closed`.
5. **Given** uma Lottery em `Draft`, **When** ela é cancelada (`POST /lotteries/{id}/cancel`), **Then** a Lottery transita para `Cancelled`.
6. **Given** uma Lottery já `Cancelled`, **When** uma tentativa de publish é feita, **Then** a API responde com erro de regra de negócio (status 4xx) e a Lottery permanece `Cancelled`.
7. **Given** uma Lottery criada com slug conhecido, **When** `GET /lotteries/{id}` e `GET /lotteries/slug/{slug}` são chamados **sem** token (endpoints `[AllowAnonymous]`), **Then** ambos retornam a Lottery com status 200.
8. **Given** a suite completou, **When** uma segunda execução é iniciada imediatamente, **Then** ela roda sem interferência da anterior (isolamento por dados únicos por execução, ex.: slug/nome com timestamp ou Guid).

---

### User Story 2 — Testes unitários cobrindo ≥ 80% do código da camada de domínio e aplicação (Priority: P2)

Um desenvolvedor que altera uma regra em um Domain Service ou em um Validator FluentValidation recebe, em poucos segundos, feedback local e no CI sobre regressões, sem depender de banco ou de serviços externos. A suite unitária mira os componentes mais ricos em lógica: Domain Services (Lottery, Raffle, Ticket, Purchase, RaffleAward, Refund, SlugService, NumberCompositionService, StoreOwnershipGuard), Validators FluentValidation, e o AppService de integração ProxyPay (parser/assinatura HMAC). Importante: como o fluxo ponta-a-ponta de Raffle está adiado na suite de API, a suite unitária é, nesta entrega, **a única cobertura automatizada** para `RaffleService`, `RaffleAwardService`, `PurchaseService` e `RefundService` — sua qualidade é especialmente crítica.

**Why this priority**: Cobertura de domínio é o maior ROI em qualidade: roda rápido, é determinística, não exige infra, e falhas apontam diretamente para a regra violada. Tem prioridade abaixo do P1 porque o stakeholder pediu explicitamente o fluxo E2E primeiro, mas é indispensável para manutenção.

**Independent Test**: Executável por `dotnet test` no projeto `Fortuno.Tests` sem API nem banco em execução. A métrica de cobertura é emitida por ferramenta de code coverage (Coverlet) e auditável em artefato do CI.

**Acceptance Scenarios**:

1. **Given** `Fortuno.Tests` existe e compila, **When** `dotnet test` é executado, **Then** todos os testes passam em máquina limpa (sem banco/API) em menos de 2 minutos.
2. **Given** a suite rodou, **When** o relatório de cobertura é gerado, **Then** a cobertura de linhas do conjunto `Fortuno.Domain` + `Fortuno.Application` + `Fortuno.Infra` (excluindo Context/Migrations/Program.cs/Startup.cs) é ≥ 80%.
3. **Given** um Domain Service tem regra de transição de status (ex.: não publicar uma Lottery já `Cancelled`), **When** o teste aciona o caminho inválido, **Then** uma exceção de domínio específica é lançada e assertada.
4. **Given** um Validator FluentValidation tem regras de obrigatoriedade e formato, **When** o teste submete DTOs válidos e inválidos, **Then** o validator aprova/reprova conforme esperado e a mensagem de erro correta é emitida.
5. **Given** uma mudança de código reduz a cobertura abaixo de 80%, **When** o CI roda, **Then** o gate de cobertura falha a build.

---

### User Story 3 — Bruno collection versionada em `/bruno` para exploração manual e onboarding (Priority: P3)

Um novo integrante do time (dev, QA, PO técnico) clona o repositório, abre a pasta `/bruno` no Bruno e consegue disparar as principais chamadas da API contra qualquer ambiente configurado, sem precisar redigitar headers de autenticação nem montar payloads do zero. Variáveis de ambiente (ex.: `baseUrl`, `accessToken`, `lotteryId`) são compartilhadas entre as requests.

**Why this priority**: É um artefato de produtividade e documentação executável. Não protege contra regressão por si só, mas reduz atrito de onboarding e de depuração em produção/staging. Tem prioridade mais baixa que os testes automatizados porque seu valor é humano, não de CI.

**Independent Test**: Um novo integrante abre a collection no Bruno, preenche `.env`/`.bru` de ambiente com URL + credenciais de um ambiente alvo, e consegue executar, sem modificar requests, pelo menos: login NAuth, criar Lottery, publicar Lottery, listar Lotteries da store, criar Raffle, preview/confirm winners, fechar Raffle e Lottery.

**Acceptance Scenarios**:

1. **Given** a pasta `/bruno` existe na raiz do repo, **When** ela é aberta no aplicativo Bruno, **Then** a collection carrega sem erros e exibe requests organizadas por domínio (Auth, Lotteries, Raffles, RaffleAwards, LotteryCombos, LotteryImages, Tickets, Purchases, Referrals, Commissions, Refunds, Webhooks).
2. **Given** ambientes `local`, `dev` e `prod` definidos, **When** o usuário alterna o ambiente, **Then** `baseUrl` e credenciais trocam sem edição das requests.
3. **Given** uma request de login NAuth, **When** ela é executada com sucesso, **Then** o `accessToken` é automaticamente armazenado em variável de coleção via script de pós-resposta e utilizado pelas demais requests como `Authorization: Bearer {{accessToken}}`.
4. **Given** a collection está versionada em git, **When** um dev adiciona um novo endpoint, **Then** o diff da request é legível (arquivos `.bru` em texto), revisável em code review.

---

### Edge Cases

- **Token expirado**: Na suite de API, o que acontece se o token do NAuth expirar no meio da execução? Expectativa: a sessão renova ou a suite é curta o suficiente para caber em um TTL.
- **Dados residuais entre execuções**: Se uma execução anterior falhou no meio, como a próxima execução lida com dados órfãos (Lottery em `Open` com mesmo slug)? Expectativa: cada execução usa identificadores únicos para evitar colisão; limpeza agressiva não é requerida.
- **NAuth indisponível**: O que acontece se o NAuth estiver offline durante a execução dos ApiTests? Expectativa: a fixture falha cedo, com mensagem clara, sem tentar rodar os demais testes.
- **Cobertura bate 80% mas com testes fracos**: O stakeholder aceita que 80% é um piso, não um teto, e que revisão de PR continua responsável por qualidade de assertividade.
- **Endpoint `[AllowAnonymous]`** (ex.: `GET /lotteries/{id}`, `GET /lotteries/slug/{slug}`, `WebhooksController`): devem ser exercitados sem header de autorização nos ApiTests e na Bruno collection.
- **Raffle / Purchase / Webhook ProxyPay**: fora do escopo dos ApiTests nesta entrega (fluxo de pagamento simulado ainda não existe); permanecem cobertos apenas por testes unitários.

## Requirements *(mandatory)*

### Functional Requirements

**Bruno Collection**

- **FR-001**: O projeto MUST conter uma pasta `/bruno` na raiz do repositório, versionada no git, contendo uma coleção Bruno.
- **FR-002**: A coleção MUST agrupar requests por domínio (pastas correspondentes aos controllers identificados: Auth, Lotteries, LotteryCombos, LotteryImages, Raffles, RaffleAwards, Tickets, Purchases, Referrals, Commissions, Refunds, Webhooks).
- **FR-003**: A coleção MUST definir pelo menos três ambientes (`local`, `dev`, `prod`) com variáveis: `baseUrl`, `nauthUrl`, `nauthUser`, `nauthPassword`, `nauthTenant`, `accessToken`.
- **FR-004**: A request de login MUST, via script de pós-resposta, capturar o token retornado pelo NAuth e armazená-lo em uma variável de coleção; demais requests MUST consumir essa variável via header `Authorization`.
- **FR-005**: Payloads de exemplo MUST ser válidos o suficiente para que a request passe pelos validators FluentValidation existentes, usando placeholders `{{ }}` onde IDs precisam ser capturados de respostas anteriores (ex.: `{{lotteryId}}`).
- **FR-006**: Arquivos de ambiente que contenham segredos MUST estar no `.gitignore`; apenas um arquivo `*.example` com chaves vazias MUST ser versionado.

**Unit Tests**

- **FR-007**: O projeto MUST incluir um projeto de testes unitários `Fortuno.Tests` usando xUnit + FluentAssertions + Moq.
- **FR-008**: A estrutura de pastas do `Fortuno.Tests` MUST espelhar a estrutura dos projetos sob teste (`Domain/Services/...`, `Application/Validations/...`, `Infra/AppServices/...`).
- **FR-009**: Todos os Domain Services identificados (LotteryService, RaffleService, TicketService, PurchaseService, RaffleAwardService, LotteryComboService, LotteryImageService, RefundService, ReferralService, SlugService, NumberCompositionService, StoreOwnershipGuard) MUST ter cobertura de cenários felizes e de pelo menos dois cenários de falha (regra de negócio violada, input inválido).
- **FR-010**: Todos os validators FluentValidation existentes (LotteryInsertInfoValidator, LotteryImageInsertInfoValidator, LotteryCancelRequestValidator, PurchasePreviewRequestValidator, PurchaseConfirmRequestValidator, LotteryComboInsertInfoValidator, RaffleCancelRequestValidator, RefundStatusChangeRequestValidator) MUST ter testes cobrindo DTO válido + cada regra negativa declarada.
- **FR-011**: A cobertura de linhas do conjunto `Fortuno.Domain` + `Fortuno.Application` + `Fortuno.Infra`, excluindo classes de bootstrap (`Program.cs`, `Startup.cs`), EF `DbContext`, migrations, e classes de Settings/POCO sem lógica, MUST ser ≥ 80%, medida por Coverlet e exportada em formato Cobertura.
- **FR-012**: Testes unitários MUST ser determinísticos e executar sem dependências externas (sem banco real, sem HTTP, sem filesystem persistente). Dependências externas MUST ser substituídas por mocks/stubs.

**API Tests (Integration)**

- **FR-013**: O projeto MUST incluir um projeto separado `Fortuno.ApiTests` (distinto de `Fortuno.Tests`) usando xUnit + Flurl.Http + FluentAssertions, alinhado ao preset `dotnet-test-api` do repositório.
- **FR-014**: O `Fortuno.ApiTests` MUST autenticar-se uma vez por execução de suite, via uma fixture compartilhada (`IAsyncLifetime`), contra a API do NAuth, usando o tenant configurado em variáveis de ambiente, e propagar o token para todas as chamadas.
- **FR-015**: A suite MUST cobrir o ciclo de vida de Lottery (Draft → Publish → Close; e Draft → Cancel), transições inválidas (ex.: publish em Lottery `Cancelled`) e os endpoints `[AllowAnonymous]` de consulta (por id e por slug), conforme os Acceptance Scenarios da User Story 1.
- **FR-016**: Os endpoints de **Raffle** (`/raffles*`, incluindo `winners/preview`, `winners/confirm`, `close`), **Purchase** (`/purchases/*`) e **Webhook ProxyPay** (`/webhooks/proxypay/*`) MUST ser explicitamente **excluídos** desta entrega nos ApiTests; serão endereçados em uma entrega futura, após existir um fluxo simulado de pagamento capaz de criar tickets reais.
- **FR-017**: Cada execução da suite MUST usar identificadores únicos (ex.: slug/nome sufixados por Guid/timestamp) para evitar colisão entre execuções consecutivas.
- **FR-018**: Falhas de pré-requisito (NAuth indisponível, credenciais ausentes, API não responde) MUST interromper a suite cedo com mensagem acionável, sem executar os cenários de negócio.
- **FR-019**: Variáveis sensíveis (credenciais NAuth, URL da API) MUST ser lidas de variáveis de ambiente ou de um `appsettings.Tests.json` não versionado, com um `*.example` versionado.

**Integração e CI**

- **FR-020**: A suite unitária MUST rodar no CI em cada push/PR; a falha do gate de cobertura (< 80%) MUST quebrar a build.
- **FR-021**: A suite de ApiTests MUST poder ser executada localmente e sob demanda; sua execução automática no CI é opcional e, se habilitada, MUST depender de um job com segredos configurados.
- **FR-022**: A implementação desta feature MUST ser conduzida pelo agente `qa-developer` (Clean Architecture + skills `dotnet-test` e `dotnet-test-api`).
- **FR-023**: A fixture da suite de API MUST descobrir o `StoreId` do teste em tempo de execução via `POST {FORTUNO_TEST_PROXYPAY_URL}/graphql` com a query `{ myStore { storeId } }`, usando o token NAuth como `Authorization: Basic {token}` e o header `X-Tenant-Id: {tenant}`. A fixture MUST interromper a suite com mensagem acionável quando: (a) a consulta retornar `errors` não-vazio, (b) `data.myStore == null`, ou (c) o ProxyPay estiver indisponível. A fixture **não** cria Stores (Stores vivem no ProxyPay — provisionamento é operacional). A Store descoberta MUST ser logada no output do teste para rastreabilidade.

### Key Entities *(incluído porque testes exercitam entidades reais)*

- **Lottery**: Sorteio-pai; campos-chave para o teste: `LotteryId`, `StoreId`, `Slug`, `Status` (Draft/Open/Closed/Cancelled), `TicketPrice`, `NumberType`. **Coberta por ApiTests + unit tests.**
- **Raffle**: Evento de sorteio vinculado a uma Lottery; campos-chave: `RaffleId`, `LotteryId`, `Status` (Open/Closed/Cancelled), `RaffleDatetime`. **Apenas unit tests nesta entrega.**
- **RaffleAward**: Posição/prêmio dentro de um Raffle; `Position`, `Description`. **Apenas unit tests nesta entrega.**
- **RaffleWinner**: Resultado do sorteio; liga `Raffle`, `RaffleAward`, `Ticket` e `UserId`. **Apenas unit tests nesta entrega.**
- **Ticket**: Unidade sorteável; criada via fluxo de Purchase. **Fora de escopo dos ApiTests** (aguarda fluxo de pagamento simulado); coberta por unit tests.
- **Store**: Escopo de negócio dono da Lottery (o `storeId` é obrigatório na criação).
- **NAuth User/Tenant**: Identidade autenticada; a suite usa um tenant dedicado para isolamento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em uma máquina de desenvolvedor limpa, executar a suite unitária finaliza em menos de 2 minutos com 100% dos testes passando.
- **SC-002**: O relatório de cobertura gerado na mesma execução reporta ≥ 80% de cobertura de linhas sobre o conjunto definido em FR-011.
- **SC-003**: Executar a suite de API contra uma instância da Fortuno.API e do NAuth em funcionamento finaliza em menos de 3 minutos com 100% dos cenários da User Story 1 passando.
- **SC-004**: Um novo integrante do time consegue, em ≤ 10 minutos, abrir a pasta `/bruno`, configurar o ambiente `local`, fazer login e executar uma requisição de criação de Lottery com sucesso, sem assistência.
- **SC-005**: Nenhuma credencial real (senha NAuth, JWT secret, URLs internas) é encontrada em arquivos versionados no repositório após a entrega (auditável por varredura textual simples).
- **SC-006**: Uma regressão proposital introduzida num Domain Service (ex.: permitir publish em Lottery `Cancelled`) faz a suite unitária falhar com mensagem que identifica inequivocamente a regra violada.
- **SC-007**: Uma regressão proposital introduzida no fluxo HTTP de Lottery (ex.: publish passa a aceitar Lottery `Cancelled`) faz a suite de API falhar e o commit é bloqueado no canal em que a suite for executada.

## Assumptions

- **A-001**: Tenant usado pelas suites é lido de variável de ambiente/config (default: `"fortuna"`, conforme `appsettings.json`). Resolvido em Clarifications (Sessão 2026-04-18).
- **A-002**: Raffle (sorteio), Purchase e Webhook ProxyPay ficam **fora do escopo dos ApiTests** nesta entrega, por dependerem de tickets reais (compra). Essas camadas permanecem cobertas apenas por testes unitários. Uma entrega futura, condicionada à existência de um fluxo de pagamento simulado, estenderá a suite de API para cobrir o ciclo completo do sorteio. Resolvido em Clarifications (Sessão 2026-04-18).
- **A-003**: A meta de 80% de cobertura é aplicada ao **conjunto** `Domain + Application + Infra` (ponderada por linhas), excluindo código de bootstrap/infra sem lógica. Cobertura por projeto isolado pode variar, desde que o agregado satisfaça o piso. Resolvido em Clarifications (Sessão 2026-04-18).
- **A-004**: O NAuth expõe um endpoint de login padrão (usuário + senha + tenant → access token). Isso é consistente com a skill `nauth-guide` do repositório.
- **A-005**: Existe ao menos um usuário de teste **pré-provisionado** no tenant alvo (NAuth), **mais** uma Store pré-existente no ProxyPay vinculada a esse usuário. A suite lê credenciais e `storeId` de variáveis de ambiente e valida ownership antes de rodar cenários. Provisionamento do par (usuário + Store no ProxyPay) é atividade operacional fora desta feature. Resolvido e emendado em Clarifications (Sessão 2026-04-18).
- **A-006**: A Bruno collection é documento vivo mantido pelo time; divergência temporária entre collection e endpoints reais é aceitável entre PRs, mas deve ser reduzida em cada revisão.
- **A-007**: CI atual (GitHub Actions, job de deploy de produção) é o alvo para o gate de cobertura; a configuração exata do workflow será detalhada no plano.
- **A-008**: A feature **não** inclui criação de fluxo de pagamento simulado; o stakeholder explicitou que cuidará disso depois. Purchase, Raffle e Webhook ProxyPay continuam **sem** cobertura E2E nesta entrega (mantêm apenas cobertura unitária).
