using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Internship.Application.Interfaces;
using Internship.Application.DTOs.Dispute;
using Internship.Application.DTOs.RefundRequest;
using Internship.Domain.Entities;
using System.Net;

namespace Internship.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(
            ISendGridClient sendGridClient,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _sendGridClient = sendGridClient ?? throw new ArgumentNullException(nameof(sendGridClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Enhanced configuration validation
            _fromEmail = configuration["SendGrid:FromEmail"];
            _fromName = configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(_fromEmail))
            {
                throw new ArgumentNullException("SendGrid:FromEmail is not configured");
            }

            if (string.IsNullOrEmpty(_fromName))
            {
                throw new ArgumentNullException("SendGrid:FromName is not configured");
            }

            _logger.LogInformation("Email service initialized with sender: {FromName} <{FromEmail}>",
                _fromName, _fromEmail);
        }

        // FIX: Use configured email settings instead of hardcoded values
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string content)
        {
            try
            {
                // FIXED: Use _fromEmail and _fromName instead of hardcoded values
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, content, content);

                // Remove problematic headers that might cause authentication issues
                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);

                _logger.LogInformation("Attempting to send email to {Email} from {FromEmail}", toEmail, _fromEmail);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                    return true;
                }
                else
                {
                    await LogSendGridError(response, "general email");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendDisputeStatusEmailAsync(string userEmail, string userName, DisputeResponse dispute)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(userEmail, userName);
                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    GetDisputeSubject(dispute.Status),
                    GetDisputePlainTextContent(dispute, userName),
                    GetDisputeHtmlContent(dispute, userName));

                // Add tracking settings
                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Dispute status email sent to {Email}", userEmail);
                    return true;
                }

                await LogSendGridError(response, "dispute status");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute status email to {Email}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendRefundStatusEmailAsync(string userEmail, string userName, RefundRequestResponse refund)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(userEmail, userName);
                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    GetRefundSubject(refund.Status),
                    GetRefundPlainTextContent(refund, userName),
                    GetRefundHtmlContent(refund, userName));

                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Refund status email sent to {Email}", userEmail);
                    return true;
                }

                await LogSendGridError(response, "refund status");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund status email to {Email}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendDisputeAssignmentEmailAsync(string assignedUserEmail, string assignedUserName, DisputeResponse dispute)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(assignedUserEmail, assignedUserName);
                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    $"New Dispute Assigned - {dispute.DisputeId}",
                    GetDisputeAssignmentPlainTextContent(dispute, assignedUserName),
                    GetDisputeAssignmentHtmlContent(dispute, assignedUserName));

                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Dispute assignment email sent to {Email}", assignedUserEmail);
                    return true;
                }

                await LogSendGridError(response, "dispute assignment");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute assignment email to {Email}", assignedUserEmail);
                return false;
            }
        }

        private async Task LogSendGridError(Response response, string emailType)
        {
            var responseBody = await response.Body.ReadAsStringAsync();
            _logger.LogError(
                "Failed to send {EmailType} email. Status: {StatusCode}, Body: {ResponseBody}",
                emailType,
                response.StatusCode,
                responseBody);
        }

        private string GetDisputeSubject(DisputeStatus status)
        {
            return status switch
            {
                DisputeStatus.Resolved => "✅ Your Dispute Has Been Resolved",
                DisputeStatus.Rejected => "❌ Your Dispute Has Been Rejected",
                DisputeStatus.UnderReview => "⏳ Your Dispute is Under Review",
                DisputeStatus.AwaitingCustomerResponse => "📝 Action Required: Response Needed for Your Dispute",
                DisputeStatus.Escalated => "⚠️ Your Dispute Has Been Escalated",
                _ => "📋 Dispute Status Update"
            };
        }

        private string GetRefundSubject(RefundStatus status)
        {
            return status switch
            {
                RefundStatus.Approved => "✅ Your Refund Request Has Been Approved",
                RefundStatus.Rejected => "❌ Your Refund Request Has Been Rejected",
                RefundStatus.Completed => "💰 Your Refund Has Been Processed",
                RefundStatus.UnderReview => "⏳ Your Refund Request is Under Review",
                RefundStatus.Failed => "⚠️ Issue Processing Your Refund",
                _ => "📋 Refund Request Status Update"
            };
        }

        private string GetDisputeHtmlContent(DisputeResponse dispute, string userName)
        {
            var isResolved = dispute.Status == DisputeStatus.Resolved;
            var isRejected = dispute.Status == DisputeStatus.Rejected;
            var transactionDate = dispute.CreatedAt.ToString("MMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Dispute Decision Notification</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 5px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            text-align: center;
            padding-bottom: 20px;
            border-bottom: 1px solid #eeeeee;
        }}
        .content {{
            padding: 20px 0;
        }}
        .transaction-details {{
            background-color: #f9f9f9;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .decision {{
            font-weight: bold;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .accepted {{
            background-color: #e6f7e6;
            color: #2d882d;
            border-left: 4px solid #2d882d;
        }}
        .rejected {{
            background-color: #fde8e8;
            color: #cc0000;
            border-left: 4px solid #cc0000;
        }}
        .footer {{
            text-align: center;
            padding-top: 20px;
            border-top: 1px solid #eeeeee;
            font-size: 12px;
            color: #777777;
        }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #0066cc;
            color: #ffffff;
            text-decoration: none;
            border-radius: 5px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""color: #0066cc; margin: 0;"">Dispute Decision</h1>
        </div>
        <div class=""content"">
            <p>Dear {userName},</p>
            <p>We have completed our review of your dispute request:</p>
            <div class=""transaction-details"">
                <p><strong>Dispute ID:</strong> {dispute.DisputeId}</p>
                <p><strong>Transaction ID:</strong> {dispute.TransactionId}</p>
                <p><strong>Date:</strong> {transactionDate}</p>
                <p><strong>Type:</strong> {dispute.Type}</p>
                <p><strong>Description:</strong> {dispute.Description}</p>
            </div>
            {(isResolved ? $@"
            <div class=""decision accepted"">
                <p>✅ <strong>Great news!</strong> Your dispute has been resolved in your favor.</p>
                {(!string.IsNullOrEmpty(dispute.ResolutionComments) ? $"<p><strong>Resolution Details:</strong> {dispute.ResolutionComments}</p>" : "")}
            </div>" : "")}
            {(isRejected ? $@"
            <div class=""decision rejected"">
                <p>❌ After careful review, we regret to inform you that your dispute request has been declined.</p>
                {(!string.IsNullOrEmpty(dispute.ResolutionComments) ? $"<p><strong>Reason:</strong> {dispute.ResolutionComments}</p>" : "")}
            </div>" : "")}
            <p>If you have any questions, please contact our support team.</p>
            <div style=""text-align: center;"">
                <a href=""mailto:support@yourcompany.com"" class=""button"">Contact Support</a>
            </div>
            <p>Sincerely,<br><strong>Customer Support Team</strong></p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} Your Company. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetDisputePlainTextContent(DisputeResponse dispute, string userName)
        {
            return $@"
Dear {userName},

Dispute Status Update:

Dispute ID: {dispute.DisputeId}
Transaction ID: {dispute.TransactionId}
Status: {dispute.Status}
Type: {dispute.Type}
Date: {dispute.CreatedAt:MMM dd, yyyy}
Description: {dispute.Description}

{(string.IsNullOrEmpty(dispute.ResolutionComments) ? "" : $"Resolution Comments: {dispute.ResolutionComments}\n")}

If you have any questions, please contact our support team.

Best regards,
Customer Support Team";
        }

        private string GetRefundHtmlContent(RefundRequestResponse refund, string userName)
        {
            var isApproved = refund.Status == RefundStatus.Approved || refund.Status == RefundStatus.Completed;
            var isRejected = refund.Status == RefundStatus.Rejected;
            var transactionDate = refund.CreatedAt.ToString("MMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Refund Decision Notification</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; padding-bottom: 20px; }}
        .details {{ background: #f9f9f9; padding: 15px; border-radius: 5px; }}
        .decision {{ padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .approved {{ background: #e6f7e6; border-left: 4px solid #2d882d; }}
        .rejected {{ background: #fde8e8; border-left: 4px solid #cc0000; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #0066cc; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""color: #0066cc;"">Refund Decision</h1>
        </div>
        <div>
            <p>Dear {userName},</p>
            <p>We have reviewed your refund request:</p>
            <div class=""details"">
                <p><strong>Refund ID:</strong> {refund.RefundId}</p>
                <p><strong>Amount:</strong> ${refund.RequestedAmount:F2}</p>
                <p><strong>Date:</strong> {transactionDate}</p>
                <p><strong>Reason:</strong> {refund.Reason}</p>
            </div>
            {(isApproved ? $@"
            <div class=""decision approved"">
                <p>✅ <strong>Approved!</strong> Your refund has been processed.</p>
                {(refund.Status == RefundStatus.Completed ?
                    "<p>The funds should appear in your account within 3-5 business days.</p>" :
                    "<p>Your refund is being processed.</p>")}
            </div>" : "")}
            {(isRejected ? $@"
            <div class=""decision rejected"">
                <p>❌ <strong>Declined.</strong> Your refund request was not approved.</p>
                {(!string.IsNullOrEmpty(refund.AdminNotes) ? $"<p><strong>Reason:</strong> {refund.AdminNotes}</p>" : "")}
            </div>" : "")}
            <p>For questions, please contact our support team.</p>
            <div style=""text-align: center;"">
                <a href=""mailto:support@yourcompany.com"" class=""button"">Contact Support</a>
            </div>
            <p>Sincerely,<br><strong>Customer Support Team</strong></p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetRefundPlainTextContent(RefundRequestResponse refund, string userName)
        {
            return $@"
Dear {userName},

Refund Status Update:

Refund ID: {refund.RefundId}
Amount: ${refund.RequestedAmount:F2}
Status: {refund.Status}
Date: {refund.CreatedAt:MMM dd, yyyy}
Reason: {refund.Reason}

{(string.IsNullOrEmpty(refund.AdminNotes) ? "" : $"Notes: {refund.AdminNotes}\n")}

For questions, please contact our support team.

Best regards,
Customer Support Team";
        }

        private string GetDisputeAssignmentHtmlContent(DisputeResponse dispute, string assignedUserName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Dispute Assignment</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ color: #17a2b8; border-bottom: 1px solid #eee; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>New Dispute Assignment</h2>
        </div>
        <p>Dear {assignedUserName},</p>
        <p>A new dispute has been assigned to you:</p>
        <div class=""details"">
            <table>
                <tr><td><strong>Dispute ID:</strong></td><td>{dispute.DisputeId}</td></tr>
                <tr><td><strong>Customer:</strong></td><td>{dispute.UserFullName}</td></tr>
                <tr><td><strong>Type:</strong></td><td>{dispute.Type}</td></tr>
                <tr><td><strong>Status:</strong></td><td>{dispute.Status}</td></tr>
                <tr><td><strong>Created:</strong></td><td>{dispute.CreatedAt:MMM dd, yyyy 'at' HH:mm}</td></tr>
            </table>
        </div>
        <p><strong>Description:</strong></p>
        <p>{dispute.Description}</p>
        <p>Please review this dispute in the admin portal.</p>
        <p>Best regards,<br>System Administration</p>
    </div>
</body>
</html>";
        }

        private string GetDisputeAssignmentPlainTextContent(DisputeResponse dispute, string assignedUserName)
        {
            return $@"
Dear {assignedUserName},

New Dispute Assignment:

Dispute ID: {dispute.DisputeId}
Customer: {dispute.UserFullName}
Type: {dispute.Type}
Status: {dispute.Status}
Created: {dispute.CreatedAt:MMM dd, yyyy 'at' HH:mm}

Description:
{dispute.Description}

Please review this dispute in the admin portal.

Best regards,
System Administration";
        }
    }
}