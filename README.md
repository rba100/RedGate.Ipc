# RedGate.Ipcserver 

Provides full duplex inter-process communication to .NET processes using a client-server model.

## Example

From the server-side

    var builder = new ServiceHostBuilder();
    builder.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
    builder.AddEndpoint(new TcpEndpoint(IPAddress.Any, 1234));
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

From the client-side

    var clientBuilder = new ClientBuilder();
    using(var client = clientBuilder.ConnectToNamedPipe("my-service-name")))
    {
        var proxy = client.CreateProxy<ISomeInterface>()
        proxy.DoThingOnServer();
    }

Methods called on a client-side proxy will sent to the server and executed on the registered delegate (`ServerImplementation` in this case).
The factory method provided will be called once per connection and the delegate handler cached until disconnection.

## Full duplex

On the client

    var builder = new ClientBuilder();
    builder.AddCallbackHandler<ICallback>(callback);

    using(var client = builder.ConnectToNamedPipe("my-service-name")))
    {
        // methods on 'callback' ICallback object may be called asynchronous by the server
    }

On the server

    var builder = new ServiceHostBuilder();
    builder.AddDuplexDelegateFactory<ITestInterface, ICallback>(DuplexBuilder);
    ...
    
    private void DuplexBuilder(ICallback callback)
    {
        // callback can be used now or persisted for later
        return new ServerImplementation();
    }

#### Remarks

The provided duplex builder method will be called at most once per connection, providing a new callback scoped to that connection. The factory method provided by the consumer should create a new instance of the delegate object for each connection, if any guarentee is required that the delegate and callback are scoped to the same connected client.

The behaviour on the client-side differs in that reconnection after connection failure is performed silently (if it can be done within the timeout interval) which means client-side proxies and registered callback handlers are not scoped to a single connection. This may cause problems if the service keeps state about each connection which the client relies on.

## Exceptions

If an unhandled exception is thrown by the server-side implementation it will be serialized and re-thrown on the client proxy. However in addition to any
exception types consumers may be expecting, the proxy can also throw `ChannelFaultedException` if a connection could not be established
within `client.ConnectionTimeoutMs` or if the `IRpcClient` was disposed.
In the event that a call cannot be satisfied by the connected server (e.g. there is no registered implementation, or the interface declaration differs)
then a proxy will throw `ContractMismatchException`.

Consumers can override `ChannelFaultedException` with an exception type of their choice using
`client.CreateProxy<ISomeInterface, MyPreferredException>()`. This can make exception handling easier
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

## Stateful consumers

If consumers keep state about connected parties, for example, if clients must call `proxy.SetName("MyName")` before calling `proxy.DoSomethingOnServer()`, then failure can occur when a client suffers a network outage and reconnects between the two calls.

To mitigate this, consumers can pass an initialisation routine to `client.CreateProxy<ISomeInterface>(Action<ISomeInterface> initialisationRoutine)`. This routine will be called against the proxy before any other methods on it are called if the underlying connection has not yet been used for that interface type.