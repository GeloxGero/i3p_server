using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace YourProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public TemplateController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("{fileName}")]
        public IActionResult DownloadTemplate(string fileName)
        {
            // 1. Locate the file within wwwroot/templates
            // Using Path.Combine ensures cross-platform compatibility (Windows/Linux)
            var filePath = Path.Combine(_env.WebRootPath, "templates", fileName);

            // 2. Security Check: Ensure the file actually exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = $"Template '{fileName}' not found on server." });
            }

            // 3. Determine the MIME type (e.g., application/vnd.ms-excel)
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                // Fallback for unknown types
                contentType = "application/octet-stream";
            }

            // 4. Read the file into a stream
            var bytes = System.IO.File.ReadAllBytes(filePath);

            // 5. Return the file with the proper headers
            // This triggers the browser's download dialog
            return File(bytes, contentType, fileName);
        }
    }
}