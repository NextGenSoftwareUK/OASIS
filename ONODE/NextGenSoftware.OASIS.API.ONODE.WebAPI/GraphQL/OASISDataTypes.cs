using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL;

public static class OASISDataTypes
{
    // These contracts describe returned model data. They are not polymorphic
    // GraphQL interfaces and must not require concrete ORM implementations.
    public static IRequestExecutorBuilder AddOasisDataTypes(this IRequestExecutorBuilder builder)
    {
        var pending = new Stack<Type>(new[] { typeof(Query), typeof(Mutation) }
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(m => m.ReturnType));
        var visited = new HashSet<Type>();
        while (pending.TryPop(out var model))
        {
            if (!visited.Add(model)) continue;
            if (model.IsArray) pending.Push(model.GetElementType()!);
            foreach (var argument in model.GetGenericArguments()) pending.Push(argument);
            if (model.Namespace?.StartsWith("NextGenSoftware.OASIS", StringComparison.Ordinal) != true || model.IsEnum) continue;
            if (model.IsInterface && !model.ContainsGenericParameters && model != typeof(IHolon) && model != typeof(IAvatar))
                builder.AddType(typeof(OASISDataObjectType<>).MakeGenericType(model));
            foreach (var property in model.GetInterfaces().Append(model).SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)))
                if (property.PropertyType != typeof(NextGenSoftware.OASIS.API.Core.Holons.GlobalHolonData)) pending.Push(property.PropertyType);
        }
        return builder;
    }
}

public sealed class OASISDataObjectType<T> : ObjectType<T>
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.Name(typeof(T).Name);
        descriptor.BindFieldsExplicitly();
        foreach (var property in typeof(T).GetInterfaces().Append(typeof(T))
                     .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                     .GroupBy(p => p.Name).Select(g => g.First()))
        {
            if (property.PropertyType == typeof(NextGenSoftware.OASIS.API.Core.Holons.GlobalHolonData)) continue;
            var field = descriptor.Field(property);
            if (property.PropertyType == typeof(Dictionary<string, object>)) field.Type<AnyType>();
        }
    }
}
