# Sorteio — Moto R$ 100.000 | IDE Sem Fronteiras

Exemplo de lottery cadastrada no Fortuno para a campanha filantrópica do **Instituto de Desenvolvimento e Educação Sem Fronteiras (IDE Sem Fronteiras)** — <https://idesemfronteiras.org.br>.

O sorteio é uma **Filantropia Premiável** prevista na legislação brasileira (Lei nº 5.768/1971 e Decreto nº 70.951/1972), operada sob autorização da SECAP/Ministério da Fazenda. Esta documentação é o material-base que alimenta a API do Fortuno ao cadastrar a lottery: descrição pública, regras oficiais e política de privacidade.

---

## Ficha resumo

| Campo | Valor |
|---|---|
| Promotora | IDE Sem Fronteiras |
| Canal oficial | <https://idesemfronteiras.org.br> |
| Prêmio | 1 motocicleta 0 km, valor total R$ 100.000,00 |
| Preço unitário do número | R$ 0,99 |
| Faixa de números | `000000` — `999999` (1.000.000 números) |
| Sorteio | 31/10/2026, 22h00 (BRT / UTC−3) |
| Transmissão | Live na Tweet TV (link oficial divulgado em até 7 dias antes) |
| Apuração | Loteria Federal da Caixa — extração de sábado 31/10/2026 |
| Mínimo de vendas para validade | 101.011 números (cobre 100% do prêmio) |
| Autorização SECAP | `CA nº XX.XXX.XXX/XXXX-XX` (preencher antes da publicação) |
| CNPJ da promotora | `XX.XXX.XXX/XXXX-XX` (preencher antes da publicação) |

---

## Conteúdo publicado

Os textos abaixo são injetados nas colunas Markdown da tabela `fortuna_lotteries`:

| Campo (API) | Arquivo |
|---|---|
| `descriptionMd` | [`descricao.md`](./descricao.md) |
| `rulesMd` | [`regras.md`](./regras.md) |
| `privacyPolicyMd` | [`politica-privacidade.md`](./politica-privacidade.md) |

---

## Payload para `POST /lotteries`

```json
{
  "storeId": 1,
  "name": "Sorteio Moto R$ 100.000 — IDE Sem Fronteiras",
  "descriptionMd": "<conteúdo de descricao.md>",
  "rulesMd": "<conteúdo de regras.md>",
  "privacyPolicyMd": "<conteúdo de politica-privacidade.md>",
  "ticketPrice": 0.99,
  "totalPrizeValue": 100000.00,
  "ticketMin": 101011,
  "ticketMax": 0,
  "ticketNumIni": 0,
  "ticketNumEnd": 999999,
  "numberType": 0,
  "numberValueMin": 0,
  "numberValueMax": 999999,
  "referralPercent": 0
}
```

> `numberType = 0` corresponde a `NumberTypeDto.Int64` (número inteiro único de 6 dígitos, alinhado à mecânica da Loteria Federal).
> `ticketMax = 0` significa sem teto na quantidade de bilhetes por comprador.

### Combos de desconto — `POST /lottery-combos`

Os combos são cadastrados individualmente após a criação da lottery (substitua `{{lotteryId}}`):

| Faixa (`quantityStart` – `quantityEnd`) | Desconto | Nome sugerido |
|---|---|---|
| 25 – 49 | 10% | `Combo 25+` |
| 50 – 74 | 15% | `Combo 50+` |
| 75 – 99 | 20% | `Combo 75+` |
| 100 – 149 | 25% | `Combo 100+` |
| 150 – 199 | 30% | `Combo 150+` |
| 200 – 0 | 35% | `Combo 200+` |

> `quantityEnd = 0` significa sem teto superior (faixa aberta). Um único pedido de 1 bilhete não aciona combo — o preço cheio de R$ 0,99 se aplica para 1 a 24 bilhetes.

```json
{
  "lotteryId": 0,
  "name": "Combo 25+",
  "discountValue": 0.10,
  "discountLabel": "10% OFF",
  "quantityStart": 25,
  "quantityEnd": 49
}
```

---

## Imagens da lottery

Subir via `POST /lottery-images` (máx. 10 imagens recomendadas):

- Foto oficial da motocicleta (frontal, lateral, dashboard)
- Logo do IDE Sem Fronteiras
- Arte promocional da campanha
- Certificado SECAP (opcional, aumenta confiança)

---

## Checklist legal pré-publicação

- [ ] CA (Certificado de Autorização) da SECAP emitido e copiado para `regras.md`
- [ ] Caução equivalente a 100% do valor do prêmio depositado junto à Caixa
- [ ] Apólice de seguro-garantia vigente (alternativa à caução)
- [ ] Nota fiscal de compra da moto ou contrato de reserva em nome do IDE Sem Fronteiras
- [ ] Responsável técnico / jurídico designado em ata
- [ ] Canal de atendimento ao consumidor (e-mail + telefone) publicado
- [ ] Política LGPD homologada pelo DPO/Encarregado
- [ ] Certificado digital da entidade válido para emissão de comprovantes
