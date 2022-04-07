using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.RabbitMqTransport;
using MassTransit.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Workflow.Masstransit;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace RuleEngine.Cli
{
    public static class XMassTransit {

        #region Function Switches, could be moved to Configuration Action

        public const string SchedulerQueueName = "scheduler";
        public static string UriScheduler => "queue:" +  SchedulerQueueName;
        public const bool UseTransactionalBus = true;
        #endregion

        static bool? _isRunningInContainer;

        public static bool IsRunningInContainer => _isRunningInContainer ??= bool.TryParse(Environment
            .GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) && inContainer;

        /// <summary> Adds DI <see cref="IRequestClient{TMessage}"/> to the <paramref name="configurator"/> </summary>
        /// <remarks>Can be used to get an async Response</remarks>
        public static void AddRequestClientFor<TMessage>(IServiceCollectionBusConfigurator configurator) 
            where TMessage : class => configurator.AddRequestClient<TMessage>();

        /// <summary> Adds all Consumers, Activities etc. from the <paramref name="assemblies"/> </summary>
        public static void AddMassTransitServicesFrom(this IServiceCollection services
            , Action<IServiceCollectionBusConfigurator>? configureBus = null
            , Action<IRabbitMqBusFactoryConfigurator>? configureRabbitMq = null
            , params Assembly[] assemblies)
        {
            services.AddMassTransit(busConfigurator =>
            {
                busConfigurator.AddDelayedMessageScheduler();

                busConfigurator.SetKebabCaseEndpointNameFormatter();
                //busConfigurator.AddTransactionalEnlistmentBus(); //automatically enlists in ambient Transaction
                if (UseTransactionalBus)
                {
                    busConfigurator.AddTransactionalBus();
                }

                if (!string.IsNullOrWhiteSpace(SchedulerQueueName))
                {
                    busConfigurator.AddMessageScheduler(new Uri(UriScheduler));
                }

                //TODO: By default, sagas are in-memory, but this should be changed to a durable saga repository.
                busConfigurator.SetInMemorySagaRepositoryProvider();

                busConfigurator.AddConsumers(assemblies);
                busConfigurator.AddSagaStateMachines(assemblies);
                busConfigurator.AddSagas(assemblies);
                busConfigurator.AddActivities(assemblies);

                busConfigurator.UsingRabbitMq((context, rabbitConfigurator) =>
                {

                    configureRabbitMq?.Invoke(rabbitConfigurator);

                    rabbitConfigurator.UseDelayedMessageScheduler();

                    rabbitConfigurator.ConfigureEndpoints(context);
                });
                configureBus?.Invoke(busConfigurator);
            });
        }

#pragma warning disable S2223, S1104 // Non-constant static fields should not be visible
        /// <summary> Global Flag to start the Message Bus synchronously </summary>
        public static bool WaitUntilHostIsStarted = true;
#pragma warning restore S2223, S1104 // Non-constant static fields should not be visible

        /// <summary> Initializes a <see cref="IHostBuilder"/> with Logging and <paramref name="args"/>,
        /// Environment-Variables and 'appSettings.json' </summary>
        public static IHostBuilder CreateDefaultBuilder(this string[] args) => Host.CreateDefaultBuilder(args);

        /// <summary> Registers the built-in hosted MassTransitHostedService so this can be run in a Windows Service </summary>
        /// <param name="args">Command Line Parameters to read and possibly override appConfig Settings</param>
        /// <param name="configureBus">Callback to configure the Bus</param>
        /// <param name="configureRabbitMq">Callback to configure RabbitMq</param>
        /// <param name="configureServices">to register additional Services;
        /// alternatively use <see cref="HostBuilder"/>.<see cref="HostBuilder.ConfigureServices"/></param>
        /// <param name="assemblies">List of Assemblies to register Consumers</param>
        /// <returns>the <see cref="IHostBuilder"/> to further configure, build and run,
        /// either as Console or Windows Service</returns>
        /// <example>
        /// Use either:
        /// <code> await hostBuilder.UseWindowsService().Build().RunAsync(cancellationToken).ConfigureAwait(false);</code>
        /// or:
        /// <code> await hostBuilder.RunConsoleAsync(); </code>.
        /// On Linux use:
        /// <code> await builder.UseSystemd().Build().RunAsync(); </code>
        /// </example>
        public static IHostBuilder CreateMassTransitHostBuilder(this string[] args
            , Action<IServiceCollectionBusConfigurator, HostBuilderContext>? configureBus = null
            , Action<IRabbitMqBusFactoryConfigurator, HostBuilderContext>? configureRabbitMq = null
            , Action<HostBuilderContext, IServiceCollection>? configureServices = null
            , params Assembly[] assemblies)
        {
            var hostBuilder = CreateDefaultBuilder(args);
            hostBuilder.ConfigureMassTransitConsumers(configureBus, configureRabbitMq, assemblies);
            if (configureServices is not null)
            {
                hostBuilder.ConfigureServices(configureServices);
            }
            return hostBuilder;
        }

        public static IHostBuilder ConfigureMassTransitConsumers(this IHostBuilder hostBuilder,
            Action<IServiceCollectionBusConfigurator, HostBuilderContext>? configureBus,
            Action<IRabbitMqBusFactoryConfigurator, HostBuilderContext>? configureRabbitMq,
            params Assembly[] assemblies)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddMassTransitServicesFrom(c
                    => configureBus?.Invoke(c, context), c
                    => (configureRabbitMq ?? ConfigureRabbitMq).Invoke(c, context), assemblies);
                services.AddMassTransitHostedService(WaitUntilHostIsStarted);
            });
            return hostBuilder;
        }

        public static IHostBuilder CreateMassTransitHostBuilder
        (this Action<IServiceCollectionBusConfigurator, HostBuilderContext>? configureBus
            , Action<IRabbitMqBusFactoryConfigurator, HostBuilderContext>? configureRabbitMq = null
            , Action<HostBuilderContext, IServiceCollection>? configureServices = null
            , params string[] args) 
            => CreateMassTransitHostBuilder(args, configureBus, configureRabbitMq
                , configureServices, Assembly.GetEntryAssembly()!);

        public static IHostBuilder CreateBareMassTransitHostBuilder(this string[] args)
        {
            var hostBuilder = CreateMassTransitHostBuilder(null
                , (rabbitConfigurator, builder) =>
                {
                    var rmqSettings = builder.Configuration.RabbitMqConfig();
                    rabbitConfigurator.Host(rmqSettings.Host, rmqSettings.Path, cfg =>
                    {
                        cfg.Username(rmqSettings.UserName);
                        cfg.Password(rmqSettings.PassWord);
                    });
                }, null, args);
            return hostBuilder;
        }

        /// <summary> Registers the built-in hosted MassTransitHostedService so this can be run in a Windows Service </summary>
        /// <param name="args">Command Line Parameters to read and possibly override appConfig Settings</param>
        /// <param name="configureBus">Callback to configure the Bus</param>
        /// <param name="configureServices">to register additional Services;
        /// alternatively use <see cref="HostBuilder"/>.<see cref="HostBuilder.ConfigureServices"/></param>
        /// <param name="configureRabbitMq">Callback to configure RabbitMq</param>
        /// <param name="cancel"></param>
        /// <param name="assemblies">List of Assemblies to register Consumers</param>
        public static async Task<IHost> StartMassTransitHost(this string[] args
            , Action<HostBuilderContext, IServiceCollection>? configureServices = null
            , Action<IRabbitMqBusFactoryConfigurator, HostBuilderContext>? configureRabbitMq = null
            , Action<IServiceCollectionBusConfigurator, HostBuilderContext>? configureBus = null
            , CancellationToken cancel = default
            , params Assembly[] assemblies)
        {
            var hostBuilder = args.CreateMassTransitHostBuilder(configureBus, configureRabbitMq, configureServices, assemblies);
            var host = hostBuilder.Build();
            await host.StartAsync(cancel).ConfigureAwait(false);
            return host;
        }

        /// <summary> Default Configuration of RabbitMq from the <see cref="RabbitMqConfig"/> Configuration
        /// in appSettings.json </summary>
        public static void ConfigureRabbitMq(this IRabbitMqBusFactoryConfigurator rabbitConfigurator
            , HostBuilderContext context)
        {
            var rmqSettings = context.Configuration.RabbitMqConfig();

            rabbitConfigurator.Host(rmqSettings.Host, rmqSettings.Path, cfg =>
            {
                cfg.Username(rmqSettings.UserName);
                cfg.Password(rmqSettings.PassWord);
            });

            rabbitConfigurator.UseConcurrencyLimit(rmqSettings.ConcurrencyLimit);

            rabbitConfigurator.ConfigureJsonSerializer(settings =>
            {
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                return settings;
            });
        }

    }
}