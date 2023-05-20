namespace RabbitMqMessages
{
    public class UserCreated
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
