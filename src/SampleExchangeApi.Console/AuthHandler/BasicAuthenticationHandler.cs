using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Database;

namespace SampleExchangeApi.Console.AuthHandler;

public record User(string Username, bool AllowUpload);

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IPartnerProvider _partnerProvider;
    private readonly UploadOptions _uploadOptions;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IPartnerProvider partnerProvider,
        IOptions<UploadOptions> uploadOptions)
        : base(options, logger, encoder, clock)
    {
        _partnerProvider = partnerProvider;
        _uploadOptions = uploadOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // skip authentication if endpoint has [AllowAnonymous] attribute
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        User? user = null;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? String.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(":", 2);
            var username = credentials[0];
            var password = credentials[1];
            if (_partnerProvider.AreCredentialsOkay(username, password))
            {
                user = new User(username, username == _uploadOptions.AllowPartnerToUpload);
            }
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (user == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.AllowUpload ? "Upload" : "NoUpload")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
