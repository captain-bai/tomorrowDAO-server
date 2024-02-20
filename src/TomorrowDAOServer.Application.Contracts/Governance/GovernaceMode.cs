using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.Governance;

public class GovernanceModesIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
}

public class GovernanceModeIndexs
{
    [Keyword] public string Id { get; set; }
    public GovernanceMechanism GovernanceMechanism { get; set; }
}

public enum GovernanceMechanism
{
    Unspecified = 0,
    Parliament = 1,
    Association = 2,
    Customize = 3,
    Referendum = 4
}

