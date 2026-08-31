using System;
using Microsoft.EntityFrameworkCore;

namespace UserManagementMvc.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Users table
    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Emailotp> Emailotps { get; set; }

    // Security question master table
    public virtual DbSet<Securityquestionmaster> SecurityQuestionMasters { get; set; }

    // User saved security questions table
    public virtual DbSet<Usersecurityquestion> UserSecurityQuestions { get; set; }

    // Site content table for Home/Privacy fixed content
    public virtual DbSet<Sitecontent> Sitecontents { get; set; }

    // Site content sections table for multiple dynamic sections
    public virtual DbSet<Sitecontentsection> Sitecontentsections { get; set; }

    public virtual DbSet<Auditlog> Auditlogs { get; set; }

    public virtual DbSet<MailMessage> MailMessages { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }


    public DbSet<ChatEmoji> ChatEmojis { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        // Security Question Master Table
        
        modelBuilder.Entity<Securityquestionmaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("securityquestionmasters");

            entity.Property(e => e.QuestionText)
                .HasMaxLength(255);

            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'");

            entity.Property(e => e.CreatedByRole)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");


        });

        
        // Users Table
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.UserName, "UserName").IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .HasMaxLength(150);

            entity.Property(e => e.Mobile)
                .HasMaxLength(15);

            entity.Property(e => e.MobileCountryCode)
                .HasMaxLength(10)
                .HasDefaultValueSql("'+91'");
           
            entity.Property(e => e.DateOfBirth)
                .HasColumnType("date");

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500);

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'User'");

            entity.Property(e => e.IsDeleted)
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.LastSeenAt)
    .HasColumnType("datetime");

            entity.Property(e => e.UserName)
                .HasMaxLength(100);

            entity.Property(e => e.SecurityQuestion)
                .HasMaxLength(255);

            entity.Property(e => e.SecurityAnswerHash)
                .HasMaxLength(500);

            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasDefaultValueSql("'General'");

            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'");
        });

        
        // User Security Questions Table
        
        modelBuilder.Entity<Usersecurityquestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usersecurityquestions");

            entity.HasIndex(e => e.UserId, "FK_UserSecurityQuestions_Users");

            entity.HasIndex(e => e.SecurityQuestionMasterId, "FK_UserSecurityQuestions_SecurityQuestionMasters");

            entity.Property(e => e.QuestionText)
                .HasMaxLength(255);

            entity.Property(e => e.SecurityAnswerHash)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Usersecurityquestions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserSecurityQuestions_Users");

            entity.HasOne(d => d.SecurityQuestionMaster)
                .WithMany(p => p.Usersecurityquestions)
                .HasForeignKey(d => d.SecurityQuestionMasterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UserSecurityQuestions_SecurityQuestionMasters");
        });

        
        // Site Contents Table
        
        modelBuilder.Entity<Sitecontent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sitecontents");

            entity.HasIndex(e => e.ContentKey, "ContentKey").IsUnique();

            entity.Property(e => e.ContentKey)
                .HasMaxLength(100);

            entity.Property(e => e.Title)
                .HasMaxLength(255);

            entity.Property(e => e.Content)
                .HasColumnType("text");

            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");
        });

        
        // Site Content Sections Table
        // Privacy page multiple heading + description
        
        modelBuilder.Entity<Sitecontentsection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sitecontentsections");

            entity.Property(e => e.PageName)
                .HasMaxLength(100);

            entity.Property(e => e.SectionTitle)
                .HasMaxLength(255);

            entity.Property(e => e.SectionContent)
                .HasColumnType("text");

            entity.Property(e => e.DisplayOrder)
                .HasDefaultValueSql("'1'");

            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");
        });

        
        // Audit Logs Table
        
        modelBuilder.Entity<Auditlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("auditlogs");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AuditLogs_Users");
        });


        // Mail Messages Table


        modelBuilder.Entity<MailMessage>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.ToTable("mailmessages");

            entity.HasIndex(e => e.SenderUserId)
                .HasDatabaseName("IX_MailMessages_SenderUserId");

            entity.HasIndex(e => e.RecipientUserId)
                .HasDatabaseName("IX_MailMessages_RecipientUserId");

            entity.HasIndex(e => e.SenderEmail)
                .HasDatabaseName("IX_MailMessages_SenderEmail");

            entity.HasIndex(e => e.RecipientEmail)
                .HasDatabaseName("IX_MailMessages_RecipientEmail");

            entity.HasIndex(e => e.ParentMessageId)
                .HasDatabaseName("IX_MailMessages_ParentMessageId");
            entity.HasIndex(e => e.ExternalMessageId)
    .IsUnique()
    .HasDatabaseName("IX_MailMessages_ExternalMessageId");

            entity.Property(e => e.SenderEmail)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.RecipientEmail)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Body)
                .HasColumnType("longtext")
                .IsRequired();

            entity.Property(e => e.MessageType)
                .HasMaxLength(30)
                .HasDefaultValue("Internal");
            entity.Property(e => e.ExternalMessageId)
    .HasMaxLength(500);

            entity.Property(e => e.IsRead)
                .HasDefaultValue(false);

            entity.Property(e => e.IsStarred)
                .HasDefaultValue(false);

            entity.Property(e => e.IsDraft)
                .HasDefaultValue(false);

            entity.Property(e => e.DraftSavedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.IsDeletedBySender)
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeletedByRecipient)
                .HasDefaultValue(false);

            entity.Property(e => e.IsPermanentlyDeletedBySender)
                .HasDefaultValue(false);

            entity.Property(e => e.IsPermanentlyDeletedByRecipient)
                .HasDefaultValue(false);

            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ReadAt)
                .HasColumnType("datetime");

            entity.HasOne(e => e.SenderUser)
                .WithMany()
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_MailMessages_SenderUser");

            entity.HasOne(e => e.RecipientUser)
                .WithMany()
                .HasForeignKey(e => e.RecipientUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_MailMessages_RecipientUser");

            entity.HasOne(e => e.ParentMessage)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_MailMessages_ParentMessage");
        });



        // Chat Messages Table

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.ToTable("chatmessages");

            entity.HasIndex(e => e.SenderUserId)
                .HasDatabaseName(
                    "IX_ChatMessages_SenderUserId");

            entity.HasIndex(e => e.RecipientUserId)
                .HasDatabaseName(
                    "IX_ChatMessages_RecipientUserId");

            entity.HasIndex(e => new
            {
                e.SenderUserId,
                e.RecipientUserId,
                e.SentAt
            })
            .HasDatabaseName(
                "IX_ChatMessages_Conversation");

            entity.Property(e => e.MessageText)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql(
                    "CURRENT_TIMESTAMP");

            entity.Property(e => e.DeliveredAt)
                .HasColumnType("datetime");

            entity.Property(e => e.SeenAt)
                .HasColumnType("datetime");

            entity.Property(e => e.IsDeletedBySender)
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeletedByRecipient)
                .HasDefaultValue(false);

            // Attachments

            entity.Property(e => e.AttachmentUrl)
                .HasMaxLength(500);

            entity.Property(e => e.AttachmentName)
                .HasMaxLength(255);

            entity.Property(e => e.AttachmentContentType)
                .HasMaxLength(100);

            entity.Property(e => e.AttachmentSize);

            entity.HasOne(e => e.SenderUser)
                .WithMany(e => e.SentChatMessages)
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_ChatMessages_SenderUser");

            entity.HasOne(e => e.RecipientUser)
                .WithMany(e => e.ReceivedChatMessages)
                .HasForeignKey(e => e.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_ChatMessages_RecipientUser");
        });

   

        OnModelCreatingPartial(modelBuilder);

        
        // Email OTP Table
        
        modelBuilder.Entity<Emailotp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("emailotps");

            entity.HasIndex(e => e.UserId, "IX_EmailOtps_UserId");

            entity.Property(e => e.Email)
                .HasMaxLength(150);

            entity.Property(e => e.OtpHash)
                .HasMaxLength(500);

            entity.Property(e => e.Purpose)
                .HasMaxLength(50);

            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.IsUsed)
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.AttemptCount)
                .HasDefaultValueSql("'0'");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Emailotps)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmailOtps_Users");
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}