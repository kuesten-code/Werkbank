using Npgsql;

namespace Kuestencode.Werkbank.Host.Services;

/// <summary>
/// Erschließt die für Diagnosezwecke nützlichste Meldung aus einer Exception-Kette.
/// Findet insbesondere eine verschachtelte <see cref="PostgresException"/> und reichert
/// deren Meldung um Tabelle/Spalte an — ohne das reicht der bloße Wrapper-Text von
/// DbUpdateException/ExecuteSqlAsync-Fehlern nicht aus, um die Ursache zu erkennen.
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

            current = current.InnerException;
        }

        return ex.InnerException?.Message ?? ex.Message;
    }
}
