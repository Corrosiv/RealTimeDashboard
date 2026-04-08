namespace RealTimeDashboard.API.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    // Placeholder controller to show structure
    [HttpPost("csv")]
    public IActionResult UploadCsv()
    {
        // TODO: Implement CSV upload handling
        // - Accept multipart/form-data with file and username
        // - Call ITransactionProcessor.ParseAndProcessAsync(stream, username)
        // - Publish ActivityEvent via ActivityFeedService
        // - Return CsvImportResult DTO with processedCount and errors
        return Ok(new { message = "TODO: CSV upload endpoint (see ITransactionProcessor)" });
    }
}
