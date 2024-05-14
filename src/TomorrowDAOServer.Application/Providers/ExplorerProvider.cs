using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;
using ProposalType = TomorrowDAOServer.Common.Enum.ProposalType;

namespace TomorrowDAOServer.Providers;

public interface IExplorerProvider
{
    Task<ExplorerProposalResponse> GetProposalPagerAsync(string chainId, ExplorerProposalListRequest request);
    Task<List<ExplorerBalanceOutput>> GetBalancesAsync(string chainId, ExplorerBalanceRequest request);
    Task<ExplorerTokenInfoResponse> GetTokenInfoAsync(string chainId, ExplorerTokenInfoRequest request);
    Task<string> GetTokenDecimalAsync(string chainId, string symbol);

    Task<ExplorerPagerResult<ExplorerTransactionResponse>> GetTransactionPagerAsync(string chainId,
        ExplorerTransactionRequest request);

    Task<ExplorerPagerResult<ExplorerTransferResult>> GetTransferListAsync(string chainId,
        ExplorerTransferRequest request);

    ProposalType GetProposalType(ProposalSourceEnum proposalSource);
    string GetProposalStatus(ProposalStatus? proposalStatus, ProposalStage? proposalStage);
}

public static class ExplorerApi
{
    public static readonly ApiInfo ProposalList = new(HttpMethod.Get, "/api/proposal/list");
    public static readonly ApiInfo Organizations = new(HttpMethod.Get, "/api/proposal/organizations");
    public static readonly ApiInfo Balances = new(HttpMethod.Get, "/api/viewer/balances");
    public static readonly ApiInfo TokenInfo = new(HttpMethod.Get, "/api/viewer/tokenInfo");
    public static readonly ApiInfo Transactions = new(HttpMethod.Get, "/api/all/transaction");
    public static readonly ApiInfo TransferList = new(HttpMethod.Get, "/api/viewer/transferList");
}

public class ExplorerProvider : IExplorerProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<ExplorerOptions> _explorerOptions;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    private const string ProposalStatusAll = "all";


    public ExplorerProvider(IHttpProvider httpProvider, IOptionsMonitor<ExplorerOptions> explorerOptions)
    {
        _httpProvider = httpProvider;
        _explorerOptions = explorerOptions;
    }

    public string BaseUrl(string chainId)
    {
        var urlExists = _explorerOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var baseUrl);
        AssertHelper.IsTrue(urlExists && baseUrl.NotNullOrEmpty(), "Explorer url not found of chainId {}", chainId);
        return baseUrl!.TrimEnd('/');
    }

    /// <summary>
    ///     GetProposalPagerAsync
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExplorerProposalResponse> GetProposalPagerAsync(string chainId,
        ExplorerProposalListRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerProposalResponse>>(BaseUrl(chainId),
            ExplorerApi.ProposalList, param: ToDictionary(request), withInfoLog: false, withDebugLog: false,
            settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    ///     Get Balances by address
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<List<ExplorerBalanceOutput>> GetBalancesAsync(string chainId, ExplorerBalanceRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<List<ExplorerBalanceOutput>>>(BaseUrl(chainId),
            ExplorerApi.Balances, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    ///     Get token info
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExplorerTokenInfoResponse> GetTokenInfoAsync(string chainId, ExplorerTokenInfoRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerTokenInfoResponse>>(BaseUrl(chainId),
            ExplorerApi.TokenInfo, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    public async Task<string> GetTokenDecimalAsync(string chainId, string symbol)
    {
        if (symbol.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return (await GetTokenInfoAsync(chainId, new ExplorerTokenInfoRequest
        {
            Symbol = symbol
        })).Decimals;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    public async Task<ExplorerPagerResult<ExplorerTransactionResponse>> GetTransactionPagerAsync(string chainId,
        ExplorerTransactionRequest request)
    {
        var resp = await _httpProvider
            .InvokeAsync<ExplorerBaseResponse<ExplorerPagerResult<ExplorerTransactionResponse>>>(BaseUrl(chainId),
                ExplorerApi.Transactions, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chainId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ExplorerPagerResult<ExplorerTransferResult>> GetTransferListAsync(string chainId,
        ExplorerTransferRequest request)
    {
        var resp = await _httpProvider.InvokeAsync<ExplorerBaseResponse<ExplorerPagerResult<ExplorerTransferResult>>>(
            BaseUrl(chainId), ExplorerApi.TransferList, param: ToDictionary(request), settings: DefaultJsonSettings);
        AssertHelper.IsTrue(resp.Success, resp.Msg);
        return resp.Data;
    }

    public ProposalType GetProposalType(ProposalSourceEnum proposalSource)
    {
        return proposalSource switch
        {
            ProposalSourceEnum.ONCHAIN_ASSOCIATION => ProposalType.Association,
            ProposalSourceEnum.ONCHAIN_REFERENDUM => ProposalType.Referendum,
            ProposalSourceEnum.ONCHAIN_PARLIAMENT => ProposalType.Parliament,
            _ => ProposalType.Parliament
        };
    }

    public string GetProposalStatus(ProposalStatus? proposalStatus, ProposalStage? proposalStage)
    {
        switch (proposalStatus)
        {
            case null:
                return ProposalStatusAll;
            //explorer support status: all, pending, approved, released, expired
            case ProposalStatus.PendingVote:
            case ProposalStatus.BelowThreshold:
            case ProposalStatus.Challenged:
                return ProposalStatusEnum.Pending.ToString().ToLower();
            case ProposalStatus.Approved:
                return ProposalStatusEnum.Approved.ToString().ToLower();
            case ProposalStatus.Executed:
                return ProposalStatusEnum.Released.ToString().ToLower();
            case ProposalStatus.Expired:
            case ProposalStatus.Vetoed:
                return ProposalStatusEnum.Expired.ToString().ToLower();
            case ProposalStatus.Empty:
            case ProposalStatus.Rejected:
            case ProposalStatus.Abstained:
            default:
                return ProposalStatusAll;
        }
    }

    private Dictionary<string, string> ToDictionary(object param)
    {
        if (param == null) return null;
        if (param is Dictionary<string, string>) return param as Dictionary<string, string>;
        var json = param is string ? param as string : JsonConvert.SerializeObject(param, DefaultJsonSettings);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
    }
}