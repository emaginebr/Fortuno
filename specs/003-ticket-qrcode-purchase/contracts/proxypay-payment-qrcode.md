# Contract — ProxyPay endpoints consumidos pelo Fortuno

**Consumed by**: `ProxyPayAppService` (`Fortuno.Infra/AppServices/`)
**Headers comuns**: `X-Tenant-Id: {{ProxyPay:TenantId}}`, `Authorization: Basic {nauthToken}` (forwarded do `HttpContext`), `Content-Type: application/json`

---

## 1. POST /payment/qrcode

### Request

```http
POST {{ProxyPay:ApiUrl}}/payment/qrcode HTTP/1.1
Authorization: Basic {{forwardedToken}}
X-Tenant-Id: fortuno
Content-Type: application/json
```

Body (`ProxyPayQRCodeRequest`):

```json
{
  "clientId": "5b2a4084154d4e88941b76aee1395348",
  "customer": {
    "name": "Rodrigo Landim",
    "email": "rodrigo@emagine.com.br",
    "documentId": "89639766100",
    "cellphone": "11999999999"
  },
  "items": [
    {
      "id": "LOTTERY-1",
      "description": "Fortuno Lottery #1 - 5 tickets",
      "quantity": 5,
      "unitPrice": 10.00,
      "discount": 0
    }
  ]
}
```

### Campos

| Campo | Origem no Fortuno |
|---|---|
| `clientId` | `Lottery.StoreClientId` (cached no momento da criação da Lottery — R-004) |
| `customer.name` | `NAuthUserInfo.Name` |
| `customer.email` | `NAuthUserInfo.Email` |
| `customer.documentId` | `NAuthUserInfo.DocumentId` (CPF) |
| `customer.cellphone` | `NAuthUserInfo.Phone` |
| `items[0].id` | `"LOTTERY-{lotteryId}"` — identificador interno, não precisa ser global |
| `items[0].description` | `"Fortuno Lottery #{lotteryId} - {quantity} tickets"` |
| `items[0].quantity` | `TicketOrderRequest.Quantity` |
| `items[0].unitPrice` | `Lottery.TicketPrice` |
| `items[0].discount` | desconto de combo aplicável (calculado antes da chamada); `0` se sem combo |

### Response — 2xx

```json
{
  "invoiceId": 1,
  "invoiceNumber": "INV-0001-000001",
  "brCode": "00020101021126580014BR.GOV.BCB.PIX0136devmode-pix-...",
  "brCodeBase64": "data:image/png;base64,iVBORw0KGgoAAAA...",
  "expiredAt": "2026-04-19T14:20:00.348+00:00"
}
```

DTO: `ProxyPayQRCodeResponse` em `Fortuno.DTO/ProxyPay/`.

### Response — Erros

| Status | Tratamento no Fortuno |
|---|---|
| 4xx | Log Information com body; propaga como `InvalidOperationException` com mensagem do provedor |
| 5xx | Log Warning com body; propaga como `InvalidOperationException("ProxyPay indisponível (status {n}).")` |
| Timeout | `InvalidOperationException("ProxyPay indisponível — timeout")` |

### Logging (seguindo padrão estabelecido)

```
INFO  ProxyPay: POST /payment/qrcode clientId=... quantity=... totalAmount=...
INFO  ProxyPay: resposta CreateQRCode status=201 body={invoiceId: 1, ...}
```

---

## 2. GET /payment/qrcode/status/{invoiceId}

### Request

```http
GET {{ProxyPay:ApiUrl}}/payment/qrcode/status/{invoiceId} HTTP/1.1
Authorization: Basic {{forwardedToken}}
X-Tenant-Id: fortuno
```

### Response — 2xx (shape assumido — R-001)

```json
{
  "invoiceId": 1,
  "status": "pending",
  "paidAt": null
}
```

Valores aceitos de `status` (case-insensitive): `"pending"`, `"paid"`, `"expired"`, `"cancelled"`.

### Mapeamento no Fortuno (`ProxyPayAppService.GetQRCodeStatusAsync`)

```csharp
return dto.Status?.ToLowerInvariant() switch
{
    "pending"   => TicketOrderStatus.Pending,
    "paid"      => TicketOrderStatus.Paid,
    "expired"   => TicketOrderStatus.Expired,
    "cancelled" => TicketOrderStatus.Cancelled,
    _           => TicketOrderStatus.Unknown
};
```

DTO: `ProxyPayQRCodeStatusResponse` em `Fortuno.DTO/ProxyPay/`.

### Response — Erros

| Status | Tratamento |
|---|---|
| 404 | retorna `Unknown` (sem exceção — idempotência do polling exige tolerância) |
| 5xx | log + retorna `Unknown` (permite retry pelo polling) |
| Timeout | log + retorna `Unknown` |

### Logging

```
INFO  ProxyPay: GET /payment/qrcode/status/1
INFO  ProxyPay: resposta QRCodeStatus status=200 body={invoiceId:1, status:"pending"}
```

---

## DTOs Fortuno.DTO/ProxyPay/

Novos arquivos (um por DTO conforme refactor anterior):

- `ProxyPayQRCodeRequest.cs`
- `ProxyPayCustomer.cs`
- `ProxyPayItem.cs`
- `ProxyPayQRCodeResponse.cs`
- `ProxyPayQRCodeStatusResponse.cs`

Anotação `[JsonPropertyName("camelCase")]` em todas as propriedades (§III).

### Exemplo: ProxyPayQRCodeRequest.cs

```csharp
using System.Text.Json.Serialization;

namespace Fortuno.DTO.ProxyPay;

public class ProxyPayQRCodeRequest
{
    [JsonPropertyName("clientId")] public string ClientId { get; set; } = string.Empty;
    [JsonPropertyName("customer")] public ProxyPayCustomer Customer { get; set; } = new();
    [JsonPropertyName("items")]    public List<ProxyPayItem> Items { get; set; } = new();
}
```

### DTOs a depreciar / remover

- `ProxyPayCreateInvoiceRequest.cs` — removido
- `ProxyPayInvoiceInfo.cs` — removido
- Em `IProxyPayAppService`: remover `CreateInvoiceAsync(ProxyPayCreateInvoiceRequest)` e `GetInvoiceAsync(long)`; adicionar `CreateQRCodeAsync(ProxyPayQRCodeRequest)` e `GetQRCodeStatusAsync(long)`.
