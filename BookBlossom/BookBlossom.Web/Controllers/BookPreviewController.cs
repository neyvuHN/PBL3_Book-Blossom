using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace BookBlossom.Web.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BookPreviewController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        // In a production app, this key should be loaded from AppSettings or Key Vault
        private static readonly string SecretKey = "BookBlossomSuperSecureSecretKey_2026"; 

        public BookPreviewController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Endpoint 1: Generate a temporary, cryptographically signed URL for the book preview.
        /// The generated token has a strict TTL (time-to-live) of 60 seconds.
        /// </summary>
        [HttpGet("{id}/preview-session")]
        public IActionResult GetPreviewSession(string id)
        {
            if (string.IsNullOrEmpty(id) || !System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9]+$"))
            {
                return BadRequest("Access Denied: Invalid book ID format.");
            }

            // Expire token in exactly 60 seconds
            var expiry = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();
            var payload = $"{id}:{expiry}";
            
            // Generate HMAC-SHA256 signature to protect the token from tampering
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = Convert.ToBase64String(hash)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", ""); // URL-safe base64 encoding

            var token = $"{payload}:{signature}";
            
            // Build the signed endpoint path
            var signedUrl = $"/api/books/{id}/preview?token={Uri.EscapeDataString(token)}";
            
            return Ok(new { signedUrl, expiry });
        }

        /// <summary>
        /// Endpoint 2: Stream the PDF file securely.
        /// Validates the HMAC signature and checks if the token has expired (60s TTL).
        /// </summary>
        [HttpGet("{id}/preview")]
        public IActionResult GetPreview(string id, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(id) || !System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9]+$"))
            {
                return Forbid("Access Denied: Invalid book ID format.");
            }

            if (string.IsNullOrEmpty(token))
            {
                return Forbid("Access Denied: Missing preview token.");
            }

            try
            {
                // Parse the payload and signature
                var parts = token.Split(':');
                if (parts.Length != 3)
                {
                    return Forbid("Access Denied: Invalid signature format.");
                }

                var bookIdStr = parts[0];
                var expiryStr = parts[1];
                var signature = parts[2];

                // 1. Verify Book ID matches
                if (bookIdStr != id)
                {
                    return Forbid("Access Denied: Book ID mismatch.");
                }

                // 2. Verify Expiration Time
                if (!long.TryParse(expiryStr, out var expiry) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                {
                    return Forbid("Access Denied: The preview session has expired.");
                }

                // 3. Cryptographically Verify Signature
                var payload = $"{bookIdStr}:{expiryStr}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
                var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var expectedSignature = Convert.ToBase64String(expectedHash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                if (signature != expectedSignature)
                {
                    return Forbid("Access Denied: Token signature verification failed.");
                }

                // 4. Securely locate and stream the preview PDF file dynamically
                // We serve Book1.pdf, Book2.pdf, Book3.pdf depending on the active book cover,
                // and fallback to Book1.pdf if the specific PDF is not yet created.
                var fileName = $"{id}.pdf";
                var pdfPath = Path.Combine(_env.WebRootPath, "PDF", fileName);
                
                if (!System.IO.File.Exists(pdfPath))
                {
                    // Fallback to Book1.pdf if requested book preview doesn't exist yet
                    pdfPath = Path.Combine(_env.WebRootPath, "PDF", "Book1.pdf");
                }

                if (!System.IO.File.Exists(pdfPath))
                {
                    return NotFound("Preview source file not found.");
                }

                var fileBytes = System.IO.File.ReadAllBytes(pdfPath);
                
                // Set security headers to prevent caching, downloading, and clickjacking
                Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
                Response.Headers.Append("Pragma", "no-cache");
                Response.Headers.Append("Expires", "0");
                Response.Headers.Append("X-Content-Type-Options", "nosniff");
                Response.Headers.Append("Content-Disposition", "inline; filename=\"preview.pdf\"");

                return File(fileBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal secure server error: {ex.Message}");
            }
        }
    }
}
