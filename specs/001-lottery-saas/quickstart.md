# Quickstart — Fortuno (001-lottery-saas)

Guia mínimo para levantar o Fortuno em ambiente `Development` e executar um
fluxo completo (criação de Lottery → compra → sorteio).

> ⚠️ **Docker** não é executado neste ambiente local (constituição §1).
> Instale PostgreSQL diretamente ou use uma instância já disponível.

---

## 1. Pré-requisitos

| Ferramenta | Versão |
|---|---|
| .NET SDK | 8.0.x |
| PostgreSQL | 15+ (instância local) |
| NAuth | microserviço rodando e acessível (tenant `fortuna`) |
| ProxyPay | microserviço rodando e acessível (tenant `fortuna`) |
| zTools | credenciais S3/MailerSend disponíveis |

> **Observação**: NAuth, ProxyPay e zTools **não** fazem parte deste repo;
> são ecossistema externo. Obtenha as credenciais e URLs com o
> responsável por cada microserviço.

---

## 2. Variáveis de ambiente (`Development`)

Crie `.env` na raiz com:

```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__FortunoContext=Host=localhost;Database=fortuno_dev;Username=fortuno;Password=...
NAuth__BaseUrl=https://nauth.local
NAuth__Tenant=fortuna
NAuth__ApiKey=...
ProxyPay__BaseUrl=https://proxypay.local
ProxyPay__Tenant=fortuna
ProxyPay__ApiKey=...
ProxyPay__WebhookSecret=change-me-strong-secret
ZTools__S3BucketName=fortuno-dev
ZTools__S3Region=us-east-1
ZTools__S3AccessKey=...
ZTools__S3SecretKey=...
```

---

## 3. Restaurar, migrar e executar

```bash
# Restaurar dependências
dotnet restore Fortuno.sln

# Compilar
dotnet build Fortuno.sln -c Debug

# Aplicar migrations (cria schema fortuna_* no PostgreSQL)
dotnet ef database update --project Fortuno.Infra --startup-project Fortuno.API

# Executar a API
dotnet run --project Fortuno.API
```

A API sobe em `https://localhost:5001` (ou conforme `launchSettings.json`).
Swagger fica em `/swagger` e GraphQL em `/graphql` (banana-cake-pop).

---

## 4. Fluxo end-to-end (smoke test manual)

### 4.1 Autenticar

Obtenha um token Basic do NAuth (tenant `fortuna`) e use em toda chamada:

```
Authorization: Basic {base64(user:token)}
```

### 4.2 Criar Store no ProxyPay (fora do Fortuno)

Garanta que o usuário autenticado é `OwnerUserId` de pelo menos uma Store
no ProxyPay. Anote `storeId`.

### 4.3 Criar Lottery em Draft

```http
POST /lotteries
Content-Type: application/json

{
  "storeId": 42,
  "name": "Rifa do Aniversário",
  "descriptionMd": "Descrição em Markdown",
  "rulesMd": "Regras em Markdown",
  "privacyPolicyMd": "Política em Markdown",
  "ticketPrice": 20.00,
  "totalPrizeValue": 5000.00,
  "ticketMin": 1,
  "ticketMax": 50,
  "numberType": 1,
  "numberValueMin": 1,
  "numberValueMax": 1000,
  "referralPercent": 10.0
}
```

Retorna `lotteryId`, `slug` e `status=1` (Draft).

### 4.4 Adicionar imagem, Raffle e RaffleAward (em Draft)

```http
POST /lottery-images
POST /raffles
POST /raffle-awards
```

### 4.5 Publicar

```http
POST /lotteries/{lotteryId}/publish
```

Retorna 200. Lottery vai para `Open`.

### 4.6 Preview de compra

```http
POST /purchases/preview
{
  "lotteryId": 1,
  "quantity": 5,
  "mode": 1,
  "referralCode": "K7X9RQ42"
}
```

Retorna `totalAmount`, `discountValue`, `availableTickets`, `referrerUserId`.

### 4.7 Confirmar compra (gera Invoice no ProxyPay)

```http
POST /purchases/confirm
```

Retorna `invoiceId`, `pixQrCode`, `pixCopyPaste`, `expiresAt`.

### 4.8 Pagar PIX (fora do sistema)

Realizar o pagamento PIX usando o QR Code. Quando o ProxyPay confirmar,
ele envia webhook → Fortuno emite os tickets.

### 4.9 Consultar "Meus Tickets"

```http
GET /tickets/mine
```

ou via GraphQL:

```graphql
query {
  myTickets {
    ticketId
    ticketNumber
    refundState
  }
}
```

### 4.10 Sortear

```http
POST /raffles/{raffleId}/winners/preview
{
  "winningNumbers": [7, 42, 100]
}
```

Revisar prévia e confirmar:

```http
POST /raffles/{raffleId}/winners/confirm
```

Fechar o Raffle:

```http
POST /raffles/{raffleId}/close
```

### 4.11 Painéis de comissão

```http
GET /referrals/me               # painel do indicador
GET /commissions/lottery/{id}   # painel do dono
```

Ambos calculam em tempo real.

---

## 5. Ambientes

| Ambiente | appsettings | Descrição |
|---|---|---|
| Development | `appsettings.Development.json` | Local Windows/Linux |
| Docker | `appsettings.Docker.json` | Container de homologação (não executado local) |
| Production | `appsettings.Production.json` | Linux server de produção |

Alterne via `ASPNETCORE_ENVIRONMENT`.

---

## 6. Testes

```bash
dotnet test tests/Fortuno.Tests
```

Alguns testes de integração exigem PostgreSQL configurado. Para pular-os
em máquinas sem DB:

```bash
dotnet test tests/Fortuno.Tests --filter Category!=Integration
```

---

## 7. Solução de problemas comuns

| Sintoma | Provável causa |
|---|---|
| 401 em toda chamada | Tenant NAuth ≠ `fortuna` ou token expirado |
| 403 ao criar Lottery | Usuário autenticado não é `OwnerUserId` da Store |
| Webhook duplicado gerando tickets extras | `fortuna_webhook_events` UNIQUE não criada (rever migration) |
| Comissão inconsistente | Mudança em `Lottery.referral_percent` — é comportamento esperado (cálculo em tempo real) |
| `UserPicks` perde reserva | TTL de 15 min expirou — comprador deve refazer |
