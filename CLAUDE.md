# Fortuno Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-19

## Active Technologies
- C# 12 / .NET 8.0 (conforme constituição, Princípio II) (002-qa-test-suite)
- PostgreSQL (Fortuno) e ProxyPay (externo, para dados de Store). `Fortuno.ApiTests` **não** acessa o banco diretamente — somente chamadas HTTP contra `Fortuno.API` rodando. (002-qa-test-suite)
- C# 12 / .NET 8.0 (Constituição Fortuno §II) + ASP.NET Core 8 (MVC Controllers), Entity Framework Core 9.x + Npgsql, NAuth ACL (`IUserClient`), zTools (não alterado nesta feature), FluentValidation, Swashbuckle, HotChocolate (não alterado), Flurl.Http + xUnit + FluentAssertions (ApiTests) (003-ticket-qrcode-purchase)
- PostgreSQL (tabela nova `fortuna_ticket_orders`; tabelas existentes `fortuna_number_reservations`, `fortuna_tickets`, `fortuna_invoice_referrers` permanecem, webhook_events **DEPRECIA**) (003-ticket-qrcode-purchase)

- C# 12 / .NET 8.0 (001-lottery-saas)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# 12 / .NET 8.0

## Code Style

C# 12 / .NET 8.0: Follow standard conventions

## Recent Changes
- 003-ticket-qrcode-purchase: Added C# 12 / .NET 8.0 (Constituição Fortuno §II) + ASP.NET Core 8 (MVC Controllers), Entity Framework Core 9.x + Npgsql, NAuth ACL (`IUserClient`), zTools (não alterado nesta feature), FluentValidation, Swashbuckle, HotChocolate (não alterado), Flurl.Http + xUnit + FluentAssertions (ApiTests)
- 002-qa-test-suite: Added C# 12 / .NET 8.0 (conforme constituição, Princípio II)

- 001-lottery-saas: Added C# 12 / .NET 8.0

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
