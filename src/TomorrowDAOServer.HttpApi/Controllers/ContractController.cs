using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Contract.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contract")]
[Route("api/")]
public class ContractController
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }
    
    [HttpGet]
    [Route("contract/function-list")]
    public List<FunctionInfoDto> FunctionList(QueryFunctionListInput input)
    {
        return _contractService.GetFunctionList(input.ChainId, input.ContractAddress);
    }
    
    [HttpGet]
    [Route("contracts-info")]
    public List<ContractInfoDto> ContractsInfo(QueryContractsInfoInput input)
    {
        return _contractService.GetContractInfo(input.ChainId);
    }
}