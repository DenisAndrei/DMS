namespace DeviceManagement.Api.Domain.Exceptions;

public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, int id)
        : base($"{entityName} with id {id} was not found.")
    {
        EntityName = entityName;
        Id = id;
    }

    public string EntityName { get; }

    public int Id { get; }
}
