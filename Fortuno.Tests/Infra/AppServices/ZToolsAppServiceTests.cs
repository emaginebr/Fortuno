using System.Text;
using FluentAssertions;
using Fortuno.DTO.Settings;
using Fortuno.Infra.AppServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using zTools.ACL.Interfaces;

namespace Fortuno.Tests.Infra.AppServices;

public class ZToolsAppServiceTests
{
    private readonly Mock<IFileClient> _fileClient = new();
    private readonly Mock<IStringClient> _stringClient = new();
    private readonly IOptions<FortunoSettings> _options = Options.Create(new FortunoSettings
    {
        ZTools = new ZToolsSettings { ApiUrl = "https://ztools.test", S3BucketName = "fortuno-bucket" }
    });

    private ZToolsAppService CreateSut() =>
        new(_fileClient.Object, _stringClient.Object, _options);

    [Fact]
    public async Task GenerateSlugAsync_ShouldDelegateToStringClient()
    {
        _stringClient.Setup(c => c.GenerateSlugAsync("Rifa QA")).ReturnsAsync("rifa-qa");

        var sut = CreateSut();
        var result = await sut.GenerateSlugAsync("Rifa QA");

        result.Should().Be("rifa-qa");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldDecodeBase64AndDelegateToFileClient()
    {
        var bytes = Encoding.UTF8.GetBytes("fake-image-bytes");
        var base64 = Convert.ToBase64String(bytes);
        _fileClient.Setup(c => c.UploadFileAsync("fortuno-bucket", It.IsAny<IFormFile>()))
            .ReturnsAsync("https://s3.example/image.png");

        var sut = CreateSut();
        var url = await sut.UploadImageAsync(base64, "img.png");

        url.Should().Be("https://s3.example/image.png");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldStripDataUriPrefix()
    {
        var bytes = Encoding.UTF8.GetBytes("content");
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(bytes);
        _fileClient.Setup(c => c.UploadFileAsync("fortuno-bucket", It.IsAny<IFormFile>()))
            .ReturnsAsync("https://s3.example/x.png");

        var sut = CreateSut();
        var url = await sut.UploadImageAsync(dataUri, "x.png");

        url.Should().Be("https://s3.example/x.png");
    }

    [Fact]
    public async Task UploadImageAsync_ShouldThrow_WhenBase64Invalid()
    {
        var sut = CreateSut();
        Func<Task> act = () => sut.UploadImageAsync("NOT_BASE64$$$", "x.png");

        await act.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task UploadFileAsync_ShouldDelegateToFileClient()
    {
        var content = new byte[] { 1, 2, 3 };
        _fileClient.Setup(c => c.UploadFileAsync("fortuno-bucket", It.IsAny<IFormFile>()))
            .ReturnsAsync("https://s3/xyz");

        var sut = CreateSut();
        var url = await sut.UploadFileAsync(content, "application/pdf", "file.pdf");

        url.Should().Be("https://s3/xyz");
    }

    [Fact]
    public async Task GeneratePdfFromMarkdownAsync_ShouldReturnHtmlBytes()
    {
        var sut = CreateSut();
        var bytes = await sut.GeneratePdfFromMarkdownAsync("# Título\n\nConteúdo.", "Docs");

        bytes.Should().NotBeNullOrEmpty();
        var html = Encoding.UTF8.GetString(bytes);
        html.Should().Contain("<h1>Docs</h1>");
        html.Should().Contain("Conteúdo");
    }
}
