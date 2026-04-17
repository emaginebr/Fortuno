namespace Fortuno.Domain.Interfaces;

public interface ISlugService
{
    Task<string> GenerateUniqueSlugAsync(string input);
}
