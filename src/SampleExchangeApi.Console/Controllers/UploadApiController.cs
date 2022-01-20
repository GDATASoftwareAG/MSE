using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;

namespace SampleExchangeApi.Console.Controllers;

/// <inheritdoc />
/// <summary>
/// </summary>
[Authorize]
public class UploadApiController : ControllerBase
{
    private readonly ILogger<UploadApiController> _logger;
    private readonly ISampleStorageHandler _sampleStorageHandler;
    private readonly ISampleMetadataHandler _sampleMetadataHandler;

    public UploadApiController(ILogger<UploadApiController> logger,
        ISampleStorageHandler sampleStorageHandler, ISampleMetadataHandler sampleMetadataHandler)
    {
        _logger = logger;
        _sampleStorageHandler = sampleStorageHandler;
        _sampleMetadataHandler = sampleMetadataHandler;
    }

    [HttpPut]
    [Route("/v1/upload")]
    public async Task<IActionResult> UploadSample([Required] IFormFile inputFile,
        [Required] ExportSample sample, CancellationToken token = default)
    {
        try
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var role = claimsIdentity?.FindFirst(ClaimTypes.Role)?.Value ?? String.Empty;
            if (role != "Upload")
            {
                return Unauthorized();
            }
            await _sampleMetadataHandler.InsertSampleAsync(sample, token);
            await _sampleStorageHandler.WriteAsync(sample.Sha256, inputFile.OpenReadStream(), token);

            return Ok("Uploaded");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong.");
            return StatusCode(500, new Error
            {
                Code = 500,
                Message = "We encountered an error while processing the request."
            });
        }
    }
}
