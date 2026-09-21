using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Backup;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Backup;

public class DirectoryCopyTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("directory-copy-test-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Dir(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void CopyOverwriting_UeberschreibtVeraltetesUndErgaenztNeuesAuchInUnterordnern()
    {
        var source = Dir("source");
        var target = Dir("target");

        // Zustand wie beim Backup: Ziel enthält den WAL-Stand von VOR pg_backup_stop.
        File.WriteAllText(Path.Combine(target, "segment-1"), "alt");
        File.WriteAllText(Path.Combine(source, "segment-1"), "neu und länger");
        File.WriteAllText(Path.Combine(source, "segment-2"), "erst nach dem Stop entstanden");
        Directory.CreateDirectory(Path.Combine(source, "archive_status"));
        File.WriteAllText(Path.Combine(source, "archive_status", "segment-1.ready"), "");

        DirectoryCopy.CopyOverwriting(source, target);

        File.ReadAllText(Path.Combine(target, "segment-1")).Should().Be("neu und länger");
        File.ReadAllText(Path.Combine(target, "segment-2")).Should().Be("erst nach dem Stop entstanden");
        File.Exists(Path.Combine(target, "archive_status", "segment-1.ready")).Should().BeTrue();
    }

    [Fact]
    public void CopyOverwriting_LaesstNurImZielVorhandeneDateienUnangetastet()
    {
        var source = Dir("source");
        var target = Dir("target");
        File.WriteAllText(Path.Combine(target, "nur-im-ziel"), "bleibt");

        DirectoryCopy.CopyOverwriting(source, target);

        File.ReadAllText(Path.Combine(target, "nur-im-ziel")).Should().Be("bleibt");
    }

    [Fact]
    public void CopyOverwriting_VerschwindetEineDateiWaehrendDesKopierens_WirdSieBeiToleranzUebersprungen()
    {
        // Wie bei einem laufenden Postgres: ein WAL-Segment wird recycelt, nachdem es aufgelistet wurde.
        var source = Dir("source");
        var target = Dir("target");
        File.WriteAllText(Path.Combine(source, "000000010000000000000008"), "recycelt");
        File.WriteAllText(Path.Combine(source, "000000010000000000000009"), "bleibt");

        var skipped = DirectoryCopy.CopyOverwriting(source, target, tolerateVanishedSource: true,
            onBeforeCopy: file =>
            {
                if (file.EndsWith("08")) File.Delete(file);
            });

        skipped.Should().Be(1);
        File.Exists(Path.Combine(target, "000000010000000000000008")).Should().BeFalse();
        File.ReadAllText(Path.Combine(target, "000000010000000000000009")).Should().Be("bleibt");
    }

    [Fact]
    public void CopyOverwriting_VerschwindetEineDateiWaehrendDesKopierens_WirftOhneToleranz()
    {
        var source = Dir("source");
        var target = Dir("target");
        File.WriteAllText(Path.Combine(source, "datei"), "x");

        var act = () => DirectoryCopy.CopyOverwriting(source, target, tolerateVanishedSource: false,
            onBeforeCopy: File.Delete);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void CopyOverwriting_VerschwindetEinUnterordnerWaehrendDesKopierens_WirdErUebersprungen()
    {
        var source = Dir("source");
        var target = Dir("target");
        Directory.CreateDirectory(Path.Combine(source, "pgsql_tmp"));
        File.WriteAllText(Path.Combine(source, "pgsql_tmp", "tmp1"), "x");
        File.WriteAllText(Path.Combine(source, "haupt"), "y");

        // Der Unterordner wird erst nach dem Auflisten der Dateien der Wurzel entfernt.
        var skipped = DirectoryCopy.CopyOverwriting(source, target, tolerateVanishedSource: true,
            onBeforeCopy: file =>
            {
                if (file.EndsWith("haupt")) Directory.Delete(Path.Combine(source, "pgsql_tmp"), recursive: true);
            });

        skipped.Should().Be(1);
        File.ReadAllText(Path.Combine(target, "haupt")).Should().Be("y");
    }

    [Fact]
    public void CopyOverwriting_LegtFehlendesZielverzeichnisAn()
    {
        var source = Dir("source");
        File.WriteAllText(Path.Combine(source, "a"), "x");
        var target = Path.Combine(_root, "neu", "tiefer");

        DirectoryCopy.CopyOverwriting(source, target);

        File.ReadAllText(Path.Combine(target, "a")).Should().Be("x");
    }
}
