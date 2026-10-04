using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Auth;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

public class FeedbackHubClient : IFeedbackHubClient
{
    public const string HttpClientName = "FeedbackHub";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<HttpStatusCode> PermanentStatusCodes = new()
    {
        HttpStatusCode.BadRequest,
        HttpStatusCode.RequestEntityTooLarge,
        HttpStatusCode.UnprocessableEntity
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public FeedbackHubClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<FeedbackInstanceInfoDto> GetInstanceAsync(FeedbackHubConnection connection, CancellationToken ct = default) =>
        SendAsync<FeedbackInstanceInfoDto>(connection, HttpMethod.Get, "api/v1/instance", null, ct);

    public Task<FeedbackReportDto> CreateReportAsync(
        FeedbackHubConnection connection, CreateFeedbackReportRequest request,
        IReadOnlyList<(string FileName, string ContentType, byte[] Content)> files, CancellationToken ct = default)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(request, JsonOptions)), "report" }
        };

        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent(file.Content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "files", file.FileName);
        }

        return SendAsync<FeedbackReportDto>(connection, HttpMethod.Post, "api/v1/reports", content, ct);
    }

    public Task<FeedbackSyncResponse> GetChangesAsync(FeedbackHubConnection connection, DateTime? since, CancellationToken ct = default)
    {
        var path = since.HasValue
            ? $"api/v1/reports?since={Uri.EscapeDataString(since.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}"
            : "api/v1/reports";
        return SendAsync<FeedbackSyncResponse>(connection, HttpMethod.Get, path, null, ct);
    }

    public Task<FeedbackCommentDto> AddCommentAsync(
        FeedbackHubConnection connection, int hubReportId, CreateFeedbackCommentRequest request, CancellationToken ct = default) =>
        SendAsync<FeedbackCommentDto>(connection, HttpMethod.Post, $"api/v1/reports/{hubReportId}/comments",
            JsonContent.Create(request, options: JsonOptions), ct);

    private async Task<T> SendAsync<T>(FeedbackHubConnection connection, HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        if (!Uri.TryCreate(connection.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new FeedbackHubException("Hub-URL ist ungültig.", isPermanent: false);
        }

        using var request = new HttpRequestMessage(method, new Uri(baseUri, path)) { Content = content };
        request.Headers.Add(FeedbackApiKeyFilter.HeaderName, connection.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FeedbackHubException($"Hub nicht erreichbar: {ex.Message}", isPermanent: false, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new FeedbackHubException(DescribeError(response.StatusCode, body), PermanentStatusCodes.Contains(response.StatusCode));
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
                ?? throw new FeedbackHubException("Leere Antwort vom Hub.", isPermanent: false);
        }
    }

    private static string DescribeError(HttpStatusCode statusCode, string body) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "API-Key ungültig oder Instanz deaktiviert.",
        HttpStatusCode.NotFound => "Hub-API nicht gefunden (URL prüfen bzw. ist die Gegenseite als Hub konfiguriert?).",
        _ => $"Hub antwortet mit {(int)statusCode}: {Truncate(body, 500)}"
    };

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
