using System.Collections.Generic;

namespace TomorrowDAOServer.Common.GraphQL;

public class HolderResult : IndexerCommonResult<HolderResult>
{
    public List<HolderDto> Data { get; set; }
}

public class HolderDto
{
    public long HolderCount { get; set; }
}