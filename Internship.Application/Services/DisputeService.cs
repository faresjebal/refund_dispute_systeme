using Internship.Application.DTOs.Dispute;
using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Internship.Application.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<DisputeService> _logger;

        public DisputeService(
            IDisputeRepository disputeRepository,
            ITransactionRepository transactionRepository,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IEmailService emailService,
            ILogger<DisputeService> logger)
        {
            _disputeRepository = disputeRepository;
            _transactionRepository = transactionRepository;
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<DisputeResponse> CreateDisputeAsync(CreateDisputeDto request, string userId)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
                if (transaction == null)
                    throw new ArgumentException("Transaction not found");

                if (transaction.UserId != userId)
                    throw new UnauthorizedAccessException("Transaction does not belong to the user");

                if (!transaction.IsDisputable)
                    throw new InvalidOperationException("Transaction is not disputable");

                var existingDispute = await _disputeRepository.ExistsForTransactionAsync(request.TransactionId);
                if (existingDispute)
                    throw new InvalidOperationException("A dispute already exists for this transaction");

                string? attachmentPath = null;
                if (request.Attachment != null)
                {
                    attachmentPath = await SaveAttachmentAsync(request.Attachment, "dispute");
                }

                var disputeId = GenerateDisputeId();
                var dispute = new Dispute
                {
                    DisputeId = disputeId,
                    TransactionId = request.TransactionId,
                    UserId = userId,
                    Type = request.Type,
                    Description = request.Description,
                    AttachmentPath = attachmentPath,
                    Status = DisputeStatus.Open,
                    CreatedAt = DateTime.UtcNow
                };

                var createdDispute = await _disputeRepository.CreateAsync(dispute);

                await _auditLogService.LogDisputeStatusChangeAsync(
                    dispute.DisputeId,
                    DisputeStatus.Open,
                    DisputeStatus.Open,
                    userId,
                    "Dispute created by customer"
                );

                _logger.LogInformation("Dispute created with ID: {DisputeId} by user {UserId}",
                    createdDispute.DisputeId, userId);

                return await MapToResponseAsync(createdDispute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dispute for transaction {TransactionId}", request.TransactionId);
                throw;
            }
        }

        public async Task<DisputeResponse> ProcessDisputeAsync(ProcessDisputeDto request, string adminUserId)
        {
            try
            {
                var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId);
                if (dispute == null)
                    throw new ArgumentException("Dispute not found");

                var previousStatus = dispute.Status;
                ValidateStatusTransition(dispute.Status, request.Status);

                dispute.Status = request.Status;
                dispute.ResolutionComments = request.ResolutionComments;

                if (!string.IsNullOrEmpty(request.AssignedToUserId))
                {
                    dispute.AssignedToUserId = request.AssignedToUserId;
                    dispute.AssignedAt = DateTime.UtcNow;
                }

                if (request.Status == DisputeStatus.Resolved || request.Status == DisputeStatus.Rejected)
                {
                    dispute.ResolvedAt = DateTime.UtcNow;
                }

                var updatedDispute = await _disputeRepository.UpdateAsync(dispute);

                await _auditLogService.LogDisputeStatusChangeAsync(
                    dispute.DisputeId,
                    previousStatus,
                    request.Status,
                    adminUserId,
                    $"Status changed by admin. Comments: {request.ResolutionComments}"
                );

                if (request.Status == DisputeStatus.Resolved || request.Status == DisputeStatus.Rejected)
                {
                    var transaction = await _transactionRepository.GetByIdAsync(dispute.TransactionId);
                    if (transaction != null && transaction.Status == TransactionStatus.Disputed)
                    {
                        var previousTransactionStatus = transaction.Status;
                        transaction.Status = TransactionStatus.Completed;
                        await _transactionRepository.UpdateAsync(transaction);

                        await _auditLogService.LogTransactionStatusChangeAsync(
                            transaction.TransactionId,
                            previousTransactionStatus,
                            transaction.Status,
                            adminUserId,
                            $"Transaction status updated due to dispute {request.Status.ToString().ToLower()}. Dispute ID: {dispute.DisputeId}"
                        );
                    }
                }

                _logger.LogInformation("Dispute {DisputeId} status changed by admin {AdminUserId} from {PreviousStatus} to {NewStatus}",
                    dispute.DisputeId, adminUserId, previousStatus, request.Status);

                if (ShouldSendDisputeEmailNotification(request.Status))
                {
                    await SendDisputeStatusEmailAsync(updatedDispute);
                }

                return await MapToResponseAsync(updatedDispute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dispute {DisputeId}", request.DisputeId);
                throw;
            }
        }

        public async Task<DisputeResponse?> GetDisputeAsync(string disputeId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            return dispute != null ? await MapToResponseAsync(dispute) : null;
        }

        public async Task<IEnumerable<DisputeResponse>> GetUserDisputesAsync(string userId, int page = 1, int pageSize = 10)
        {
            var disputes = await _disputeRepository.GetByUserIdAsync(userId, page, pageSize);
            var responses = new List<DisputeResponse>();

            foreach (var dispute in disputes)
            {
                responses.Add(await MapToResponseAsync(dispute));
            }

            return responses;
        }

        public async Task<IEnumerable<DisputeResponse>> GetAllDisputesAsync(int page = 1, int pageSize = 10)
        {
            var disputes = await _disputeRepository.GetAllAsync(page, pageSize);
            var responses = new List<DisputeResponse>();

            foreach (var dispute in disputes)
            {
                responses.Add(await MapToResponseAsync(dispute));
            }

            return responses;
        }

        public async Task<IEnumerable<DisputeResponse>> GetAssignedDisputesAsync(string assignedToUserId, int page = 1, int pageSize = 10)
        {
            var disputes = await _disputeRepository.GetAssignedDisputesAsync(assignedToUserId, page, pageSize);
            var responses = new List<DisputeResponse>();

            foreach (var dispute in disputes)
            {
                responses.Add(await MapToResponseAsync(dispute));
            }

            return responses;
        }

        public async Task<IEnumerable<DisputeResponse>> GetDisputesByStatusAsync(DisputeStatus status, int page = 1, int pageSize = 10)
        {
            var disputes = await _disputeRepository.GetByStatusAsync(status, page, pageSize);
            var responses = new List<DisputeResponse>();

            foreach (var dispute in disputes)
            {
                responses.Add(await MapToResponseAsync(dispute));
            }

            return responses;
        }

        public async Task<DisputeResponse> AssignDisputeAsync(string disputeId, string assignedToUserId, string adminUserId)
        {
            try
            {
                var dispute = await _disputeRepository.GetByIdAsync(disputeId);
                if (dispute == null)
                    throw new ArgumentException("Dispute not found");

                if (dispute.UserId == assignedToUserId)
                {
                    _logger.LogWarning("Cannot assign dispute {DisputeId}: Assigned user cannot be same as creator", disputeId);
                    throw new ArgumentException("Cannot assign dispute to same user who created it");
                }

                var previousStatus = dispute.Status;
                var previousAssignedUser = dispute.AssignedToUserId;

                dispute.AssignedToUserId = assignedToUserId;
                dispute.AssignedAt = DateTime.UtcNow;

                if (dispute.Status == DisputeStatus.Open)
                {
                    dispute.Status = DisputeStatus.UnderReview;
                }

                var updatedDispute = await _disputeRepository.UpdateAsync(dispute);

                var assignedUser = await _userManager.FindByIdAsync(assignedToUserId);
                var assignedUserName = assignedUser != null ? $"{assignedUser.FirstName} {assignedUser.LastName}".Trim() : assignedToUserId;

                var auditMessage = string.IsNullOrEmpty(previousAssignedUser)
                    ? $"Dispute assigned to user: {assignedUserName} ({assignedToUserId})"
                    : $"Dispute reassigned from previous user to: {assignedUserName} ({assignedToUserId})";

                await _auditLogService.LogDisputeStatusChangeAsync(
                    dispute.DisputeId,
                    previousStatus,
                    dispute.Status,
                    adminUserId,
                    auditMessage
                );

                _logger.LogInformation("Dispute {DisputeId} assigned to {AssignedUserName} by admin {AdminUserId}",
                    dispute.DisputeId, assignedUserName, adminUserId);

                if (assignedUser?.Email != null)
                {
                    var disputeResponse = await MapToResponseAsync(updatedDispute);
                    await SendDisputeAssignmentEmailAsync(assignedUser.Email, assignedUserName, disputeResponse);
                }

                return await MapToResponseAsync(updatedDispute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning dispute {DisputeId}", disputeId);
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

        private void ValidateStatusTransition(DisputeStatus currentStatus, DisputeStatus newStatus)
        {
            var validTransitions = new Dictionary<DisputeStatus, DisputeStatus[]>
            {
                { DisputeStatus.Open, new[] { DisputeStatus.UnderReview, DisputeStatus.Rejected } },
                { DisputeStatus.UnderReview, new[] { DisputeStatus.AwaitingCustomerResponse, DisputeStatus.AwaitingMerchantResponse,
                                                     DisputeStatus.Resolved, DisputeStatus.Rejected, DisputeStatus.Escalated } },
                { DisputeStatus.AwaitingCustomerResponse, new[] { DisputeStatus.UnderReview, DisputeStatus.Rejected } },
                { DisputeStatus.AwaitingMerchantResponse, new[] { DisputeStatus.UnderReview, DisputeStatus.Escalated } },
                { DisputeStatus.Escalated, new[] { DisputeStatus.Resolved, DisputeStatus.Rejected } }
            };

            if (!validTransitions.ContainsKey(currentStatus) ||
                !validTransitions[currentStatus].Contains(newStatus))
            {
                throw new InvalidOperationException(
                    $"Invalid status transition from {currentStatus} to {newStatus}");
            }
        }

        private string GenerateDisputeId()
        {
            return $"DSP_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        private async Task<DisputeResponse> MapToResponseAsync(Dispute dispute)
        {
            string userFullName = "N/A";
            string? assignedToUserName = null;

            if (!string.IsNullOrEmpty(dispute.UserId))
            {
                var user = await _userManager.FindByIdAsync(dispute.UserId);
                userFullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "N/A";
            }

            if (!string.IsNullOrEmpty(dispute.AssignedToUserId))
            {
                var assignedUser = await _userManager.FindByIdAsync(dispute.AssignedToUserId);
                assignedToUserName = assignedUser != null ? $"{assignedUser.FirstName} {assignedUser.LastName}".Trim() : null;
            }

            return new DisputeResponse
            {
                DisputeId = dispute.DisputeId,
                TransactionId = dispute.TransactionId,
                TransactionReference = dispute.Transaction?.TransactionId ?? "N/A",
                Type = dispute.Type,
                Description = dispute.Description,
                AttachmentPath = dispute.AttachmentPath,
                Status = dispute.Status,
                ResolutionComments = dispute.ResolutionComments,
                CreatedAt = dispute.CreatedAt,
                AssignedAt = dispute.AssignedAt,
                ResolvedAt = dispute.ResolvedAt,
                UserFullName = userFullName,
                AssignedToUserName = assignedToUserName,
                UserId = dispute.UserId
            };
        }

        private bool ShouldSendDisputeEmailNotification(DisputeStatus status)
        {
            return status == DisputeStatus.Resolved ||
                   status == DisputeStatus.Rejected;
        }

        private async Task SendDisputeStatusEmailAsync(Dispute dispute)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dispute.UserId);
                if (user?.Email != null)
                {
                    var disputeResponse = await MapToResponseAsync(dispute);
                    var userName = $"{user.FirstName} {user.LastName}".Trim();

                    var emailSent = await _emailService.SendDisputeStatusEmailAsync(user.Email, userName, disputeResponse);

                    if (emailSent)
                    {
                        _logger.LogInformation("Dispute status email sent successfully for dispute {DisputeId} to {Email}",
                            dispute.DisputeId, user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send dispute status email for dispute {DisputeId} to {Email}",
                            dispute.DisputeId, user.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute status email for dispute {DisputeId}", dispute.DisputeId);
            }
        }

        private async Task SendDisputeAssignmentEmailAsync(string assignedUserEmail, string assignedUserName, DisputeResponse dispute)
        {
            try
            {
                var emailSent = await _emailService.SendDisputeAssignmentEmailAsync(assignedUserEmail, assignedUserName, dispute);

                if (emailSent)
                {
                    _logger.LogInformation("Dispute assignment email sent successfully for dispute {DisputeId} to {Email}",
                        dispute.DisputeId, assignedUserEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to send dispute assignment email for dispute {DisputeId} to {Email}",
                        dispute.DisputeId, assignedUserEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute assignment email for dispute {DisputeId}", dispute.DisputeId);
            }
        }
    }
}