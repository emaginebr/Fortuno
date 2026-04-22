using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;

namespace Fortuno.Domain.Services;

public class NumberCompositionService : INumberCompositionService
{
    // Cada componente de 2 dígitos ocupa 2 casas decimais (00-99).
    private const long ComponentBase = 100L;

    public int ComponentCount(NumberType type) => type switch
    {
        NumberType.Int64 => 1,
        NumberType.Composed3 => 3,
        NumberType.Composed4 => 4,
        NumberType.Composed5 => 5,
        NumberType.Composed6 => 6,
        NumberType.Composed7 => 7,
        NumberType.Composed8 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public long Compose(NumberType type, IReadOnlyList<int> components)
    {
        if (type == NumberType.Int64)
        {
            if (components.Count != 1) throw new ArgumentException("Int64 requer exatamente 1 componente.");
            return components[0];
        }

        var n = ComponentCount(type);
        if (components.Count != n) throw new ArgumentException($"Tipo {type} requer exatamente {n} componentes.");

        long value = 0;
        foreach (var c in components)
        {
            if (c < 0 || c > 99) throw new ArgumentException($"Componente {c} fora da faixa 0-99.");
            value = value * ComponentBase + c;
        }
        return value;
    }

    public List<int> Decompose(NumberType type, long composedValue)
    {
        if (type == NumberType.Int64)
            return new List<int> { (int)composedValue };

        var n = ComponentCount(type);
        var result = new int[n];
        for (int i = n - 1; i >= 0; i--)
        {
            result[i] = (int)(composedValue % ComponentBase);
            composedValue /= ComponentBase;
        }
        return result.ToList();
    }

    public bool IsValid(NumberType type, long composedValue, int min, int max)
    {
        if (type == NumberType.Int64)
            return composedValue >= min && composedValue <= max;

        foreach (var c in Decompose(type, composedValue))
        {
            if (c < min || c > max) return false;
        }
        return true;
    }

    public long CountPossibilities(NumberType type, int min, int max)
    {
        if (max < min) return 0;
        long perComponent = max - min + 1;
        if (type == NumberType.Int64) return perComponent;

        var n = ComponentCount(type);
        long total = 1;
        for (int i = 0; i < n; i++)
        {
            total = checked(total * perComponent);
        }
        return total;
    }

    /// <summary>
    /// Formata o número composto em string legível. Int64 → a própria string
    /// decimal. Tipos compostos → componentes de 2 dígitos (zero-padded)
    /// **ordenados em ordem ascendente** e separados por "-" (ex.:
    /// "60-39-05-28-11" vira "05-11-28-39-60"). A ordenação é determinística
    /// e permite busca/comparação por valor.
    /// </summary>
    public string Format(NumberType type, long composedValue)
    {
        if (type == NumberType.Int64)
            return composedValue.ToString();

        var components = Decompose(type, composedValue);
        return string.Join("-", components.OrderBy(c => c).Select(c => c.ToString("D2")));
    }

    public IEnumerable<long> EnumerateAll(NumberType type, int min, int max)
    {
        if (type == NumberType.Int64)
        {
            for (long v = min; v <= max; v++) yield return v;
            yield break;
        }

        var n = ComponentCount(type);
        var components = new int[n];
        for (int i = 0; i < n; i++) components[i] = min;

        while (true)
        {
            yield return Compose(type, components);
            int idx = n - 1;
            while (idx >= 0)
            {
                components[idx]++;
                if (components[idx] <= max) break;
                components[idx] = min;
                idx--;
            }
            if (idx < 0) yield break;
        }
    }
}
