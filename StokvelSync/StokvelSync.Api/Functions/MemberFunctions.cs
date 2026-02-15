using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using StokvelSync.Api.Data;
using StokvelSync.Shared;

namespace StokvelSync.Api.Functions;

public class MemberFunctions
{
    private readonly MemberRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MemberFunctions(MemberRepository repository)
    {
        _repository = repository;
    }

    [Function("GetMembers")]
    public async Task<HttpResponseData> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var members = await _repository.GetAllMembersAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(members);
        return response;
    }

    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        // We expect a simple JSON object like { "Email": "user@example.com" }
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Member>(body, _jsonOptions);

        if (data == null || string.IsNullOrEmpty(data.Email))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        var member = await _repository.GetMemberByEmailAsync(data.Email);

        if (member == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(member);
        return response;
    }

    [Function("AddMember")]
    public async Task<HttpResponseData> AddMember(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var member = await JsonSerializer.DeserializeAsync<Member>(req.Body, _jsonOptions);

        if (member == null || string.IsNullOrEmpty(member.Email))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        // Standard check: Don't allow duplicate registrations
        var existing = await _repository.GetMemberByEmailAsync(member.Email);
        if (existing != null)
        {
            var conflict = req.CreateResponse(HttpStatusCode.Conflict);
            await conflict.WriteStringAsync("Account already exists.");
            return conflict;
        }

        // Initialize new account based on your business rules
        member.TotalContribution = 0;
        member.PenaltyBalance = 0;
        member.HasPaidCurrentMonth = false;

        await _repository.UpsertMemberAsync(member);

        return req.CreateResponse(HttpStatusCode.OK);
    }
}