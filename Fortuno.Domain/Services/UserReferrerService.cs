using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class UserReferrerService : IUserReferrerService
{
    // Alfabeto: A-Z sem I e O (24 letras) + 2-9 (8 dígitos) = 32 símbolos. FR-R02a.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private static readonly Random _rng = new();

    private readonly IUserReferrerRepository<UserReferrer> _repo;

    public UserReferrerService(IUserReferrerRepository<UserReferrer> repo)
    {
        _repo = repo;
    }

    public async Task<string> GetOrCreateCodeForUserAsync(long userId)
    {
        var existing = await _repo.GetByUserIdAsync(userId);
        if (existing is not null) return existing.ReferralCode;

        var code = await GenerateUniqueCodeAsync();
        var entity = new UserReferrer { UserId = userId, ReferralCode = code };
        try
        {
            await _repo.InsertAsync(entity);
            return code;
        }
        catch
        {
            // corrida de inserção — busca o código persistido
            var now = await _repo.GetByUserIdAsync(userId);
            return now?.ReferralCode ?? code;
        }
    }

    public async Task<string?> GetCodeForUserAsync(long userId)
    {
        var existing = await _repo.GetByUserIdAsync(userId);
        return existing?.ReferralCode;
    }

    public async Task<long?> ResolveReferrerUserIdAsync(string? referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode)) return null;
        var normalized = referralCode.Trim().ToUpperInvariant();
        var owner = await _repo.GetByCodeAsync(normalized);
        return owner?.UserId;
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var code = RandomCode();
            if (!await _repo.CodeExistsAsync(code)) return code;
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
