using System.Reflection;
using System.Text.Json.Serialization;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;
// Configure logging
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});
services
    .AddControllers()
    .AddJsonOptions(opts => { opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
const string clientSecretFilename = "client_secret.json";
ClientSecrets clientSecrets = GoogleClientSecrets.FromFile(clientSecretFilename).Secrets;

services
    .AddAuthentication(o =>
    {
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = false
        };
    });

services.AddHttpContextAccessor();
services.AddEndpointsApiExplorer();
services.AddRouting(options => options.LowercaseUrls = true);
services.AddSwaggerGen(options =>
{
    options.CustomOperationIds(apiDescription =>
        apiDescription.TryGetMethodInfo(out MethodInfo info) ? info.Name : string.Empty);
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Extensions = new Dictionary<string, IOpenApiExtension>
        {
            // {"x-tokenName", new OpenApiString("id_token")},
            {"x-tokenName", new OpenApiString("access_token")}
        },
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth"),
                TokenUrl = new Uri("https://accounts.google.com/o/oauth2/token"),
                RefreshUrl = new Uri("https://accounts.google.com/o/oauth2/token"),
                Scopes = new Dictionary<string, string>
                {
                    {"openid", "openid"},
                    {"email", "email"},
                    {
                        "profile", "profile"
                    },
                    {
                        PeopleServiceService.ScopeConstants.UserinfoProfile,
                        PeopleServiceService.ScopeConstants.UserinfoProfile
                    },
                    {
                        PeopleServiceService.ScopeConstants.ContactsReadonly,
                        PeopleServiceService.ScopeConstants.ContactsReadonly
                    },
                    {
                        PeopleServiceService.ScopeConstants.ContactsOtherReadonly,
                        PeopleServiceService.ScopeConstants.ContactsOtherReadonly
                    }
                }
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                },
                Scheme = "oauth2",
                Name = "oauth2",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuthClientId(clientSecrets.ClientId);
    c.OAuthClientSecret(clientSecrets.ClientSecret);
    c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
    // perhaps configure a custom redirect rule
});

app.UseCors(policyBuilder =>
{
    policyBuilder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .SetIsOriginAllowed(_ => true); // this is bad but simple for this example
});

app.UseRouting();
app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();
app.Logger.LogInformation(
    "Starting {ApplicationName} in environment {EnvironmentName}", app.Environment.ApplicationName,
    app.Environment.EnvironmentName);
app.Run();