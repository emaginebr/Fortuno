# Contract — Bruno Collection Layout

**Feature**: 002-qa-test-suite
**User Story**: 3 (Priority P3)

Estrutura e convenções da coleção Bruno em `/bruno/`. O que está neste contrato é a **forma**; os payloads de exemplo específicos são copiados/derivados de `contracts/lottery-lifecycle.md` e dos contratos dos demais controllers.

---

## Estrutura de pastas

```text
bruno/
├── bruno.json                        # metadata da coleção
├── environments/
│   ├── local.bru                     # variáveis para ambiente local
│   ├── dev.bru                       # ambiente de desenvolvimento
│   ├── prod.bru                      # produção (somente leitura recomendada)
│   └── *.example.bru                 # template versionado (sem segredos)
├── _Auth/
│   └── login.bru
├── Lotteries/
│   ├── create.bru
│   ├── get-by-id.bru
│   ├── get-by-slug.bru
│   ├── list-by-store.bru
│   ├── update.bru
│   ├── publish.bru
│   ├── close.bru
│   └── cancel.bru
├── LotteryCombos/
│   ├── create.bru
│   ├── update.bru
│   ├── delete.bru
│   └── list-by-lottery.bru
├── LotteryImages/
│   ├── create.bru
│   ├── update.bru
│   ├── delete.bru
│   └── list-by-lottery.bru
├── Raffles/
│   ├── create.bru
│   ├── get-by-id.bru
│   ├── list-by-lottery.bru
│   ├── preview-winners.bru
│   ├── confirm-winners.bru
│   └── close.bru
├── RaffleAwards/
│   ├── create.bru
│   ├── update.bru
│   ├── delete.bru
│   └── list-by-raffle.bru
├── Tickets/
│   ├── list-mine.bru
│   └── get-by-id.bru
├── Purchases/
│   ├── preview.bru
│   └── confirm.bru
├── Referrals/
│   ├── get-me.bru
│   └── get-code.bru
├── Commissions/
│   └── list-by-lottery.bru
├── Refunds/
│   ├── list-pending.bru
│   └── mark-refunded.bru
└── Webhooks/
    └── proxypay-invoice-paid.bru
```

## Variáveis de ambiente

Cada arquivo `environments/{env}.bru` declara:

```text
vars {
  baseUrl:       https://api.fortuno.example
  nauthUrl:      https://auth.fortuno.example
  nauthTenant:   fortuna
  nauthUser:     <preenchido localmente, não versionado em segredos reais>
  nauthPassword: <idem>
  accessToken:   <populado via post-response de login.bru>
  storeId:       <id da store no ProxyPay>
  lotteryId:     <populado via post-response de Lotteries/create.bru>
  raffleId:      <populado via post-response de Raffles/create.bru>
}
```

Apenas os arquivos `*.example.bru` (com placeholders) são versionados. Os `.bru` reais (com credenciais) ficam em `.gitignore`.

## Convenções de request

1. **Headers padrão** em toda request autenticada:

   ```text
   headers {
     Authorization: Basic {{accessToken}}
     Content-Type:  application/json
   }
   ```

2. **Sem Authorization** em requests marcadas `[AllowAnonymous]` (get-by-id, get-by-slug de Lotteries, webhooks).

3. **Post-response scripts** capturam IDs para a próxima request:

   ```text
   script:post-response {
     if (res.status === 201 || res.status === 200) {
       if (res.body.lotteryId) bru.setVar("lotteryId", res.body.lotteryId);
       if (res.body.raffleId)  bru.setVar("raffleId",  res.body.raffleId);
     }
   }
   ```

4. **Payloads de exemplo** devem passar pelos validators FluentValidation atuais — ver `data-model.md` para a lista de regras por validator.

## Ordem sugerida de execução (runner Bruno)

1. `_Auth/login` → popula `accessToken`.
2. `Lotteries/create` → popula `lotteryId`.
3. `Lotteries/publish`.
4. `Raffles/create` (usa `lotteryId`) → popula `raffleId`. *(exploratório — ApiTests não cobre)*
5. `RaffleAwards/create` (usa `raffleId`). *(exploratório)*
6. `Raffles/preview-winners` → `Raffles/confirm-winners`. *(exploratório, requer tickets)*
7. `Raffles/close` → `Lotteries/close`.

A sequência é **documentação**; o Bruno runner pode executar subconjuntos conforme necessidade manual.
