using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Proposal.Provider;

public interface IProposalProvider
{
    Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId, long startBlockHeight,
        long endBlockHeight, int maxResultCount);

    public Task<List<ProposalIndex>> GetProposalListAsync(QueryProposalListInput input);
    
    public Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId);
    public Task<List<ProposalIndex>> GetProposalByDAOIdAsync(string chainId, string proposalId);
    
    public Task<Dictionary<string, ProposalIndex>> GetProposalListByIds(string chainId, List<string> ids);

    public Task<long> GetProposalCountByDAOIds(string chainId, string DAOId);

    public Task BulkAddOrUpdateAsync(List<ProposalIndex> list);

    public Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList);
}

public class ProposalProvider : IProposalProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<ProposalIndex, string> _proposalIndexRepository;

    public ProposalProvider(IGraphQlHelper graphQlHelper,
        INESTRepository<ProposalIndex, string> proposalIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _proposalIndexRepository = proposalIndexRepository;
    }

    public async Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId,
        long startBlockHeight, long endBlockHeight, int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerProposalSync>(new GraphQLRequest
        {
            Query =
                @"query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!){
            dataList:getSyncProposalInfos(input: {skipCount:$skipCount,chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount})
            {
                id,chainId,blockHeight,
                dAOId,proposalId,proposalTitle,proposalDescription,forumUrl,proposalType,
                activeStartTime,activeEndTime,executeStartTime,executeEndTime,
                proposalStatus,proposalStage,proposer,schemeAddress,
                transaction {
                    toAddress,contractMethodName,params
                },            
                voteSchemeId,vetoProposalId,deployTime,executeTime,
                governanceMechanism,
                minimalRequiredThreshold,minimalVoteThreshold,minimalApproveThreshold,
                maximalRejectionThreshold,maximalAbstentionThreshold,
                activeTimePeriod,vetoActiveTimePeriod,pendingTimePeriod,executeTimePeriod,vetoExecuteTimePeriod,isNetworkDAO
            }}",
            Variables = new
            {
                skipCount,
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount
            }
        });
        return graphQlResponse?.DataList ?? new List<IndexerProposal>();
    }

    public async Task<List<ProposalIndex>> GetProposalListAsync(QueryProposalListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        var shouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        AssemblyBaseQuery(input, mustQuery);

        AssemblyContentQuery(input.Content, shouldQuery);

        if (shouldQuery.Any())
        {
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        //add sorting
        var sortDescriptor = GetQuerySortDescriptor();

        var result =  await _proposalIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: input.SkipCount,
            limit: input.MaxResultCount);
        return result.Item2 ?? new List<ProposalIndex>();
    }

    public async Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ProposalId).Value(proposalId)));
      
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _proposalIndexRepository.GetAsync(Filter);
    }

    public async Task<List<ProposalIndex>> GetProposalByDAOIdAsync(string chainId, string DAOId)
    {
        //todo query all
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.DAOId).Value(DAOId)));
      
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return (await _proposalIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<Dictionary<string, ProposalIndex>> GetProposalListByIds(string chainId, List<string> proposalIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));
        
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.ProposalId).Terms(proposalIds)));
        
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        var tuple = await _proposalIndexRepository.GetListAsync(Filter);

        return tuple.Item2.ToDictionary(p => p.ProposalId, p => p);
    }

    public async Task<long> GetProposalCountByDAOIds(string chainId, string DAOId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.ChainId).Terms(chainId)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DAOId).Terms(DAOId)));
        
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        return (await _proposalIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task BulkAddOrUpdateAsync(List<ProposalIndex> list)
    {
       await _proposalIndexRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => !q.Terms(i =>
                i.Field(f => f.ProposalStage).Terms(stageList))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var tuple = await _proposalIndexRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }
    
    private static void AssemblyBaseQuery(QueryProposalListInput input,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> mustQuery)
    {
        if (!input.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }
        
        if (!input.DaoId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.DAOId).Value(input.DaoId)));
        }

        if (input.GovernanceMechanism != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.GovernanceMechanism).Value(input.GovernanceMechanism)));
        }
        
        if (input.ProposalType != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalType).Value(input.ProposalType)));
        }
        
        if (input.ProposalStatus != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalStatus).Value(input.ProposalStatus)));
        }
    }

    private static void AssemblyContentQuery(string content,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> shouldQuery)
    {
        var titleMustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        var descriptionMustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        var proposalIdMustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        
        titleMustQuery.Add(q => q.
            Match(m => m.Field(f => f.ProposalTitle).Query(content)));
        descriptionMustQuery.Add(q => q.
            Match(m => m.Field(f => f.ProposalDescription).Query(content)));
        proposalIdMustQuery.Add(q => q.
            Match(m => m.Field(f => f.ProposalId).Query(content)));
        
        shouldQuery.Add(s => s.Bool(sb => sb.Must(titleMustQuery)));
        shouldQuery.Add(s => s.Bool(sb => sb.Must(descriptionMustQuery)));
        shouldQuery.Add(s => s.Bool(sb => sb.Must(proposalIdMustQuery)));
    }

    private static Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>> GetQuerySortDescriptor()
    {
        //use default
        var sortDescriptor = new SortDescriptor<ProposalIndex>();

        sortDescriptor.Descending(a => a.DeployTime);

        return _ => sortDescriptor;
    }
}