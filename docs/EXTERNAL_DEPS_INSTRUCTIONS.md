# Instruções para microserviços parceiros

> **Objetivo**: este documento lista as lacunas que o Fortuno encontrou
> nos repositórios `NAuth`, `ProxyPay` e `zTools` durante a implementação
> da feature 001-lottery-saas. **Nenhuma alteração foi feita nesses
> repositórios** — as mudanças devem ser implementadas pelos times
> responsáveis. Referência original: `specs/001-lottery-saas/research.md §9`.

---

## NAuth (c:/repos/NAuth)

### 1. Endpoint em lote para buscar usuários por IDs (recomendado)

**Contexto**: Painéis de sorteio, painéis de indicação e listas de
ganhadores precisam resolver N `UserId` → dados do usuário. Hoje o
`IUserClient` só expõe `GetByIdAsync(long, string)`, o que obriga o
Fortuno a fazer N chamadas HTTP serializadas.

**Solicitação**: adicionar ao `IUserClient` e ao endpoint HTTP do NAuth:

```csharp
Task<IList<UserInfo>> GetByIdsAsync(IEnumerable<long> userIds, string token);
```

Ideal: endpoint `POST /users/by-ids` recebendo `{ userIds: [1,2,3] }`.

**Impacto se não implementado**: o Fortuno cai para N chamadas (fallback
atual em `NAuthAppService.GetByIdsAsync`). SC-008 (≤5s para 100 ganhadores)
fica em risco em cenários com latência alta no NAuth.

### 2. Confirmação dos campos usados pelo Fortuno

O `NAuthAppService` lê do `UserInfo`:

- `UserId`, `Name`, `Email` — confirmados.
- `IdDocument` (CPF) — confirmar se todos os usuários o têm preenchido
  quando passam pelo "cadastro completo" do Fortuno (doc pessoal
  obrigatório para criar loja/loteria).
- `Phones[0].Phone`, `Addresses[0].{Address, City, State}` — idem.

Se o NAuth não obrigar esses campos no cadastro completo, o Fortuno
precisa validar e rejeitar criação de Store/Lottery por usuários sem
CPF/endereço — já previsto (FR-002).

---

## ProxyPay (c:/repos/ProxyPay)

### 1. Suporte a multi-tenant com tenant `fortuna` (**bloqueante**)

**Contexto**: conforme a spec, o ProxyPay ainda não é multi-tenant, mas
isso será implementado. O tenant do Fortuno é `"fortuna"`.

**Solicitação**: implementar tenant no ProxyPay. O Fortuno envia o
header `X-Tenant: fortuna` em todas as chamadas; é esperado que o
ProxyPay:

1. Valide o header.
2. Isole dados por tenant (Stores, Invoices, webhooks).
3. Emita o webhook `invoice.paid` com o campo `tenant: "fortuna"` no
   payload JSON (ver contrato em
   `specs/001-lottery-saas/contracts/webhook-proxypay.md`).

**Sem isso em produção, o Fortuno não pode ir ao ar.**

### 2. Webhook `invoice.paid` assinado com HMAC-SHA256

**Contexto**: o Fortuno expõe `POST /webhooks/proxypay/invoice-paid`
que valida o header `X-ProxyPay-Signature` contra o corpo bruto
(`sha256=<hex-hmac>`), usando segredo compartilhado em
`ProxyPaySettings:WebhookSecret`.

**Solicitação**: o ProxyPay deve enviar esse header em todas as
notificações. Segredo distribuído manualmente (ou via cofre/KMS).

### 3. `GET /stores/{id}` incluindo `ownerUserId`

**Contexto**: FR-045a do Fortuno restringe todas as escritas
(Lottery/Image/Combo/Raffle/Award/Winner, estorno, comissões) ao
usuário proprietário da Store associada. O Fortuno chama
`ProxyPayAppService.GetStoreAsync(storeId)` e espera a resposta:

```json
{ "storeId": 42, "ownerUserId": 99, "name": "Loja Teste" }
```

**Solicitação**: confirmar que o endpoint `GET /api/stores/{id}` retorna
`ownerUserId`. Se hoje for apenas `OwnerId` ou outro nome, ajustar (ou
nos avisar para ajustar o client do lado Fortuno).

### 4. `POST /invoices` aceitando `metadata` arbitrário

**Contexto**: o Fortuno cria Invoices passando metadata chave-valor que
volta no webhook para correlacionar a compra:

```json
{
  "storeId": 42,
  "amount": 90.00,
  "description": "Fortuno Lottery #1 - 5 tickets",
  "metadata": {
    "fortunoLotteryId": "1",
    "fortunoUserId": "99",
    "fortunoQuantity": "5",
    "fortunoMode": "1",
    "fortunoReferralCode": "K7X9RQ42"
  }
}
```

**Solicitação**: o endpoint de criação de Invoice deve armazenar
`metadata` e ecoá-lo no payload do webhook `invoice.paid`.

A resposta esperada do Fortuno:

```json
{
  "invoiceId": 123456,
  "storeId": 42,
  "amount": 90.00,
  "status": "pending",
  "pixQrCode": "...",
  "pixCopyPaste": "...",
  "expiresAt": "2026-04-17T12:49:00Z"
}
```

### 5. **Fora do escopo ProxyPay**: estornos não são chamados pelo Fortuno

Por decisão de produto (SC-011), o Fortuno **NÃO** chama o ProxyPay para
estornar tickets. Toda liquidação de estorno/comissão ocorre off-platform
pelo dono da Lottery. Nenhuma chamada de estorno precisa ser suportada
por conta do Fortuno.

---

## zTools (c:/repos/zTools)

### 1. Geração de PDF a partir de Markdown (**importante**)

**Contexto**: FR-008 exige que regras e política de privacidade da Lottery
possam ser baixadas em PDF. Hoje o `zTools` expõe `IFileClient`,
`IStringClient`, `IChatGPTClient`, `IMailClient`, `IDocumentClient`,
`IInVideoClient` — mas nenhum método dedicado a PDF.

**Solicitação**: adicionar ao zTools um endpoint/método:

```csharp
public interface IPdfClient
{
    Task<byte[]> GeneratePdfFromMarkdownAsync(string markdown, string title);
    Task<byte[]> GeneratePdfFromHtmlAsync(string html, string title);
}
```

**Alternativa temporária aplicada no Fortuno**: o `ZToolsAppService.GeneratePdfFromMarkdownAsync`
renderiza HTML com Markdig e retorna os bytes do HTML (não é PDF real).
O `Content-Type` enviado pelo controller é `application/pdf`, então o
comportamento funcional está errado até o zTools expor PDF de verdade.

### 2. Confirmação do `IFileClient.UploadFileAsync(bucket, IFormFile)`

**Contexto**: o Fortuno chama `_fileClient.UploadFileAsync(S3BucketName, formFile)`
para uploads de imagens e vídeos. O bucket vem da config
`ZTools:S3BucketName`.

**Solicitação**: confirmar que o bucket usado em `Development` (ex.:
`fortuno-dev`) existe no S3 e que o zTools tem credenciais para gravar
nele. Mesmo para `Docker`/`Production`.

### 3. Confirmação do `IStringClient.GenerateSlugAsync(name)`

**Contexto**: `SlugService` chama esse método para gerar o slug de uma
Lottery a partir do nome, com fallback para geração local caso o zTools
esteja offline. Confirmar que a saída respeita:

- Apenas `a-z`, `0-9`, `-`.
- Trim de hífens nos extremos.
- Tamanho ≤ ~100 caracteres.

---

## Checklist para operar o Fortuno em produção

- [ ] NAuth exposto publicamente com tenant `fortuna` e contrato
      `IUserClient` inalterado.
- [ ] NAuth expõe `GetByIdsAsync` em lote (ou aceitar degradação).
- [ ] ProxyPay com tenant `fortuna` e webhook HMAC implementados.
- [ ] ProxyPay devolve `ownerUserId` em `GET /stores/{id}`.
- [ ] ProxyPay ecoa `metadata` no webhook `invoice.paid`.
- [ ] zTools expõe geração de PDF (ou aceitar HTML em vez de PDF
      temporariamente).
- [ ] Segredos compartilhados distribuídos via cofre:
      `NAuth:ApiKey`, `ProxyPay:ApiKey`, `ProxyPay:WebhookSecret`,
      `ZTools:ApiKey`, `ZTools:S3AccessKey`, `ZTools:S3SecretKey`.

---

## Como o Fortuno se comporta quando algo está pendente

- NAuth offline → controllers retornam 401 nas rotas autenticadas.
- ProxyPay offline → `POST /api/purchases/confirm` retorna 5xx.
- Webhook não assinado ou sem tenant → `401 Unauthorized` no Fortuno.
- zTools offline no upload → `InvalidOperationException` que cai para
  500 pelo exception handler.
- zTools offline no slug → `SlugService` usa fallback local (regex sem
  acentos) — loteria é criada normalmente.
- zTools sem PDF → endpoints `*.pdf` retornam um HTML embalado como
  `application/pdf` (comportamento temporário).
