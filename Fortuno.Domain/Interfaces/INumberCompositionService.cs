using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Interfaces;

public interface INumberCompositionService
{
    int ComponentCount(NumberType type);
    long Compose(NumberType type, IReadOnlyList<int> components);
    List<int> Decompose(NumberType type, long composedValue);
    bool IsValid(NumberType type, long composedValue, int min, int max);
    long CountPossibilities(NumberType type, int min, int max);
    IEnumerable<long> EnumerateAll(NumberType type, int min, int max);
    string Format(NumberType type, long composedValue);
}
