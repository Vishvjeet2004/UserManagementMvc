namespace UserManagementMvc.Services;

public class IncomingMailSettings
{
    public string Host { get; set; } = "";

    public int Port { get; set; }

    public bool UseSsl { get; set; }

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int PollIntervalSeconds { get; set; } = 30;
}