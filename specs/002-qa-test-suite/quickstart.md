# Quickstart — QA Test Suite

**Feature**: 002-qa-test-suite
**Audience**: desenvolvedores, QA, reviewers

Como rodar cada artefato desta feature localmente, com exemplos.

---

## 1. Unit tests (`Fortuno.Tests`)

Pré-requisitos: `.NET 8 SDK` instalado. Nada de banco, nada de API rodando.

```bash
# da raiz do repo
dotnet test Fortuno.Tests \
  --settings Fortuno.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage"
```

Expected:

- 100% dos testes passam em < 2 minutos (SC-001).
- Relatório Cobertura em `Fortuno.Tests/TestResults/**/coverage.cobertura.xml`.

Gerar relatório HTML local (opcional):

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:**/coverage.cobertura.xml \
  -targetdir:./coverage-report \
  -reporttypes:Html;TextSummary \
  -assemblyfilters:"+Fortuno.Domain;+Fortuno.Application;+Fortuno.Infra"
# abrir coverage-report/index.html
```

Verificar o gate localmente:

```bash
# Summary.txt tem uma linha "Line coverage: XX.X%"
grep "Line coverage" coverage-report/Summary.txt
# deve ser >= 80%
```

---

## 2. API tests (`Fortuno.ApiTests`)

Pré-requisitos:

- `Fortuno.API` rodando e acessível em `{apiBaseUrl}` (`dotnet run --project Fortuno.API` ou ambiente remoto).
- NAuth acessível em `{nauthUrl}`.
- ProxyPay GraphQL acessível em `{proxyPayUrl}/graphql`.
- Usuário NAuth de teste **pré-provisionado** (FORTUNO_TEST_NAUTH_USER + password).
- Store **pré-existente** no ProxyPay vinculada ao usuário (descoberta automaticamente pela fixture via query `{ myStore { storeId } }` — não precisa exportar `FORTUNO_TEST_STORE_ID`).

### 2.1. Preencher env vars

Copiar o template e preencher:

```bash
cp Fortuno.ApiTests/appsettings.Tests.example.json Fortuno.ApiTests/appsettings.Tests.json
# editar o JSON com valores reais — arquivo está em .gitignore
```

Ou exportar via shell (Windows PowerShell):

```powershell
$env:FORTUNO_TEST_API_BASE_URL   = "https://localhost:5001"
$env:FORTUNO_TEST_NAUTH_URL      = "https://auth.fortuno.example"
$env:FORTUNO_TEST_NAUTH_TENANT   = "fortuna"
$env:FORTUNO_TEST_NAUTH_USER     = "qa-bot@fortuno.example"
$env:FORTUNO_TEST_NAUTH_PASSWORD = "…"
$env:FORTUNO_TEST_PROXYPAY_URL   = "https://pay.fortuno.example"
```

### 2.2. Rodar a suite

```bash
dotnet test Fortuno.ApiTests
```

Expected:

- Fixture autentica em NAuth, descobre a Store via `{ myStore { storeId } }` no ProxyPay, e roda os 8 cenários da User Story 1.
- Tempo total < 3 minutos (SC-003).
- Cada Lottery criada tem slug sufixado por timestamp + guid (FR-017), permitindo reexecução imediata sem conflito.

### 2.3. Falhas comuns

| Sintoma | Causa provável | Ação |
|---|---|---|
| "NAuth indisponível em {url}" | NAuth down ou URL incorreta | Verificar `FORTUNO_TEST_NAUTH_URL` e conectividade |
| "Credenciais de teste inválidas" | User/password errados ou tenant errado | Verificar `FORTUNO_TEST_NAUTH_*` |
| "Consulta `{ myStore { storeId } }` retornou vazio" | Usuário de teste não possui Store associada no ProxyPay | Criar/associar Store ao user no ProxyPay (ou usar outro user) |
| "ProxyPay recusou o token NAuth" | Token válido mas sem permissão no ProxyPay | Confirmar tenant + escopo do user |
| "ProxyPay indisponível em {url}/graphql" | ProxyPay down ou URL incorreta | Verificar `FORTUNO_TEST_PROXYPAY_URL` e conectividade |
| "Missing env var FORTUNO_TEST_..." | Env var ausente | Exportar conforme 2.1 |

---

## 3. Bruno collection (`/bruno`)

Pré-requisitos: [Bruno](https://www.usebruno.com/) instalado.

### 3.1. Abrir a coleção

1. Abrir o Bruno app.
2. **Open Collection** → selecionar a pasta `bruno/` do repo.
3. A lateral esquerda deve mostrar as pastas: `_Auth`, `Lotteries`, `Raffles`, …

### 3.2. Configurar ambiente

1. Copiar `bruno/environments/local.example.bru` para `local.bru` (arquivo real fica em `.gitignore`).
2. Preencher `baseUrl`, `nauthUrl`, `nauthUser`, `nauthPassword`, `nauthTenant`, `storeId`.
3. No canto superior direito do Bruno, selecionar o ambiente `local`.

### 3.3. Fluxo de onboarding (SC-004 — 10 min)

1. Abrir `_Auth/login.bru` → Run. Verificar status `200` e que `accessToken` foi capturado (aba Environment).
2. Abrir `Lotteries/create.bru` → ajustar `storeId` na body se necessário → Run. Capturar `lotteryId` na resposta.
3. Abrir `Lotteries/publish.bru` → Run (usa `{{lotteryId}}` do passo 2).
4. Abrir `Lotteries/get-by-id.bru` → Run (sem token — `[AllowAnonymous]`).

Se todos os 4 passos retornarem 2xx em < 10 minutos sem assistência, SC-004 está satisfeito.

---

## 4. CI — gate de cobertura

Arquivo: `.github/workflows/coverage-check.yml` (criado pela tarefa associada ao FR-020).

Trigger: `push` e `pull_request` em `main` e `002-*`.

Passos:

1. Checkout.
2. Setup .NET 8.
3. `dotnet test Fortuno.Tests --settings Fortuno.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`.
4. `reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:TextSummary -assemblyfilters:"+Fortuno.Domain;+Fortuno.Application;+Fortuno.Infra"`.
5. Shell step compara `Line coverage` do `Summary.txt` com `80`; `exit 1` se menor.

A suite `Fortuno.ApiTests` **não** roda neste workflow (FR-021). Para habilitar num workflow separado no futuro, será necessário configurar secrets `FORTUNO_TEST_*` no repositório.

---

## 5. Troubleshooting

### 5.1. Cobertura abaixo de 80% no CI

1. Rodar localmente (§1) para reproduzir.
2. Abrir `coverage-report/index.html` e identificar arquivos com cobertura baixa.
3. Adicionar testes ou ajustar `coverlet.runsettings` (apenas para excludes legítimos — POCO sem lógica).

### 5.2. Unit test falha intermitentemente

Testes unitários são determinísticos (FR-012). Flakiness indica:
- Uso indevido de `DateTime.Now` sem abstração → introduzir `IClock`/injetar timestamp.
- Teste dependente de ordem → remover dependência.
- Uso de filesystem/rede → substituir por mock.

### 5.3. ApiTest falha só em CI (passa local)

Diferenças comuns:
- TLS/cert em ambiente remoto vs. localhost.
- Tenant ou Store diferente (verificar secrets do job).
- Latência do NAuth (aumentar timeout da fixture).

---

## Referências cruzadas

- Requisitos: [spec.md](./spec.md)
- Decisões de design: [research.md](./research.md)
- Entidades: [data-model.md](./data-model.md)
- Contratos: [contracts/](./contracts/)
