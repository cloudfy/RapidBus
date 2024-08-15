namespace RabidBus.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class IntegrationEventAttribute(string name, string? queue = null) : Attribute
{
    /// <summary>
    /// Gets the name of the event.
    /// </summary>
    public string Name { get; } = name;
    /// <summary>
    /// 
    /// </summary>
    public string? Queue { get; } = queue;
}