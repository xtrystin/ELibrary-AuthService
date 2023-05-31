namespace ELibrary_AuthService.ServiceBus;

public interface IMessagePublisher
{
    Task Publish<T>(T message);
}
