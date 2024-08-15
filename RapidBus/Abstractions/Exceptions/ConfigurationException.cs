namespace RapidBus.Abstractions.Exceptions;

[Serializable]
public class ConfigurationException : RapidBusException
{
    internal ConfigurationException(string? message) : base(message) { }
}
