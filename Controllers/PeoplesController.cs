using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace GoogleApiSwashbuckle.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class PeoplesController
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public PeoplesController(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }


    [HttpGet]
    [Route("search/{query}")]
    public async Task<ActionResult<List<string>>> Search(string query)
    {
        string? accessToken = httpContextAccessor
            .HttpContext?
            .Request
            .Headers["Authorization"]
            .FirstOrDefault();

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Unauthenticated");
        }
        
        GoogleCredential credential = GoogleCredential.FromAccessToken(accessToken.Replace("Bearer ", ""));
        PeopleServiceService peopleService = new(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Test"
        });

        OtherContactsResource.SearchRequest peopleRequest =
            peopleService.OtherContacts.Search();
        peopleRequest.Query = query;
        peopleRequest.ReadMask = "names,emailAddresses";

        SearchResponse searchResponse = await peopleRequest.ExecuteAsync();
        List<string> emailAddresses = searchResponse
            .Results != null
            ? searchResponse.Results.Select(r => r
                    .Person
                    .EmailAddresses.Any()
                    ? r
                        .Person
                        .EmailAddresses
                        .First()
                        .Value
                    : "null")
                .ToList()
            : new List<string>();

        return emailAddresses;
    }
}