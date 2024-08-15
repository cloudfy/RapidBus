using RabidBus.Abstractions;

namespace RapidBus.Sample.Middleware;

public class SampleMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(EventContext context)
    {
        await _next(context);
    }
}
