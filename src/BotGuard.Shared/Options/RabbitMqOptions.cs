namespace BotGuard.Shared.Options;

public class RabbitMqOptions
{
    public const string RabbitMq = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}