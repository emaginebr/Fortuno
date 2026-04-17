<!--
Sync Impact Report
==================
Version change: 0.0.0 (template, unratified) → 1.0.0 (initial ratification)
Rationale: MAJOR bump — first concrete ratification of the project constitution, replacing
all template placeholders with binding principles.

Modified principles:
- [PRINCIPLE_1_NAME] → I. Skills Obrigatórias (dotnet-architecture)
- [PRINCIPLE_2_NAME] → II. Stack Tecnológica Fixa
- [PRINCIPLE_3_NAME] → III. Convenções de Código .NET
- [PRINCIPLE_4_NAME] → IV. Convenções de Banco de Dados (PostgreSQL)
- [PRINCIPLE_5_NAME] → V. Autenticação e Segurança

Added sections:
- Restrições Adicionais (Variáveis de Ambiente + Tratamento de Erros)
- Fluxo de Desenvolvimento (Checklist para Novos Contribuidores)
- Governance (amendment procedure, versioning policy, compliance review)

Removed sections:
- None (all template placeholders were replaced; nothing else removed)

Templates requiring updates:
- ✅ .specify/memory/constitution.md (this file)
- ⚠ .specify/templates/plan-template.md — "Constitution Check" section is generic;
  manual review recommended to add explicit gates (skill usage, snake_case, [Authorize],
  no alternative ORMs, no local docker).
- ✅ .specify/templates/spec-template.md (no constitution-coupled changes required)
- ✅ .specify/templates/tasks-template.md (no constitution-coupled changes required)

Follow-up TODOs: none deferred.
-->

# Constituição do Projeto Fortuno

> Padrões obrigatórios de stack tecnológica, convenções de código e arquitetura backend
> que devem ser seguidos por todos os contribuidores em todos os projetos.

## Core Principles

### I. Skills Obrigatórias (NÃO NEGOCIÁVEL)

Para criação ou modificação de entidades, services, repositories, DTOs, migrations e
configuração de DI no backend, a skill `dotnet-architecture` **DEVE** ser invocada
(`/dotnet-architecture`). Contribuidores **NÃO DEVEM** reimplementar manualmente padrões
já cobertos pela skill, incluindo:

- Estrutura de projetos e fluxo de dependência (Clean Architecture backend).
- Regras de repositórios genéricos, mapeamento manual e DI centralizado.
- Configuração de DbContext, Fluent API e migrações via `dotnet ef`.
- Convenções de nomeação de DTOs (`Info`, `InsertInfo`, `Result`) e chaves portuguesas
  em respostas (`sucesso`, `mensagem`, `erros`).

**Rationale**: a skill consolida decisões arquiteturais já validadas; duplicá-las
manualmente provoca divergência, retrabalho e perda de rastreabilidade.

### II. Stack Tecnológica Fixa

As seguintes tecnologias são obrigatórias no backend e **NÃO DEVEM** ser substituídas
sem emenda formal desta constituição:

| Tecnologia | Versão | Finalidade |
|---|---|---|
| .NET | 8.0 | Runtime e framework principal |
| Entity Framework Core | 9.x | ORM e migrações |
| PostgreSQL | Latest | Banco de dados relacional |
| NAuth | Latest | Autenticação (Basic token) |
| zTools | Latest | Upload S3, e-mail (MailerSend), slugs |
| Swashbuckle | 8.x | Swagger / OpenAPI |

Regras adicionais:

- **NÃO** introduzir ORMs alternativos (Dapper, NHibernate, etc.) — EF Core é o único
  ORM permitido.
- **NÃO** executar comandos `docker` ou `docker compose` no ambiente local — Docker
  não está acessível.

**Rationale**: homogeneidade da stack reduz custo cognitivo, elimina incompatibilidades
de migração e garante suporte de longo prazo das ferramentas já aprovadas.

### III. Convenções de Código .NET

Todo código .NET **DEVE** aderir às seguintes convenções:

| Elemento | Convenção | Exemplo |
|---|---|---|
| Namespaces | PascalCase, file-scoped | `namespace Fortuno.Domain.Services;` |
| Classes / Interfaces | PascalCase | `CampaignService`, `ICampaignRepository` |
| Métodos | PascalCase | `GetById()`, `MapToDto()` |
| Propriedades | PascalCase | `CampaignId`, `CreatedAt` |
| Campos privados | `_camelCase` | `_repository`, `_context` |
| Constantes | `UPPER_CASE` | `BUCKET_NAME` |

JSON:

- Todas as propriedades de DTOs **DEVEM** ser anotadas com
  `[JsonPropertyName("camelCase")]`.

**Rationale**: convenções uniformes tornam a base de código previsível e permitem
revisão de PRs centrada em lógica, não em estilo.

### IV. Convenções de Banco de Dados (PostgreSQL)

Modelagem e migrations **DEVEM** seguir:

| Elemento | Convenção | Exemplo |
|---|---|---|
| Tabelas | snake_case plural | `campaigns`, `campaign_entries` |
| Colunas | snake_case | `campaign_id`, `created_at` |
| Primary Keys | `{entidade}_id`, `bigint` identity | `campaign_id bigint PK` |
| Constraint PK | `{tabela}_pkey` | `campaigns_pkey` |
| Foreign Keys | `fk_{pai}_{filho}` | `fk_campaign_entry` |
| Delete behavior | `ClientSetNull` | Nunca `Cascade` |
| Timestamps | `timestamp without time zone` | Sem timezone |
| Strings | `varchar` com `MaxLength` | `varchar(260)` |
| Booleans | `boolean` com default | `DEFAULT true` |
| Status/Enums | `integer` | `DEFAULT 1` |

Configuração de DbContext, Fluent API e comandos de migração são detalhados na skill
`dotnet-architecture` e **DEVEM** seguir essa referência.

**Rationale**: padronização do schema simplifica queries, migrações e ferramentas de
análise; `ClientSetNull` evita deleções em cascata acidentais em produção.

### V. Autenticação e Segurança (NÃO NEGOCIÁVEL)

Toda rota que lide com dados sensíveis **DEVE** ser protegida conforme a tabela:

| Aspecto | Padrão |
|---|---|
| Esquema | Basic Authentication via NAuth |
| Header | `Authorization: Basic {token}` |
| Handler | `NAuthHandler` registrado no DI |
| Proteção de rotas | Atributo `[Authorize]` nos controllers |

Regras:

- Controllers com dados sensíveis **DEVEM** ter `[Authorize]`.
- **NUNCA** expor connection strings, tokens ou secrets em respostas da API, logs
  públicos ou mensagens de erro.
- CORS `AllowAnyOrigin` é permitido **somente** em `ASPNETCORE_ENVIRONMENT=Development`.

**Rationale**: exposição acidental de credenciais é irreversível; centralizar o
esquema de auth em `NAuthHandler` garante trilha de auditoria consistente.

## Restrições Adicionais

### Variáveis de Ambiente Obrigatórias

| Variável | Obrigatória | Descrição |
|---|---|---|
| `ConnectionStrings__<nome-do-projeto>Context` | Sim | Connection string PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | Sim | `Development`, `Docker` ou `Production` |

Builds ou deploys **NÃO DEVEM** prosseguir sem essas variáveis definidas.

### Padrão de Tratamento de Erros

Controllers **DEVEM** envolver lógica externa em try/catch e devolver `500` com a
mensagem da exceção:

```csharp
try { /* lógica */ }
catch (Exception ex) { return StatusCode(500, ex.Message); }
```

Nenhuma exceção não tratada **DEVE** escapar do controller.

## Fluxo de Desenvolvimento

### Checklist para Novos Contribuidores

Antes de submeter qualquer PR, o autor **DEVE** verificar:

- [ ] Utilizou a skill `dotnet-architecture` para novas entidades backend.
- [ ] Tabelas e colunas seguem `snake_case` no PostgreSQL.
- [ ] Controllers com dados sensíveis possuem `[Authorize]`.
- [ ] DTOs possuem `[JsonPropertyName("camelCase")]` em todas as propriedades.
- [ ] Nenhum ORM alternativo foi introduzido.
- [ ] Nenhum `docker` / `docker compose` foi executado no ambiente local.

Revisores **DEVEM** rejeitar PRs que violem qualquer item desta checklist sem
justificativa registrada na seção de Complexity Tracking do plano.

## Governance

Esta constituição **prevalece** sobre quaisquer outras práticas informais do projeto.

**Procedimento de emenda**:

1. Alterações exigem PR que modifique `.specify/memory/constitution.md` com Sync
   Impact Report atualizado.
2. A versão **DEVE** ser incrementada conforme semver:
   - **MAJOR**: remoção ou redefinição incompatível de princípio ou regra de
     governança.
   - **MINOR**: adição de princípio/seção ou expansão material de guidance.
   - **PATCH**: esclarecimentos, correções de redação, ajustes não semânticos.
3. Templates dependentes (`plan-template.md`, `spec-template.md`, `tasks-template.md`,
   comandos em `.specify/templates/commands/`) **DEVEM** ser revisados e atualizados
   no mesmo PR quando afetados.
4. Datas de ratificação e última emenda **DEVEM** permanecer em formato ISO
   `YYYY-MM-DD`.

**Revisão de conformidade**:

- Todo PR **DEVE** verificar conformidade com os princípios I–V e com a Checklist
  de Contribuidores.
- Desvios **DEVEM** ser justificados em seção dedicada do plano (Complexity
  Tracking) e aprovados explicitamente por revisor.
- Complexidade adicional sem justificativa é motivo de rejeição.

Para guidance de runtime de desenvolvimento, consulte a skill `dotnet-architecture`
e demais skills referenciadas no Princípio I.

**Version**: 1.0.0 | **Ratified**: 2026-04-02 | **Last Amended**: 2026-04-02
