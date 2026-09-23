using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using Internship.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class SendGridTestController : ControllerBase
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridTestController> _logger;
    private readonly IEmailService _emailService;

    public SendGridTestController(
        ISendGridClient sendGridClient,
        IConfiguration config,
        ILogger<SendGridTestController> logger,
        IEmailService emailService)
    {
        _sendGridClient = sendGridClient;
        _config = config;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpGet("config-check")]
    public IActionResult CheckConfiguration()
    {
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"];

            return Ok(new
            {
                ApiKeyPresent = !string.IsNullOrEmpty(apiKey),
                ApiKeyFormat = apiKey?.StartsWith("SG.") == true ? "Valid" : "Invalid",
                ApiKeyLength = apiKey?.Length ?? 0,
                ApiKeyPrefix = apiKey?[..Math.Min(15, apiKey?.Length ?? 0)] + "...",
                FromEmail = fromEmail,
                FromName = fromName,
                FromEmailPresent = !string.IsNullOrEmpty(fromEmail),
                FromNamePresent = !string.IsNullOrEmpty(fromName),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SendGrid configuration");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("test-direct")]
    public async Task<IActionResult> TestSendGridDirect([FromQuery] string toEmail = "test@example.com")
    {
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"];

            // Validate configuration first
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new { Error = "SendGrid:ApiKey is not configured" });

            if (!apiKey.StartsWith("SG."))
                return BadRequest(new { Error = "Invalid SendGrid API key format" });

            if (string.IsNullOrEmpty(fromEmail))
                return BadRequest(new { Error = "SendGrid:FromEmail is not configured" });

            _logger.LogInformation("Testing SendGrid directly with API key length: {Length}", apiKey.Length);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(fromEmail, fromName),
                Subject = $"SendGrid Direct Test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                PlainTextContent = "This is a direct test email from SendGrid API",
                HtmlContent = "<strong>This is a direct test email from SendGrid API</strong>"
            };

            msg.AddTo(new EmailAddress(toEmail));

            // Minimal tracking settings
            msg.SetClickTracking(false, false);
            msg.SetOpenTracking(false);

            var response = await _sendGridClient.SendEmailAsync(msg);
            var responseBody = await response.Body.ReadAsStringAsync();

            _logger.LogInformation("SendGrid response status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("SendGrid response body: {ResponseBody}", responseBody);

            return Ok(new
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Status = response.StatusCode.ToString(),
                Response = responseBody,
                From = fromEmail,
                To = toEmail,
                ApiKeyPrefix = apiKey[..Math.Min(15, apiKey.Length)] + "...",
                Headers = response.Headers?.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid direct test failed");
            return StatusCode(500, new
            {
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("test-service")]
    public async Task<IActionResult> TestEmailService([FromQuery] string toEmail = "test@example.com")
    {
        try
        {
            _logger.LogInformation("Testing EmailService.SendEmailAsync");

            var result = await _emailService.SendEmailAsync(
                toEmail,
                "Email Service Test",
                "This is a test email from the EmailService");

            return Ok(new
            {
                Success = result,
                Message = result ? "Email sent successfully via EmailService" : "Failed to send email via EmailService",
                To = toEmail,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailService test failed");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("test-all")]
    public async Task<IActionResult> TestAll([FromQuery] string toEmail = "test@example.com")
    {
        var results = new
        {
            Configuration = await Task.FromResult(CheckConfiguration()),
            DirectTest = await TestSendGridDirect(toEmail),
            ServiceTest = await TestEmailService(toEmail)
        };

        return Ok(results);
    }
}