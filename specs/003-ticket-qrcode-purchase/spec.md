# Feature Specification: Compra de Ticket via QR Code PIX (sem webhook, sem preview)

**Feature Branch**: `003-ticket-qrcode-purchase`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "Faça alguma mudanças no processo de compra do ticket: Use o TicketController para os endpoints; Não precisa desse preview, isso será feito no frontend, pode eliminar isso; Para o pagamento o ProxyPayAppService deve usar o endpoint POST /payment/qrcode com {clientId, customer, items} e response {invoiceId, invoiceNumber, brCode, brCodeBase64, expiredAt}; Para validar o pagamento o frontend irá chamar um endpoint chamado CheckQRCodeStatus no Fortuno que acessa GET /payment/qrcode/status/{invoiceId} no ProxyPay; Todos os endpoints do ProxyPay usam o X-Tenant-Id; Se o endpoint no ProxyPay confirmar o pagamento, deve processar um ProcessPayment no TicketService; Não deve usar Webhooks nesse projeto."

## Clarifications

### Session 2026-04-19

- Q: No modo `Random`, o sistema deve reservar números na criação do QR Code ou só sortear no `ProcessPayment`? → A: Manter comportamento atual — `Random` não reserva; sorteia em `ProcessPayment` sobre o pool disponível no momento da confirmação; se faltar pool, pagamento vai para estorno manual (FR-016).
- Q: Fonte dos dados de `customer` enviados ao `POST /payment/qrcode`? → A: Ler do NAuth via `INAuthAppService.GetCurrentAsync()` no momento da criação do QR Code; se algum campo obrigatório (`name`, `email`, `documentId`, `cellphone`) vier vazio, retornar 400 orientando o comprador a completar o cadastro no NAuth.
- Q: Quem dispara a liberação das reservas `UserPicks` expiradas? → A: Lazy em toda consulta de disponibilidade — as leituras filtram por `ExpiresAt > now` (nenhum job em background, nenhum write-off explícito na consulta de status).
- Q: Shape da resposta do `CheckQRCodeStatus`? → A: Resposta completa — `{ status, invoiceId, expiredAt, brCode (quando Pending), brCodeBase64 (quando Pending), tickets: [...] (quando Paid), refundHint (quando Paid mas Lottery fora de Open) }`. Uma chamada cobre todos os usos do frontend (evita GET /tickets/mine imediatamente após pagar; suporta reload com perda de QR Code).
- Q: Como o sistema verifica a posse do `invoiceId` em `CheckQRCodeStatus`? → A: **Sem checagem de posse**. Qualquer usuário autenticado pode consultar qualquer `invoiceId` — confia na imprevisibilidade do ID. Isso simplifica o endpoint e evita necessidade de query em `TicketOrder` só para autorização. FR-004 é revertido.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Comprador inicia compra e recebe QR Code PIX (Priority: P1)

Comprador autenticado escolhe a Lottery, informa quantidade e modo de atribuição (aleatório ou escolha própria). O sistema calcula o valor final, cria a cobrança PIX no provedor de pagamento e devolve o QR Code (imagem base64 + `brCode` copia-e-cola) com prazo de expiração. O comprador paga via app bancário.

**Why this priority**: É o ponto de entrada do fluxo monetário do produto. Sem este passo, não há receita. A ausência do preview no backend simplifica o endpoint e remove uma fonte de inconsistência entre o valor mostrado ao usuário e o valor efetivamente cobrado.

**Independent Test**: O comprador faz `POST` no endpoint de compra do `TicketController` com `lotteryId`, `quantity`, `mode` (e `pickedNumbers` quando `UserPicks`), e recebe o QR Code PIX pronto para pagamento. Pode ser validado sem executar o passo de confirmação, bastando inspecionar a resposta.

**Acceptance Scenarios**:

1. **Given** uma Lottery em status `Open` com pool de números suficiente, **When** o comprador envia compra com `mode=Random` e `quantity=5`, **Then** o sistema devolve `invoiceId`, `invoiceNumber`, `brCode`, `brCodeBase64` e `expiredAt`.
2. **Given** uma Lottery em status `Open`, **When** o comprador envia `mode=UserPicks` com `pickedNumbers` de tamanho igual a `quantity` e números disponíveis, **Then** o sistema reserva os números, cria o QR Code e devolve a resposta.
3. **Given** uma Lottery em qualquer status diferente de `Open`, **When** o comprador tenta comprar, **Then** o sistema rejeita com mensagem indicando que compras estão disponíveis apenas em Lottery `Open`.
4. **Given** o comprador informa `quantity` fora dos limites `TicketMin`/`TicketMax` da Lottery, **When** a compra é enviada, **Then** o sistema rejeita informando o limite violado.
5. **Given** o comprador envia `mode=UserPicks` com algum número já reservado ou vendido, **When** a compra é enviada, **Then** o sistema rejeita antes de criar a cobrança.
6. **Given** o provedor de pagamento está indisponível, **When** a compra é enviada, **Then** o sistema devolve erro claro e não deixa reserva órfã (a reserva é criada somente após o QR Code ser gerado com sucesso ou é expirada caso a criação falhe).

---

### User Story 2 - Frontend valida pagamento consultando status do QR Code (Priority: P1)

Após exibir o QR Code ao comprador, o frontend faz polling periódico em um endpoint do Fortuno (`CheckQRCodeStatus`) passando o `invoiceId`. O Fortuno repassa a consulta ao provedor de pagamento, traduz o estado devolvido e, caso o pagamento tenha sido confirmado, emite os tickets e vincula a indicação (se houver). Nas consultas seguintes, o frontend recebe o estado "pago" sem nova emissão de tickets (idempotência).

**Why this priority**: É a contrapartida da US1. Sem ela, o comprador nunca tem seus tickets emitidos. A remoção do webhook significa que este polling é a **única** fonte de confirmação — portanto precisa ser idempotente e robusto.

**Independent Test**: Após criar um QR Code em US1, o frontend chama o endpoint `CheckQRCodeStatus` no `TicketController` passando o `invoiceId`. Resposta contém o estado atual (`Pending`, `Paid`, `Expired`, `Cancelled`). Quando `Paid`, os tickets correspondentes ao `invoiceId` passam a existir no Fortuno. Chamadas subsequentes não duplicam tickets.

**Acceptance Scenarios**:

1. **Given** uma compra recém-criada com QR Code ainda não pago, **When** o frontend consulta o status, **Then** o sistema retorna estado `Pending` e nenhum ticket é emitido.
2. **Given** um QR Code que foi pago entre duas consultas, **When** o frontend consulta o status pela primeira vez após o pagamento, **Then** o sistema: (a) emite os tickets associados ao `invoiceId`; (b) registra `InvoiceReferrer` quando houver código de indicação válido; (c) devolve estado `Paid` com a lista/quantidade de tickets emitidos.
3. **Given** um QR Code já confirmado em consulta anterior, **When** o frontend consulta novamente, **Then** o sistema devolve `Paid` e **não** duplica a emissão de tickets nem registra `InvoiceReferrer` duplicado.
4. **Given** um QR Code com `expiredAt` no passado e sem pagamento, **When** o frontend consulta o status, **Then** o sistema devolve estado `Expired` e libera reservas `UserPicks` associadas.
5. **Given** a Lottery foi fechada entre a criação do QR Code e o pagamento, **When** o frontend consulta o status após pagamento, **Then** o sistema devolve estado `Paid` **mas** não emite tickets e marca o `invoiceId` para estorno manual (visível no fluxo de refund já existente).
6. **Given** o provedor de pagamento retorna erro ao consultar status, **When** o frontend consulta, **Then** o sistema devolve estado `Unknown` (ou código de erro) sem alterar o estado interno, permitindo retry.
7. **Given** um `invoiceId` pertencente a outro comprador, **When** um usuário autenticado consulta o status, **Then** o sistema responde normalmente (o endpoint exige apenas autenticação, sem verificação de posse — confia na imprevisibilidade do ID); a emissão de tickets, quando ocorre, é creditada sempre ao `TicketOrder.UserId`, independente de quem disparou a consulta.

---

### User Story 3 - Eliminação definitiva do Webhook e do Preview (Priority: P2)

O código de produção e os artefatos de teste não devem mais conter caminhos relacionados a webhook do ProxyPay nem endpoint de preview de compra. O fluxo de confirmação de pagamento **é exclusivamente** o polling do frontend via `CheckQRCodeStatus`.

**Why this priority**: Garante que o refactor não deixe dois caminhos concorrentes de confirmação de pagamento (polling + webhook), o que geraria risco de dupla emissão ou divergência. O preview migrou para o frontend — mantê-lo no backend duplicaria regra e valor exibido.

**Independent Test**: Inspeção do repositório mostra: (a) nenhum controller responde rotas de webhook de pagamento; (b) `PurchaseService.ProcessPaidWebhookAsync` removido; (c) nenhuma rota de preview de compra exposta; (d) idempotência do pagamento é controlada exclusivamente no `CheckQRCodeStatus`/`ProcessPayment`. Os testes automatizados passam após remoção.

**Acceptance Scenarios**:

1. **Given** a branch mesclada, **When** se inspeciona `Fortuno.API/Controllers`, **Then** não existe `WebhooksController` com rota de pagamento (filtro HMAC, etc.).
2. **Given** a branch mesclada, **When** se inspeciona `PurchaseService` / `TicketService`, **Then** não há método que aceite um `ProxyPayWebhookPayload` como fluxo de emissão de ticket.
3. **Given** a collection Bruno, **When** se inspeciona o diretório, **Then** não há request de preview de compra e o request de webhook de pagamento foi removido ou marcado como obsoleto.
4. **Given** a documentação (`quickstart.md`, `contracts/`), **When** se lê o fluxo de compra, **Then** está descrito somente o par `POST compra → polling de status`.

---

### Edge Cases

- **Pagamento fora do prazo mas antes de o sistema marcar Expired**: o provedor pode aceitar pagamentos ligeiramente após `expiredAt`. O sistema deve tratar o estado devolvido pelo provedor como autoritativo — se veio `paid`, emite tickets (desde que a Lottery ainda esteja `Open`).
- **Pagamento parcial**: o provedor PIX não permite pagamento parcial; caso venha, o sistema deve tratar como não pago.
- **Mesmo `invoiceId` consultado por dois clientes simultaneamente após pagamento**: somente um deve emitir os tickets; o segundo deve ver estado `Paid` sem duplicar (corrida de concorrência — exige controle de idempotência por `invoiceId`).
- **Lottery fechada entre criação do QR Code e o pagamento**: QR Code fica pago mas tickets não são emitidos; fica registrado para estorno manual.
- **Indicação (referral) inválida no pagamento**: o código de indicação foi armazenado na compra; se o usuário dono do código foi suspenso entre a compra e o pagamento, a indicação não é registrada (comportamento igual ao fluxo atual).
- **Polling muito frequente**: o endpoint deve ser idempotente e barato o suficiente para sustentar polling de alguns segundos por cliente, sem bloquear outros fluxos.
- **`invoiceId` não encontrado no provedor**: o sistema deve devolver `Unknown` / `NotFound` com mensagem, sem travar.
- **Reserva `UserPicks` criada mas QR Code falhou**: a reserva deve ser revertida na mesma transação da falha, senão o número fica "preso" até o TTL.

## Requirements *(mandatory)*

### Functional Requirements

#### Endpoints do Ticket Controller

- **FR-001**: O sistema MUST expor em `TicketController` um endpoint autenticado para iniciar compra de tickets, recebendo `lotteryId`, `quantity`, `mode` (Random ou UserPicks), `pickedNumbers` (obrigatório em `UserPicks`) e `referralCode` (opcional).
- **FR-002**: O sistema MUST expor em `TicketController` um endpoint autenticado `CheckQRCodeStatus` recebendo `invoiceId` e devolvendo uma resposta estruturada com **todos** os campos úteis ao frontend em uma única chamada:
  - `status` ∈ { `Pending`, `Paid`, `Expired`, `Cancelled`, `Unknown` }
  - `invoiceId`
  - `expiredAt` (sempre presente se conhecido)
  - `brCode` e `brCodeBase64` — presentes quando `status == Pending` (permite o front recuperar o QR após reload)
  - `tickets: [...]` — lista dos tickets emitidos (mesmo shape de `TicketInfo`) quando `status == Paid` e a emissão ocorreu
  - `refundHint` — presente quando `status == Paid` mas a emissão **não** ocorreu (Lottery saiu de `Open` entre a compra e o pagamento, ou reserva `UserPicks` expirou, ou pool insuficiente em `Random`); orienta o front a informar o comprador e direciona para o fluxo de refund manual.
- **FR-003**: O sistema MUST NOT expor endpoint de preview de compra — o cálculo de preço/desconto/disponibilidade é responsabilidade do frontend a partir dos campos da Lottery, combos e disponibilidade expostos nas consultas já existentes.
- **FR-004**: O endpoint `CheckQRCodeStatus` exige apenas autenticação válida — NÃO verifica posse do `invoiceId` por usuário. A confiança baseia-se na imprevisibilidade do ID gerado pelo provedor; qualquer usuário autenticado que conheça um `invoiceId` pode consultar seu status. Se o comportamento de emissão de tickets (quando a primeira consulta pós-pagamento chega) deve ser creditado corretamente ao comprador, isso é garantido pelo `TicketOrder.UserId` armazenado no momento da compra (não pelo usuário que consulta).

#### Integração com o provedor de pagamento (ProxyPay)

- **FR-005**: O sistema MUST criar a cobrança PIX chamando `POST /payment/qrcode` no provedor, enviando `clientId`, `customer` (`name`, `email`, `documentId`, `cellphone`) e `items` (`id`, `description`, `quantity`, `unitPrice`, `discount`).
- **FR-006**: O sistema MUST incluir o header `X-Tenant-Id` em todas as chamadas ao provedor de pagamento.
- **FR-007**: O sistema MUST devolver ao frontend os campos `invoiceId`, `invoiceNumber`, `brCode`, `brCodeBase64` e `expiredAt` vindos do provedor.
- **FR-008**: O sistema MUST consultar o status do pagamento chamando `GET /payment/qrcode/status/{invoiceId}` no provedor e traduzir o resultado para o vocabulário interno (`Pending`, `Paid`, `Expired`, `Cancelled`, `Unknown`).
- **FR-009**: O sistema MUST popular o payload de `POST /payment/qrcode` com `items` cujos `unitPrice`, `quantity` e `discount` totalizem exatamente o valor final cobrado do comprador, aplicando descontos de combo se houver.
- **FR-010**: O sistema MUST obter os dados de `customer` (`name`, `email`, `documentId`, `cellphone`) **do NAuth** via `INAuthAppService.GetCurrentAsync()` no momento da criação do QR Code (NAuth é a única fonte da verdade do perfil do comprador — o Fortuno não duplica esses dados). Se qualquer dos 4 campos obrigatórios vier vazio/ausente, o sistema MUST retornar `400 Bad Request` orientando o comprador a completar o cadastro no NAuth antes de comprar.

#### Fluxo de emissão de tickets após pagamento

- **FR-011**: Quando `CheckQRCodeStatus` identifica `Paid` no provedor pela primeira vez, o sistema MUST chamar `TicketService.ProcessPayment(invoiceId)` para emitir os tickets associados.
- **FR-012**: `TicketService.ProcessPayment` MUST ser idempotente por `invoiceId` — chamadas repetidas (inclusive concorrentes) NÃO devem emitir tickets duplicados nem registrar `InvoiceReferrer` duplicado.
- **FR-013**: `TicketService.ProcessPayment` MUST preservar o modo de atribuição escolhido na compra: em `UserPicks` usa os números reservados ao `invoiceId`; em `Random` sorteia entre os disponíveis **no momento da confirmação do pagamento** (não reserva na criação do QR Code). Se o pool estiver insuficiente nesse momento, ver FR-016.
- **FR-014**: Se a Lottery estiver fora do status `Open` no momento da confirmação, `ProcessPayment` NÃO deve emitir tickets; o pagamento fica marcado para estorno manual pelo fluxo de refund existente.
- **FR-015**: Em `UserPicks`, se as reservas vinculadas ao `invoiceId` estiverem expiradas ou incompletas no momento da confirmação, `ProcessPayment` NÃO deve emitir tickets parciais; o pagamento fica marcado para estorno manual.
- **FR-016**: Em `Random`, se o pool de números disponíveis no momento da confirmação for insuficiente para a `quantity` contratada, `ProcessPayment` NÃO deve emitir tickets parciais; o pagamento fica marcado para estorno manual.

#### Eliminação de webhook e preview

- **FR-017**: O sistema NÃO deve possuir endpoint de webhook de pagamento. Qualquer artefato existente (`WebhooksController`, filtros HMAC de pagamento, `PurchaseService.ProcessPaidWebhookAsync`) MUST ser removido nesta feature.
- **FR-018**: O sistema NÃO deve possuir endpoint de preview de compra. `PurchasesController`, `PurchaseService.PreviewAsync` e DTOs `PurchasePreviewRequest`/`PurchasePreviewInfo` MUST ser removidos ou realocados para uso interno sem exposição HTTP.
- **FR-019**: Documentação existente (`quickstart.md`, `contracts/`, Bruno collection) MUST ser atualizada para refletir o novo fluxo.

#### Dados de indicação (referral)

- **FR-020**: O código de indicação (`referralCode`) informado na compra MUST ser persistido junto à cobrança (por exemplo, em uma tabela `TicketOrder` ou equivalente) de modo que `ProcessPayment` saiba, ao confirmar o pagamento, qual foi o referrer sem depender de metadata no provedor.
- **FR-021**: Registro de `InvoiceReferrer` MUST ocorrer no momento da emissão dos tickets (dentro de `ProcessPayment`) e MUST preservar o valor do `ReferralPercent` da Lottery **no momento da compra**, não no momento do pagamento.

#### Controle de concorrência e idempotência

- **FR-022**: O sistema MUST garantir idempotência de `ProcessPayment` mesmo sob chamadas concorrentes do polling (dois clients fazendo polling simultâneo do mesmo `invoiceId`).
- **FR-023**: O sistema MUST registrar a associação `invoiceId → compra` de forma consistente ao criar o QR Code, de modo que o status retornado pelo provedor possa ser correlacionado à compra original sem depender de metadata no provedor.

#### Liberação de reservas expiradas

- **FR-024**: O sistema MUST liberar reservas `UserPicks` expiradas de forma **lazy** — toda consulta de disponibilidade (cálculo de pool disponível, validação de números escolhidos, sorteio no `Random` em `ProcessPayment`) MUST filtrar por `ExpiresAt > now`, ignorando reservas vencidas sem necessidade de job em background ou write-off explícito. Reservas vencidas permanecem na tabela como histórico mas não contam na disponibilidade.

### Key Entities *(include if feature involves data)*

- **Ticket Order (novo ou adaptado)**: Representa a intenção de compra que gerou a cobrança PIX. Carrega `invoiceId` devolvido pelo provedor, `userId`, `lotteryId`, `quantity`, `mode`, `pickedNumbers` (se `UserPicks`), `referralCode` (se houver), `totalAmount`, `referralPercentAtPurchase` (snapshot), `createdAt`, `expiredAt`, `status` interno (`Pending`, `Paid`, `Expired`, `Cancelled`). Substitui o papel que a metadata do webhook fazia hoje.
- **NumberReservation (existente, preservado)**: Reservas criadas no modo `UserPicks` continuam existindo, agora vinculadas ao `invoiceId` do provedor.
- **Ticket (existente, preservado)**: Emitido por `TicketService.ProcessPayment` a partir do `Ticket Order`.
- **InvoiceReferrer (existente, preservado)**: Registrado em `ProcessPayment` com base no `referralCode` armazenado no `Ticket Order`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O comprador recebe o QR Code PIX pronto para pagamento em menos de 3 segundos após enviar o pedido (em condições normais de rede e disponibilidade do provedor).
- **SC-002**: Após o pagamento ser confirmado pelo provedor, os tickets aparecem na lista do comprador em no máximo 2 segundos após a próxima chamada de `CheckQRCodeStatus`.
- **SC-003**: 100% dos pagamentos confirmados resultam em exatamente uma emissão de tickets — zero duplicações — mesmo sob polling concorrente.
- **SC-004**: 100% dos QR Codes não pagos dentro do prazo resultam em liberação das reservas `UserPicks` associadas, restaurando a disponibilidade para outros compradores.
- **SC-005**: Pagamentos confirmados sobre Lottery fora de `Open` resultam em 0 tickets emitidos e aparecem no painel de refund existente para tratamento manual.
- **SC-006**: O repositório, após o merge, contém zero referências a webhook de pagamento e zero endpoints HTTP de preview de compra.
- **SC-007**: A suíte automatizada de testes cobre os cenários de: criação bem-sucedida de QR Code (Random e UserPicks), polling idempotente pós-pagamento, expiração, Lottery fechada pós-pagamento, `CheckQRCodeStatus` sobre `invoiceId` de terceiro retornando o status sem erro e creditando a emissão ao `TicketOrder.UserId`.

## Assumptions

- O perfil do usuário autenticado (via NAuth ou equivalente) possui todos os dados exigidos por `customer` em `POST /payment/qrcode`: `name`, `email`, `documentId` (CPF) e `cellphone`. Casos sem esses dados retornam erro orientando a completar o cadastro antes de comprar.
- O provedor `ProxyPay` devolve no `GET /payment/qrcode/status/{invoiceId}` um vocabulário de estados que o Fortuno traduz para `Pending | Paid | Expired | Cancelled`. O mapeamento exato dos nomes será definido na fase de plano olhando a resposta real.
- O polling do frontend é aceitável como único mecanismo de confirmação — a ausência de webhook é decisão explícita do produto neste momento; se no futuro se quiser emitir recibo/push, isso será uma feature separada.
- `TicketService.ProcessPayment(invoiceId)` é um novo método (ou refatoração do que hoje vive em `PurchaseService.ProcessPaidWebhookAsync`) que recebe o `invoiceId` e deriva todos os dados da compra a partir do armazenamento interno (`Ticket Order`).
- O `clientId` enviado ao provedor será configurado via settings do `ProxyPay` por tenant/Store (já existe um equivalente hoje em `ProxyPaySettings`).
- `items[]` pode ser simplificado para uma única linha com `quantity = quantity-do-pedido`, `unitPrice = preço unitário`, `discount = desconto-do-combo-se-houver`, desde que o total bata com o `totalAmount` calculado — não é obrigatório um item por ticket.
- Os valores expostos hoje no `PurchasePreviewInfo` continuarão disponíveis ao frontend via consulta direta da Lottery + combos + disponibilidade (as três consultas já existem e não são alteradas por esta feature).
- Concorrência em `ProcessPayment` pode ser resolvida com uma garantia de unicidade por `invoiceId` no armazenamento de emissão (índice único, update condicional, ou transação com SELECT ... FOR UPDATE) — a escolha técnica fica para a fase de plano.
- A remoção do `WebhooksController` e filtros HMAC é segura porque não há outros consumidores de webhook do ProxyPay dependendo destas rotas — o fluxo de webhook foi substituído integralmente pelo polling.
