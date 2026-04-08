namespace RealTimeDashboard.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    // Placeholder controller to show structure
    [HttpPost("csv")]
    public async Task<IActionResult> UploadCsv([FromForm] IFormFile? file, [FromForm] string? username)
    {
        // TODO: wire to ITransactionProcessor in FinanceTracker.Core and ActivityFeedService
        if (file == null)
            return BadRequest(new { error = "file is required" });

        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { error = "username is required" });

        // Read the stream minimally to validate upload works in integration tests
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            // Do not process here in placeholder; return a safe response
            return Ok(new { processedCount = 0, errors = new string[0], activityMessage = $"{username} uploaded a CSV (0 transactions)" });
        }
        catch
        {
            return StatusCode(500, new { error = "failed to read uploaded file" });
        }
    }
}
