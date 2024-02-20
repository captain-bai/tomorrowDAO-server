using System.Collections.Generic;

namespace TomorrowDAOServer.Governance;

public class GovernanceModesDto
{
    public List<GovernanceModeDto> GovernanceModes { get; set; }
}

public class GovernanceModeDto
{
    public string Id { get; set; }
    public GovernanceMechanism GovernanceMechanism { get; set; }
    public string ChainId { get; set; }
}
