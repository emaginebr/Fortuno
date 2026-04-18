namespace Fortuno.ApiTests._Fixtures;

public static class UniqueId
{
    public static string New(string prefix)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("n")[..8];
        return $"{prefix}-{timestamp}-{guid}";
    }
}
