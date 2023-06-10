using MassTransit;
using ServiceBusMessages;

namespace ELibrary_AuthService.ServiceBus;


public class MessagePublisher : IMessagePublisher
{
    private readonly IBus _bus;
    private readonly IConfiguration _configuration;

    public MessagePublisher(IBus bus, IConfiguration configuration)
    {
        _bus = bus;
        _configuration = configuration;
    }

    public async Task Publish<T>(T message)
    {
        if (_configuration["Flags:UserRabbitMq"] == "1")
        {
            await _bus.Publish(message);
        }
        else
        {
            // Publisg to many queues -> because Basic Tier ASB allowed only 1-1 queues, no topics
            if (message is UserCreated)
            {
                var m = message as UserCreated;
                var userServiceMessage = new UserCreatedU() { UserId = m.UserId, FirstName = m.FirstName, LastName = m.LastName };
                var borrowingServiceMessage = new UserCreatedBr() { UserId = m.UserId, FirstName = m.FirstName, LastName = m.LastName };
                
                await _bus.Send(userServiceMessage);
                await _bus.Send(borrowingServiceMessage);
            }
            else if (message is UserDeleted)
            {
                var m = message as UserDeleted;
                var userServiceMessage = new UserDeletedU() { UserId = m.UserId };
                var borrowingServiceMessage = new UserDeletedBr() { UserId = m.UserId };

                await _bus.Send(userServiceMessage);
                await _bus.Send(borrowingServiceMessage);
            }
            else
            {
                await _bus.Send(message);   // send to one queue
            }
        }
    }
}
