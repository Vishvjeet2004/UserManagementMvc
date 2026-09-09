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

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySQL(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection"));
    });

builder.Services.AddScoped<MessengerService>();

builder.Services.AddSingleton<
    MailHtmlSanitizer>();

builder.Services.AddScoped<
    IAuditLogService,
    AuditLogService>();

builder.Services.AddScoped<
    IExportService,
    ExportService>();

builder.Services.AddScoped<
    IMailService,
    MailService>();

builder.Services.AddScoped<
    LoginService>();

builder.Services.AddScoped<
    IEmailService,
    EmailService>();

builder.Services.Configure<SmtpSettings>(
    builder.Configuration
        .GetSection("SmtpSettings"));

builder.Services.Configure<IncomingMailSettings>(
    builder.Configuration
        .GetSection("IncomingMailSettings"));

builder.Services.AddHostedService<
    IncomingMailService>();

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