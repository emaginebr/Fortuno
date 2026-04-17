using Fortuno.DTO.Common;
using Fortuno.Domain.Models;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.API.Middlewares;

/// <summary>
/// Garante que todo usuário autenticado tenha um UserReferrer (FR-R02).
/// Gera o código na primeira requisição autenticada do usuário.
/// </summary>
public class EnsureUserReferrerMiddleware
{
    // Alfabeto do referral: A-Z sem I e O (24 letras) + 2-9 (8 dígitos) = 32 símbolos.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private static readonly Random _rng = new();

    private readonly RequestDelegate _next;

    public EnsureUserReferrerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserReferrerRepository<UserReferrer> repo)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.TryGetCurrentUserId();
            if (userId.HasValue)
            {
                var existing = await repo.GetByUserIdAsync(userId.Value);
                if (existing is null)
                {
                    var code = await GenerateUniqueCodeAsync(repo);
                    var newEntry = new UserReferrer
                    {
                        UserId = userId.Value,
                        ReferralCode = code
                    };
                    try { await repo.InsertAsync(newEntry); }
                    catch { /* possivel corrida — ignora se já foi criado por outra requisição */ }
                }
            }
        }

        await _next(context);
    }

    private static async Task<string> GenerateUniqueCodeAsync(IUserReferrerRepository<UserReferrer> repo)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var code = RandomCode();
            if (!await repo.CodeExistsAsync(code)) return code;
        }
        throw new InvalidOperationException("Não foi possível gerar um código de indicação único após 10 tentativas.");
    }

    private static string RandomCode()
    {
        var chars = new char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
            chars[i] = Alphabet[_rng.Next(Alphabet.Length)];
        return new string(chars);
    }
}
