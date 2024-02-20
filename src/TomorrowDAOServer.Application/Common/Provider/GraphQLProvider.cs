using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Governance;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common.Provider;

public interface IGraphQLProvider
{
    public Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType);
    public Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height);
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    public Task<long> GetHoldersAsync(string symbol, string chainId, int skipCount, int maxResultCount);
    public Task<List<GovernanceModeDto>> GetGovernanceModesAsync(string chainId);
}

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly IGraphQLClient _graphQLClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQlClientFactory _graphQlClientFactory;

    public GraphQLProvider(IGraphQLClient graphQLClient, ILogger<GraphQLProvider> logger,
        IClusterClient clusterClient, IGraphQlClientFactory graphQlClientFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQlClientFactory = graphQlClientFactory;
        _graphQLClient = graphQLClient;
    }
    
    public async Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {id} error", chainId);
            return CommonConstant.LongError;
        }
    }

    public async Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
            await grain.SetStateAsync(height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetIndexBlockHeight on chain {id} error", chainId);
        }
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<ConfirmedBlockHeightRecord>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(input: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        });

        return graphQlResponse.Data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<long> GetHoldersAsync(string symbol, string chainId, int skipCount, int maxResultCount)
    {
        try
        {
            var response = await _graphQlClientFactory.GetClient(GraphQLClientEnum.ModuleClient)
                .SendQueryAsync<HolderResult>(new GraphQLRequest
                {
                    Query = @"
                    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$symbol:String!){
                        data:tokenInfo(input:{chainId: $chainId,skipCount: $skipCount,maxResultCount: $maxResultCount,symbol: $symbol})
                        {
                            holderCount
                        }}",
                    Variables = new
                    {
                        chainId, skipCount, maxResultCount, symbol
                    }
                }); 
            return response.Data?.Data?.First().HolderCount ?? 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetHoldersAsyncException chainId={chainId}, symbol={symbol}", chainId, symbol);
        }
        return 0;
    }

    public async Task<List<GovernanceModeDto>> GetGovernanceModesAsync(string chainId)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<ConfirmedBlockHeightRecord>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(input: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        });

        return graphQlResponse.Data;
    }


    public async Task<long> GetProjectIdAsync(string chainId, string projectId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(projectId + "-" + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetProjectIdAsync on chain-projectId {id}-{projectId} error", chainId, projectId);
            return CommonConstant.LongError;
        }
    }
}