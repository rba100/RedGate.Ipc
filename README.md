# RedGate.Ipc

Provides full duplex inter-process communication to .NET processes using a client-server model.

## Example

From the server side

    var builder = new ServiceHostBuilder();
    builder.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
    builder.AddDelegateFactory(type => type == typeof(ISomeInterface) ? new ServerImplementation() : null);

    var host = builder.Create();
    // listening
    host.Dispose();
	// closed

From the client side

    using(var client = new ClientBuilder().ConnectToNamedPipe("my-service-name")))
    {
        var proxy = client.CreateProxy<ISomeInterface>()
        proxy.DoThingOnServer();
    }

Methods called on a client side proxy will sent to the server and executed on the registered delegate (`ServerImplementation` in this case).
The factory method provided will be called once per connection and the delegate handler cached until disconnection.

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
	builder.AddDelegateFactory(GetHandler);
	builder.AddTypeAlias("MySoftware.Client.ISomeInterface", typeof(MySoftware.Server.ISomeInterface));

	private object GetHandler(Type delegateType)
	{
        return delegateType == typeof(MySoftware.Server.ISomeInterface) ? new ServerImplementation() : null;
	}

In the above example, a client interface that is interpreted as `MySoftware.Client.ISomeInterface, MySoftware.Client 1.0.0.0, PublicKey=...`
(or any other interface starting with `MySoftware.Client.ISomeInterface`) would be serviced by `ServerImplementation()`.
It is obviously important that the interfaces are functionally identical or an `ContractMismatchException` will be thrown.

Note: `RegisterAlias` maps a name to an *interface type*. Consumers must also use `Register` to map that interface type
to an *implementation* as has been done in the example.

## Full duplex

The server application can supply a `ClientConnected` event handler to obtain a handle to `IConnection`s when clients connect.
Using this connection object, the server can create proxies to services running on the client.
The important difference between the client-created and server-created proxies is that the client will attempt to reconnect
to the server in the event of connection failure, such as when the service is restarting, but server-created proxies will immediately
throw `ChannelFaultedException` and attempt no reconnection in the event of disconnection.

On the client

    var builder = new ClientBuilder();
	builder.Register<ICallback>(new ClientSideHandler());

    using(var client = builder.ConnectToNamedPipe("my-service-name")))
    {
        // client can handle server requests as soon as connection is established.
    }

On the server

    var builder = new ServiceHostBuilder();
    builder.ClientConnected += OnClientConnected;
    ...
    
    private void OnClientConnected(ConnectedEventArgs args)
    {
        var client = new SingleConnectionRpcClient(args.Connection);
        var proxy = client.CreateProxy<ICallback>();
        proxy.ClientCallback();
    }

## Naughty hacks

WCF provides the static variable `OperationContext.Current.SessionId` so that delegates can distinguish connected parties.
In this framework `Connection.RequestHandlerConnection.ConnectionId` can be used in the same way, but must only be called from the thread
used to invoke the delegate. I.e. in the simple example the implementation of `ServerImplementation.DoThingOnServer` may read `Connection.RequestHandlerConnection` but if it spins off a thread, that new thread must not read the variable as it is `[ThreadStatic]`.

Under consideration: removing this hack and providing a different mechanism for delegates to distinguish connected parties.