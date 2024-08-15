namespace RapidBus.Abstractions.Exceptions;

[Serializable]
public abstract class RapidBusException : Exception
{
    protected RapidBusException(string? message) : base(message) { }
    protected RapidBusException(string? message, Exception? innerException)
            : base(message, innerException) { }
}