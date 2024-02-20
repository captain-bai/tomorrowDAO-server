using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AutoMapper.Internal.Mappers;
using Nest;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.DAO;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GovernanceAppService : ApplicationService, IGovernanceService
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INESTRepository<GovernanceModeIndex, string> _governanceIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public GovernanceAppService(INESTRepository<GovernanceModeIndex, string> governanceIndexRepository, IGraphQLProvider graphQlProvider, IObjectMapper objectMapper)
    {
        _governanceIndexRepository = governanceIndexRepository;
        _objectMapper = objectMapper;
    }


    public Task<List<GovernanceModeDto>> GetGovernanceModesAsync(GovernanceModesInput input)
    {
        var dao = await _graphQlProvider.GetGovernanceModesAsync(input.ChainId);
        
        var items = ObjectMapper.Map<List<GovernanceModeIndex>, List<GovernanceModeDto>>(dao);
        return items;
    }
}