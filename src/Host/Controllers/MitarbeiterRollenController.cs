using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/mitarbeiter-rollen")]
public class MitarbeiterRollenController : ControllerBase
{
    private readonly HostDbContext _context;

    public MitarbeiterRollenController(HostDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<MitarbeiterRolleDto>>> GetAll()
    {
        var rollen = await _context.MitarbeiterRollen
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return Ok(rollen.Select(Map).ToList());
    }

    [HttpPost]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<MitarbeiterRolleDto>> Create([FromBody] MitarbeiterRolleRequest request)
    {
        var rolle = new MitarbeiterRolle
        {
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };

        _context.MitarbeiterRollen.Add(rolle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), Map(rolle));
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<MitarbeiterRolleDto>> Update(int id, [FromBody] MitarbeiterRolleRequest request)
    {
        var rolle = await _context.MitarbeiterRollen.FindAsync(id);
        if (rolle == null) return NotFound();

        rolle.Name = request.Name.Trim();
        rolle.SortOrder = request.SortOrder;
        await _context.SaveChangesAsync();

        return Ok(Map(rolle));
    }

    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var inUse = await _context.TeamMembers.AnyAsync(m => m.MitarbeiterRolleId == id);
        if (inUse)
            return BadRequest(new { error = "Diese Rolle wird noch von Mitarbeitern verwendet und kann nicht gelöscht werden." });

        var rolle = await _context.MitarbeiterRollen.FindAsync(id);
        if (rolle == null) return NotFound();

        _context.MitarbeiterRollen.Remove(rolle);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static MitarbeiterRolleDto Map(MitarbeiterRolle r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        SortOrder = r.SortOrder
    };
}

public class MitarbeiterRolleRequest
{
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}
