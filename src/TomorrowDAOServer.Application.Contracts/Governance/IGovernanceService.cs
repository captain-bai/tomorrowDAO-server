using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.DAO.Dtos;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Governance;

public interface IGovernanceService
{
    Task<GovernanceModesDto> GetGovernanceModesAsync(GovernanceModesInput input);
}