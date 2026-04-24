# Frontend — Migração do formato de número de ticket e faixas por NumberType

**Audiência:** agente `frontend-react-developer` (ou dev mantendo o cliente web/mobile do Fortuno).
**Status backend:** implementado no branch `003-ticket-qrcode-purchase`.
**Impacto:** 3 mudanças que quebram contrato na API de Tickets e Lotteries.

---

## 1. O que mudou e por quê

### Problema
Antes, o número de um ticket trafegava em dois formatos paralelos pela API:
- `long` (numérico puro) em `TicketOrderRequest.pickedNumbers`, `TicketSearchQuery.number` e `NumberReservationRequest.ticketNumber`.
- `string` formatada (ex.: `"05-11-28-39-60"`) em `TicketInfo.ticketValue`.

Isso forçava o frontend a converter manualmente entre as duas representações e abria espaço para bugs (ordem de componentes, zero-padding, etc.). Além disso, as regras de faixa dos números da lottery estavam **sobrecarregadas**: `TicketNumIni/End` e `NumberValueMin/Max` eram validados juntos independentemente do `NumberType`, o que gerava configurações inválidas.

### Solução
Todos os inputs/outputs de número passaram a usar **o mesmo formato string canônico** que já existia no `ticketValue`. O backend faz parse, normalização (ordena + zero-pad em composed) e validação. E agora cada `NumberType` usa **apenas uma das duas faixas** da lottery, eliminando a sobrecarga.

---

## 2. Resumo executivo das mudanças

| # | Endpoint/DTO | Antes | Depois |
|---|---|---|---|
| 1 | `POST /tickets/qrcode` body | `pickedNumbers: long[]` | `pickedNumbers: string[]` |
| 2 | `POST /tickets/reserve-number` body | `ticketNumber: long` | `ticketNumber: string` |
| 2 | `POST /tickets/reserve-number` response | `ticketNumber: long` | `ticketNumber: string` (formato canônico) |
| 3 | `GET /tickets/mine` query | `number: long` + `ticketValue: string` | `number: string` (único) |
| 4 | `POST /lotteries` validação | ambas faixas sempre validadas | regras por `NumberType` (ver seção 5) |

---

## 3. Formato do número ao trafegar

### Regra única
O número sempre trafega como **string** em 2 formatos possíveis:

| NumberType | Formato | Exemplo |
|---|---|---|
| `Int64` (0) | decimal direto | `"42"`, `"1000"` |
| `Composed2..Composed8` (1..7) | componentes de 2 dígitos separados por `-` | `"05-11-28-39-60"` |

### Normalização no backend
Para composed, a ordem que o frontend envia **não importa**. O backend:
1. Divide por `-`.
2. Verifica cada componente ∈ `[0..99]`.
3. Ordena ascendente.
4. Reformata com zero-pad de 2 dígitos.

Exemplos equivalentes (todos viram `"05-11-28-39-60"` no banco):
- `"5-11-28-39-60"`
- `"60-39-5-28-11"`
- `"28-60-05-11-39"`

### Nota sobre `TicketInfo.ticketValue`
O campo `ticketValue` na resposta (já existente) **continua sendo o formato canônico** (ordenado + zero-padded). Quando você precisar exibir ou comparar, use `ticketValue`.

---

## 4. Mudança 1 — `POST /tickets/qrcode` com `pickedNumbers: string[]`

### Request — antes
```json
{
  "lotteryId": 1,
  "quantity": 3,
  "mode": 2,
  "pickedNumbers": [42, 123, 500]
}
```

### Request — depois
```json
{
  "lotteryId": 1,
  "quantity": 3,
  "mode": 2,
  "pickedNumbers": ["42", "123", "500"]
}
```

Para lottery Composed (exemplo Composed5):
```json
{
  "lotteryId": 7,
  "quantity": 2,
  "mode": 2,
  "pickedNumbers": ["05-11-28-39-60", "02-14-22-31-48"]
}
```

### Erros novos possíveis (400 BadRequest)
- `"Número '05-11-28-39-60' em formato inválido para tipo Int64."` — formato não corresponde ao `NumberType` da lottery.
- `"Número '05-99-100' em formato inválido para tipo Composed3."` — componente fora de `[0..99]`, contagem de componentes errada etc.
- `"Número '05-11-28-39-60': componentes fora da faixa [1..50]."` — componente fora de `[NumberValueMin..NumberValueMax]` da lottery (Composed).
- `"Número '9999' fora da faixa [1..1000]."` — Int64 fora de `[TicketNumIni..TicketNumEnd]`.

### Tipagem TS

```ts
export interface TicketOrderRequest {
  lotteryId: number;
  quantity: number;
  mode: 1 | 2;                  // Random | UserPicks
  pickedNumbers?: string[];     // obrigatório em UserPicks, mesma contagem de quantity
  referralCode?: string;
}
```

---

## 5. Mudança 2 — `POST /tickets/reserve-number` com `ticketNumber: string`

### Request — antes
```json
{ "lotteryId": 1, "ticketNumber": 42 }
```

### Request — depois
```json
{ "lotteryId": 1, "ticketNumber": "42" }
```

```json
{ "lotteryId": 7, "ticketNumber": "60-39-05-28-11" }
```

### Response — mudança de tipo em `ticketNumber`

**Antes:**
```json
{
  "success": true,
  "status": 1,
  "message": "Número 42 reservado com sucesso por 5 minutos.",
  "lotteryId": 1,
  "ticketNumber": 42,
  "expiresAt": "2026-04-24T14:35:00Z"
}
```

**Depois** (note que `ticketNumber` vem no formato canônico mesmo que o cliente tenha enviado desordenado):
```json
{
  "success": true,
  "status": 1,
  "message": "Número 05-11-28-39-60 reservado com sucesso por 5 minutos.",
  "lotteryId": 7,
  "ticketNumber": "05-11-28-39-60",
  "expiresAt": "2026-04-24T14:35:00Z"
}
```

### Tipagem TS

```ts
export type NumberReservationStatus = 1 | 2 | 3; // Reserved | AlreadyPurchased | AlreadyReserved

export interface NumberReservationRequest {
  lotteryId: number;
  ticketNumber: string;
}

export interface NumberReservationResult {
  success: boolean;
  status: NumberReservationStatus;
  message: string;
  lotteryId: number;
  ticketNumber: string;       // formato canônico
  expiresAt?: string | null;  // ISO, só quando success=true
}
```

---

## 6. Mudança 3 — `GET /tickets/mine` com filtro `number: string`

### Query params — antes
```http
GET /tickets/mine?number=42&ticketValue=05-11-28-39-60
```

Dois filtros paralelos (redundantes) para o mesmo conceito.

### Query params — depois
```http
GET /tickets/mine?number=42
GET /tickets/mine?number=05-11-28-39-60
GET /tickets/mine?number=60-39-05-28-11       # backend normaliza
```

`ticketValue` **foi removido** — o campo `number` cobre os dois formatos.

### Tipagem TS

```ts
export interface TicketSearchQuery {
  lotteryId?: number;
  number?: string;       // Int64 → "42"; Composed → "05-11-28-39-60"
  fromDate?: string;     // ISO
  toDate?: string;       // ISO
  page?: number;         // default 1
  pageSize?: number;     // default 20, max 200
}
```

---

## 7. Mudança 4 — Regras de faixa por `NumberType`

### Semântica nova

| NumberType | Campo usado para faixa | Campo ignorado |
|---|---|---|
| `Int64` (0) | `TicketNumIni` e `TicketNumEnd` | `NumberValueMin`, `NumberValueMax` |
| `Composed*` (1..7) | `NumberValueMin` e `NumberValueMax` (faixa de cada componente) | `TicketNumIni`, `TicketNumEnd` |

### Validação no `POST /lotteries`

**Se `NumberType = Int64`:**
- `TicketNumEnd > 0`
- `TicketNumEnd >= TicketNumIni`
- `NumberValueMin/Max` podem ser `0` (são ignorados)

Mensagem de erro: `"Para NumberType Int64, TicketNumEnd deve ser maior ou igual a TicketNumIni e maior que zero."`

**Se `NumberType != Int64` (Composed):**
- `NumberValueMin ∈ [0..99]`
- `NumberValueMax ∈ [0..99]`
- `NumberValueMin <= NumberValueMax`
- `TicketNumIni/End` podem ser `0` (são ignorados)

Mensagem de erro: `"Para tipos compostos, NumberValueMin/Max deve estar em [0..99] e Min ≤ Max."`

### UI sugerida

No formulário de criação de lottery, sugestão:

1. Selecionar o `NumberType` primeiro.
2. Mostrar **apenas os campos relevantes** ao tipo selecionado:
   - Int64: inputs "Número inicial" (`TicketNumIni`) e "Número final" (`TicketNumEnd`).
   - Composed: inputs "Valor mínimo por componente" (`NumberValueMin`) e "Valor máximo por componente" (`NumberValueMax`) — ambos entre 0 e 99.
3. Preencher os campos ignorados com `0` ao submeter (são persistidos mas não utilizados).

### Payload exemplo — Int64

```json
{
  "name": "Rifa simples",
  "ticketPrice": 5.00,
  "totalPrizeValue": 10000,
  "numberType": 0,
  "ticketNumIni": 1,
  "ticketNumEnd": 1000,
  "numberValueMin": 0,
  "numberValueMax": 0,
  "... outros campos": "..."
}
```

### Payload exemplo — Composed5

```json
{
  "name": "Mega da sorte",
  "ticketPrice": 10.00,
  "totalPrizeValue": 100000,
  "numberType": 5,
  "ticketNumIni": 0,
  "ticketNumEnd": 0,
  "numberValueMin": 1,
  "numberValueMax": 60,
  "... outros campos": "..."
}
```

---

## 8. Checklist para o dev

### Criação de lottery (`POST /lotteries`)
- [ ] Form condicional: mostrar `TicketNumIni/End` só quando NumberType = Int64.
- [ ] Form condicional: mostrar `NumberValueMin/Max` só quando NumberType ≠ Int64.
- [ ] Tratar os novos erros de validação de faixa (mensagens em seção 7).

### Compra (`POST /tickets/qrcode` em modo UserPicks)
- [ ] Converter o componente de seleção de números para emitir **strings** na lista `pickedNumbers`.
- [ ] Para composed, pode enviar ordem arbitrária — backend normaliza.
- [ ] Atualizar tipagem `TicketOrderRequest` conforme seção 4.

### Reserva (`POST /tickets/reserve-number`)
- [ ] Enviar `ticketNumber` como string.
- [ ] Ler `ticketNumber` da resposta como string (formato canônico) — use direto para exibir/comparar.
- [ ] Atualizar tipagem `NumberReservationRequest` / `NumberReservationResult`.

### Listagem (`GET /tickets/mine`)
- [ ] Remover o envio do parâmetro `ticketValue` (não existe mais).
- [ ] Enviar `number` como string — unifica filtragem.
- [ ] Backend normaliza componentes desordenados, então UI pode enviar o que o usuário digitou.

### Geral
- [ ] Atualizar tipagens TS de `TicketInfo.ticketValue` (já era string — sem mudança) e garantir que a UI use `ticketValue` para exibir.
- [ ] Atualizar mocks/fixtures de teste.
- [ ] Rodar smoke test: criar lottery Int64 + Composed, comprar via Random e UserPicks, reservar número, listar tickets filtrando por número.

---

## 9. Tipagens TS consolidadas

```ts
export enum NumberType {
  Int64 = 0,
  Composed2 = 1,
  Composed3 = 2,
  Composed4 = 3,
  Composed5 = 4,
  Composed6 = 5,
  Composed7 = 6,
  Composed8 = 7,
}

export interface TicketOrderRequest {
  lotteryId: number;
  quantity: number;
  mode: 1 | 2;
  pickedNumbers?: string[];
  referralCode?: string;
}

export interface NumberReservationRequest {
  lotteryId: number;
  ticketNumber: string;
}

export interface NumberReservationResult {
  success: boolean;
  status: 1 | 2 | 3;
  message: string;
  lotteryId: number;
  ticketNumber: string;
  expiresAt?: string | null;
}

export interface TicketSearchQuery {
  lotteryId?: number;
  number?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface TicketInfo {
  ticketId: number;
  lotteryId: number;
  userId: number;
  invoiceId: number;
  ticketNumber: number;       // valor long canônico — backend
  ticketValue: string;        // representação exibível — use este
  refundState: number;
  createdAt: string;
}
```

---

## 10. Helpers utilitários sugeridos no frontend

### Parse e formatação de input pelo usuário
Para compor o input numérico no form, o frontend não precisa replicar toda a lógica do backend — basta permitir texto livre e deixar o backend validar. Mas para UX melhor (ex.: mostrar preview ordenado), pode usar:

```ts
/**
 * Normaliza input composed no cliente — mesmo algoritmo do backend.
 * Útil para preview antes de enviar ao servidor.
 *
 * Int64: trim + valida long.
 * Composed: split "-", valida cada componente [0..99], ordena, zero-pad.
 */
export function normalizeTicketNumber(input: string, componentCount: number): string | null {
  const text = input.trim();
  if (componentCount === 1) {
    return /^\d+$/.test(text) ? text : null;
  }
  const parts = text.split('-');
  if (parts.length !== componentCount) return null;
  const nums = parts.map(p => Number(p));
  if (nums.some(n => !Number.isInteger(n) || n < 0 || n > 99)) return null;
  nums.sort((a, b) => a - b);
  return nums.map(n => String(n).padStart(2, '0')).join('-');
}

export function componentCountFromNumberType(type: NumberType): number {
  switch (type) {
    case NumberType.Int64: return 1;
    case NumberType.Composed2: return 2;
    case NumberType.Composed3: return 3;
    case NumberType.Composed4: return 4;
    case NumberType.Composed5: return 5;
    case NumberType.Composed6: return 6;
    case NumberType.Composed7: return 7;
    case NumberType.Composed8: return 8;
  }
}
```

---

## 11. Referências no backend

- `Fortuno.DTO/Ticket/TicketOrderRequest.cs` — `PickedNumbers: List<string>?`.
- `Fortuno.DTO/Ticket/TicketSearchQuery.cs` — `Number: string?`, sem `TicketValue`.
- `Fortuno.DTO/Ticket/NumberReservationRequest.cs` / `NumberReservationResult.cs` — `TicketNumber: string`.
- `Fortuno.Domain/Services/NumberCompositionService.cs` — métodos `TryParse` / `Parse` / `Format`.
- `Fortuno.Domain/Services/TicketService.cs` — `ParseAndValidatePickedNumbers`, `ValidateNumberRange`, `NormalizeNumberFilter`.
- `Fortuno.API/Validators/LotteryInsertInfoValidator.cs` — regras condicionais `.When(NumberType == Int64)` / `.When(NumberType != Int64)`.
- `bruno/Tickets/reserve-number.bru` e `bruno/Tickets/qrcode-create.bru` — exemplos funcionais.
