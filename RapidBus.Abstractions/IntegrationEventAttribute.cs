namespace RabidBus.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class IntegrationEventAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the event.
    /// </summary>
    public string Name { get; } = name;
}