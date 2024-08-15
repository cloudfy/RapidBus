using Microsoft.Extensions.DependencyInjection;
using RabidBus.Abstractions;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RapidBus;

public static class UseEventMiddlewareExtensions
{
    private const string _invokeMethodName = "Invoke";
    private const string _invokeAsyncMethodName = "InvokeAsync";
    private readonly static List<Func<RequestDelegate, RequestDelegate>> _components = [];
    private readonly static MethodInfo _getServiceInfo
        = typeof(UseEventMiddlewareExtensions).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static RapidBusOptionsBuilder UseMiddleware<TMiddleware>(
        this RapidBusOptionsBuilder app
        , params object?[] args)
    {
        return app.UseMiddleware(typeof(TMiddleware), args);
    }

    private static RapidBusOptionsBuilder UseMiddleware(
        this RapidBusOptionsBuilder app
        , Type middleware
        , params object?[] args)
    {
        var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo? invokeMethod = null;
        foreach (var method in methods)
        {
            if (string.Equals(method.Name, _invokeMethodName, StringComparison.Ordinal) || string.Equals(method.Name, _invokeAsyncMethodName, StringComparison.Ordinal))
            {
                if (invokeMethod is not null)
                {
                    throw new InvalidOperationException();
                }

                invokeMethod = method;
            }
        }
        if (invokeMethod is null)
        {
            throw new InvalidOperationException();
        }

        if (!typeof(Task).IsAssignableFrom(invokeMethod.ReturnType))
        {
            throw new InvalidOperationException();
        }
        var parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(EventContext))
        {
            throw new InvalidOperationException();
        }

        var reflectionBinder = new ReflectionMiddlewareBinder(app, middleware, args, invokeMethod, parameters);
        return app.Use(reflectionBinder.CreateMiddleware);
    }

    internal static RapidBusOptionsBuilder Use(
        this RapidBusOptionsBuilder app
        , Func<RequestDelegate, RequestDelegate> middleware)
    {
        _components.Add(middleware);
        return app;
    }

    //internal static RequestDelegate BuildByRequestDelegate()
    //{
    //    RequestDelegate app = context => {
    //        return Task.CompletedTask;
    //    };

    //    for (var c = _components.Count - 1; c >= 0; c--)
    //    {
    //        app = _components[c](app);
    //    }

    //    return app;
    //}

    /// <summary>
    /// Builds the execution pipeline for the application, and starts executing the pipeline outside > in.
    /// </summary>
    /// <param name="innerEventDelegate">Required. Task of the inner item to execute in the pipeline.</param>
    /// <returns></returns>
    public static RequestDelegate BuildByRequestDelegate(Func<Task> innerEventDelegate)
    {
        RequestDelegate app = context => {
            return innerEventDelegate();
        };

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }

        return app;
    }

    private sealed class ReflectionMiddlewareBinder
    {
        private readonly RapidBusOptionsBuilder _app;
        private readonly Type _middleware;
        private readonly object?[] _args;
        private readonly MethodInfo _invokeMethod;
        private readonly ParameterInfo[] _parameters;

        public ReflectionMiddlewareBinder(
            RapidBusOptionsBuilder app,
            Type middleware,
            object?[] args,
            MethodInfo invokeMethod,
            ParameterInfo[] parameters)
        {
            _app = app;
            _middleware = middleware;
            _args = args;
            _invokeMethod = invokeMethod;
            _parameters = parameters;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public RequestDelegate CreateMiddleware(RequestDelegate next)
        {
            var ctorArgs = new object[_args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(_args, 0, ctorArgs, 1, _args.Length);
            var instance = ActivatorUtilities.CreateInstance(_app.Services, _middleware, ctorArgs);
            if (_parameters.Length == 1)
            {
                return (RequestDelegate)_invokeMethod.CreateDelegate(typeof(RequestDelegate), instance);
            }

            // Performance optimization: Use compiled expressions to invoke middleware with services injected in Invoke.
            // If IsDynamicCodeCompiled is false then use standard reflection to avoid overhead of interpreting expressions.
            var factory = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled
                ? CompileExpression<object>(_invokeMethod, _parameters)
                : ReflectionFallback<object>(_invokeMethod, _parameters);

            return context => {
                var serviceProvider = context.RequestServices ?? _app.Services;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException();
                }

                return factory(instance, context, serviceProvider);
            };
        }

        public override string ToString() => _middleware.ToString();
    }

    private static Func<T, EventContext, IServiceProvider, Task> ReflectionFallback<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
    {
        Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported, "Use reflection fallback when dynamic code is not supported.");

        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(_invokeMethodName);
            }
        }

        return (middleware, context, serviceProvider) => {
            var methodArguments = new object[parameters.Length];
            methodArguments[0] = context;
            for (var i = 1; i < parameters.Length; i++)
            {
                methodArguments[i] = GetService(serviceProvider, parameters[i].ParameterType, methodInfo.DeclaringType!);
            }

            return (Task)methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null)!;
        };
    }

    private static Func<T, EventContext, IServiceProvider, Task> CompileExpression<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
    {
        Debug.Assert(RuntimeFeature.IsDynamicCodeSupported, "Use compiled expression when dynamic code is supported.");

        // If we call something like
        //
        // public class Middleware
        // {
        //    public Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
        //    {
        //
        //    }
        // }
        //

        // We'll end up with something like this:
        //   Generic version:
        //
        //   Task Invoke(Middleware instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return instance.Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        //   Non generic version:
        //
        //   Task Invoke(object instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return ((Middleware)instance).Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        var middleware = typeof(T);

        var httpContextArg = Expression.Parameter(typeof(EventContext), "httpContext");
        var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var instanceArg = Expression.Parameter(middleware, "middleware");

        var methodArguments = new Expression[parameters.Length];
        methodArguments[0] = httpContextArg;
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(_invokeMethodName);
            }

            var parameterTypeExpression = new Expression[]
            {
                providerArg,
                Expression.Constant(parameterType, typeof(Type)),
                Expression.Constant(methodInfo.DeclaringType, typeof(Type))
            };

            var getServiceCall = Expression.Call(_getServiceInfo, parameterTypeExpression);
            methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
        }

        Expression middlewareInstanceArg = instanceArg;
        if (methodInfo.DeclaringType != null && methodInfo.DeclaringType != typeof(T))
        {
            middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodInfo.DeclaringType);
        }

        var body = Expression.Call(middlewareInstanceArg, methodInfo, methodArguments);

        var lambda = Expression.Lambda<Func<T, EventContext, IServiceProvider, Task>>(body, instanceArg, httpContextArg, providerArg);

        return lambda.Compile();
    }

    private static object GetService(IServiceProvider sp, Type type, Type middleware)
    {
        var service = sp.GetService(type);
        if (service == null)
        {
            throw new InvalidOperationException($"{type.Name}, {middleware.Name}");
        }

        return service;
    }
}