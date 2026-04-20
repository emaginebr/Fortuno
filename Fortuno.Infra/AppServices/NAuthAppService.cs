using Fortuno.DTO.NAuth;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.AspNetCore.Http;
using NAuth.ACL.Interfaces;

namespace Fortuno.Infra.AppServices;

public class NAuthAppService : INAuthAppService
{
    private readonly IUserClient _userClient;
    private readonly IHttpContextAccessor _httpContext;

    public NAuthAppService(IUserClient userClient, IHttpContextAccessor httpContext)
    {
        _userClient = userClient;
        _httpContext = httpContext;
    }

    private string ExtractToken()
    {
        var header = _httpContext.HttpContext?.Request.Headers.Authorization.ToString() ?? string.Empty;
        if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return header.Substring(6);
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header.Substring(7);
        return header;
    }

    public async Task<NAuthUserInfo?> GetByIdAsync(long userId)
    {
        var user = await _userClient.GetByIdAsync(userId, ExtractToken());
        return user is null ? null : Map(user);
    }

    public async Task<List<NAuthUserInfo>> GetByIdsAsync(IEnumerable<long> userIds)
    {
        // NOTE: NAuth IUserClient does not expose a batch method (ver research.md §9).
        // Fallback: chama GetByIdAsync N vezes. Trocar por endpoint em lote quando disponível.
        var list = new List<NAuthUserInfo>();
        foreach (var id in userIds.Distinct())
        {
            var u = await _userClient.GetByIdAsync(id, ExtractToken());
            if (u is not null) list.Add(Map(u));
        }
        return list;
    }

    public async Task<NAuthUserInfo?> GetCurrentAsync()
    {
        var token = ExtractToken();
        if (string.IsNullOrEmpty(token)) return null;
        var user = await _userClient.GetMeAsync(token);
        return user is null ? null : Map(user);
    }

    private static NAuthUserInfo Map(NAuth.DTO.User.UserInfo u) => new()
    {
        UserId = u.UserId,
        Name = u.Name ?? string.Empty,
        Email = u.Email ?? string.Empty,
        DocumentId = u.IdDocument,
        Phone = u.Phones?.FirstOrDefault()?.Phone,
        Address = u.Addresses?.FirstOrDefault() is { } a
            ? $"{a.Address} - {a.City}/{a.State}"
            : null
    };
}
