using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _teamMemberService;

    public TeamMembersController(ITeamMemberService teamMemberService)
    {
        _teamMemberService = teamMemberService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamMemberDto>>> GetTeamMembersAsync()
    {
        var members = await _teamMemberService.GetAllAsync(includeInactive: true);
        return Ok(members.Select(Map).ToList());
    }

    private static TeamMemberDto Map(Kuestencode.Werkbank.Host.Models.TeamMember member) => new()
    {
        Id = member.Id,
        DisplayName = member.DisplayName,
        Email = member.Email
    };
}
