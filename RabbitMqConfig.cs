using Microsoft.Extensions.Configuration;

namespace Workflow.Masstransit
{
    public static class XRabbitMqConfig
    {
        /// <summary> Reads the <see cref="Masstransit.RabbitMqConfig"/> from the <paramref name="configuration"/> </summary>
        public static RabbitMqConfig RabbitMqConfig(this IConfiguration configuration)
            => configuration.GetSection(nameof(Masstransit.RabbitMqConfig))?.Get<RabbitMqConfig>() ?? new RabbitMqConfig();
    }

    /// <summary> Configuration Key and Container for appSettings.json </summary>
    public class RabbitMqConfig
    {
        /// <summary> RabbitMq Server Name or IP-Address </summary>
        public string? Host { get; set; } = "localhost";

        /// <summary> Default virtual Host (to partition Messages) </summary>
        public string? Path { get; set; } = "capmatix";

        /// <summary> Username (Default works only for localhost) </summary>
        public string? UserName { get; set; } = "guest";

        /// <summary> Password (Default works only for localhost) </summary>
        public string? PassWord { get; set; } = "guest";

        /// <summary> Batch-Size, Number of concurrently processed Messages </summary>
        public int ConcurrencyLimit { get; set; }= 1;
    }
}