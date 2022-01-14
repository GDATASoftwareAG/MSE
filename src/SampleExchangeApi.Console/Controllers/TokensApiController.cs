using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleExchangeApi.Console.Attributes;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SampleExchangeApi.Console.Controllers;

/// <inheritdoc />
/// <summary>
/// </summary>
public sealed class TokensApiController : Controller
{
    private readonly ILogger<TokensApiController> _logger;
    private readonly IListRequester _listRequester;
    private readonly IPartnerProvider _partnerProvider;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="listRequester"></param>
    /// <param name="partnerProvider"></param>
    public TokensApiController(ILogger<TokensApiController> logger, IListRequester listRequester, IPartnerProvider partnerProvider)
    {
        _logger = logger;
        _listRequester = listRequester;
        _partnerProvider = partnerProvider;
    }

    private string ValidateCredentialsAndReturnUsername(string authorizationHeaders)
    {
        if (string.IsNullOrEmpty(authorizationHeaders))
        {
            _logger.LogWarning("No credentials given.");
            throw new LoginFailedException();
        }

        if (!authorizationHeaders.Contains(":"))
        {
            var data = Convert.FromBase64String(authorizationHeaders.Split(" ")[1]);
            authorizationHeaders = Encoding.UTF8.GetString(data);
        }

        var basicAuth = authorizationHeaders.Split(':');
        if (_partnerProvider.AreCredentialsOkay(basicAuth[0], basicAuth[1]))
        {
            return basicAuth[0];
        }

        _logger.LogWarning($"Failed login attempt for user: ${basicAuth[0]}");
        throw new LoginFailedException();
    }

    /// <summary>
    /// List all available samples as jwt
    /// </summary>
    /// <param name="start">Start date for sample request range</param>
    /// <param name="end">(Default: today's date )</param>
    [HttpGet]
    [Route("/v1/list")]
    [ValidateModelState]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Start Date has to be before end date")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized!")]
    [SwaggerResponse(StatusCodes.Status402PaymentRequired, "Start date cannot be older than 7 days")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "We encountered an error while processing the request")]
    public async Task<IActionResult> ListTokens([FromQuery][Required] DateTime start, [FromQuery] DateTime? end, CancellationToken token = default)
    {
        try
        {
            var authorizationHeaders = Request.Headers["Authorization"].ToString();
            var username = ValidateCredentialsAndReturnUsername(authorizationHeaders);

            if (start >= end)
            {
                return StatusCode(400, new Error
                {
                    Code = 400,
                    Message = "Start Date has to be before end date."
                });
            }

            if (start < DateTime.Now.AddDays(-7))
            {
                return StatusCode(402, new Error
                {
                    Code = 402,
                    Message = "Start date cannot be older than 7 days."
                });
            }
            _logger.LogInformation("Incoming ListRequest");

            return Ok(await _listRequester.RequestListAsync(username, start, end, token));
        }
        catch (LoginFailedException)
        {
            return StatusCode(401, new Error { Code = 401, Message = "Login Failure." });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong. The Customer got an 500.");
            return StatusCode(500, new Error
            {
                Code = 500,
                Message = "We encountered an error while processing the request."
            });
        }
    }
}
