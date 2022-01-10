using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SampleExchangeApi.Console.Attributes;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.Controllers;

/// <inheritdoc />
public class LoginFailedException : Exception
{
};

/// <inheritdoc />
/// <summary>
/// </summary>
public sealed class TokensApiController : Controller
{
    private readonly ILogger _logger;
    private readonly IListRequester _listRequester;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="listRequester"></param>
    public TokensApiController(ILogger logger, IListRequester listRequester)
    {
        _logger = logger;
        _listRequester = listRequester;
    }

    private string ValidateCredentialsAndReturnUsername(string authorizationHeaders, string correlationToken)
    {
        if (string.IsNullOrEmpty(authorizationHeaders))
        {
            _logger.LogWarning(correlationToken, $"No credentials given.");
            throw new LoginFailedException();
        }

        if (!authorizationHeaders.Contains(":"))
        {
            var data = Convert.FromBase64String(authorizationHeaders.Split(" ")[1]);
            authorizationHeaders = Encoding.UTF8.GetString(data);
        }

        var basicAuth = authorizationHeaders.Split(':');
        if (_listRequester.AreCredentialsOkay(basicAuth[0], basicAuth[1], correlationToken))
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
    /// <response code="503">Bad Gateway!</response>
    /// <response code="401">Unauthorized!</response>
    /// <response code="200">A list of samples as jwt</response>
    /// <response code="0">unexpected error</response>
    [HttpGet]
    [Route("/v1/list")]
    [ValidateModelState]
    public async Task<IActionResult> ListTokens([FromQuery][Required()] DateTime start, [FromQuery] DateTime? end)
    {
        var correlationToken = Guid.NewGuid().ToString();
        try
        {
            if (end != null)
            {
                if (start >= end)
                {
                    return StatusCode(400, new Error
                    {
                        Code = 400,
                        Message = "Start Date has to be before end date."
                    });
                }
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

            var authorizationHeaders = Request.Headers["Authorization"].ToString();
            var username = ValidateCredentialsAndReturnUsername(authorizationHeaders, correlationToken);

            return new ObjectResult(
                JsonConvert.SerializeObject(
                    await _listRequester.RequestListAsync(username, start, end, correlationToken)));
        }
        catch (LoginFailedException)
        {
            return StatusCode(401, new Error { Code = 401, Message = "Login Failure." });
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Something went wrong. The Customer got an 502.");
            return StatusCode(502, new Error
            {
                Code = 502,
                Message = "We encountered an error while processing the request."
            });
        }
    }
}
