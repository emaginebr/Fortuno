# Feature Specification: Plataforma SaaS de Sorteios Online (Fortuno)

**Feature Branch**: `001-lottery-saas`
**Created**: 2026-04-16
**Status**: Draft
**Input**: User description: "SaaS de sorteios online integrado a NAuth (usuários), ProxyPay (Stores + PIX) e zTools (mídia/IA), permitindo que donos de loja criem loterias, vendam tickets via PIX e realizem sorteios com registro de ganhadores; consulta pública das entidades via GraphQL."

## Clarifications

### Session 2026-04-16

- Q: Como o sistema atribui os números dos tickets ao comprador? → A: Os dois modos ficam **sempre** disponíveis em toda Lottery; o próprio comprador escolhe, a cada compra, entre "aleatório" (o sistema sorteia N números ainda não vendidos) ou "escolher meus números" (o comprador seleciona individualmente cada número, com reserva temporária durante a finalização). Não há configuração por Lottery.
- Q: Como tratar tickets já pagos quando uma Lottery vira `Cancelled`? → A: O sistema marca os tickets como `PendingRefund` e apenas exibe a lista para o dono; o pagamento é feito fora do sistema (off-platform). O dono usa uma UI do Fortuno apenas para mudar o status para `Refunded` após liquidar manualmente (individual ou em lote), opcionalmente anexando uma referência externa para auditoria. O sistema NÃO chama o ProxyPay para estornar.
- Q: O Ticket vincula-se à Lottery ou a um Raffle específico? → A: À Lottery. Todos os Raffles da mesma Lottery sorteiam sobre o mesmo pool de tickets vendidos — cada Raffle é um sorteio independente sobre o universo completo de bilhetes.
- Q: Quem pode gerenciar uma Lottery e disparar o sorteio? → A: Somente o usuário proprietário da Store associada. Sem papéis de colaborador ou admin global nesta versão.
- Q: O que acontece quando um Raffle (não a Lottery toda) é `Cancelled`? → A: Os tickets permanecem válidos e continuam concorrendo nos demais Raffles; nenhum estorno é disparado; porém, antes de efetivar o cancelamento do Raffle, o dono é **obrigado** a redistribuir os RaffleAwards desse Raffle entre os Raffles remanescentes (`Open`) da mesma Lottery.

### Session 2026-04-17

- Q: Como o Fortuno detecta a confirmação do pagamento PIX? → A: O ProxyPay envia um webhook (HTTP POST) ao Fortuno quando o pagamento é confirmado. O Fortuno reage ao webhook — baixa o Invoice, gera os Tickets e, em caso de `UserPicks`, consolida a reserva temporária. Não há polling; o webhook é o canal único de confirmação.
- Q: Quando o sorteio de um Raffle pode ser executado? → A: Livre. O dono pode executar o sorteio de qualquer Raffle em status `Open` a qualquer momento, mesmo com a Lottery ainda em `Open` vendendo tickets. O sistema não impõe gate de Lottery `Closed` nem de data/hora do Raffle — a data/hora é apenas informativa para o público.
- Q: Qual é o formato do código de indicação (referrer)? → A: 8 caracteres alfanuméricos maiúsculos, gerados aleatoriamente, excluindo caracteres ambíguos (`I`, `O`, `0`, `1`). Alfabeto efetivo: A-Z (sem I e O) + 2-9. Ex.: `K7X9RQ42`. Único global; imutável.
- Q: Qual o TTL da reserva de números no modo `UserPicks`? → A: 15 minutos, alinhado com a expiração típica de cobrança PIX. Após 15 minutos sem confirmação do pagamento, a reserva expira e os números voltam ao pool.
- Q: Qual a fricção exigida para cancelar uma Lottery com tickets vendidos? → A: Confirmação dupla + motivo textual obrigatório (mínimo 20 caracteres), persistido no histórico da Lottery com usuário e timestamp. A operação é irreversível; não há janela de "undo".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criador de loteria publica sua primeira loteria (Priority: P1)

Um empreendedor cria uma conta, completa seu cadastro, cria sua loja, configura uma loteria
com regras, imagens, pacotes, raffles e prêmios, e publica a loteria para venda.

**Why this priority**: sem uma loteria publicada nenhum outro fluxo existe; é o caminho
crítico que libera aquisição de tickets, sorteios e todo o valor do produto.

**Independent Test**: pode ser totalmente validado criando uma conta nova, completando
cadastro pessoal, criando uma loja, criando uma loteria em rascunho, adicionando ao
menos uma imagem, um raffle e um prêmio, e mudando o status para `Open`.

**Acceptance Scenarios**:

1. **Given** um visitante sem conta, **When** se cadastra com dados mínimos (nome, e-mail,
   senha), **Then** recebe uma conta ativa em estado "cadastro simples".
2. **Given** um usuário com cadastro simples tentando criar uma loteria, **When** acessa
   a criação, **Then** o sistema exige completar o cadastro (documentId, telefone,
   endereço) antes de permitir o próximo passo.
3. **Given** um usuário com cadastro completo sem loja, **When** tenta criar uma loteria,
   **Then** o sistema primeiro exige a criação de uma loja.
4. **Given** um usuário com loja, **When** cria uma loteria, **Then** a loteria é criada
   em status `Draft` e recebe um slug único gerado automaticamente a partir do nome.
5. **Given** uma loteria em `Draft` sem imagem/raffle/prêmio, **When** o criador tenta
   mudar para `Open`, **Then** a mudança é rejeitada com mensagem indicando os
   requisitos ausentes.
6. **Given** uma loteria em `Draft` com pelo menos 1 imagem, 1 raffle e 1 RaffleAward,
   **When** o criador muda para `Open`, **Then** a loteria fica disponível para venda.
7. **Given** o tipo de número "3 números de 2 dígitos" com valor mínimo 10 e máximo 50,
   **When** o criador solicita o cálculo de possibilidades, **Then** o sistema retorna
   a quantidade total de combinações válidas dentro da faixa.
8. **Given** uma loteria em `Open`, `Closed` ou `Cancelled`, **When** o criador tenta
   excluí-la, **Then** a operação é negada — apenas mudança de status é permitida.

---

### User Story 2 - Comprador adquire tickets via PIX (Priority: P1)

Um visitante escolhe uma loteria aberta, seleciona a quantidade de tickets dentro
dos limites da loteria, confirma o cadastro simples, paga via PIX e recebe seus
tickets imediatamente após confirmação do pagamento.

**Why this priority**: é o fluxo de receita — sem ele a plataforma não gera dinheiro
para criadores nem sustenta o negócio.

**Independent Test**: pode ser validado com uma loteria `Open` já existente,
simulando um comprador novo, escolhendo quantidade válida, confirmando o pagamento
PIX e verificando que tickets foram emitidos e ficam visíveis na área do usuário.

**Acceptance Scenarios**:

1. **Given** uma loteria `Open` com Ticket Min = 1 e Ticket Max = 50, **When** o
   comprador escolhe 0 ou 51 tickets, **Then** o sistema rejeita a seleção com mensagem
   clara sobre os limites.
2. **Given** o comprador seleciona uma quantidade dentro dos limites, **When** existe
   um LotteryCombo cuja faixa (Quantidade Inicial–Final) contém a quantidade, **Then**
   o sistema aplica automaticamente o desconto do combo no preço total.
3. **Given** o comprador tenta comprar N tickets, **When** restam menos de N
   tickets disponíveis no pool da Lottery (faixa `[Ticket Num Ini, Ticket Num
   End]` para tipos compostos ou `[Valor Minimo, Valor Máximo]` para tipo
   int64), **Then** a compra é bloqueada e o sistema exibe quantos ainda
   estão disponíveis.
4. **Given** um comprador sem conta, **When** confirma a intenção de compra, **Then**
   é solicitado cadastro simples (sem exigir documentId/endereço) antes de prosseguir.
5. **Given** o comprador na tela de confirmação, **When** vê a prévia, **Then** são
   exibidos: quantidade, valor unitário, desconto aplicado (se houver), valor total
   e botão "Pagar".
6. **Given** uma loteria `Open`, **When** o comprador escolhe a quantidade e
   opta por "aleatório", **Then** ao confirmar o pagamento PIX o sistema sorteia
   N números ainda não vendidos da faixa disponível e emite os tickets.
7. **Given** a mesma loteria, **When** o comprador opta por "escolher meus
   números" e seleciona N números individualmente, **Then** esses números são
   reservados temporariamente (com expiração automática) até o pagamento PIX;
   após a confirmação do pagamento, os tickets são emitidos com os números
   escolhidos.
8. **Given** um número reservado por outro comprador no modo "escolher meus
   números", **When** o comprador atual tenta selecioná-lo, **Then** o número
   aparece como indisponível.
9. **Given** um comprador na sua área pessoal, **When** pesquisa seus tickets por
   loteria, data ou número, **Then** os resultados são retornados corretamente.
10. **Given** um ticket emitido, **When** qualquer usuário (inclusive o dono)
    tenta alterar ou excluir, **Then** a operação é negada.
11. **Given** o comprador na tela de compra, **When** informa um código de
    indicação (referral) válido, **Then** os tickets gerados ficam vinculados
    àquele código e, após o pagamento, o valor de comissão calculado é
    **registrado para exibição** (não pago pelo sistema) tanto para o
    indicador quanto para o dono da Lottery.
12. **Given** o comprador informa um código de indicação inválido ou o próprio
    código, **Then** o sistema rejeita o código; o comprador pode prosseguir
    sem indicação.

---

### User Story 3 - Dono da loteria realiza o sorteio e registra ganhadores (Priority: P2)

Após o encerramento do período de vendas, o dono acessa a tela de sorteio de um
Raffle, informa os números ganhadores, revisa a prévia (número, nome, CPF, posição,
prêmio) e confirma. Pode repetir até fechar o Raffle.

**Why this priority**: entrega o desfecho esperado pelo comprador e a obrigação legal
do criador; sem isso a plataforma é incompleta, mas depende de US1 e US2.

**Independent Test**: com uma loteria `Open` e tickets já vendidos, o dono abre a
tela do Raffle, informa uma lista de números ganhadores, vê a prévia com matches,
confirma e observa `RaffleWinner` registrado por posição; repete; fecha o Raffle e
confirma que novas alterações são rejeitadas. O sorteio pode ser executado
mesmo com a Lottery ainda em `Open` e mesmo antes da data/hora informada
no Raffle.

**Acceptance Scenarios**:

1. **Given** um Raffle `Open`, **When** o dono informa uma lista de números
   ganhadores, **Then** o sistema retorna uma prévia com: número, nome do ganhador
   (ou indicação de "não atribuído"), CPF (mascarado conforme política), posição e
   prêmio associado.
2. **Given** a prévia exibida, **When** o dono confirma, **Then** os registros de
   `RaffleWinner` são criados vinculando posição, prêmio, usuário e ticket.
3. **Given** um Raffle `Open`, **When** o dono registra mais ganhadores em uma
   segunda rodada, **Then** os novos registros são adicionados sem sobrescrever os
   anteriores.
4. **Given** um Raffle fechado (`Closed`), **When** o dono tenta registrar ou alterar
   ganhadores, **Then** a operação é rejeitada.
5. **Given** um número ganhador informado que não corresponde a nenhum ticket
   vendido, **Then** o sistema ainda exibe a linha na prévia marcada como "sem
   ganhador" e, ao confirmar, registra o prêmio como não atribuído.
6. **Given** um Raffle com `IncludePreviousWinners = false` e um ticket que já
   venceu em um Raffle `Closed` anterior da mesma Lottery, **When** o dono
   informa o número desse ticket na prévia, **Then** o sistema rejeita a
   linha com mensagem explicando que o ticket foi excluído pelo flag.
7. **Given** um Raffle com `IncludePreviousWinners = true`, **When** o dono
   informa um número que já venceu em outro Raffle, **Then** o sistema aceita
   e o Ticket recebe o novo prêmio (pode acumular prêmios de múltiplos
   Raffles).

---

### User Story 4 - Gestão de imagens, combos, raffles e prêmios da loteria (Priority: P2)

O dono ajusta componentes da loteria enquanto ela está em `Draft`: adiciona/remove
imagens, edita combos de desconto, cria/edita raffles e seus prêmios.

**Why this priority**: habilita a criação completa de US1 e permite correções antes
da publicação; após publicação grande parte fica congelada.

**Independent Test**: criar uma loteria em `Draft`, executar operações CRUD em cada
entidade (LotteryImage, LotteryCombo, Raffle, RaffleAward) e confirmar que em
estados não-draft essas operações são bloqueadas.

**Acceptance Scenarios**:

1. **Given** uma loteria em `Draft`, **When** o dono adiciona uma imagem via upload,
   **Then** a imagem recebe uma URL hospedada e uma ordem; a primeira imagem (ordem
   mínima) é marcada como principal.
2. **Given** uma loteria em `Open`/`Closed`/`Cancelled`, **When** o dono tenta
   excluir uma imagem, **Then** a operação é negada.
3. **Given** uma loteria em `Draft`, **When** o dono cria um LotteryCombo com faixa
   Quantidade Inicial 10 e Final 49, **Then** compras dentro dessa faixa aplicarão
   o desconto definido.
4. **Given** dois combos com faixas sobrepostas na mesma loteria, **When** o dono
   tenta salvar, **Then** o sistema bloqueia com erro de sobreposição.
5. **Given** uma loteria em `Draft`, **When** o dono cria um Raffle com data futura,
   nome, descrição (Markdown) e vídeo (URL ou upload), **Then** o Raffle é salvo
   em status `Open`.
6. **Given** um Raffle em loteria `Draft`, **When** o dono adiciona/edita/remove
   RaffleAwards (posição + descrição), **Then** as operações são aceitas;
   em loteria não-draft, são rejeitadas.

---

### User Story 5 - Sistema de indicação (referrer) e painéis de comissão (Priority: P2)

Todo usuário cadastrado recebe um código único de indicação (referral code). Ao
divulgar esse código, outros compradores podem aplicá-lo na compra de tickets;
o sistema vincula a compra ao indicador e calcula uma comissão (percentual
configurado na Lottery). O sistema **apenas exibe** os valores calculados — em
dois painéis distintos: um painel pessoal do usuário indicador (quanto ele tem
a receber, agregado) e um painel do dono da Lottery (lista de comissões a
pagar, por indicador). O pagamento efetivo é responsabilidade do dono da
Lottery e ocorre **fora do sistema**.

**Why this priority**: é um canal de aquisição com efeito viral direto sobre
US2 (vendas); sem indicação a plataforma perde um vetor importante de
crescimento, mas o fluxo principal de compra funciona mesmo sem ela.

**Independent Test**: criar dois usuários (indicador e comprador), o comprador
efetua uma compra informando o código do indicador, o pagamento é confirmado,
e (a) o indicador vê no seu painel o valor a receber e (b) o dono da Lottery
vê a comissão correspondente na sua lista de comissões a pagar.

**Acceptance Scenarios**:

1. **Given** um usuário recém-cadastrado, **When** acessa seu perfil pela
   primeira vez, **Then** já possui um código de indicação único e imutável
   associado.
2. **Given** uma Lottery com `ReferralPercent = 10%`, **When** um comprador
   aplica um código de indicação válido e conclui a compra de 5 tickets de
   R$ 20,00 cada com um LotteryCombo que aplica 10% de desconto (valor pago =
   R$ 90,00), **Then** o sistema registra uma comissão de R$ 9,00
   (10% × R$ 90,00, base = `Invoice.PaidAmount`); o valor aparece no painel
   do indicador como "a receber" e no painel do dono da Lottery como "a pagar".
3. **Given** uma Lottery com `ReferralPercent = 0`, **When** uma compra é feita
   com código válido, **Then** o vínculo de indicação é registrado, mas o
   valor de comissão calculado é R$ 0,00 em ambos os painéis.
4. **Given** o comprador informa um código inexistente, **When** submete a
   compra, **Then** o sistema rejeita o código e permite ao comprador
   prosseguir sem indicação ou corrigir o código.
5. **Given** o comprador é o próprio dono do código, **When** tenta aplicá-lo
   em sua compra, **Then** o sistema rejeita — auto-indicação não é permitida.
6. **Given** um usuário com ao menos uma compra indicada, **When** acessa o
   painel de indicações, **Then** visualiza: código pessoal, total de compras
   indicadas, valor total calculado a receber e detalhamento por Lottery
   (com indicação explícita de que o pagamento ocorre fora do sistema).
7. **Given** o dono de uma Lottery, **When** acessa o painel administrativo de
   comissões da sua Lottery, **Then** visualiza a lista de comissões calculadas
   agrupadas por indicador, com totalizadores; o sistema NÃO oferece botão de
   "pagar" — é apenas consulta.
8. **Given** uma compra vinculada a um indicador com tickets pagos, **When**
   o dono marca manualmente um ou mais tickets como `Refunded` pela UI de
   gestão de status, **Then** na próxima consulta aos painéis de comissão os
   tickets `Refunded` já não compõem os totais — o cálculo é em tempo real e
   não existe etapa de "reversão" persistida.

---

### User Story 6 - Consulta pública via GraphQL (Priority: P3)

Aplicações cliente ou integradores consultam loterias, imagens, combos, raffles,
prêmios e ganhadores através de uma API GraphQL, permitindo montar vitrines, apps
móveis ou dashboards sem acoplamento a endpoints REST específicos.

**Why this priority**: acelera integrações de terceiros e frontends, mas não bloqueia
nenhum fluxo de negócio central.

**Independent Test**: com dados de exemplo persistidos, executar queries GraphQL
para listar loterias filtradas por status, detalhar uma loteria pelo slug e listar
ganhadores de um Raffle.

**Acceptance Scenarios**:

1. **Given** a API GraphQL exposta, **When** um cliente consulta loterias com filtro
   `status = Open`, **Then** apenas loterias abertas são retornadas com campos
   solicitados.
2. **Given** uma consulta por slug, **When** o slug existe, **Then** a loteria é
   retornada com seus relacionamentos navegáveis (imagens, combos, raffles, prêmios).
3. **Given** dados sensíveis (ex.: dados pessoais de ganhadores), **When** um cliente
   não autorizado consulta, **Then** campos sensíveis são ocultados conforme política.

---

### Edge Cases

- Quantidade total de tickets vendidos iguala o pool da Lottery (faixa Ticket
  Num Ini–End para tipos compostos, ou faixa Valor Minimo–Máximo para tipo
  int64): novas compras são bloqueadas e a loteria pode ser sinalizada como
  esgotada.
- Ticket Max = 0 ou Ticket Min = 0 (defaults): tratados como "sem limite
  mínimo/máximo" respectivamente; se ambos 0, a única restrição é a disponibilidade.
- Ticket Num Ini / Ticket Num End: campos **ignorados** quando o tipo de
  número da Lottery é "1 número int64" — nesse caso o pool é determinado
  diretamente por `[Valor Minimo, Valor Máximo]`. Para tipos compostos
  (3/4/5/6/7/8 números de 2 dígitos), Ticket Num End = 0 (default)
  interpretado como "loteria sem teto de numeração pré-definido"; a Lottery
  não pode sair de `Draft` até que Ticket Num End seja definido > Ticket
  Num Ini.
- Slug colidindo entre loterias: o sistema sufixa automaticamente até garantir
  unicidade global.
- Comprador deixa a tela de pagamento sem confirmar PIX: o invoice expira conforme
  política do provedor e nenhum ticket é emitido.
- Pagamento PIX recebido após o encerramento das vendas: o sistema recusa a emissão
  de tickets e encaminha para estorno.
- Webhook do ProxyPay chega duplicado para a mesma Invoice: o segundo
  processamento é ignorado (idempotência por InvoiceId) — não há emissão
  dupla de tickets.
- Webhook do ProxyPay não chega (ex.: falha temporária de rede): o Invoice
  permanece pendente no lado do Fortuno; o ProxyPay é responsável por
  retry do webhook até receber resposta de sucesso do Fortuno.
- Combo com Quantidade Inicial = Final = 0: ignorado (combo inválido).
- Tipo de número "3 números de 2 dígitos" com min=10, max=50: cada componente deve
  estar entre 10 e 50 inclusive; combinações fora da faixa em qualquer componente
  são inválidas.
- Dono tenta registrar um mesmo ticket em duas posições diferentes no mesmo
  Raffle: a segunda tentativa é rejeitada (cada posição de um mesmo Raffle
  deve ter ticket distinto).
- Raffle com `IncludePreviousWinners = false` e nenhum ticket elegível (todos
  os tickets já venceram em Raffles anteriores): o dono é alertado de que o
  pool está vazio antes de abrir a tela de sorteio; a prévia rejeitará
  qualquer número informado.
- Comprador aplica o próprio código de indicação: rejeitado como auto-indicação;
  a compra pode prosseguir sem código.
- Comprador aplica código de indicação em uma Lottery com `ReferralPercent = 0`:
  o vínculo é registrado mas nenhum valor é creditado ao indicador.
- Indicador deleta a conta no NAuth depois de ter recebido comissões: o vínculo
  histórico e o saldo permanecem no Fortuno com o UserId órfão; a exibição em UI
  indica "indicador indisponível".
- Loteria é marcada como `Cancelled`: novas vendas são imediatamente
  bloqueadas; todos os tickets emitidos associados recebem o estado de
  estorno `PendingRefund`; o dono realiza o estorno **fora do sistema** e,
  em seguida, usa a UI de gestão de status para marcar os tickets como
  `Refunded` (individual ou em lote). Cada transição é registrada para
  auditoria; o Fortuno não executa chamadas financeiras ao ProxyPay.

## Requirements *(mandatory)*

### Functional Requirements

**Cadastro e Loja**

- **FR-001**: O sistema DEVE permitir cadastro simples de usuário (nome, e-mail,
  senha) suficiente para comprar tickets.
- **FR-002**: O sistema DEVE exigir cadastro completo (documentId, telefone,
  endereço) para um usuário poder criar loja ou loteria.
- **FR-003**: O sistema DEVE exigir que o usuário tenha ao menos uma loja antes de
  permitir a criação de uma Lottery.
- **FR-004**: Uma Lottery DEVE pertencer a exatamente uma Store (relação 1:N
  Store → Lottery).

**Lottery**

- **FR-005**: O sistema DEVE gerar automaticamente um slug único global ao criar
  uma Lottery a partir do nome; colisões DEVEM ser resolvidas com sufixo
  incremental.
- **FR-006**: O slug DEVE ser editável na atualização da Lottery, mantendo
  unicidade global.
- **FR-007**: A Lottery DEVE armazenar os campos: nome, slug, descrição (Markdown),
  regras (Markdown), política de privacidade (Markdown), ticket price, valor total
  do prêmio, ticket min, ticket max, ticket num ini, ticket num end, tipo de número,
  valor mínimo e valor máximo do número do ticket, percentual de comissão por
  indicação (`ReferralPercent`, float, default 0), status, created_at, updated_at.
- **FR-008**: O sistema DEVE permitir baixar as regras e a política de privacidade
  como arquivos PDF gerados a partir do Markdown.
- **FR-009**: Os defaults DEVEM ser: Ticket Min = 0, Ticket Max = 0, Ticket Num
  Ini = 1, Ticket Num End = 0. Os campos `Ticket Num Ini` e `Ticket Num End`
  SOMENTE têm efeito quando o tipo de número for composto (3/4/5/6/7/8
  números de 2 dígitos); para o tipo "1 número int64", o pool de tickets é
  determinado diretamente por `[Valor Minimo, Valor Máximo]` e
  `Ticket Num Ini/End` são ignorados.
- **FR-010**: O "tipo de número" DEVE suportar uma das opções: 1 número int64, ou
  composição de 3, 4, 5, 6, 7 ou 8 números de 2 dígitos.
- **FR-011**: Para tipos compostos, o sistema DEVE compor/decompor os componentes
  em um valor int64 canônico para armazenamento do número do ticket.
- **FR-012**: O sistema DEVE validar que cada componente do número composto esteja
  entre Valor Mínimo e Valor Máximo do Número do Ticket, inclusive.
- **FR-013**: O sistema DEVE oferecer um método para calcular a quantidade total
  de combinações válidas dado o tipo de número e os valores mínimo/máximo.
- **FR-014**: O status da Lottery DEVE ser um de: `Draft`, `Open`, `Closed`,
  `Cancelled`.
- **FR-015**: Uma Lottery DEVE ser criada em status `Draft`.
- **FR-016**: A transição `Draft → Open` DEVE exigir pelo menos 1 LotteryImage,
  1 Raffle e 1 RaffleAward associados.
- **FR-017**: O sistema NÃO DEVE permitir exclusão de Lottery; apenas mudança de
  status.

**LotteryImage**

- **FR-018**: O sistema DEVE suportar CRUD de LotteryImage apenas enquanto a
  Lottery estiver em `Draft`.
- **FR-019**: Cada LotteryImage DEVE ter: ImageUrl (obtida por upload), descrição
  e ordem.
- **FR-020**: A LotteryImage de menor ordem DEVE ser tratada como imagem
  principal.
- **FR-021**: O upload de imagens DEVE ser delegado ao serviço de mídia externo
  (zTools) que retorna a URL hospedada.

**LotteryCombo**

- **FR-022**: O sistema DEVE suportar CRUD de LotteryCombo apenas enquanto a
  Lottery estiver em `Draft`.
- **FR-023**: Cada LotteryCombo DEVE ter: nome do pacote, valor de desconto
  (float, default 0), label do desconto, quantidade inicial (default 0),
  quantidade final (default 0).
- **FR-024**: Não DEVE haver sobreposição de faixas de quantidade entre combos da
  mesma Lottery.
- **FR-025**: Na compra, o sistema DEVE aplicar automaticamente o combo cuja
  faixa `[Quantidade Inicial, Quantidade Final]` contém a quantidade escolhida.

**Compra e Ticket**

- **FR-026**: O sistema DEVE validar que a quantidade de tickets escolhida está
  dentro de `[Ticket Min, Ticket Max]` quando esses valores forem > 0.
- **FR-027**: O sistema DEVE calcular e exibir, antes da compra, quantos
  tickets ainda estão disponíveis no pool da Lottery. O pool é definido por
  `[Ticket Num Ini, Ticket Num End]` quando o tipo de número é composto, e
  por `[Valor Minimo, Valor Máximo]` quando o tipo é "1 número int64".
- **FR-028**: O sistema DEVE exibir uma tela de confirmação com prévia
  (quantidade, valor unitário, desconto aplicado, total) e botão "Pagar".
- **FR-029**: O pagamento DEVE ser exclusivamente via PIX, delegado ao
  microserviço de pagamentos externo.
- **FR-029a**: O Fortuno DEVE expor um endpoint de webhook para receber
  notificações de confirmação de pagamento do ProxyPay. Esse webhook é o
  **canal único** de confirmação — o Fortuno NÃO DEVE fazer polling no
  ProxyPay.
- **FR-029b**: O endpoint de webhook DEVE: (i) autenticar a origem como
  sendo o ProxyPay (assinatura/segredo compartilhado), (ii) ser idempotente
  (requisições repetidas para a mesma Invoice não devem gerar tickets
  duplicados) e (iii) responder rapidamente ao ProxyPay confirmando
  recebimento, processando a emissão de tickets de forma assíncrona quando
  necessário.
- **FR-030**: Ao receber o webhook de confirmação de pagamento, o sistema
  DEVE: baixar o invoice, gerar os tickets, atribuir números dentro da faixa
  disponível e vincular ao usuário.
- **FR-030a**: Em **toda** Lottery `Open`, o comprador DEVE poder escolher, por
  compra, entre dois modos de atribuição: `Random` (sistema sorteia) ou
  `UserPicks` (comprador seleciona números individualmente). O modo é um
  atributo **da compra**, não da Lottery.
- **FR-030b**: No modo `Random`, o sistema DEVE sortear N números ainda não
  vendidos da faixa disponível no momento da confirmação do pagamento.
- **FR-030c**: No modo `UserPicks`, o sistema DEVE permitir que o comprador
  selecione individualmente seus N números antes do pagamento, reservá-los
  temporariamente por **15 minutos** contados da criação da reserva e, após
  a confirmação do pagamento, emitir os tickets exatamente com os números
  escolhidos.
- **FR-030d**: Se a reserva do modo `UserPicks` expirar (15 minutos) antes do
  webhook de confirmação do pagamento, os números DEVEM voltar à
  disponibilidade; se o webhook chegar após a expiração, o sistema DEVE
  recusar a emissão e encaminhar para estorno (liquidação off-platform).
- **FR-031**: Cada Ticket DEVE conter: InvoiceId, LotteryId, UserId, número do
  ticket (long), estado de estorno (`None` | `PendingRefund` | `Refunded`,
  default `None`), created_at. O Ticket NÃO DEVE referenciar um Raffle
  específico — ele pertence à Lottery. O vínculo com o indicador (referrer) é
  registrado no Invoice da compra, **não** no Ticket (ver FR-R05).
- **FR-031a**: Cada Raffle da Lottery é um sorteio independente. O escopo dos
  tickets elegíveis é controlado pelo flag `IncludePreviousWinners` do próprio
  Raffle (ver FR-034): quando `true`, o sorteio considera o pool completo de
  tickets vendidos da Lottery e um mesmo Ticket PODE ganhar em mais de um
  Raffle; quando `false` (padrão), tickets que já foram registrados como
  ganhadores (`RaffleWinner`) em Raffles `Closed` anteriores da mesma Lottery
  DEVEM ser excluídos do pool elegível deste Raffle.
- **FR-032**: Tickets NÃO DEVEM ser alteráveis nem excluíveis após emissão,
  **exceto** pela evolução controlada do estado de estorno
  (`None → PendingRefund → Refunded`) disparada pelo fluxo de cancelamento.
- **FR-033**: Usuários DEVEM poder listar e pesquisar seus próprios tickets por
  loteria, número ou data, com exibição do estado de estorno quando aplicável.
- **FR-033a**: Quando uma Lottery transiciona para `Cancelled`, o sistema DEVE
  marcar todos os seus tickets ativos como `PendingRefund` e bloquear novas
  vendas imediatamente.
- **FR-033a1**: A transição `Open/Draft → Cancelled` DEVE exigir confirmação
  dupla (ex.: duas caixas de diálogo sucessivas ou digitação do nome da
  Lottery para confirmar) **e** um campo textual obrigatório "motivo do
  cancelamento" com no mínimo 20 caracteres. O motivo, o usuário que
  disparou o cancelamento e o timestamp DEVEM ser persistidos como parte
  do histórico da Lottery. A operação é irreversível — não há transição
  `Cancelled → Open`.
- **FR-033b**: O sistema DEVE oferecer ao dono da Store proprietária uma UI
  **apenas de gestão de status** para listar tickets `PendingRefund`,
  filtrar/ordenar, e marcar tickets como `Refunded` individualmente ou em
  lote, após ele realizar o pagamento fora do sistema. O sistema NÃO DEVE
  disparar nenhuma transferência, estorno ou chamada de pagamento ao
  ProxyPay — a liquidação é responsabilidade do dono e ocorre off-platform.
- **FR-033c**: Cada mudança de status `PendingRefund → Refunded` DEVE ser
  registrada com: usuário que efetuou a mudança, timestamp, valor de
  referência do ticket e, opcionalmente, uma referência externa textual
  (ex.: id de comprovante bancário) informada pelo dono, para fins de
  auditoria.

**Raffle, RaffleAward, RaffleWinner**

- **FR-034**: Um Raffle DEVE conter: LotteryId, data e hora do sorteio, nome,
  descrição (Markdown), URL do vídeo (upload via zTools ou URL externa
  informada), flag `IncludePreviousWinners` (boolean, default `false`),
  status.
- **FR-034a**: Quando `IncludePreviousWinners = false`, o sistema DEVE,
  durante a geração da prévia de ganhadores, rejeitar qualquer número
  informado que corresponda a um Ticket já registrado como `RaffleWinner` em
  um Raffle `Closed` anterior da mesma Lottery, exibindo mensagem explicando
  que o ticket foi excluído pelo flag e permitindo ao dono alterar o número
  ou o flag.
- **FR-034b**: Quando `IncludePreviousWinners = true`, o sistema DEVE aceitar
  que um mesmo Ticket seja registrado como ganhador em mais de um Raffle da
  Lottery; o painel de tickets do comprador DEVE listar todos os prêmios
  atribuídos a aquele ticket.
- **FR-035**: O status do Raffle DEVE ser um de: `Open`, `Closed`, `Cancelled`.
- **FR-036**: Um RaffleAward DEVE conter: RaffleId, posição (int), descrição do
  prêmio (varchar(300)).
- **FR-037**: O sistema DEVE permitir CRUD de Raffle e RaffleAward apenas quando
  a Lottery estiver em `Draft`.
- **FR-038**: O sistema DEVE oferecer um método para receber uma lista de números
  ganhadores e calcular, para cada um, o ganhador (User + Ticket) ou indicar
  "sem ganhador".
- **FR-039**: O sistema DEVE apresentar uma prévia dos ganhadores (número, nome
  do ganhador, CPF conforme política de privacidade, posição, prêmio) antes de
  gravar.
- **FR-040**: Após confirmação da prévia, RaffleWinner DEVE ser criado por
  posição (RaffleId, UserId, TicketId, AwardId, posição, prêmio).
- **FR-041**: O registro de ganhadores DEVE ser permitido múltiplas vezes
  enquanto o Raffle estiver `Open`.
- **FR-041a**: O sorteio de um Raffle PODE ser executado a qualquer momento
  em que o Raffle estiver `Open`, independentemente do status da Lottery
  (`Open`, `Closed`) e independentemente da data/hora informada no Raffle.
  O sistema NÃO DEVE bloquear o sorteio por esses fatores — a data/hora do
  Raffle é informação pública, não um gate.
- **FR-042**: Quando o Raffle for `Closed`, nenhuma alteração nos ganhadores DEVE
  ser permitida.
- **FR-042a**: Um Raffle pode ser `Cancelled` mesmo com tickets já vendidos; os
  tickets permanecem válidos e continuam concorrendo nos Raffles remanescentes
  da mesma Lottery. Cancelar um Raffle NÃO dispara estorno de tickets.
- **FR-042b**: Antes de efetivar a transição de um Raffle para `Cancelled`, o
  sistema DEVE exigir que o dono redistribua todos os RaffleAwards desse Raffle
  entre os Raffles remanescentes em status `Open` da mesma Lottery. A operação
  DEVE ser rejeitada se restar ao menos um RaffleAward sem destino.
- **FR-042c**: Se não houver nenhum outro Raffle `Open` na Lottery, o
  cancelamento de um Raffle com tickets já vendidos DEVE ser bloqueado; o
  caminho correto passa a ser o cancelamento da Lottery inteira (que aciona o
  fluxo de `PendingRefund`).

**Consulta GraphQL**

- **FR-043**: O sistema DEVE expor um endpoint GraphQL para consulta de Lottery,
  LotteryImage, LotteryCombo, Raffle, RaffleAward, RaffleWinner e Ticket do
  usuário autenticado.
- **FR-044**: Dados pessoais sensíveis de terceiros NÃO DEVEM ser expostos via
  GraphQL sem autorização apropriada; campos como CPF de ganhadores DEVEM ser
  mascarados em consultas públicas.

**Indicações (Referrer)**

- **FR-R01**: O sistema Fortuno DEVE manter, em banco próprio (tabela
  `fortuna_user_referrers`), um código único de indicação por usuário. A
  informação NÃO DEVE ser armazenada no NAuth — apenas referencia o UserId do
  NAuth.
- **FR-R02**: O código de indicação DEVE ser gerado automaticamente na primeira
  interação do usuário com o sistema Fortuno (cadastro, primeira compra ou
  primeiro acesso ao perfil), ser único globalmente e imutável após criação.
- **FR-R02a**: O código DEVE ter exatamente 8 caracteres, sorteados
  aleatoriamente de um alfabeto que exclui caracteres ambíguos — alfabeto
  permitido: `A-Z` sem `I` e `O`, e `2-9` sem `0` e `1` (32 símbolos). Ex.:
  `K7X9RQ42`. Em caso de colisão na geração, o sistema DEVE tentar novamente
  até obter um código único.
- **FR-R03**: O comprador DEVE poder informar, opcionalmente, um código de
  indicação na tela de compra antes do pagamento.
- **FR-R04**: O sistema DEVE validar o código: rejeitar códigos inexistentes e
  rejeitar o próprio código do comprador (auto-indicação é proibida); ao
  rejeitar, a compra PODE prosseguir sem indicação.
- **FR-R05**: Quando uma compra for concluída com código válido, o sistema
  DEVE registrar em tabela local (`fortuna_invoice_referrers` ou estrutura
  equivalente) o vínculo `InvoiceId → ReferrerUserId`. O vínculo é **por
  Invoice** (compra inteira), não por Ticket, porque descontos de combos são
  aplicados ao valor total da compra.
- **FR-R06**: A comissão de indicação DEVE ser calculada em **tempo real** a
  cada consulta dos painéis, a partir do estado atual dos tickets. O valor
  NÃO DEVE ser persistido em tabela própria. A fórmula por Invoice indicada
  é: `Invoice.PaidAmount × (tickets_válidos / tickets_totais_do_invoice) ×
  Lottery.ReferralPercent / 100`, onde `tickets_válidos` são os tickets da
  Invoice cujo estado de estorno é diferente de `Refunded` e
  `ReferralPercent` é o percentual **vigente** da Lottery no momento da
  consulta.
- **FR-R07**: Quando o estado de um ticket muda para `Refunded` (via FR-033b),
  a comissão exibida em ambos os painéis reflete a nova situação na próxima
  leitura — por ser cálculo em tempo real, a reversão é implícita e não
  requer processamento adicional nem armazenamento de valor revertido.
- **FR-R08**: O sistema DEVE expor um painel pessoal do usuário indicador com:
  código pessoal de indicação, total de compras indicadas, valor total
  calculado a receber (resultado do cálculo em tempo real sobre os tickets
  válidos) e detalhamento por Lottery. O painel DEVE deixar explícito que o
  pagamento ocorre fora do sistema.
- **FR-R09**: O sistema DEVE expor, para o dono da Store proprietária da
  Lottery, um painel administrativo de comissões com a lista de comissões
  calculadas agrupadas por indicador, totalizadores por Lottery e
  detalhamento por Ticket/compra. Este painel é **apenas de consulta** — o
  sistema NÃO DEVE oferecer mecanismo de pagamento das comissões.
- **FR-R10**: O sistema NÃO DEVE creditar, transferir, pagar ou executar
  qualquer movimentação financeira relativa a comissões de indicação. Toda
  liquidação é responsabilidade do dono da Lottery e ocorre fora do sistema.

**Segurança e auditoria**

- **FR-045**: Rotas de criação, atualização e operações de sorteio DEVEM exigir
  autenticação.
- **FR-045a**: Todas as operações de escrita sobre Lottery, LotteryImage,
  LotteryCombo, Raffle, RaffleAward, RaffleWinner e disparo de estorno DEVEM
  ser autorizadas **exclusivamente** ao usuário proprietário da Store associada
  à Lottery. Qualquer outro usuário autenticado DEVE receber erro de
  autorização.
- **FR-045b**: Nesta versão NÃO HAVERÁ papéis de colaborador por Store nem papel
  de administrador global da plataforma.
- **FR-046**: O sistema NÃO DEVE expor segredos, connection strings ou dados
  sensíveis em respostas de erro ou logs públicos.

### Key Entities *(include if feature involves data)*

- **Store (externa, ProxyPay)**: representa a loja do criador da loteria; já
  possui usuário proprietário; é referência 1:N para Lottery.
- **User (externo, NAuth)**: identidade autenticada; pode ser dono de loja
  (criador de loteria) ou comprador (detentor de tickets).
- **Invoice (externo, ProxyPay)**: nota de cobrança PIX associada a uma intenção
  de compra; uma Invoice paga gera N tickets.
- **Lottery**: a loteria em si, pertencente a uma Store, com regras de
  numeração, preço, status, composição de número e percentual de comissão
  por indicação (`ReferralPercent`). Quando cancelada, armazena também o
  motivo textual obrigatório do cancelamento, o usuário que disparou e o
  timestamp para auditoria. O modo de atribuição de número (`Random` ou
  `UserPicks`) **não** é atributo da Lottery — é escolhido pelo comprador
  em cada compra.
- **LotteryImage**: imagem de divulgação da loteria com URL, descrição e ordem.
- **LotteryCombo**: pacote promocional com faixa de quantidade e desconto
  aplicado automaticamente.
- **Ticket**: bilhete (praticamente imutável) vinculado a um User, a uma
  Invoice e a uma **Lottery** (não a um Raffle específico), com um número
  dentro da faixa da Lottery e um estado de estorno (`None` | `PendingRefund`
  | `Refunded`). Concorre em todos os Raffles da sua Lottery. O indicador da
  compra não fica no Ticket — é registrado na Invoice via `InvoiceReferrer`.
- **UserReferrer**: tabela local do Fortuno que associa um UserId do NAuth a
  um código único imutável de indicação. Não duplica identidade; apenas
  estende o User com o código utilizado em compras.
- **InvoiceReferrer**: tabela local que registra, por Invoice do ProxyPay, o
  `ReferrerUserId` aplicado naquela compra. É o ponto único de verdade do
  vínculo indicador ↔ compra — substitui qualquer campo de referrer no
  Ticket. Como a Invoice é externa, esta tabela local é o canal para
  associar o indicador sem modificar o ProxyPay.
- **ReferralEarning (computado, não persistido)**: valor de comissão derivado
  em tempo real a partir de `InvoiceReferrer` + estado atual dos Tickets da
  Invoice + `Lottery.ReferralPercent` vigente. Não é uma tabela — é uma
  agregação calculada sob demanda pelos painéis do indicador e do dono da
  Lottery. Tickets com estado `Refunded` são automaticamente excluídos do
  cálculo. Não representa obrigação financeira do sistema; liquidação
  ocorre fora do sistema.
- **RefundLog**: registro de auditoria de cada transição de status
  `PendingRefund → Refunded` executada manualmente pelo dono da Store.
  Contém: ticket alvo, usuário que efetuou a mudança, timestamp, valor de
  referência e, opcionalmente, uma referência externa textual (ex.: id de
  comprovante bancário). O sistema NÃO movimenta valores; apenas registra
  a mudança de estado.
- **Raffle**: evento de sorteio de uma Lottery em uma data/hora específica,
  com flag `IncludePreviousWinners` que controla se tickets já vencedores em
  Raffles anteriores concorrem (true) ou são excluídos do pool (false,
  padrão).
- **RaffleAward**: prêmio do Raffle identificado por posição e descrição.
- **RaffleWinner**: registro final do ganhador (User + Ticket) de uma posição
  de um Raffle.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um novo criador consegue publicar sua primeira loteria (do
  cadastro ao estado `Open`) em até 15 minutos.
- **SC-002**: Um comprador consegue completar a compra de tickets (desde a
  escolha até a confirmação PIX) em até 3 minutos.
- **SC-003**: Tickets aparecem na área "Meus Tickets" do comprador em até 30
  segundos após a confirmação do pagamento PIX.
- **SC-004**: 99% das tentativas de compra com quantidade válida e tickets
  disponíveis são concluídas sem erro de sistema.
- **SC-005**: Nenhuma alteração ou exclusão de Ticket ou RaffleWinner `Closed`
  é aceita pelo sistema (0% de falhas de política).
- **SC-006**: O método de cálculo de possibilidades retorna resultado correto em
  100% das combinações de tipo de número e faixa em testes de amostra.
- **SC-007**: Consultas GraphQL para listar loterias `Open` retornam em até 1
  segundo p95 em cenário com até 1.000 loterias cadastradas.
- **SC-008**: O registro de até 100 ganhadores em um Raffle é concluído em até
  5 segundos após a confirmação da prévia.
- **SC-009**: A comissão calculada exibida nos dois painéis (indicador e dono
  da Lottery) reflete 100% das compras pagas vinculadas ao código em até 30
  segundos após a confirmação do pagamento PIX correspondente.
- **SC-010**: A mudança de um ticket para `Refunded` (manual, via UI de gestão
  de status) reflete imediatamente nos dois painéis de comissão na próxima
  consulta — não há defasagem de processamento porque o cálculo é sempre em
  tempo real.
- **SC-011**: O sistema NÃO executa nenhuma movimentação financeira: 0%
  de chamadas de pagamento ou de estorno disparadas pelos módulos de
  indicação e de Lottery cancelada. Toda liquidação (comissões e
  estornos) é responsabilidade do dono e ocorre fora do sistema.

## Assumptions

- O sistema reutiliza a identidade de usuários do microserviço externo **NAuth**;
  cadastros e autenticação ocorrem nesse serviço. O tenant Fortuno é "fortuna".
- Pagamentos PIX e a entidade **Store** (com seu usuário proprietário) vêm do
  microserviço externo **ProxyPay**; o tenant a ser usado é "fortuna".
- Uploads de imagem e vídeo, bem como geração de PDF a partir de Markdown e
  recursos de IA quando necessários, são delegados ao **zTools**.
- Esta plataforma Fortuno **não** é multi-tenant internamente, mas integra-se a
  serviços multi-tenant usando sempre o tenant "fortuna".
- Todas as tabelas e objetos de banco criados por esta plataforma usam o prefixo
  `fortuna_` (ex.: `fortuna_lotteries`, `fortuna_tickets`).
- O cadastro "simples" de comprador exige apenas dados mínimos suficientes para
  comunicação e associação de tickets; exigências legais adicionais (CPF) são
  coletadas somente se o usuário for premiado ou se tornar criador de loteria.
- A política de unicidade do slug é global no sistema (não por Store).
- Ticket Min/Ticket Max iguais a 0 significam "sem restrição".
- `Ticket Num Ini` / `Ticket Num End` só têm efeito quando o tipo de número
  da Lottery é composto (3/4/5/6/7/8 números de 2 dígitos). Para o tipo
  "1 número int64", o pool de tickets é determinado diretamente por
  `[Valor Minimo, Valor Máximo]`. Consequência: uma Lottery com tipo composto
  e `Ticket Num End = 0` não pode sair de `Draft`; uma Lottery com tipo
  int64 pode ser publicada ignorando esses campos.
- Os dois modos de atribuição de número (`Random` e `UserPicks`) ficam sempre
  disponíveis em toda Lottery. O comprador decide a cada compra: `Random`
  sorteia os números ainda não vendidos no momento do pagamento; `UserPicks`
  permite selecionar individualmente cada número, com reserva temporária
  durante a finalização da compra.
- O sistema de indicação (referrer) é **interno ao Fortuno**: os códigos ficam
  em uma tabela local (`fortuna_user_referrers`) referenciando apenas o UserId
  do NAuth. O NAuth NÃO é modificado para acomodar essa feature.
- A comissão de indicação é **calculada em tempo real**, sem persistência em
  tabela própria. Base de cálculo por Invoice indicada: valor pago
  proporcional aos tickets ainda válidos (não `Refunded`) × `ReferralPercent`
  **vigente** da Lottery no momento da consulta. Consequência explícita:
  alterações em `ReferralPercent` refletem-se retroativamente nos valores
  exibidos — como o sistema não paga nem promete valores, essa volatilidade é
  aceitável. O único dado persistido é o vínculo indicador ↔ compra na
  tabela `InvoiceReferrer`.
- CPF de ganhadores é mascarado em exibições públicas (ex.: `***.456.789-**`);
  exibição completa apenas para o dono da loteria na tela de sorteio.
- Ambiente: o projeto será executado em três ambientes lógicos
  (`Development`, `Docker`, `Production`), cada um com sua configuração própria.
  Docker **não** está disponível no ambiente local do desenvolvedor; comandos
  `docker`/`docker compose` não serão executados durante o desenvolvimento.
