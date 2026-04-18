# Contract — NAuth Authentication

**Feature**: 002-qa-test-suite
**Used by**: `Fortuno.ApiTests` (`ApiSessionFixture`) + Bruno collection (`_Auth/login.bru`)

## Endpoint

`POST {nauthUrl}/auth/login`

## Request

Headers:

```text
Content-Type: application/json
```

Body:

```json
{
  "tenant":   "{{nauthTenant}}",
  "user":     "{{nauthUser}}",
  "password": "{{nauthPassword}}"
}
```

## Response (success)

Status: `200 OK`

Body (shape minimamente exigida pela suite; campos adicionais são ignorados):

```json
{
  "token": "<string — valor a ser usado em Authorization: Basic {token}>"
}
```

## Response (failure modes esperados)

| Código | Significado | Tratamento na suite |
|---|---|---|
| `400` | Payload inválido | `ApiSessionFixture` falha com mensagem "Credenciais de teste inválidas — verifique FORTUNO_TEST_NAUTH_*". |
| `401` | Credenciais incorretas | Mesmo tratamento acima. |
| `5xx` | NAuth indisponível | Falha com "NAuth indisponível em {nauthUrl}". |

## Uso downstream

Após captura do `token`:

- ApiTests: `flurlClient.WithHeader("Authorization", $"Basic {token}")`.
- Bruno: `bru.setVar("accessToken", res.body.token)` e `Authorization: Basic {{accessToken}}` em cada request subsequente.

## Notas de implementação

- O endpoint exato (`/auth/login`, `/login`, `/token`) depende da versão do pacote NAuth usada em produção. Confirmar em `Fortuno.API/Program.cs` durante a implementação; se divergir, ajustar a constante em um único lugar (`TestSettings.NAuthLoginPath`).
- A suite **não** reusa token entre execuções — cada `dotnet test` faz um novo login.
