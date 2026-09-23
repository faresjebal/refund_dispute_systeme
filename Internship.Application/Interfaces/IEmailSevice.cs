using Internship.Application.DTOs.Dispute;
using Internship.Application.DTOs.RefundRequest;
using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface IEmailService
    {
        // Add the missing SendEmailAsync method
        Task<bool> SendEmailAsync(string toEmail, string subject, string content);

        Task<bool> SendDisputeStatusEmailAsync(string userEmail, string userName, DisputeResponse dispute);
        Task<bool> SendRefundStatusEmailAsync(string userEmail, string userName, RefundRequestResponse refund);
        Task<bool> SendDisputeAssignmentEmailAsync(string assignedUserEmail, string assignedUserName, DisputeResponse dispute);
    }
}