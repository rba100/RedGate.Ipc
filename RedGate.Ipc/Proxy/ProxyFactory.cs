using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace RedGate.Ipc.Proxy
{
    /// <summary>
    /// Generates objects that implement a given interface. When methods on the
    /// generated object are called, the call is deledated to the ICallHandler provider
    /// that consumers must implement.
    /// </summary>
    internal class ProxyFactory
    {
        private static ModuleBuilder s_ModuleBuilder;

        private static readonly Dictionary<Type, Type> s_InterfaceToProxyCache = new Dictionary<Type, Type>();

        /// <summary>
        /// Creates a proxy object for the given interface.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface to implement.
        /// </param>
        /// <param name="callHandler">
        /// ICallHandler.HandleCall will be called on any method call on the proxy object.
        /// </param>
        public object Create(Type interfaceType, ICallHandler callHandler)
        {
            if (!interfaceType.IsInterface)
            {
                throw new NotSupportedException("DynamicProxy can only generate proxies for interfaces.");
            }

            Type proxyType;
            lock (s_InterfaceToProxyCache)
            {
                if (!s_InterfaceToProxyCache.TryGetValue(interfaceType, out proxyType))
                {
                    proxyType = CreateInterfaceImplementation(interfaceType);
                    s_InterfaceToProxyCache.Add(interfaceType, proxyType);
                }
            }
            return Activator.CreateInstance(proxyType, callHandler);
        }

        /// <summary>
        /// Creates a proxy object for the given interface.
        /// </summary>
        /// <typeparam name="T">Must be an interface.</typeparam>
        /// <param name="callHandler">
        /// ICallHandler.HandleCall will be called on any method call on the proxy object.
        /// </param>
        public T Create<T>(ICallHandler callHandler)
        {
            if (callHandler == null) throw new ArgumentNullException(nameof(callHandler));
            var interfaceType = typeof(T);
            return (T)Create(interfaceType, callHandler);
        }

        private void InitialiseModuleBuilder()
        {
            if (s_ModuleBuilder == null)
            {
                var domain = Thread.GetDomain();
                var assemblyName = new AssemblyName
                {
                    Name = "DynamicProxies",
                    Version = new Version(1, 0, 0, 0)
                };
                var assemblyBuilder = domain.DefineDynamicAssembly(
                    assemblyName, AssemblyBuilderAccess.Run);

                s_ModuleBuilder = assemblyBuilder.DefineDynamicModule(
                    assemblyBuilder.GetName().Name, false);
            }
        }

        private TypeBuilder CreateClassBuilder(Type interfaceType)
        {
            InitialiseModuleBuilder();

            var typeBuilder = s_ModuleBuilder.DefineType(
                interfaceType.Name + "_Proxy",
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout);
            typeBuilder.AddInterfaceImplementation(interfaceType);

            // All proxy objects shall be disposable
            typeBuilder.AddInterfaceImplementation(typeof(IDisposable));

            return typeBuilder;
        }

        private Type CreateInterfaceImplementation(Type interfaceType)
        {
            var typeBuilder = CreateClassBuilder(interfaceType);

            // Add 'private readonly ICallHandler _callHandler'
            var callHandlerFieldBuilder = typeBuilder.DefineField("_callHandler", typeof(ICallHandler),
                FieldAttributes.Private | FieldAttributes.InitOnly);

            var getMethodFromHandle = typeof(MethodBase).GetMethod(
                "GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

            GenerateConstructor(typeBuilder, callHandlerFieldBuilder);

            var interfaces = interfaceType.GetInterfaces().Union(new[] { interfaceType }).ToArray();

            var properties = interfaces.SelectMany(i => i.GetProperties());
            var events = interfaces.SelectMany(i => i.GetEvents());
            var methods = interfaces.SelectMany(i => i.GetMethods())
                .Where(m => !m.IsSpecialName && m.Name != "Dispose");

            foreach (var methodInfo in methods)
            {
                GenerateProxyMethodFromInfo(methodInfo, typeBuilder, callHandlerFieldBuilder, getMethodFromHandle);
            }

            GenerateProxyDispose(typeBuilder, callHandlerFieldBuilder);

            foreach (var property in properties)
            {
                var propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.None,
                    property.PropertyType,
                    null);

                var setter = property.GetSetMethod();
                if (setter != null)
                {
                    propertyBuilder.SetSetMethod(GenerateProxyMethodFromInfo(
                        setter,
                        typeBuilder,
                        callHandlerFieldBuilder,
                        getMethodFromHandle));
                }

                var getter = property.GetGetMethod();
                if (getter != null)
                {
                    propertyBuilder.SetGetMethod(GenerateProxyMethodFromInfo(
                        getter,
                        typeBuilder,
                        callHandlerFieldBuilder,
                        getMethodFromHandle));
                }
            }

            foreach (var evt in events)
            {
                var eventBuilder = typeBuilder.DefineEvent(evt.Name, EventAttributes.None, evt.EventHandlerType);

                eventBuilder.SetAddOnMethod(GenerateProxyMethodFromInfo(
                    evt.GetAddMethod(),
                    typeBuilder,
                    callHandlerFieldBuilder,
                    getMethodFromHandle));

                eventBuilder.SetRemoveOnMethod(GenerateProxyMethodFromInfo(
                    evt.GetRemoveMethod(),
                    typeBuilder,
                    callHandlerFieldBuilder,
                    getMethodFromHandle));
            }

            return typeBuilder.CreateType();
        }

        private MethodBuilder GenerateProxyMethodFromInfo(
            MethodInfo methodInfo,
            TypeBuilder typeBuilder,
            FieldBuilder callHandlerFieldBuilder,
            MethodInfo getMethodFromHandle)
        {
            var basicAttributes =
                MethodAttributes.Public
                | MethodAttributes.Virtual;

            var specialNameAttributes = basicAttributes
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName;

            var attributes = methodInfo.IsSpecialName ? specialNameAttributes : basicAttributes;
            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = typeBuilder.DefineMethod(
                methodInfo.Name,
                attributes,
                methodInfo.ReturnType,
                parameters);

            var isAsync = methodInfo.GetCustomAttributes(true).Any(a => a.GetType() == typeof(ProxyNonBlockingAttribute));
            if (isAsync)
            {
                if (methodInfo.ReturnType != typeof(void))
                {
                    throw new InvalidOperationException(
                        $"Cannot create asynchronous proxy for '{methodInfo.DeclaringType}.{methodInfo.Name}' because it does not return null.");
                }
                var cac = typeof(ProxyNonBlockingAttribute).GetConstructor(Type.EmptyTypes);
                method.SetCustomAttribute(new CustomAttributeBuilder(cac, new object[0]));
            }
            var g = method.GetILGenerator();

            // var args = new object[parameters.Length]
            g.DeclareLocal(typeof(object[]));
            g.Emit(OpCodes.Ldc_I4, parameters.Length);
            g.Emit(OpCodes.Newarr, typeof(object));
            g.Emit(OpCodes.Stloc_0);

            // Copy each method paramter to the args array
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ldc_I4, index);
                g.Emit(OpCodes.Ldarg, index + 1);
                if (parameter.IsValueType) g.Emit(OpCodes.Box, parameter);
                g.Emit(OpCodes.Stelem_Ref);
            }

            // Call ICallHandler.HandleCall
            // ARG 0 is @_callHandler
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldfld, callHandlerFieldBuilder);
            // ARG 1 is MethodInfo for this method
            g.Emit(OpCodes.Ldtoken, methodInfo);
            g.Emit(OpCodes.Call, getMethodFromHandle);
            g.Emit(OpCodes.Castclass, typeof(MethodInfo));
            // ARG 2 is object[] args
            g.Emit(OpCodes.Ldloc_0);
            // The call
            g.Emit(OpCodes.Callvirt, typeof(ICallHandler).GetMethod("HandleCall"));

            // If method returns void then ditch the HandleCall result from the stack
            if (methodInfo.ReturnType == typeof(void))
            {
                g.Emit(OpCodes.Pop);
            }
            // otherwise unbox the return value if needed
            else if (methodInfo.ReturnType.IsValueType)
            {
                g.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
            }
            g.Emit(OpCodes.Ret);

            return method;
        }

        private void GenerateProxyDispose(TypeBuilder typeBuilder, FieldBuilder callHandlerFieldBuilder)
        {
            var disposeMethod = typeBuilder.DefineMethod(
                "Dispose",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(void),
                Type.EmptyTypes);
            var g = disposeMethod.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldfld, callHandlerFieldBuilder);
            g.Emit(OpCodes.Callvirt, typeof(ICallHandler).GetMethod("HandleDispose"));
            g.Emit(OpCodes.Ret);
        }

        private static void GenerateConstructor(TypeBuilder typeBuilder, FieldBuilder callbackFieldBuilder)
        {
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(ICallHandler) });

            var g = ctor.GetILGenerator();
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Call, typeof(object).GetConstructors().First());
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Stfld, callbackFieldBuilder);
            g.Emit(OpCodes.Ret);
        }
    }

    public interface ICallHandler
    {
        object HandleCall(MethodInfo methodInfo, object[] args);
        void HandleDispose();
    }

    /// <summary>
    /// Can only be used on void methods.
    /// When this is delclared on a method on interface declaration, calls to proxies of this interface
    /// will return immediately after the request is sent, without waiting for the opertion to complete on the server side.
    /// </summary>
    public class ProxyNonBlockingAttribute : Attribute
    {
    }
}