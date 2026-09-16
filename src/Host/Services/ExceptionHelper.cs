using Amazon.Runtime.Internal;
using Npgsql;

namespace Kuestencode.Werkbank.Host.Services;

/// <summary>
/// Erschließt die für Diagnosezwecke nützlichste Meldung aus einer Exception-Kette.
/// Findet insbesondere eine verschachtelte <see cref="PostgresException"/> und reichert
/// deren Meldung um Tabelle/Spalte an — ohne das reicht der bloße Wrapper-Text von
/// DbUpdateException/ExecuteSqlAsync-Fehlern nicht aus, um die Ursache zu erkennen.
/// Ebenso für <see cref="HttpErrorResponseException"/> (AWS SDK, z.B. S3-Backup-Ziele): die
/// wirft das SDK ohne eigene Message ("Exception of type '...' was thrown."), der eigentliche
/// HTTP-Statuscode steckt nur in der Response-Eigenschaft.
/// </summary>
public static class ExceptionHelper
{
    public static string Describe(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current is PostgresException pgEx)
            {
                var location = string.Join(", ", new[] { pgEx.TableName, pgEx.ColumnName }
                    .Where(s => !string.IsNullOrEmpty(s)));
                return string.IsNullOrEmpty(location)
                    ? pgEx.MessageText
                    : $"{pgEx.MessageText} (Tabelle/Spalte: {location})";
            }

            if (current is HttpErrorResponseException httpEx)
            {
                return $"S3-Server antwortete mit HTTP {(int)httpEx.Response.StatusCode} {httpEx.Response.StatusCode}";
            }

            current = current.InnerException;
        }

        return ex.InnerException?.Message ?? ex.Message;
    }
}
