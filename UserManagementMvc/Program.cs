using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using UserManagementMvc.Hubs;
using UserManagementMvc.Models;
using UserManagementMvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly =
        true;

    options.Cookie.IsEssential =
        true;
});

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySQL(
            connectionString);
    });

builder.Services.AddScoped<MessengerService>();

builder.Services.AddScoped<
    IMailServerService,
    MailServerService>();

builder.Services.AddSingleton<
    MailHtmlSanitizer>();

builder.Services.AddScoped<
    IAuditLogService,
    AuditLogService>();

builder.Services.AddScoped<
    IExportService,
    ExportService>();

builder.Services.AddScoped<
    LoginService>();

builder.Services.Configure<SmtpSettings>(
    options =>
    {
        var mailSettings =
            builder.Configuration
                .GetSection(
                    "MailServerSettings");

        options.Host =
            mailSettings["SmtpHost"]
            ?? "";

        options.Port =
            int.TryParse(
                mailSettings["SmtpPort"],
                out int smtpPort)
                ? smtpPort
                : 587;

        options.SenderName =
            "User Management System";

        options.SenderEmail =
            mailSettings["MailboxEmail"]
            ?? "";

        options.Password =
            builder.Configuration[
                "SmtpSettings:Password"]
            ?? "";
    });

builder.Services.AddScoped<
    IEmailService,
    EmailService>();

builder.Services.AddScoped<
    IMailService,
    MailService>();

QuestPDF.Settings.License =
    LicenseType.Community;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapHub<MessengerHub>(
    "/messengerHub");

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();