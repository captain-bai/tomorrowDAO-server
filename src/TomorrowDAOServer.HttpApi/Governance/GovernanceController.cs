using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Governance")]
[Route("api/")]
public class GovernanceController
{
    private readonly IGovernanceService _governanceAppService;
    
    public GovernanceController(IGovernanceService governanceService)
    {
        _governanceAppService = governanceService;
    }
    
    [HttpGet("governance-modes")]
    public async Task<GovernanceModesDto> GetGovernanceModesAsync(GovernanceModesInput request)
    {
        return await _governanceAppService.GetGovernanceModesAsync(request);
    }
}