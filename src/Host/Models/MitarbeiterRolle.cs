using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Host.Models;

public class MitarbeiterRolle
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    public int SortOrder { get; set; }
}
