# Frontend — Migração para Store Transparente

**Audiência:** agente `frontend-react-developer` (ou qualquer dev mantendo o cliente web/mobile do Fortuno).
**Status backend:** implementado no branch `003-ticket-qrcode-purchase`, PR pendente.
**Impacto:** mudança que quebra contrato na API pública de Lotteries (3 pontos). Frontend precisa se adaptar antes do merge para produção.

---

## 1. O que mudou e por quê

### Problema
A API exigia que o cliente (frontend, Bruno) soubesse e passasse `storeId`:
- `POST /lotteries` exigia `storeId` no body.
- `GET /lotteries/store/{storeId}` exigia `storeId` na URL.

Consequência prática: o frontend tinha **`storeId: 1` hardcoded**, o que fazia todos os usuários compartilharem a mesma loja (Store 1). Usuário novo que tentasse comprar um bilhete recebia `Store 1 não encontrada no ProxyPay` porque não era dono daquela loja.

### Solução
A Store passou a ser **transparente para o cliente**:
- O backend resolve automaticamente a store do usuário logado via GraphQL `myStore` no ProxyPay.
- Se o usuário ainda não tem store, o backend **cria uma** (REST `POST /store` no ProxyPay) usando nome e e-mail vindos do NAuth.
- O frontend nunca mais toca em `storeId`.

Princípio: o cliente autentica, cria lotteries e lista as próprias lotteries. O conceito de "loja" é detalhe interno do backend/ProxyPay.

---

## 2. Diff de endpoints

| Endpoint | Antes | Depois |
|---|---|---|
| `POST /lotteries` | body incluía `"storeId": number` (obrigatório) | body **sem** `storeId` |
| `GET /lotteries/store/{storeId}` | rota autenticada, precisa passar `storeId` na URL | **removida** — substituída por `/lotteries/mine` |
| `GET /lotteries/mine` | — | **nova** rota autenticada, lista as lotteries do usuário logado |
| Demais rotas de Lottery (`GET /lotteries/{id}`, `/slug/{slug}`, `/open`, `PUT`, `/publish`, `/revert-to-draft`, `/close`, `/cancel`, `DELETE`, `/possibilities`, `/rules.pdf`, `/privacy-policy.pdf`) | — | **sem mudança** |

---

## 3. Mudanças de payload

### 3.1 `POST /lotteries`

**Request — antes**
```json
{
  "storeId": 1,
  "editionNumber": 1,
  "name": "Sorteio Moto R$ 100.000",
  "descriptionMd": "...",
  "rulesMd": "...",
  "privacyPolicyMd": "...",
  "ticketPrice": 0.99,
  "totalPrizeValue": 100000,
  "ticketMin": 1,
  "ticketMax": 0,
  "ticketNumIni": 0,
  "ticketNumEnd": 999999,
  "numberType": 0,
  "numberValueMin": 0,
  "numberValueMax": 999999,
  "referralPercent": 0
}
```

**Request — depois**
```json
{
  "editionNumber": 1,
  "name": "Sorteio Moto R$ 100.000",
  "descriptionMd": "...",
  "rulesMd": "...",
  "privacyPolicyMd": "...",
  "ticketPrice": 0.99,
  "totalPrizeValue": 100000,
  "ticketMin": 1,
  "ticketMax": 0,
  "ticketNumIni": 0,
  "ticketNumEnd": 999999,
  "numberType": 0,
  "numberValueMin": 0,
  "numberValueMax": 999999,
  "referralPercent": 0
}
```

**Response — sem mudança na forma**, mas `storeId` agora vem definido automaticamente pelo backend (valor derivado da store do usuário logado). Também há o campo `storeClientId` preenchido (identificador do tenant no ProxyPay, **não** mostrar ao usuário final — é dado interno):

```json
{
  "lotteryId": 77,
  "storeId": 42,
  "storeClientId": "5b2a4084154d4e88941b76aee1395348",
  "editionNumber": 1,
  "name": "...",
  "slug": "sorteio-moto-r-100000",
  "status": 1,
  "...": "..."
}
```

**Novos códigos de erro possíveis em `POST /lotteries`:**
- `400 Bad Request` com mensagem `"Usuário autenticado não encontrado no NAuth."` — problema no token/NAuth; frontend deve tratar como "sua sessão expirou ou tem problema" e levar para re-login.
- `400 Bad Request` com `"Usuário sem nome ou e-mail cadastrado — não é possível criar Store automaticamente."` — o perfil do usuário no NAuth está incompleto. Frontend deve orientar o usuário a completar o cadastro (nome e e-mail) antes de criar sua primeira lottery.
- `500 Internal Server Error` da chamada ao ProxyPay ao tentar criar a store (indisponibilidade externa). Frontend trata como erro genérico de retry.

### 3.2 `GET /lotteries/mine` (substitui `GET /lotteries/store/{storeId}`)

**Request**
```http
GET /lotteries/mine
Authorization: Bearer <token>
```

Sem parâmetros de query, sem path params.

**Response** (mesma forma que o endpoint antigo retornava)
```json
[
  {
    "lotteryId": 77,
    "storeId": 42,
    "editionNumber": 1,
    "name": "...",
    "status": 1,
    "...": "..."
  },
  { "...": "..." }
]
```

- Retorna `[]` se o usuário **nunca criou uma lottery** (porque ainda nem tem store).
- Ordenação: mesma que `ListByStoreAsync` mantinha (não estipulada no código — ordem do banco).

---

## 4. Ações para o frontend

### 4.1 Remover `storeId` do fluxo de criação
- Deletar o campo do formulário (se existia), do state local, do redux/zustand/etc.
- Remover do payload de `POST /lotteries`.
- Se o frontend tinha configuração tipo `const DEFAULT_STORE_ID = 1` — **deletar**.

### 4.2 Trocar listagem "por store" por listagem "minhas"
- Onde hoje chama `GET /lotteries/store/{storeId}`: trocar por `GET /lotteries/mine` sem parâmetros.
- Se houver UI que mostra uma seleção de store (dropdown, menu), **remover** — não há mais múltiplas stores por usuário no fluxo do cliente.
- Se o usuário acabou de criar conta e não tem lotteries: `/lotteries/mine` retorna `[]`. Renderizar empty state convidando a criar a primeira lottery.

### 4.3 Esconder `storeId` da UI
- `storeId` continua vindo na resposta de `GET /lotteries/{id}`, `GET /lotteries/slug/{slug}`, etc., mas **não deve ser mostrado** ao usuário. É detalhe interno.
- `storeClientId` (também retornado) é ainda mais sensível — é o identificador do tenant no ProxyPay. Nunca exibir, nunca logar.

### 4.4 Tratamento de erro de perfil incompleto
Adicionar tratamento para o caso `400 "Usuário sem nome ou e-mail cadastrado..."`:
- Sugestão: modal ou tela que leva o usuário para editar o perfil no NAuth antes de criar a lottery.
- Mensagem amigável do tipo: _"Complete seu cadastro (nome e e-mail) antes de criar seu primeiro sorteio."_

### 4.5 Fluxo de compra de ticket (comprador)
- **Nenhuma mudança de API** para o comprador (`POST /tickets/qrcode`, `GET /tickets/qrcode/{invoiceId}/status`).
- O bug original — `Store 1 não encontrada no ProxyPay` ao tentar comprar — deixa de acontecer. Pode remover qualquer workaround que tenha sido adicionado para contornar esse erro.

---

## 5. Checklist para o dev

- [ ] Remover `storeId` do form/state de criação de lottery.
- [ ] Remover hardcode de `storeId: 1` (ou qualquer outro) em configs/mocks.
- [ ] Substituir chamadas a `/lotteries/store/{storeId}` por `/lotteries/mine`.
- [ ] Esconder `storeId` e `storeClientId` de qualquer view/componente onde hoje apareçam.
- [ ] Tratar novo erro de NAuth/perfil incompleto no create de lottery.
- [ ] Testar fluxo completo com usuário recém-cadastrado: criar lottery → listar `/lotteries/mine` → criar ticket como outro usuário → pagar.
- [ ] Atualizar tipagens TS (`LotteryInsertInfo` sem `storeId`; resposta `LotteryInfo` mantém `storeId` e adiciona `storeClientId` opcional).
- [ ] Atualizar fixtures/mocks de teste.
- [ ] Atualizar Storybook/docs de componentes que antes mostravam Store Selector.

---

## 6. Tipagens TypeScript sugeridas

```ts
export interface LotteryInsertInfo {
  editionNumber: number;
  name: string;
  descriptionMd: string;
  rulesMd: string;
  privacyPolicyMd: string;
  ticketPrice: number;
  totalPrizeValue: number;
  ticketMin: number;
  ticketMax: number;
  ticketNumIni: number;
  ticketNumEnd: number;
  numberType: 0 | 1 | 2 | 3 | 4 | 5;  // Int64 | Composed2..Composed5
  numberValueMin: number;
  numberValueMax: number;
  referralPercent: number;
}

export interface LotteryInfo extends LotteryInsertInfo {
  lotteryId: number;
  storeId: number;               // interno — não exibir
  storeClientId?: string | null; // interno — não exibir nem logar
  slug: string;
  status: 1 | 2 | 3 | 4;          // Draft | Open | Closed | Cancelled
  createdAt: string;
  updatedAt: string;
  images: LotteryImageInfo[];
  combos: LotteryComboInfo[];
  raffles: RaffleInfo[];
}
```

---

## 7. Dados legados

Lotteries que já existiam no banco antes desta mudança têm `store_id = 1` (a store compartilhada antiga). Elas continuam navegáveis via `GET /lotteries/{id}` e `/slug/{slug}`, mas **não aparecem em `/lotteries/mine`** para seus donos reais (exceto quem for o dono da Store 1 no ProxyPay).

Se isso precisar ser corrigido retroativamente:
- Opção (a): o dono recria a lottery (se ainda estiver em `Draft`/`Cancelled`, `DELETE /lotteries/{id}` + criar de novo).
- Opção (b): backfill via SQL em produção — fora do escopo do frontend.

Não é necessário o frontend fazer nada sobre isso, mas esteja ciente de que usuários que criaram lotteries na versão antiga podem não vê-las em `/lotteries/mine`.

---

## 8. Referências do backend

Arquivos-chave para quem precisar inspecionar o lado servidor:

- `Fortuno.API/Controllers/LotteriesController.cs` — endpoints.
- `Fortuno.Domain/Services/LotteryService.cs` — `CreateAsync`, `ListMineAsync`.
- `Fortuno.Infra/AppServices/ProxyPayAppService.cs` — `GetMyStoreAsync`, `CreateStoreAsync`, `EnsureMyStoreAsync`.
- `Fortuno.DTO/Lottery/LotteryInsertInfo.cs` — DTO sem `StoreId`.
- `bruno/Lotteries/create.bru` e `bruno/Lotteries/list-mine.bru` — exemplos funcionais.

Spec de origem: `specs/003-ticket-qrcode-purchase/research.md` §R-004.
