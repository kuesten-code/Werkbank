using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Services.Pdf;

/// <summary>
/// Generische PDF-Engine für plattformweite PDF-Generierung.
/// Module registrieren ihre eigenen Renderer.
/// </summary>
public class PdfEngine : IPdfEngine
{
    private readonly ICompanyService _companyService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PdfEngine> _logger;

    public PdfEngine(
        ICompanyService companyService,
        IServiceProvider serviceProvider,
        ILogger<PdfEngine> logger)
    {
        _companyService = companyService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public byte[] GeneratePdf<TDocument>(TDocument document, string rendererName)
    {
        var renderer = GetRenderer<TDocument>(rendererName);
        var company = _companyService.GetCompanyAsync().GetAwaiter().GetResult();

        _logger.LogInformation("Generiere PDF mit Renderer '{RendererName}'", rendererName);

        return renderer.Render(document, company);
    }

    public async Task<string> GenerateAndSaveAsync<TDocument>(
        TDocument document,
        string rendererName,
        string outputPath)
    {
        var pdfBytes = GeneratePdf(document, rendererName);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(outputPath, pdfBytes);

        _logger.LogInformation("PDF gespeichert unter '{OutputPath}'", outputPath);

        return outputPath;
    }

    public bool HasRenderer(string rendererName)
    {
        // Wir können nicht direkt prüfen ohne den Dokumenttyp zu kennen
        // Aber wir können zumindest prüfen ob der Name registriert ist
        var allRenderers = GetAllRegisteredRendererNames();
        return allRenderers.Contains(rendererName);
    }

    public IEnumerable<string> GetRegisteredRenderers()
    {
        return GetAllRegisteredRendererNames();
    }

    private IPdfDocumentRenderer<TDocument> GetRenderer<TDocument>(string rendererName)
    {
        var renderers = _serviceProvider.GetServices<IPdfDocumentRenderer<TDocument>>();
        var renderer = renderers.FirstOrDefault(r => r.RendererName == rendererName);

        if (renderer == null)
        {
            _logger.LogError("PDF-Renderer '{RendererName}' für Typ {DocumentType} nicht gefunden",
                rendererName, typeof(TDocument).Name);
            throw new InvalidOperationException(
                $"PDF-Renderer '{rendererName}' für Typ {typeof(TDocument).Name} nicht gefunden");
        }

        return renderer;
    }

    private IEnumerable<string> GetAllRegisteredRendererNames()
    {
        // Sammle alle registrierten Renderer-Namen
        var names = new List<string>();

        // Wir müssen alle möglichen Renderer-Typen durchgehen
        // Da wir den generischen Typ nicht kennen, nutzen wir Reflection
        var rendererTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IPdfDocumentRenderer<>)));

        foreach (var type in rendererTypes)
        {
            var instance = _serviceProvider.GetService(type);
            if (instance != null)
            {
                var prop = type.GetProperty("RendererName");
                if (prop != null)
                {
                    var name = prop.GetValue(instance) as string;
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }
        }

        return names.Distinct();
    }
}
