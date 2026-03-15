using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/templates")]
public class TemplateController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public TemplateController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }
    
    //helper function for downloading xlsx files
    private async Task<IActionResult> ProxyFileFromCloudinary(string url, string contentType, string fileName)
    {
        var client = _httpFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return NotFound("Template file could not be retrieved from cloud storage.");

        var stream = await response.Content.ReadAsStreamAsync();
        
        // This returns the file stream directly to the user's browser
        return File(stream, contentType, fileName);
    }

    // 1. Download Image from Cloudinary
    [HttpGet("download-image")]
    public async Task<IActionResult> DownloadImage()
    {
        var url = "https://res.cloudinary.com/demo/image/upload/sample.jpg"; // Replace with your config
        return await ProxyFileFromCloudinary(url, "image/jpeg", "TemplateImage.jpg");
    }

    //Download download-procurement-plan from Cloudinary
    [HttpGet("download-procurement-plan")]
    public async Task<IActionResult> DownloadAnnualPlan()
    {
        var url = "https://res.cloudinary.com/dlzobzben/raw/upload/v1773595856/AnnualProcurementPlan_Template_tep3fq.xlsx"; // Replace with your config
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        return await ProxyFileFromCloudinary(url, contentType, "AnnualPlanTemplate.xlsx");
    }
    
    //Download download-school-implementation-plan from Cloudinary
    [HttpGet("download-school-implementation-plan")]
    public async Task<IActionResult> DownloadSchoolImplementationPlan()
    {
        var url = "https://res.cloudinary.com/dlzobzben/raw/upload/v1773595856/SchoolImplementationPlan_Template_vn7ijg.xlsx"; // Replace with your config
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        return await ProxyFileFromCloudinary(url, contentType, "SchoolImplementationPlanTemplate.xlsx");
    }


}