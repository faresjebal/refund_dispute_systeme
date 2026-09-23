using Internship.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Data
{
    public class RefundDisputeContext : IdentityDbContext<ApplicationUser>
    {
        public RefundDisputeContext(DbContextOptions<RefundDisputeContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // New Payment System DbSets
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
        public DbSet<Dispute> Disputes { get; set; } = null!;
        public DbSet<TransactionLog> TransactionLogs { get; set; } = null!;
        public DbSet<RefundRequestLog> RefundRequestLogs { get; set; } = null!;
        public DbSet<DisputeLog> DisputeLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Must call base first for Identity configuration
            base.OnModelCreating(builder);

            // Configure Identity table names
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("Users");
                b.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<IdentityRole>(b => b.ToTable("Roles"));
            builder.Entity<IdentityUserRole<string>>(b => b.ToTable("UserRoles"));
            builder.Entity<IdentityUserClaim<string>>(b => b.ToTable("UserClaims"));
            builder.Entity<IdentityUserLogin<string>>(b => b.ToTable("UserLogins"));
            builder.Entity<IdentityUserToken<string>>(b => b.ToTable("UserTokens"));
            builder.Entity<IdentityRoleClaim<string>>(b => b.ToTable("RoleClaims"));

            // Configure all entities with explicit typing
            ConfigureAllEntities(builder);
        }

        private void ConfigureAllEntities(ModelBuilder builder)
        {
            // Configure RefreshToken
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_RefreshTokens");
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500).HasColumnName("TokenValue");
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd();
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.CreatedByIp).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.RevokedByIp).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
                entity.Property(e => e.ReasonRevoked).HasMaxLength(200);

                entity.Ignore(e => e.IsActive);
                entity.Ignore(e => e.IsExpired);
                entity.Ignore(e => e.IsRevoked);

                entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).HasConstraintName("FK_RefreshTokens_Users").OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.ExpiresAt, e.RevokedAt });
            });

            // Configure Transaction - PRIMARY ENTITY
            builder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.TransactionId);
                entity.Property(t => t.TransactionId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(t => t.UserId).IsRequired().HasMaxLength(450);
                entity.Property(t => t.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(t => t.Currency).IsRequired().HasMaxLength(3);
                entity.Property(t => t.Type).IsRequired().HasConversion<int>();
                entity.Property(t => t.Status).IsRequired().HasConversion<int>();
                entity.Property(t => t.PaymentMethod).IsRequired().HasConversion<int>();
                entity.Property(t => t.Description).HasMaxLength(500);
                entity.Property(t => t.MerchantReference).HasMaxLength(100);
                entity.Property(t => t.PaymentProviderTransactionId).HasMaxLength(50);
                entity.Property(t => t.PaymentProviderName).HasMaxLength(100);
                entity.Property(t => t.MaskedCardNumber).HasMaxLength(20);
                entity.Property(t => t.CardHolderName).HasMaxLength(50);
                entity.Property(t => t.CardType).HasMaxLength(20);
                entity.Property(t => t.ErrorMessage).HasMaxLength(500);
                entity.Property(t => t.ErrorCode).HasMaxLength(50);
                entity.Property(t => t.CreatedAt).IsRequired();

                entity.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(t => t.TransactionId).IsUnique();
                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.CreatedAt);
            });

            // Configure RefundRequest
            builder.Entity<RefundRequest>(entity =>
            {
                entity.HasKey(r => r.RefundId);
                entity.Property(r => r.RefundId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(r => r.TransactionId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(r => r.UserId).IsRequired().HasMaxLength(450);
                entity.Property(r => r.RequestedAmount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(r => r.ApprovedAmount).HasColumnType("decimal(18,2)");
                entity.Property(r => r.Reason).IsRequired().HasMaxLength(1000);
                entity.Property(r => r.AttachmentPath).HasMaxLength(500);
                entity.Property(r => r.Status).IsRequired().HasConversion<int>();
                entity.Property(r => r.AdminNotes).HasMaxLength(1000);
                entity.Property(r => r.CreatedAt).IsRequired();

                entity.HasIndex(r => r.RefundId).IsUnique();
                entity.HasIndex(r => r.TransactionId);
                entity.HasIndex(r => r.UserId);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
            });

            // Configure Dispute
            builder.Entity<Dispute>(entity =>
            {
                entity.HasKey(d => d.DisputeId);
                entity.Property(d => d.DisputeId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(d => d.TransactionId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(d => d.UserId).IsRequired().HasMaxLength(450);
                entity.Property(d => d.Type).IsRequired().HasConversion<int>();
                entity.Property(d => d.Description).IsRequired().HasMaxLength(2000);
                entity.Property(d => d.AttachmentPath).HasMaxLength(500);
                entity.Property(d => d.Status).IsRequired().HasConversion<int>();
                entity.Property(d => d.ResolutionComments).HasMaxLength(2000);
                entity.Property(d => d.CreatedAt).IsRequired();

                entity.HasIndex(d => d.DisputeId).IsUnique();
                entity.HasIndex(d => d.TransactionId);
                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.Status);
                entity.HasIndex(d => d.CreatedAt);
            });

            // Configure TransactionLog
            builder.Entity<TransactionLog>(entity =>
            {
                entity.HasKey(tl => tl.Id);
                entity.Property(tl => tl.Id).IsRequired();
                entity.Property(tl => tl.TransactionId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(tl => tl.PreviousStatus).IsRequired().HasConversion<int>();
                entity.Property(tl => tl.NewStatus).IsRequired().HasConversion<int>();
                entity.Property(tl => tl.Notes).HasMaxLength(1000);
                entity.Property(tl => tl.ChangedAt).IsRequired();

                entity.HasIndex(tl => tl.TransactionId);
                entity.HasIndex(tl => tl.ChangedAt);
            });

            // Configure RefundRequestLog
            builder.Entity<RefundRequestLog>(entity =>
            {
                entity.HasKey(rrl => rrl.Id);
                entity.Property(rrl => rrl.Id).IsRequired();
                entity.Property(rrl => rrl.RefundId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(rrl => rrl.PreviousStatus).IsRequired().HasConversion<int>();
                entity.Property(rrl => rrl.NewStatus).IsRequired().HasConversion<int>();
                entity.Property(rrl => rrl.Notes).HasMaxLength(1000);
                entity.Property(rrl => rrl.ChangedAt).IsRequired();

                entity.HasIndex(rrl => rrl.RefundId);
                entity.HasIndex(rrl => rrl.ChangedAt);
            });

            // Configure DisputeLog
            builder.Entity<DisputeLog>(entity =>
            {
                entity.HasKey(dl => dl.Id);
                entity.Property(dl => dl.Id).IsRequired();
                entity.Property(dl => dl.DisputeId).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
                entity.Property(dl => dl.PreviousStatus).IsRequired().HasConversion<int>();
                entity.Property(dl => dl.NewStatus).IsRequired().HasConversion<int>();
                entity.Property(dl => dl.Notes).HasMaxLength(1000);
                entity.Property(dl => dl.ChangedAt).IsRequired();

                entity.HasIndex(dl => dl.DisputeId);
                entity.HasIndex(dl => dl.ChangedAt);
            });

            // Configure ALL relationships AFTER all entities are configured
            ConfigureRelationships(builder);
        }

        private void ConfigureRelationships(ModelBuilder builder)
        {
            // Transaction -> RefundRequest relationship
            builder.Entity<RefundRequest>()
                .HasOne(r => r.Transaction)
                .WithMany(t => t.RefundRequests)
                .HasForeignKey(r => r.TransactionId)
                .HasPrincipalKey(t => t.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> RefundRequest relationship
            builder.Entity<RefundRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProcessedByUser -> RefundRequest relationship
            builder.Entity<RefundRequest>()
                .HasOne(r => r.ProcessedByUser)
                .WithMany()
                .HasForeignKey(r => r.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Transaction -> Dispute relationship
            builder.Entity<Dispute>()
                .HasOne(d => d.Transaction)
                .WithMany(t => t.Disputes)
                .HasForeignKey(d => d.TransactionId)
                .HasPrincipalKey(t => t.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Dispute relationship
            builder.Entity<Dispute>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AssignedToUser -> Dispute relationship
            builder.Entity<Dispute>()
                .HasOne(d => d.AssignedToUser)
                .WithMany()
                .HasForeignKey(d => d.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Transaction -> TransactionLog relationship
            builder.Entity<TransactionLog>()
                .HasOne(tl => tl.Transaction)
                .WithMany(t => t.TransactionLogs)
                .HasForeignKey(tl => tl.TransactionId)
                .HasPrincipalKey(t => t.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> TransactionLog relationship
            builder.Entity<TransactionLog>()
                .HasOne(tl => tl.ChangedByUser)
                .WithMany()
                .HasForeignKey(tl => tl.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // RefundRequest -> RefundRequestLog relationship
            builder.Entity<RefundRequestLog>()
                .HasOne(rrl => rrl.RefundRequest)
                .WithMany(rr => rr.RefundLogs)
                .HasForeignKey(rrl => rrl.RefundId)
                .HasPrincipalKey(rr => rr.RefundId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> RefundRequestLog relationship
            builder.Entity<RefundRequestLog>()
                .HasOne(rrl => rrl.ChangedByUser)
                .WithMany()
                .HasForeignKey(rrl => rrl.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Dispute -> DisputeLog relationship
            builder.Entity<DisputeLog>()
                .HasOne(dl => dl.Dispute)
                .WithMany(d => d.DisputeLogs)
                .HasForeignKey(dl => dl.DisputeId)
                .HasPrincipalKey(d => d.DisputeId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> DisputeLog relationship
            builder.Entity<DisputeLog>()
                .HasOne(dl => dl.ChangedByUser)
                .WithMany()
                .HasForeignKey(dl => dl.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}