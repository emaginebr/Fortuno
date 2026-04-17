using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class SlugService : ISlugService
{
    private readonly ILotteryRepository<Lottery> _lotteryRepository;
    private readonly IZToolsAppService _zTools;

    public SlugService(ILotteryRepository<Lottery> lotteryRepository, IZToolsAppService zTools)
    {
        _lotteryRepository = lotteryRepository;
        _zTools = zTools;
    }

    public async Task<string> GenerateUniqueSlugAsync(string input)
    {
        string baseSlug;
        try
        {
            baseSlug = (await _zTools.GenerateSlugAsync(input ?? string.Empty)).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = LocalSlug(input ?? string.Empty);
        }
        catch
        {
            // Fallback se zTools estiver indisponível.
            baseSlug = LocalSlug(input ?? string.Empty);
        }

        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "lottery";

        var candidate = baseSlug;
        var suffix = 2;
        while (await _lotteryRepository.SlugExistsAsync(candidate))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return candidate;
    }

    private static string LocalSlug(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = Regex.Replace(clean, @"[^a-z0-9]+", "-");
        clean = clean.Trim('-');
        return clean;
    }
}
