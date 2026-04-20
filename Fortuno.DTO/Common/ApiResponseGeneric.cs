using System.Text.Json.Serialization;

namespace Fortuno.DTO.Common;

public class ApiResponse<T> : ApiResponse
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string mensagem = "") => new()
    {
        Sucesso = true,
        Mensagem = mensagem,
        Data = data
    };

    public new static ApiResponse<T> Fail(string mensagem, IEnumerable<string>? erros = null) => new()
    {
        Sucesso = false,
        Mensagem = mensagem,
        Erros = erros?.ToList() ?? new List<string>()
    };
}
