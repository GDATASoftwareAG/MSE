using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
[Authorize]
public sealed class TokensApiController : Controller
{
    private readonly ILogger<TokensApiController> _logger;
    private readonly IListRequester _listRequester;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="listRequester"></param>
    /// <param name="partnerProvider"></param>
    public TokensApiController(ILogger<TokensApiController> logger, IListRequester listRequester)
    {
        _logger = logger;
        _listRequester = listRequester;
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
            _logger.LogInformation("Incoming ListRequest");
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value ?? String.Empty;
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

            return Ok(await _listRequester.RequestListAsync(username, start, end, token));
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
