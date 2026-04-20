using System.Text.Json.Serialization;

namespace Fortuno.DTO.Common;

public class ApiResponse
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = string.Empty;

    [JsonPropertyName("erros")]
    public List<string> Erros { get; set; } = new();

    public static ApiResponse Ok(string mensagem = "") => new() { Sucesso = true, Mensagem = mensagem };

    public static ApiResponse Fail(string mensagem, IEnumerable<string>? erros = null) => new()
    {
        Sucesso = false,
        Mensagem = mensagem,
        Erros = erros?.ToList() ?? new List<string>()
    };
}
