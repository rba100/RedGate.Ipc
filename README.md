# RedGate.Ipc

Provides full duplex inter-process communication to .NET processes using a client-server model.

## Example

From the server side

    var builder = new ServiceHostBuilder();
    builder.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
    builder.AddDelegateFactory(DelegateFactory);

    var host = builder.Create();
    // listening
    host.Dispose();
    // closed

    private void DelegateFactory(Type type)
    {
        if(type == typeof(ISomeInterface)) return new ServerImplementation();
        return null;
    }

From the client side

    var clientBuilder = new ClientBuilder();
    using(var client = clientBuilder.ConnectToNamedPipe("my-service-name")))
    {
        var proxy = client.CreateProxy<ISomeInterface>()
        proxy.DoThingOnServer();
    }

Methods called on a client side proxy will sent to the server and executed on the registered delegate (`ServerImplementation` in this case).
The factory method provided will be called once per connection and the delegate handler cached until disconnection.

## Full duplex

On the client

    var builder = new ClientBuilder();
    builder.AddCallbackHandler<ICallback>(new Callback());

    using(var client = builder.ConnectToNamedPipe("my-service-name")))
    {
        // client can handle server requests as soon as connection is established.
    }

On the server

    var builder = new ServiceHostBuilder();
    builder.AddDuplexDelegateFactory<ITestInterface, ICallback>(DuplexBuilder);
    ...
    
    private void DuplexBuilder(ICallback callback)
    {
        // use or persist callback for later.
        return new ServerImplementation();
    }

When a client attempts to invoke a method on `ITestInterface`, the framework will create a proxy for `ICallback` which the server can use to initiate communications and push things to the client asynchonously.

### Remarks

Server-side callback proxies are scoped to a single connection, which is guarenteed to be the same connection routed to the service delegate the consumer returns in the factory. When the client disconnects, the proxy with throw `ChannelFaultedException` when a method is called.

The behaviour on the client side differs in that both the proxy and the supplied callback are re-used accross reconnections.

## Exceptions

Exceptions thrown by the server-side implementation will be thrown by the proxy. However in addition to any
expected exception types, the proxy can also throw `ChannelFaultedException` if a connection could not be established
within `client.ConnectionTimeoutMs` or if the client was disposed.
In the event that a call cannot be satisfied by the connected server (e.g. there is no registered implementation)
then a proxy can throw `ContractMismatchException`.

Consumers can override `ChannelFaultedException` with an exception type of their choice using
`client.CreateProxy<ISomeInterface,MyPreferredException>()`. This can make exception handling easier
in the consuming architecture. `ContractMismatchException` cannot be overridden, but should hopefully be
a very exceptional case during development or with backwards compatibility.

## API Versioning

The framework uses the assembly qualified name of the interface type to match client requests against server implementations.
This can cause a problem if the interface asked for by the client isn't the exact same interface registered on the server.
For example if the assembly version or namespace of the interface changes it will be necessary to indicate what aliases to
expect for backwards compatibility. Aliases are a substring match from the start of the interface name.

    var builder = new ServiceHostBuilder();
    builder.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
    builder.AddDelegateFactory(DelegateFactory);
    builder.AddTypeAlias("MySoftware.Client.ISomeInterface", typeof(MySoftware.Server.ISomeInterface));

    private object DelegateFactory(Type delegateType)
    {
        return delegateType == typeof(MySoftware.Server.ISomeInterface) ? new ServerImplementation() : null;
    }

In the above example, a client interface that is interpreted as `MySoftware.Client.ISomeInterface, MySoftware.Client 1.0.0.0, PublicKey=...`
(or any other interface starting with `MySoftware.Client.ISomeInterface`) would be serviced by `ServerImplementation()`.
It is obviously important that the interfaces are functionally identical or an `ContractMismatchException` will be thrown.

Note: `RegisterAlias` maps a name to an *interface type*. Consumers must also use `Register` to map that interface type
to an *implementation* as has been done in the example.