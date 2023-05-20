using MassTransit;

namespace ELibrary_AuthService.RabbitMq
{
    public static class RabbitMqCollection
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, RabbitMqOptions rabbitMqOptions)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((hostContext, cfg) =>
                {
                    cfg.Host(rabbitMqOptions.Uri, "/", c =>
                    {
                        c.Username(rabbitMqOptions.UserName);
                        c.Password(rabbitMqOptions.Password);
                    });
                });
            });

            return services;
        }
    }
}
