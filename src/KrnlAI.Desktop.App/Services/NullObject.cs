using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KrnlAI.Desktop.App.Services;

public static class NullObject
{
    public static T Create<T>() where T : class
    {
        return DispatchProxy.Create<T, NullDispatchProxy>();
    }

    private class NullDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            var returnType = targetMethod.ReturnType;

            if (returnType == typeof(void))
                return null;

            if (returnType == typeof(Task))
                return Task.CompletedTask;
            if (returnType == typeof(ValueTask))
                return default(ValueTask);

            if (returnType.IsGenericType)
            {
                var def = returnType.GetGenericTypeDefinition();
                if (def == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];
                    var defaultValue = SafeDefault(resultType);
                    var method = typeof(Task).GetMethod("FromResult", BindingFlags.Static | BindingFlags.Public)!
                        .MakeGenericMethod(resultType);
                    return method.Invoke(null, [defaultValue]);
                }

                if (def == typeof(ValueTask<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];
                    var defaultValue = SafeDefault(resultType);
                    var vtCtor = typeof(ValueTask<>).MakeGenericType(resultType)
                        .GetConstructor([resultType]);
                    return vtCtor?.Invoke([defaultValue]);
                }
            }

            if (returnType.IsValueType)
                return Activator.CreateInstance(returnType);

            return SafeDefault(returnType);
        }

        private static object? SafeDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (type == typeof(string))
                return "";

            if (type == typeof(byte[]))
                return Array.Empty<byte>();

            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType()!, 0);

            if (type.IsClass && !type.IsAbstract)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    try { return Activator.CreateInstance(type); }
                    catch { }
                }
                try { return RuntimeHelpers.GetUninitializedObject(type); }
                catch { }
            }

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IList<>)
                    || def == typeof(IReadOnlyCollection<>) || def == typeof(IReadOnlyList<>))
                {
                    var elem = type.GetGenericArguments()[0];
                    return Array.CreateInstance(elem, 0);
                }
            }

            return null;
        }
    }
}
