using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/templates")]
public class TemplateController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;

    public TemplateController(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    private async Task<IActionResult> ProxyFile(string url, string fileName)
    {
        var client = _httpFactory.CreateClient();
        // Start reading headers immediately
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            return NotFound("File not found in storage.");

        var stream = await response.Content.ReadAsStreamAsync();
        
        // application/octet-stream is a safe catch-all for downloads, 
        // or use the specific Excel MIME type we discussed earlier.
        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = fileName
        };
    }

    [HttpGet("download-school-implementation-plan")]
    public async Task<IActionResult> DownloadSIP() =>
        await ProxyFile("https://res.cloudinary.com/dlzobzben/raw/upload/SchoolImplementationPlan_Template_vn7ijg.xlsx", "SchoolImplementationPlan_Template.xlsx");

    [HttpGet("download-procurement-plan")]
    public async Task<IActionResult> DownloadAPP() =>
        await ProxyFile("https://res.cloudinary.com/dlzobzben/raw/upload/AnnualProcurementPlan_Template_tep3fq.xlsx", "AnnualProcurementPlan_Template.xlsx");
}