using System;
using NUnit.Framework;
using RedGate.Ipc.Rpc;
using Rhino.Mocks;

namespace RedGate.Ipc.Tests
{
    [TestFixture]
    public class RpcMessageBrokerTests
    {
        [Test]
        public void Broker_matches_messages_on_queryId()
        {
            var writer = MockRepository.GenerateStub<IRpcMessageWriter>();
            var requestHandler = MockRepository.GenerateStub<IRpcRequestHandler>();
            var broker = new RpcMessageBroker(writer, requestHandler);

            var queryId = Guid.NewGuid().ToString();

            var request = new RpcRequest(queryId, "Interface", "Method", new[] { "arg" });
            var response = new RpcResponse(queryId, "ReturnValue");
            using (var token = new RequestToken())
            {
                broker.BeginRequest(request, token);
                broker.HandleInbound(response);

                var handled = token.Completed.WaitOne(0);
                Assert.True(handled, "The request should have been marked as handled");
                Assert.AreEqual("ReturnValue", token.Response.ReturnValue);
            }
        }

        [Test]
        public void Broker_does_not_match_unrelated_queries()
        {
            var writer = MockRepository.GenerateStub<IRpcMessageWriter>();
            var requestHandler = MockRepository.GenerateStub<IRpcRequestHandler>();
            var broker = new RpcMessageBroker(writer, requestHandler);

            var queryId1 = Guid.NewGuid().ToString();
            var queryId2 = Guid.NewGuid().ToString();

            var request = new RpcRequest(queryId1, "Interface", "Method", new[] { "arg" });
            var response = new RpcResponse(queryId2, "ReturnValue");

            using (var token = new RequestToken())
            {
                broker.BeginRequest(request, token);
                broker.HandleInbound(response);

                var handled = token.Completed.WaitOne(0);
                Assert.False(handled, "The request should not have been marked as handled");
                Assert.Null(token.Response, "No response should have been set");
            }
        }
    }
}
