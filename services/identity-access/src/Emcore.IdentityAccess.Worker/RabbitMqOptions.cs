namespace Emcore.IdentityAccess.Worker;

public class RabbitMqOptions
{
    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Exchange { get; set; } = "emcore.events";
    public string ConnectionName { get; set; } = "identity-access-worker";
    public int PublisherConfirmTimeoutSeconds { get; set; } = 10;
}
