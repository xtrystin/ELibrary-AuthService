using ELibrary_AuthService.RabbitMq;
using MassTransit;
using ServiceBusMessages;

namespace ELibrary_AuthService.ServiceBus;

public static class MassTransitCollection
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            if (configuration["Flags:UserRabbitMq"] == "1")   //todo change to preprocessor directive #if
            {
                RabbitMqOptions rabbitMqOptions = configuration.GetSection(nameof(RabbitMqOptions)).Get<RabbitMqOptions>();

                x.UsingRabbitMq((hostContext, cfg) =>
                {
                    cfg.Host(rabbitMqOptions.Uri, "/", c =>
                    {
                        c.Username(rabbitMqOptions.UserName);
                        c.Password(rabbitMqOptions.Password);
                    });
                });
            }
            else
            {
                // Azure Basic Tier - only 1-1 queues
                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(configuration["AzureServiceBusConnectionString"]);

                    /// Publishers configuration ///
                    // UserCreated
                    EndpointConvention.Map<UserCreatedU>(new Uri($"queue:{nameof(UserCreatedU)}"));
                    cfg.Message<UserCreatedU>(cfgTopology => cfgTopology.SetEntityName(nameof(UserCreatedU)));
                    EndpointConvention.Map<UserCreatedBr>(new Uri($"queue:{nameof(UserCreatedBr)}"));
                    cfg.Message<UserCreatedBr>(cfgTopology => cfgTopology.SetEntityName(nameof(UserCreatedBr)));

                    // UserDeleted
                    EndpointConvention.Map<UserDeletedU>(new Uri($"queue:{nameof(UserDeletedU)}"));
                    cfg.Message<UserDeletedU>(cfgTopology => cfgTopology.SetEntityName(nameof(UserDeletedU)));
                    EndpointConvention.Map<UserDeletedBr>(new Uri($"queue:{nameof(UserDeletedBr)}"));
                    cfg.Message<UserDeletedBr>(cfgTopology => cfgTopology.SetEntityName(nameof(UserDeletedBr)));
                });
            }

        });

        return services;
    }
}