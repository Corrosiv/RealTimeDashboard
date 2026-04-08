using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using RealTimeDashboard.Tests.Integration.Helpers;

namespace RealTimeDashboard.Tests.Integration;

public class CsvUploadTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CsvUploadTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadCsv_HappyPath_ReturnsProcessedCount()
    {
        using var client = _factory.CreateClient();

        var csv = new StringBuilder();
        csv.AppendLine("timestamp,amount,description");
        csv.AppendLine($"{DateTime.UtcNow:o},-10.00,Sample1");
        csv.AppendLine($"{DateTime.UtcNow:o},100.00,Sample2");

        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "transactions.csv");
        content.Add(new StringContent("TestUser"), "username");

        var response = await client.PostAsync("/api/upload/csv", content);
        var body = await response.Content.ReadAsStringAsync();

        // If server returned an error, include response body in assertion message for debugging
        Assert.True(response.IsSuccessStatusCode, $"Upload failed: {response.StatusCode}. Body: {body}");
        Assert.Contains("processedCount", body, StringComparison.OrdinalIgnoreCase);
    }
}
