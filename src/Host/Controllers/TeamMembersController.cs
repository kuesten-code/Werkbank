using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/team-members")]
[RequireRole(UserRole.Admin)]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _teamMemberService;
    private readonly IInviteService _inviteService;

    public TeamMembersController(ITeamMemberService teamMemberService, IInviteService inviteService)
    {
        _teamMemberService = teamMemberService;
        _inviteService = inviteService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamMemberDto>>> GetTeamMembersAsync()
    {
        var members = await _teamMemberService.GetAllAsync(includeInactive: true);
        return Ok(members.Select(Map).ToList());
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> SendInvite(Guid id)
    {
        try
        {
            await _inviteService.CreateInviteAsync(id);
            return Ok(new { message = "Einladung gesendet." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("invite/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token, [FromBody] AcceptInviteRequest request)
    {
        try
        {
            await _inviteService.AcceptInviteAsync(token, request.Password);
            return Ok(new { message = "Passwort gesetzt. Account ist aktiv." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("invite/{token}/valid")]
    public async Task<IActionResult> ValidateInviteToken(string token)
    {
        var member = await _inviteService.ValidateInviteTokenAsync(token);
        if (member == null)
            return Ok(new { valid = false });

        return Ok(new { valid = true, displayName = member.DisplayName });
    }

    private static TeamMemberDto Map(Kuestencode.Werkbank.Host.Models.TeamMember member) => new()
    {
        Id = member.Id,
        DisplayName = member.DisplayName,
        Email = member.Email,
        Role = member.Role.ToString(),
        IsActive = member.IsActive
    };
}

public class AcceptInviteRequest
{
    public string Password { get; set; } = string.Empty;
}
