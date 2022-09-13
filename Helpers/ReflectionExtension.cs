using System.Reflection;

namespace MyORMInMemory.Helpers
{
    public static class ReflectionExtension
    {
        public static object? CallMethodGenericByReflection<T, G>(this T thing, string method, object?[]? args)
        {
            return thing.CallMethodByReflection<T>(method: method, generics: new Type[] { typeof(G) }, argsTypes: args?.Select(s => s?.GetType()).ToArray(), args: args);
        }

        public static object? CallMethodByReflection<T>(this T thing, string method)
        {
            return thing.CallMethodByReflection<T>(method, null);
        }

        public static object? CallMethodByReflection<T>(this T thing, string method, object?[]? args)
        {
            return thing.CallMethodByReflection<T>(method: method, generics: null, argsTypes: args?.Select(s => s?.GetType()).ToArray(), args: args);
        }

        public static object? CallMethodByReflection<T>(this T thing, string method, Type?[]? generics, Type?[]? argsTypes, object?[]? args)
        {
#pragma warning disable
            MethodInfo? methodInfo = null;

            methodInfo = thing.GetType().GetMethod(method, argsTypes ?? new Type[] { });

            if (argsTypes != null && argsTypes.Length > 0)
            {
                if (methodInfo == null)
                    ThrowMissingMethodException($"The type {thing.GetType().Name} do not have a method called {method} with {argsTypes?.Length} args");
            }


            if (generics != null && generics.Length > 0)
            {
                methodInfo = methodInfo.MakeGenericMethod(generics) ?? thing.GetType().GetMethod(method, new Type[] { }).MakeGenericMethod(generics);

                if (methodInfo == null)
                    ThrowMissingMethodException($"The type {thing.GetType().Name} do not have a method called {method} with {generics?.Length} generics args");
            }

            if (methodInfo == null)
                ThrowMissingMethodException($"The type {thing.GetType().Name} do not have a method called {method}");

            return methodInfo?.Invoke(thing, args ?? new object[] { });
#pragma warning enable

            void ThrowMissingMethodException(string msg) => throw new MissingMethodException(msg);
        }


        public static PropertyInfo[]? GetPublicProperties(Type thing, Func<PropertyInfo, bool> expression)
        {
            return thing.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(expression)
               .ToArray();
        }

        public static PropertyInfo? GetPublicProperty(Type thing, Func<PropertyInfo, bool> expression)
        {
            return thing.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(expression)
               .FirstOrDefault();
        }

        public static PropertyInfo[]? GetPublicProperties<T>(this T thing, Func<PropertyInfo, bool> expression)
        {
            return thing.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(expression)
               .ToArray();
        }

        public static PropertyInfo? GetPublicProperty<T>(this T thing, Func<PropertyInfo, bool> expression)
        {
            return thing.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(expression)
               .FirstOrDefault();
        }

    }
}
