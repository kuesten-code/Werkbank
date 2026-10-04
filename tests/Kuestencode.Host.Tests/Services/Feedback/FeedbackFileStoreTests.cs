using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackFileStoreTests : IDisposable
{
    private readonly string _basePath = Directory.CreateTempSubdirectory("werkbank-feedback-store-").FullName;
    private readonly FeedbackFileStore _store;

    public FeedbackFileStoreTests()
    {
        _store = FeedbackTestData.CreateFileStore(_basePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, true);
    }

    [Fact]
    public async Task SaveAsync_SpeichertUnterGeneriertemNamenUndLiestWiederEin()
    {
        var path = await _store.SaveAsync("hub/1/abc", ".png", FeedbackTestData.Png);

        path.Should().StartWith("hub/1/abc/").And.EndWith(".png");
        (await _store.ReadAllBytesAsync(path)).Should().Equal(FeedbackTestData.Png);
        using var stream = _store.OpenRead(path);
        stream.Length.Should().Be(FeedbackTestData.Png.Length);
    }

    [Fact]
    public async Task Delete_EntferntDatei_UndIgnoriertFehlendeDatei()
    {
        var path = await _store.SaveAsync("x", ".pdf", FeedbackTestData.Pdf);

        _store.Delete(path);
        _store.Delete(path);

        File.Exists(Path.Combine(_basePath, path)).Should().BeFalse();
    }

    [Theory]
    [InlineData("../ausserhalb.png")]
    [InlineData("../../etc/passwd")]
    public void OpenRead_PfadAusserhalbDesBasisordners_WirdAbgelehnt(string relativePath)
    {
        var act = () => _store.OpenRead(relativePath);

        act.Should().Throw<InvalidOperationException>();
    }
}
