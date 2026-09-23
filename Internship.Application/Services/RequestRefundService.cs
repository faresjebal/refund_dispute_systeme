using Internship.Application.DTOs.RefundRequest;
using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Internship.Application.Services
{
    public class RefundRequestService : IRefundRequestService
    {
        private readonly IRefundRequestRepository _refundRequestRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<RefundRequestService> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RefundRequestService(
            IRefundRequestRepository refundRequestRepository,
            ITransactionRepository transactionRepository,
            ITransactionService transactionService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ILogger<RefundRequestService> logger)
        {
            _refundRequestRepository = refundRequestRepository;
            _transactionRepository = transactionRepository;
            _transactionService = transactionService;
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<RefundRequestResponse> CreateRefundRequestAsync(CreateRefundRequestDto request, string userId)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
                if (transaction == null)
                    throw new ArgumentException("Transaction not found");

                if (transaction.UserId != userId)
                    throw new UnauthorizedAccessException("Transaction does not belong to the user");

                if (!transaction.IsRefundable)
                    throw new InvalidOperationException("Transaction is not refundable");

                var existingRefunds = await _refundRequestRepository.GetTotalRefundedAmountForTransactionAsync(request.TransactionId);
                var availableAmount = transaction.Amount - existingRefunds;

                if (request.RequestedAmount > availableAmount)
                    throw new InvalidOperationException($"Requested amount exceeds available refund amount of {availableAmount:C}");

                string? attachmentPath = null;
                if (request.Attachment != null)
                {
                    attachmentPath = await SaveAttachmentAsync(request.Attachment, "refund");
                }

                var refundId = GenerateRefundId();
                var refundRequest = new RefundRequest
                {
                    RefundId = refundId,
                    TransactionId = request.TransactionId,
                    UserId = userId,
                    RequestedAmount = request.RequestedAmount,
                    Reason = request.Reason,
                    AttachmentPath = attachmentPath,
                    Status = RefundStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                var createdRefundRequest = await _refundRequestRepository.CreateAsync(refundRequest);

                await _auditLogService.LogRefundStatusChangeAsync(
                    refundRequest.RefundId,
                    RefundStatus.Pending,
                    RefundStatus.Pending,
                    userId,
                    $"Refund request created for transaction {request.TransactionId}"
                );

                _logger.LogInformation("Refund request created with ID: {RefundId} by user {UserId}",
                    createdRefundRequest.RefundId, userId);

                return await MapToResponseAsync(createdRefundRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request for transaction {TransactionId}", request.TransactionId);
                throw;
            }
        }

        public async Task<RefundRequestResponse> ProcessRefundRequestAsync(ProcessRefundRequestDto request, string adminUserId)
        {
            try
            {
                var refundRequest = await _refundRequestRepository.GetByIdAsync(request.RefundId);
                if (refundRequest == null)
                    throw new ArgumentException("Refund request not found");

                if (refundRequest.Status != RefundStatus.Pending && refundRequest.Status != RefundStatus.UnderReview)
                    throw new InvalidOperationException($"Refund request is already {refundRequest.Status}");

                var previousStatus = refundRequest.Status;
                refundRequest.Status = request.Status;
                refundRequest.ProcessedByUserId = adminUserId;
                refundRequest.ProcessedAt = DateTime.UtcNow;
                refundRequest.AdminNotes = request.AdminNotes;

                if (request.Status == RefundStatus.Approved && request.ApprovedAmount.HasValue)
                {
                    refundRequest.ApprovedAmount = request.ApprovedAmount.Value;
                }

                var updatedRefundRequest = await _refundRequestRepository.UpdateAsync(refundRequest);

                await _auditLogService.LogRefundStatusChangeAsync(
                    refundRequest.RefundId,
                    previousStatus,
                    request.Status,
                    adminUserId,
                    request.AdminNotes
                );

                if (ShouldSendRefundEmailNotification(refundRequest.Status))
                {
                    await SendRefundStatusEmailAsync(updatedRefundRequest);
                }

                _logger.LogInformation("Refund request {RefundId} status updated by admin {AdminUserId} from {PreviousStatus} to {NewStatus}",
                    refundRequest.RefundId, adminUserId, previousStatus, refundRequest.Status);

                return await MapToResponseAsync(updatedRefundRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund request {RefundRequestId}", request.RefundId);
                throw;
            }
        }

        public async Task<RefundRequestResponse?> GetRefundRequestAsync(string refundRequestId)
        {
            var refundRequest = await _refundRequestRepository.GetByIdAsync(refundRequestId);
            return refundRequest != null ? await MapToResponseAsync(refundRequest) : null;
        }

        public async Task<IEnumerable<RefundRequestResponse>> GetUserRefundRequestsAsync(string userId, int page = 1, int pageSize = 10)
        {
            var refundRequests = await _refundRequestRepository.GetByUserIdAsync(userId, page, pageSize);
            var responses = new List<RefundRequestResponse>();

            foreach (var refundRequest in refundRequests)
            {
                responses.Add(await MapToResponseAsync(refundRequest));
            }

            return responses;
        }

        public async Task<IEnumerable<RefundRequestResponse>> GetAllRefundRequestsAsync(int page = 1, int pageSize = 10)
        {
            var refundRequests = await _refundRequestRepository.GetAllAsync(page, pageSize);
            var responses = new List<RefundRequestResponse>();

            foreach (var refundRequest in refundRequests)
            {
                responses.Add(await MapToResponseAsync(refundRequest));
            }

            return responses;
        }

        public async Task<IEnumerable<RefundRequestResponse>> GetRefundRequestsByStatusAsync(RefundStatus status, int page = 1, int pageSize = 10)
        {
            var refundRequests = await _refundRequestRepository.GetByStatusAsync(status, page, pageSize);
            var responses = new List<RefundRequestResponse>();

            foreach (var refundRequest in refundRequests)
            {
                responses.Add(await MapToResponseAsync(refundRequest));
            }

            return responses;
        }

        public async Task<bool> RefundExistsForTransactionAsync(string transactionId)
        {
            return await _refundRequestRepository.ExistsForTransactionAsync(transactionId);
        }

        public async Task<RefundRequestResponse> UpdateRefundStatusAsync(string refundRequestId, RefundStatus newStatus, string? adminNotes = null)
        {
            try
            {
                var refundRequest = await _refundRequestRepository.GetByIdAsync(refundRequestId);
                if (refundRequest == null)
                    throw new ArgumentException("Refund request not found");

                var previousStatus = refundRequest.Status;
                refundRequest.Status = newStatus;

                if (newStatus == RefundStatus.Completed || newStatus == RefundStatus.Failed)
                {
                    refundRequest.ProcessedAt = DateTime.UtcNow;
                }

                if (!string.IsNullOrEmpty(adminNotes))
                {
                    refundRequest.AdminNotes = adminNotes;
                }

                var updatedRefundRequest = await _refundRequestRepository.UpdateAsync(refundRequest);

                await _auditLogService.LogRefundStatusChangeAsync(
                    refundRequest.RefundId,
                    previousStatus,
                    newStatus,
                    refundRequest.ProcessedByUserId ?? "System",
                    adminNotes ?? $"Status updated to {newStatus}"
                );

                if (ShouldSendRefundEmailNotification(newStatus))
                {
                    await SendRefundStatusEmailAsync(updatedRefundRequest);
                }

                _logger.LogInformation("Refund request {RefundId} status updated from {PreviousStatus} to {NewStatus}",
                    refundRequest.RefundId, previousStatus, newStatus);

                return await MapToResponseAsync(updatedRefundRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refund request status for {RefundRequestId}", refundRequestId);
                throw;
            }
        }

        private async Task<string> SaveAttachmentAsync(IFormFile file, string category)
        {
            var uploadsPath = Path.Combine("wwwroot", "uploads", category);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("uploads", category, fileName);
        }

        private async Task LogRefundStatusChangeAsync(string refundRequestId, RefundStatus previousStatus, RefundStatus newStatus, string changedByUserId, string? notes)
        {
            try
            {
                if (!string.IsNullOrEmpty(changedByUserId) && changedByUserId != "System")
                {
                    var user = await _userManager.FindByIdAsync(changedByUserId);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found for refund log, using system identifier", changedByUserId);
                        changedByUserId = null;
                    }
                }

                await _auditLogService.LogRefundStatusChangeAsync(
                    refundRequestId,
                    previousStatus,
                    newStatus,
                    changedByUserId,
                    notes
                );

                _logger.LogInformation("Refund request {RefundRequestId} status changed from {PreviousStatus} to {NewStatus}",
                    refundRequestId, previousStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund status change for {RefundRequestId}", refundRequestId);
            }
        }

        private string GenerateRefundId()
        {
            return $"REF_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        private async Task<RefundRequestResponse> MapToResponseAsync(RefundRequest refundRequest)
        {
            string processedByName = "System";
            if (!string.IsNullOrEmpty(refundRequest.ProcessedByUserId))
            {
                var processedByUser = await _userManager.FindByIdAsync(refundRequest.ProcessedByUserId);
                processedByName = processedByUser != null ? $"{processedByUser.FirstName} {processedByUser.LastName}" : refundRequest.ProcessedByUserId;
            }

            return new RefundRequestResponse
            {
                RefundId = refundRequest.RefundId,
                TransactionId = refundRequest.TransactionId,
                TransactionReference = refundRequest.Transaction?.TransactionId ?? "N/A",
                RequestedAmount = refundRequest.RequestedAmount,
                ApprovedAmount = refundRequest.ApprovedAmount,
                Reason = refundRequest.Reason,
                AttachmentPath = refundRequest.AttachmentPath,
                Status = refundRequest.Status,
                AdminNotes = refundRequest.AdminNotes,
                CreatedAt = refundRequest.CreatedAt,
                ProcessedAt = refundRequest.ProcessedAt,
                UserFullName = refundRequest.User?.FullName ?? "N/A",
                ProcessedByUserName = processedByName
            };
        }

        private bool ShouldSendRefundEmailNotification(RefundStatus status)
        {
            return status == RefundStatus.Approved ||
                   status == RefundStatus.Rejected ||
                   status == RefundStatus.Completed ||
                   status == RefundStatus.Failed;
        }

        private async Task SendRefundStatusEmailAsync(RefundRequest refundRequest)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(refundRequest.UserId);
                if (user?.Email != null)
                {
                    var refundResponse = await MapToResponseAsync(refundRequest);
                    var userName = $"{user.FirstName} {user.LastName}".Trim();

                    var emailSent = await _emailService.SendRefundStatusEmailAsync(user.Email, userName, refundResponse);

                    if (emailSent)
                    {
                        _logger.LogInformation("Refund status email sent successfully for refund {RefundId} to {Email}",
                            refundRequest.RefundId, user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send refund status email for refund {RefundId} to {Email}",
                            refundRequest.RefundId, user.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund status email for refund {RefundId}", refundRequest.RefundId);
            }
        }
    }
}