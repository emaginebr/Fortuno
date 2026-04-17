namespace Fortuno.Infra.Interfaces.AppServices;

public interface IZToolsAppService
{
    Task<string> UploadImageAsync(string base64Content, string fileNameHint);
    Task<string> UploadFileAsync(byte[] content, string contentType, string fileNameHint);
    Task<byte[]> GeneratePdfFromMarkdownAsync(string markdown, string title);
    Task<string> GenerateSlugAsync(string input);
}
