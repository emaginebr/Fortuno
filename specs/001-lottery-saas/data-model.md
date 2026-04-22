# Data Model — 001-lottery-saas

**Date**: 2026-04-17
**Scope**: Entidades locais do Fortuno (tabelas com prefixo `fortuna_`) e
referências a entidades externas (NAuth, ProxyPay). Convenções seguem a
constituição §3.

---

## Enums (Domain)

```csharp
namespace Fortuno.Domain.Enums;

public enum LotteryStatus   { Draft = 1, Open = 2, Closed = 3, Cancelled = 4 }
public enum RaffleStatus    { Open = 1, Closed = 2, Cancelled = 3 }
public enum TicketRefundState { None = 1, PendingRefund = 2, Refunded = 3 }
public enum NumberType      { Int64 = 1, Composed3 = 3, Composed4 = 4,
                              Composed5 = 5, Composed6 = 6, Composed7 = 7,
                              Composed8 = 8 }
public enum TicketOrderMode { Random = 1, UserPicks = 2 }
```

---

## Entidades locais (todas em `fortuna_*`)

### `fortuna_lotteries` — Lottery

| Coluna | Tipo | Notas |
|---|---|---|
| `lottery_id` | `bigint` PK identity | `lotteries_pkey` |
| `store_id` | `bigint` FK | referencia `Store` (ProxyPay); `fk_store_lottery` |
| `name` | `varchar(160)` NOT NULL | |
| `slug` | `varchar(200)` NOT NULL UNIQUE | único global |
| `description_md` | `text` NOT NULL | Markdown |
| `rules_md` | `text` NOT NULL | Markdown (gera PDF via zTools) |
| `privacy_policy_md` | `text` NOT NULL | Markdown (gera PDF via zTools) |
| `ticket_price` | `numeric(12,2)` NOT NULL | |
| `total_prize_value` | `numeric(14,2)` NOT NULL | |
| `ticket_min` | `integer` NOT NULL DEFAULT 0 | 0 = sem mínimo |
| `ticket_max` | `integer` NOT NULL DEFAULT 0 | 0 = sem máximo |
| `ticket_num_ini` | `bigint` NOT NULL DEFAULT 1 | só aplica a tipos compostos |
| `ticket_num_end` | `bigint` NOT NULL DEFAULT 0 | só aplica a tipos compostos |
| `number_type` | `integer` NOT NULL DEFAULT 1 | `NumberType` |
| `number_value_min` | `integer` NOT NULL | min por componente |
| `number_value_max` | `integer` NOT NULL | max por componente |
| `referral_percent` | `real` NOT NULL DEFAULT 0 | float %; 0..100 |
| `status` | `integer` NOT NULL DEFAULT 1 | `LotteryStatus` |
| `cancel_reason` | `varchar(1000)` NULL | obrigatório ao cancelar (≥20 chars) |
| `cancelled_by_user_id` | `bigint` NULL | UserId (NAuth) |
| `cancelled_at` | `timestamp without time zone` NULL | |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |
| `updated_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Regras de estado** (FR-014/015/016/017/033a/033a1):

- Criação → `Draft`.
- `Draft → Open` exige ≥1 LotteryImage, ≥1 Raffle, ≥1 RaffleAward. Para
  tipos compostos, também `ticket_num_end > ticket_num_ini`.
- `Open → Closed` livre (dono fecha vendas manualmente).
- `{Draft|Open|Closed} → Cancelled` exige `cancel_reason` (≥20 chars),
  registra `cancelled_by_user_id` e `cancelled_at`. Irreversível.
- `Cancelled → *` proibido. DELETE proibido (FR-017).

### `fortuna_lottery_images` — LotteryImage

| Coluna | Tipo | Notas |
|---|---|---|
| `lottery_image_id` | `bigint` PK identity | `lottery_images_pkey` |
| `lottery_id` | `bigint` NOT NULL | FK `fk_lottery_lottery_image`, `ClientSetNull` |
| `image_url` | `varchar(500)` NOT NULL | retornada pelo zTools |
| `description` | `varchar(260)` NULL | |
| `display_order` | `integer` NOT NULL DEFAULT 0 | menor = principal |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Regra**: CRUD apenas se `Lottery.status = Draft` (FR-018).

### `fortuna_lottery_combos` — LotteryCombo

| Coluna | Tipo | Notas |
|---|---|---|
| `lottery_combo_id` | `bigint` PK identity | |
| `lottery_id` | `bigint` NOT NULL | FK `fk_lottery_lottery_combo` |
| `name` | `varchar(120)` NOT NULL | |
| `discount_value` | `real` NOT NULL DEFAULT 0 | %; 0..100 |
| `discount_label` | `varchar(80)` NOT NULL | ex.: "18% off" |
| `quantity_start` | `integer` NOT NULL DEFAULT 0 | |
| `quantity_end` | `integer` NOT NULL DEFAULT 0 | |
| `created_at` / `updated_at` | `timestamp without time zone` | |

**Regras** (FR-022/023/024/025):

- CRUD apenas em `Draft`.
- Constraint de aplicação: faixas `[quantity_start, quantity_end]` de combos
  da mesma Lottery **não** podem sobrepor-se (verificação em código +
  exclusion constraint via `btree_gist` opcional).

### `fortuna_tickets` — Ticket

| Coluna | Tipo | Notas |
|---|---|---|
| `ticket_id` | `bigint` PK identity | |
| `lottery_id` | `bigint` NOT NULL | FK |
| `user_id` | `bigint` NOT NULL | UserId do NAuth |
| `invoice_id` | `bigint` NOT NULL | Id da Invoice (ProxyPay) |
| `ticket_number` | `bigint` NOT NULL | composto em int64 |
| `refund_state` | `integer` NOT NULL DEFAULT 1 | `TicketRefundState` |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Constraints**:

- UNIQUE `(lottery_id, ticket_number)` — impede over-sell.
- Tickets são **quase-imutáveis**: só evolui `refund_state`
  (`None → PendingRefund → Refunded`) via FR-032.
- **Sem** `raffle_id` — Ticket pertence à Lottery (Q do 2026-04-16).
- **Sem** `referrer_user_id` — vínculo via `fortuna_invoice_referrers`
  (Q do 2026-04-17).

### `fortuna_raffles` — Raffle

| Coluna | Tipo | Notas |
|---|---|---|
| `raffle_id` | `bigint` PK identity | |
| `lottery_id` | `bigint` NOT NULL | FK |
| `name` | `varchar(160)` NOT NULL | |
| `description_md` | `text` NULL | Markdown |
| `raffle_datetime` | `timestamp without time zone` NOT NULL | informativo |
| `video_url` | `varchar(500)` NULL | upload zTools ou URL externa |
| `include_previous_winners` | `boolean` NOT NULL DEFAULT FALSE | FR-034 |
| `status` | `integer` NOT NULL DEFAULT 1 | `RaffleStatus` |
| `created_at` / `updated_at` | `timestamp without time zone` | |

**Regras**: CRUD apenas em Lottery `Draft` (FR-037). Sorteio livre em
qualquer momento com status `Open` (FR-041a). Cancelamento exige
redistribuição dos Awards (FR-042b/c).

### `fortuna_raffle_awards` — RaffleAward

| Coluna | Tipo | Notas |
|---|---|---|
| `raffle_award_id` | `bigint` PK identity | |
| `raffle_id` | `bigint` NOT NULL | FK |
| `position` | `integer` NOT NULL | |
| `description` | `varchar(300)` NOT NULL | |

**Constraint**: UNIQUE `(raffle_id, position)` — uma posição por Raffle.

### `fortuna_raffle_winners` — RaffleWinner

| Coluna | Tipo | Notas |
|---|---|---|
| `raffle_winner_id` | `bigint` PK identity | |
| `raffle_id` | `bigint` NOT NULL | FK |
| `raffle_award_id` | `bigint` NOT NULL | FK |
| `ticket_id` | `bigint` NULL | null ⇒ "sem ganhador" |
| `user_id` | `bigint` NULL | null ⇒ "sem ganhador" |
| `position` | `integer` NOT NULL | denormalizado do Award |
| `prize_text` | `varchar(300)` NOT NULL | denormalizado |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Constraint**: UNIQUE `(raffle_id, raffle_award_id)` enquanto Raffle
`Open`; após `Closed`, imutável.

**Regras**:

- Tickets já vencedores são excluídos do pool elegível do próximo Raffle
  quando `include_previous_winners = false` (FR-034a).
- Um mesmo Ticket pode aparecer em múltiplos `raffle_winners` quando o
  flag for `true` (FR-034b).

### `fortuna_user_referrers` — UserReferrer

| Coluna | Tipo | Notas |
|---|---|---|
| `user_referrer_id` | `bigint` PK identity | |
| `user_id` | `bigint` NOT NULL UNIQUE | UserId do NAuth |
| `referral_code` | `varchar(8)` NOT NULL UNIQUE | 8 chars, alfabeto de 32 símbolos |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Regras** (FR-R01/R02/R02a):

- Criado na primeira interação do usuário com o Fortuno.
- Código imutável; colisão retenta na geração.
- `user_id` UNIQUE — um código por usuário.

### `fortuna_invoice_referrers` — InvoiceReferrer

| Coluna | Tipo | Notas |
|---|---|---|
| `invoice_referrer_id` | `bigint` PK identity | |
| `invoice_id` | `bigint` NOT NULL UNIQUE | Id da Invoice do ProxyPay |
| `referrer_user_id` | `bigint` NOT NULL | UserId do indicador |
| `lottery_id` | `bigint` NOT NULL | cache da Lottery da compra |
| `referral_percent_at_purchase` | `real` NOT NULL | snapshot **descritivo** do `%` no momento da compra (exibição opcional) |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Notas**:

- `invoice_id` UNIQUE impede dupla vinculação.
- `referral_percent_at_purchase` é **apenas histórico descritivo**; a
  comissão exibida usa o `Lottery.referral_percent` **vigente** (Q do
  2026-04-17).

### `fortuna_refund_logs` — RefundLog

| Coluna | Tipo | Notas |
|---|---|---|
| `refund_log_id` | `bigint` PK identity | |
| `ticket_id` | `bigint` NOT NULL | FK |
| `executed_by_user_id` | `bigint` NOT NULL | quem mudou o status |
| `reference_value` | `numeric(12,2)` NOT NULL | valor de referência |
| `external_reference` | `varchar(160)` NULL | id do comprovante |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Uso**: auditoria da transição manual `PendingRefund → Refunded`.

### `fortuna_number_reservations` — reserva temporária (UserPicks)

| Coluna | Tipo | Notas |
|---|---|---|
| `reservation_id` | `bigint` PK identity | |
| `lottery_id` | `bigint` NOT NULL | |
| `user_id` | `bigint` NOT NULL | |
| `invoice_id` | `bigint` NULL | preenchido após gerar a Invoice |
| `ticket_number` | `bigint` NOT NULL | composto int64 |
| `expires_at` | `timestamp without time zone` NOT NULL | `now() + 15min` |
| `created_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |

**Constraints**:

- Partial UNIQUE index `(lottery_id, ticket_number) WHERE expires_at > now()`
  — garante exclusividade apenas de reservas vigentes.
- Ao emitir tickets no webhook, promove reservas para `fortuna_tickets` e
  remove registro (ou marca `expires_at = now()`).

### `fortuna_webhook_events` — idempotência de webhook

| Coluna | Tipo | Notas |
|---|---|---|
| `webhook_event_id` | `bigint` PK identity | |
| `invoice_id` | `bigint` NOT NULL | |
| `event_type` | `varchar(40)` NOT NULL | ex.: `invoice.paid` |
| `received_at` | `timestamp without time zone` NOT NULL DEFAULT now() | |
| `payload_hash` | `varchar(64)` NULL | SHA-256 opcional |

**Constraint**: UNIQUE `(invoice_id, event_type)` — garante idempotência
(FR-029b). Segundo webhook com mesmo par é ignorado.

---

## Entidades externas (referências)

| Entidade | Origem | Campos usados pelo Fortuno |
|---|---|---|
| `User` | NAuth | `UserId`, `Name`, `Email`, `DocumentId` (CPF), `Phone`, `Address` |
| `Store` | ProxyPay | `StoreId`, `OwnerUserId`, `Name` |
| `Invoice` | ProxyPay | `InvoiceId`, `StoreId`, `Amount`, `PaidAt`, `Status` |

O Fortuno **não replica** esses dados — apenas guarda os IDs e busca dados
sob demanda via `NAuthAppService` / `ProxyPayAppService`.

---

## Relacionamentos (resumo)

```text
Store (externa) ──1:N──▶ Lottery
  Lottery ──1:N──▶ LotteryImage
  Lottery ──1:N──▶ LotteryCombo
  Lottery ──1:N──▶ Raffle
    Raffle ──1:N──▶ RaffleAward
    Raffle ──1:N──▶ RaffleWinner
      RaffleWinner ──N:1──▶ Ticket (nullable — "sem ganhador")
  Lottery ──1:N──▶ Ticket
    Ticket ──N:1──▶ Invoice (externa, ProxyPay)
    Ticket ──N:1──▶ User (externo, NAuth)
    Ticket ──1:N──▶ RefundLog

User (externo) ──1:1──▶ UserReferrer
Invoice (externa) ──1:1──▶ InvoiceReferrer

Lottery ──1:N──▶ NumberReservation (transient)
Invoice (externa) ──1:N──▶ WebhookEvent
```

Todas as FKs locais usam `DeleteBehavior.ClientSetNull` (constituição §3).
Nenhuma FK aponta para entidade externa com constraint do banco — apenas
`bigint` sem FK física.

---

## Índices adicionais

- `fortuna_tickets (lottery_id, refund_state)` — acelera painel do dono.
- `fortuna_tickets (user_id, created_at DESC)` — acelera "Meus Tickets".
- `fortuna_lotteries (status)` — acelera queries GraphQL de listagem
  (SC-007).
- `fortuna_lotteries (slug)` já UNIQUE — lookup por slug.
- `fortuna_invoice_referrers (referrer_user_id, lottery_id)` — painel do
  indicador.

---

## Validações (FluentValidation — skill)

Validadores por DTO:

- `LotteryInsertInfoValidator`: `name` 3..160, `ticket_price > 0`,
  `total_prize_value > 0`, `referral_percent 0..100`,
  `number_value_min <= number_value_max`, para compostos
  `ticket_num_end >= ticket_num_ini` ou ambos zero (zero só em `Draft`).
- `LotteryCancelInfoValidator`: `cancel_reason` length ≥ 20.
- `PurchaseRequestInfoValidator`: `quantity >= 1`, dentro de
  `[ticket_min, ticket_max]` quando > 0, `mode ∈ {Random, UserPicks}`,
  para `UserPicks` `numbers.Count == quantity` e cada número dentro do
  pool/faixa composta.
- `RaffleInsertInfoValidator`: `raffle_datetime` livre (a data não é
  gate), `name` 3..160.
- `RaffleCancelRequestValidator`: `redistributions[]` cobre 100% dos
  Awards órfãos.
- `RaffleWinnersPreviewRequestValidator`: `numbers[]` não vazio; cada
  número é int64 válido conforme tipo da Lottery.
