using CVision.Services.MailService;
using Microsoft.AspNetCore.Mvc;
using Aspose.Pdf.Text;
using Aspose.Pdf;

namespace CVision.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvReviewController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly HashSet<string> _suspiciousWords = new HashSet<string>
        {
            "hack", "cheat", "fake", "exploit", "bypass", "crack", "illegal", "malware", "phish", "scam", "fraud"
        };
        public CvReviewController(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost]
        [Route("extract")]
        public async Task<IActionResult> ExtractText([FromBody] PdfRequest request)
        {
            byte[] pdfBytes = System.Convert.FromBase64String(request.Base64Pdf);
            string tempFilePath = Path.GetTempFileName();
            await System.IO.File.WriteAllBytesAsync(tempFilePath, pdfBytes);
            if (!System.IO.File.Exists(tempFilePath))
            {
                return NotFound("Belirtilen dosya bulunamadı!");
            }
            string pdfText = ExtractTextFromPdf(tempFilePath);
            if (ContainsSuspiciousWords(pdfText))
            {
                return BadRequest("Şüpheli veya zararlı ifadeler tespit edildi!");
            }
            string response = await SendToOpenAI(pdfText);
            System.IO.File.Delete(tempFilePath);
            await _emailService.SendNotificationEmail();
            return Ok(response);
        }

        private bool ContainsSuspiciousWords(string text)
        {
            foreach (var word in _suspiciousWords)
            {
                if (text.ToLower().Contains(word))
                {
                    return true;
                }
            }
            return false;
        }
        private string ExtractTextFromPdf(string filePath)
        {
            Document pdfDocument = new Document(filePath);
            TextAbsorber textAbsorber = new TextAbsorber();
            pdfDocument.Pages.Accept(textAbsorber);
            return textAbsorber.Text;
        }

        private async Task<string> SendToOpenAI(string text)
        {
            string apiKey = _configuration["OpenAIApiKey"] ?? "YourSecretKey";
            string apiUrl = _configuration["OpenAIUrl"] ?? "ApiUrl";

            var requestData = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an AI that provides feedback on resumes." },
                    new { role = "user", content = "Sen bir yapay zekasın ve özgeçmişlere geri bildirim sağlıyorsun. Lütfen verilen Sana gönderilen dil bilgisinde özgeçmişi gözden geçir ve geri bildirim ver:\n\n" + text }
                }
            };

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                HttpContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    return responseText;
                }
                else
                {
                    return $"API Hatası: {response.StatusCode}";
                }
            }
        }
    }

    public class PdfRequest
    {
        public string Base64Pdf { get; set; }
    }
}
