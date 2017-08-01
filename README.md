# RedGate.Ipc

Provides full duplex interprocess communication to .NET processes using a client-server model.

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

Exceptions thrown by the serverside implementation will be thrown by the proxy, however in addition to any
expected exception the proxy can also throw `ChannelFaultedException` if a connection could not be established
within `client.ConnectionTimeoutMs` or if the client was disposed during or before a call a proxy it created.
In the event that a call cannot be satisfied by the connected server (e.g. there is no registered implementation)
then a proxy can throw `InvalidOperationException`.

Consumers can override `ChannelFaultedException` with an exception type of their choice using
`client.CreateProxy<ISomeInterface,MyPreferredException>()`. This can make exception handling easier
in the consuming archetecture. `InvalidOperationException` cannot be overridden, but should hopefully be
a very exceptional case during development or with backwards compatability.

## Versioning API

Internally the framework uses the assembly qualified name of proxy's interface type to match requests against 
implementations registered on the server.
This can cause a problem if the interface asked for by the client isn't the exact same interface registered on the server.
For example if the version of the assembly defining the interface changes it will be necessary to tell the `ServiceManager`
what aliases to expect for backwards comptability. Aliases are a subtring match from the start of the interface name.

   var sm = new ServiceManager();
   sm.AddEndpoint(new NamedPipeEndpoint("my-service-name"));
   sm.Register<MySoftware.Server.ISomeInterface>(new ServerImplementation());
   sm.RegisterTypeAlias("MySoftware.Client.ISomeInterface", typeof(MySoftware.Server.ISomeInterface));
   sm.Start();

In the above example, clients requsting calls to `MySoftware.Client.ISomeInterface, MySoftware.Client 1.0.0.0, PublicKey=...`
and any other interface starting with `MySoftware.Client.ISomeInterface` would be serviced by `ServerImplementation()`.
It is obviously important that the interfaces are functionally identical or an `InvalidOperationException` will be thrown.