using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JWT.Algorithms;
using Microsoft.AspNetCore.Mvc;
using JWT.Builder;
using JWT.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Attributes;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;
using Microsoft.IdentityModel.Tokens;
using SampleExchangeApi.Console.ListRequester;
using Swashbuckle.AspNetCore.Annotations;

namespace SampleExchangeApi.Console.Controllers;

/// <inheritdoc />
/// <summary>
/// </summary>
public sealed class SampleApiController : ControllerBase
{
    private readonly ISampleStorageHandler _sampleStorageHandler;
    private readonly ILogger<SampleApiController> _logger;
    private readonly ListRequesterOptions _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sampleStorageHandler"></param>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    public SampleApiController(ISampleStorageHandler sampleStorageHandler, ILogger<SampleApiController> logger,
        IOptions<ListRequesterOptions> options)
    {
        _sampleStorageHandler = sampleStorageHandler;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Download sample
    /// </summary>
    /// <param name="token">download</param>
    [HttpGet]
    [Route("/v1/download")]
    [AllowAnonymous]
    [ValidateModelState]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The token is expired.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Bad request.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "File not found.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "We encountered an error while processing the request")]
    public async Task<IActionResult> DownloadSample([FromQuery][Required] string token,
        CancellationToken cancellationToken = default)
    {
        var partner = string.Empty;

        try
        {
            var deserializedToken = new JwtBuilder()
                .WithAlgorithm(new HMACSHA512Algorithm())
                .WithSecret(_options.Secret)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(token);
            var sha256 = deserializedToken["sha256"].ToString();
            partner = deserializedToken["partner"].ToString();

            return await _sampleStorageHandler.GetAsync(sha256, partner, cancellationToken);
        }
        catch (SecurityTokenExpiredException tokenExpiredException)
        {
            _logger.LogWarning(tokenExpiredException, $"Token {token} expired.");
            return StatusCode(401, new Error
            {
                Code = 401,
                Message = "The token is expired."
            });
        }
        catch (InvalidTokenPartsException exception)
        {
            _logger.LogError(exception, $"Bad format. Token: {token}!");
            return StatusCode(400, new Error
            {
                Code = 400,
                Message = "Bad request."
            });
        }
        catch (FormatException exception)
        {
            _logger.LogError(exception, $"Bad format. Token: {token}!");
            return StatusCode(400, new Error
            {
                Code = 400,
                Message = "Bad request."
            });
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            _logger.LogError(fileNotFoundException, $"File not found. Token: {token}!");
            return StatusCode(404, new Error
            {
                Code = 404,
                Message = "File not found."
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Something went wrong. Partner \"{partner}\" got an 500.");
            return StatusCode(500, new Error
            {
                Code = 500,
                Message = "We encountered an error while processing the request."
            });
        }
    }
}
