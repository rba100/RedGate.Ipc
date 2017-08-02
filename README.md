# RedGate.Ipc

Provides full duplex inter-process communication to .NET processes using a client-server model.

## Simple example

From the server side

    var sm = new ServiceManager();
    sm.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
    sm.Register<ISomeInterface>(new ServerImplementation());
    sm.Start();

From the client side

	var client = RpcClient.CreateNamedPipeClient("my-service-name"))
	var proxy = client.CreateProxy<ISomeInterface>()
	proxy.DoThingOnServer();

Methods called on a client side proxy will block whilst the registered delegate is executed by the server,
with the return result being returned by the proxy.

## Exceptions

Exceptions thrown by the server-side implementation will be thrown by the proxy. However in addition to any
expected exception types, the proxy can also throw `ChannelFaultedException` if a connection could not be established
within `client.ConnectionTimeoutMs` or if the client was disposed.
In the event that a call cannot be satisfied by the connected server (e.g. there is no registered implementation)
then a proxy can throw `InvalidOperationException`.

Consumers can override `ChannelFaultedException` with an exception type of their choice using
`client.CreateProxy<ISomeInterface,MyPreferredException>()`. This can make exception handling easier
in the consuming architecture. `InvalidOperationException` cannot be overridden, but should hopefully be
a very exceptional case during development or with backwards compatibility.

## API Versioning

The framework uses the assembly qualified name of interface type to match client requests against server implementations.
This can cause a problem if the interface asked for by the client isn't the exact same interface registered on the server.
For example if the assembly version or namespace of the interface changes it will be necessary to tell the `ServiceManager`
what aliases to expect for backwards compatibility. Aliases are a substring match from the start of the interface name.

	var sm = new ServiceManager();
	sm.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
	sm.Register<MySoftware.Server.ISomeInterface>(new ServerImplementation());
	sm.RegisterAlias("MySoftware.Client.ISomeInterface", typeof(MySoftware.Server.ISomeInterface));
	sm.Start();

In the above example, a client interface that is interpreted as `MySoftware.Client.ISomeInterface, MySoftware.Client 1.0.0.0, PublicKey=...`
(or any other interface starting with `MySoftware.Client.ISomeInterface`) would be serviced by `ServerImplementation()`.
It is obviously important that the interfaces are functionally identical or an `InvalidOperationException` will be thrown.

Note: `RegisterAlias` maps a partial type name to an interface type. Consumers must also use `Register` to map that interface type
to an implementation.

## Registering service implementations on the server

In the simple example, a concrete object is passed the `ServiceManager` and this same instance will be used to satisfy all
requests whilst the server is running.

As an alternative strategy consumers can pass a dependency injector or factory method to the `ServiceManager` which will create
service delegates on demand, scoped to individual connected clients.

	public void StartServer()
	{
		_serviceManager = new ServiceManager();
		_serviceManager.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
		_serviceManager.RegisterDi(GetDelegate);
		_serviceManager.Start();
	}

	public object GetDelegate(Type type)
	{
		if(type == typeof(ISomeInterface)) return new ConnectionScopedHandler();
		return null; // return null if cannot satisfy type request.
	}

Under consideration: automatic disposing of `IDisposable` when connections disconnect.
