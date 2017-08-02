using System;
using NUnit.Framework;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;
using Rhino.Mocks;

namespace RedGate.Ipc.Tests.Rpc
{
    [TestFixture]
    public class RpcRequestHandlerTests
    {
        [Test]
        public void HandleDelegatesToObjectFromTypeResolver()
        {
            var serialiser = MockRepository.GenerateStub<IJsonSerializer>();

            var handler = MockRepository.GenerateStrictMock<ITestInterface>();
            handler.Expect(h => h.VoidCall()).Repeat.Once();

            var typeResolver = MockRepository.GenerateStub<IDelegateProvider>();
            typeResolver.Stub(t => t.Get("TypeName")).Return(handler);

            var request = new RpcRequest("id", "TypeName", "VoidCall", new string[0]);

            var rpcRequestHandler = new RpcRequestHandler(typeResolver, serialiser);

            rpcRequestHandler.Handle(request);

            handler.VerifyAllExpectations();
        }

        [Test]
        public void HandleCachesObjectFromTypeResolver()
        {
            var serialiser = MockRepository.GenerateStub<IJsonSerializer>();

            var handler = MockRepository.GenerateStrictMock<ITestInterface>();
            handler.Expect(h => h.VoidCall()).Repeat.Twice();

            var typeResolver = MockRepository.GenerateStrictMock<IDelegateProvider>();
            typeResolver.Expect(t => t.Get("TypeName")).Return(handler).Repeat.Once();

            var request = new RpcRequest("id", "TypeName", "VoidCall", new string[0]);

            var rpcRequestHandler = new RpcRequestHandler(typeResolver, serialiser);

            rpcRequestHandler.Handle(request);
            rpcRequestHandler.Handle(request);

            handler.VerifyAllExpectations();
        }

        [Test]
        public void HandleSetsThreadStaticVariable()
        {
            var serialiser = MockRepository.GenerateStub<IJsonSerializer>();
            var connection = MockRepository.GenerateStub<IConnection>();
            var requestDelegate = MockRepository.GenerateStub<ITestInterface>();

            var typeResolver = MockRepository.GenerateStub<IDelegateProvider>();
            typeResolver.Stub(t => t.Get("TypeName")).Return(requestDelegate);

            var request = new RpcRequest("id", "TypeName", "VoidCall", new string[0]);

            var rpcRequestHandler = new RpcRequestHandler(typeResolver, serialiser)
            {
                OwningConnection = connection
            };

            requestDelegate.Stub(h => h.VoidCall()).Do(new Action(() => Assert.AreSame(connection, Connection.RequestHandlerConnection)));
            Assert.Null(Connection.RequestHandlerConnection);
            rpcRequestHandler.Handle(request);
        }

        [Test]
        public void HandlePolymorphicMethods()
        {
            var serialiser = MockRepository.GenerateStub<IJsonSerializer>();
            serialiser.Stub(s => s.Deserialize(typeof(string), "0")).Return("0");
            serialiser.Stub(s => s.Deserialize(typeof(int), "0")).Return(0);

            var requestDelegate = MockRepository.GenerateStrictMock<ITestInterface>();
            requestDelegate.Expect(d => d.Polymorphic("0")).Repeat.Once();
            requestDelegate.Expect(d => d.Polymorphic(0)).Repeat.Once();
            var typeResolver = MockRepository.GenerateStub<IDelegateProvider>();
            typeResolver.Stub(t => t.Get("TypeName")).Return(requestDelegate);

            var request1 = new RpcRequest("id", "TypeName", "Polymorphic_String", new[] { "0" });
            var request2 = new RpcRequest("id", "TypeName", "Polymorphic_Int32", new[] { "0" });

            var rpcRequestHandler = new RpcRequestHandler(typeResolver, serialiser);
            rpcRequestHandler.Handle(request1);
            rpcRequestHandler.Handle(request2);
        }
    }
}
