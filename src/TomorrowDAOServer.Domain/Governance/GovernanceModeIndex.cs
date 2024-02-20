using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.Governance;

public class GovernanceModeIndex : AbstractEntity<string>, IIndexBuild
{
    public string Id { get; set; }
    public GovernanceMechanism GovernanceMechanism { get; set; }
    public string ChainId { get; set; }
}

public enum GovernanceMechanism
{
    Unspecified = 0,
    Parliament = 1,
    Association = 2,
    Customize = 3,
    Referendum = 4
}
