using Fortuno.DTO.Settings;
using Fortuno.Infra.Interfaces.AppServices;
using Markdig;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using zTools.ACL.Interfaces;

namespace Fortuno.Infra.AppServices;

public class ZToolsAppService : IZToolsAppService
{
    private readonly IFileClient _fileClient;
    private readonly IStringClient _stringClient;
    private readonly ZToolsSettings _settings;

    public ZToolsAppService(
        IFileClient fileClient,
        IStringClient stringClient,
        IOptions<FortunoSettings> options)
    {
        _fileClient = fileClient;
        _stringClient = stringClient;
        _settings = options.Value.ZTools;
    }

    public async Task<string> UploadImageAsync(string base64Content, string fileNameHint)
    {
        var bytes = DecodeBase64(base64Content);
        return await UploadFileAsync(bytes, "image/png", fileNameHint);
    }

    public async Task<string> UploadFileAsync(byte[] content, string contentType, string fileNameHint)
    {
        using var ms = new MemoryStream(content);
        var formFile = new FormFile(ms, 0, content.Length, "file", fileNameHint)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return await _fileClient.UploadFileAsync(_settings.S3BucketName, formFile);
    }

    public Task<byte[]> GeneratePdfFromMarkdownAsync(string markdown, string title)
    {
        // NOTE: o pacote zTools não expõe um método público de geração de PDF
        // (ver research.md §9). Implementação temporária: renderiza HTML e
        // retorna como bytes. Substituir por zTools.GeneratePdfAsync quando
        // disponível.
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(markdown ?? string.Empty, pipeline);
        var doc = $"<!doctype html><html><head><meta charset=\"utf-8\"><title>{System.Net.WebUtility.HtmlEncode(title)}</title></head><body><h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>{html}</body></html>";
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(doc));
    }

    public Task<string> GenerateSlugAsync(string input)
        => _stringClient.GenerateSlugAsync(input);

    private static byte[] DecodeBase64(string input)
    {
        var payload = input;
        var commaIdx = payload.IndexOf(',');
        if (payload.StartsWith("data:") && commaIdx > 0)
            payload = payload[(commaIdx + 1)..];
        return Convert.FromBase64String(payload);
    }
}
