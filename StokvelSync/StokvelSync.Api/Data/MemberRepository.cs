using Azure;
using Azure.Data.Tables;
using StokvelSync.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StokvelSync.Api.Data;

public class MemberRepository
{
    private readonly TableClient _tableClient;
    private const string TableName = "Members";
    private const string PartitionKey = "StokvelGroup";

    public MemberRepository(TableServiceClient serviceClient)
    {
        // Initializes the table client and ensures the table exists in Azure/Azurite
        _tableClient = serviceClient.GetTableClient(TableName);
        _tableClient.CreateIfNotExists();
    }

    /// <summary>
    /// Saves or updates a member's record in Azure Table Storage.
    /// Maps the SelectedTiers list to a string for storage.
    /// </summary>
  public async Task UpsertMemberAsync(Member member)
{
    var entity = new TableEntity("StokvelGroup", member.Email)
    {
        { "FullName", member.FullName },
        { "WhatsAppNumber", member.WhatsAppNumber },
        { "SelectedTiers", string.Join(",", member.SelectedTiers) },
        { "TotalContribution", (double)member.TotalContribution },
        { "PenaltyBalance", (double)member.PenaltyBalance },
        { "HasPaidCurrentMonth", member.HasPaidCurrentMonth }
    };

    await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
}

// Ensure the GetAllMembersAsync also maps these back:
// ... inside the loop ...
// FullName = entity.GetString("FullName") ?? "",
// WhatsAppNumber = entity.GetString("WhatsAppNumber") ?? "",

    /// <summary>
    /// Retrieves all members from the table and converts them back to the Shared Member model.
    /// </summary>
    public async Task<List<Member>> GetAllMembersAsync()
    {
        var members = new List<Member>();
        
        // Querying all entities within our specific partition
        AsyncPageable<TableEntity> queryResults = _tableClient.QueryAsync<TableEntity>(ent => ent.PartitionKey == PartitionKey);

        await foreach (var entity in queryResults)
        {
            members.Add(new Member
            {
                Email = entity.RowKey, // The Email is our RowKey (Unique Identifier)
                TotalContribution = (decimal)(entity.GetDouble("TotalContribution") ?? 0),
                PenaltyBalance = (decimal)(entity.GetDouble("PenaltyBalance") ?? 0),
                HasPaidCurrentMonth = entity.GetBoolean("HasPaidCurrentMonth") ?? false,
                SelectedTiers = entity.GetString("SelectedTiers")?
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList() ?? new List<int>()
            });
        }

        return members;
    }

    /// <summary>
    /// Retrieves a single member by their email address for login/lookup purposes.
    /// </summary>
    public async Task<Member?> GetMemberByEmailAsync(string email)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableEntity>(PartitionKey, email);
            var entity = response.Value;

            return new Member
            {
                Email = entity.RowKey,
                TotalContribution = (decimal)(entity.GetDouble("TotalContribution") ?? 0),
                PenaltyBalance = (decimal)(entity.GetDouble("PenaltyBalance") ?? 0),
                HasPaidCurrentMonth = entity.GetBoolean("HasPaidCurrentMonth") ?? false,
                SelectedTiers = entity.GetString("SelectedTiers")?
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList() ?? new List<int>()
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null; // Return null if member doesn't exist
        }
    }
}