using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using StokvelSync.Shared;

namespace StokvelSync.Api.Data;

public class MemberRepository
{
    private readonly TableClient _memberTable;

    public MemberRepository(TableServiceClient tableService)
    {
        // Initializes the connection to the 'Members' table in Azure Table Storage
        _memberTable = tableService.GetTableClient("Members");
        _memberTable.CreateIfNotExists();
    }

    /// <summary>
    /// Retrieves a single member by their email address.
    /// Fixes 'GetMemberByEmailAsync' error in MemberFunctions.cs.
    /// </summary>
    public async Task<MemberEntity?> GetMemberByEmailAsync(string email)
    {
        try 
        {
            // Uses 'StokvelMember' as the PartitionKey for consistent data organization
            var response = await _memberTable.GetEntityAsync<MemberEntity>("StokvelMember", email);
            return response.Value;
        }
        catch 
        { 
            // Returns null if the member is not found, allowing the Function to return a 404
            return null; 
        }
    }

    /// <summary>
    /// Retrieves all registered members in the group.
    /// Fixes 'GetAllMembersAsync' error in MemberFunctions.cs.
    /// </summary>
    public async Task<List<MemberEntity>> GetAllMembersAsync()
    {
        var members = new List<MemberEntity>();
        
        // Queries all entities under the 'StokvelMember' partition
        var queryResults = _memberTable.QueryAsync<MemberEntity>(filter: $"PartitionKey eq 'StokvelMember'");
        
        await foreach (var entity in queryResults)
        {
            members.Add(entity);
        }
        
        return members;
    }

    /// <summary>
    /// Saves or updates a member's profile information.
    /// Fixes 'UpsertMemberAsync' error in MemberFunctions.cs.
    /// </summary>
public async Task UpsertMemberAsync(Member member)
    {
        // We map the Shared Member model to the Data Entity for Table Storage
        var entity = new MemberEntity
        {
            PartitionKey = "StokvelMember",
            RowKey = member.Email,
            FullName = member.FullName ?? string.Empty,
            WhatsAppNumber = member.WhatsAppNumber ?? string.Empty,
            
            // FIX: Changed member.ActiveTiers to member.SelectedTiers
            ActiveTiers = member.SelectedTiers != null 
                ? string.Join(",", member.SelectedTiers) 
                : string.Empty,
            
            // Explicitly cast decimal to double for Azure Table Storage compatibility
            TotalContribution = (double)member.TotalContribution,
            PenaltyBalance = (double)member.PenaltyBalance,
            
            IsAdmin = false 
        };

        await _memberTable.UpsertEntityAsync(entity);
    }
}