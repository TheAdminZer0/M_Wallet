using Microsoft.AspNetCore.Mvc;

namespace M_Wallet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public UploadsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? productName)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Ensure the uploads directory exists
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string fileName;
        if (!string.IsNullOrWhiteSpace(productName))
        {
            // Sanitize the product name to create a safe filename
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(productName.Where(ch => !invalidChars.Contains(ch)).ToArray()).Trim();
            
            // If sanitization removed everything, fallback to GUID
            if (string.IsNullOrEmpty(safeName))
                safeName = Guid.NewGuid().ToString();
                
            fileName = $"{safeName}{Path.GetExtension(file.FileName)}";
        }
        else
        {
            // Generate a unique filename to prevent overwriting
            fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        }

        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return the relative URL
        var url = $"/uploads/{fileName}";
        return Ok(new { url });
    }
}
