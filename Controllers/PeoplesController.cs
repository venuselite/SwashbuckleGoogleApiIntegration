using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleApiSwashbuckle.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class PeoplesController
{
    [GoogleScopedAuthorize(
        PeopleServiceService.ScopeConstants.ContactsOtherReadonly)
    ]
    [HttpGet]
    [Authorize]
    [Route("search/{query}")]
    public async Task<ActionResult<List<string>>> Search([FromServices] IGoogleAuthProvider auth, string query)
    {
        GoogleCredential credential = await auth.GetCredentialAsync();
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