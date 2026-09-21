using FluentAssertions;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Services.Docker;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Docker;

public class ManuallyStoppedModulesTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("stopped-modules-test-").FullName;
    private string FilePath => Path.Combine(_dir, "data", "manually-stopped-modules.json");

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Mark_UeberlebtNeuinstanziierung()
    {
        new ManuallyStoppedModules(FilePath).Mark("Saldo", "Saldo Finanzen");

        var reloaded = new ManuallyStoppedModules(FilePath);

        reloaded.IsStopped("Saldo").Should().BeTrue();
        reloaded.All["Saldo"].Should().Be("Saldo Finanzen");
    }

    [Fact]
    public void Schluessel_SindCaseInsensitive()
    {
        var store = new ManuallyStoppedModules(FilePath);
        store.Mark("Faktura", "Faktura");

        store.IsStopped("faktura").Should().BeTrue();
        store.Clear("FAKTURA");
        store.IsStopped("Faktura").Should().BeFalse();
    }

    [Fact]
    public void Clear_EntferntAuchAusDerDatei()
    {
        var store = new ManuallyStoppedModules(FilePath);
        store.Mark("Acta", "Acta");
        store.Clear("Acta");

        new ManuallyStoppedModules(FilePath).All.Should().BeEmpty();
    }

    [Fact]
    public void DateiFehlt_ErgibtLeereListe()
    {
        new ManuallyStoppedModules(FilePath).All.Should().BeEmpty();
    }

    [Fact]
    public void BeschaedigteDatei_WirdAlsLeereListeBehandelt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{kaputt");

        new ManuallyStoppedModules(FilePath).All.Should().BeEmpty();
    }
}

public class ModuleStatusResolverTests
{
    private readonly Moq.Mock<IManuallyStoppedModules> _store = new();

    public ModuleStatusResolverTests()
    {
        _store.Setup(s => s.All).Returns(new Dictionary<string, string>());
    }

    private static ModuleInfoDto Module(string name) => new() { ModuleName = name, DisplayName = name };

    [Fact]
    public void LaufendesModul_IstOnlineUndEinVeralteterMerkerWirdEntfernt()
    {
        var result = ModuleStatusResolver.Resolve(new[] { (Module("Faktura"), true) }, _store.Object);

        result.Should().ContainSingle().Which.State.Should().Be(ModuleDisplayState.Online);
        _store.Verify(s => s.Clear("Faktura"), Moq.Times.Once);
    }

    [Fact]
    public void OfflineOhneMerker_IstAusgefallen()
    {
        _store.Setup(s => s.All).Returns(new Dictionary<string, string>());

        var result = ModuleStatusResolver.Resolve(new[] { (Module("Faktura"), false) }, _store.Object);

        result.Should().ContainSingle().Which.State.Should().Be(ModuleDisplayState.Offline);
    }

    [Fact]
    public void OfflineMitMerker_IstManuellGestoppt()
    {
        _store.Setup(s => s.IsStopped("Faktura")).Returns(true);
        _store.Setup(s => s.All).Returns(new Dictionary<string, string> { ["Faktura"] = "Faktura" });

        var result = ModuleStatusResolver.Resolve(new[] { (Module("Faktura"), false) }, _store.Object);

        result.Should().ContainSingle().Which.State.Should().Be(ModuleDisplayState.ManuallyStopped);
    }

    [Fact]
    public void ManuellGestopptesModulOhneRegistryEintrag_WirdAusDemMerkerErgaenzt()
    {
        _store.Setup(s => s.All).Returns(new Dictionary<string, string> { ["Saldo"] = "Saldo Finanzen" });

        var result = ModuleStatusResolver.Resolve(new[] { (Module("Faktura"), true) }, _store.Object);

        result.Should().Equal(
            ("Faktura", ModuleDisplayState.Online),
            ("Saldo Finanzen", ModuleDisplayState.ManuallyStopped));
    }

    [Fact]
    public void Ergebnis_IstNachNamenSortiert()
    {
        _store.Setup(s => s.All).Returns(new Dictionary<string, string>());

        var result = ModuleStatusResolver.Resolve(
            new[] { (Module("Saldo"), true), (Module("Acta"), true), (Module("Faktura"), false) }, _store.Object);

        result.Select(r => r.Name).Should().Equal("Acta", "Faktura", "Saldo");
    }
}
