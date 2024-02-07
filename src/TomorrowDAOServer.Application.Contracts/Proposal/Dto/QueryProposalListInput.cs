using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryProposalListInput : PagedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    
    [Required]
    public string DaoId { get; set; }
    
    public GovernanceMechanism? GovernanceMechanism { get; set; }

    public ProposalType? ProposalType { get; set; }

    public ProposalStatus? ProposalStatus { get; set; }

    public string Content { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}