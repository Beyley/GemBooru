using System.Reflection;
using Bunkum.Core;
using Bunkum.Core.Database;
using Bunkum.Core.Responses;
using Bunkum.Core.Services;
using Bunkum.Listener.Request;
using GemBooru.Attributes;
using NotEnoughLogs;

namespace GemBooru.Services;

public class RequiresInputService : Service
{
    internal RequiresInputService(Logger logger) : base(logger)
    {
    }

    public override Response? OnRequestHandled(ListenerContext context, MethodInfo method, Lazy<IDatabaseContext> database)
    {
        var attr = method.GetCustomAttribute<RequiresInputAttribute>();
        if (attr != null)
        {
            var input = context.Query["input"];
            if ((!attr.AllowEmpty && string.IsNullOrWhiteSpace(input)) || (attr.AllowEmpty && input == null))
                return new Response(attr.Query, statusCode: Continue);
        }
        
        return base.OnRequestHandled(context, method, database);
    }

    public override object? AddParameterToEndpoint(ListenerContext context, BunkumParameterInfo parameter, Lazy<IDatabaseContext> database)
    {
        if (parameter.Name == "input" && parameter.ParameterType == typeof(string))
            return context.Query["input"];
        
        return base.AddParameterToEndpoint(context, parameter, database);
    }
}