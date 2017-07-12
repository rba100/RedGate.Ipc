using System;
using System.Threading;
using System.Threading.Tasks;
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

        [Test]
        public void Send_unblocks_on_query_resolution()
        {
            var writer = MockRepository.GenerateStub<IRpcMessageWriter>();
            var requestHandler = MockRepository.GenerateStub<IRpcRequestHandler>();
            var broker = new RpcMessageBroker(writer, requestHandler);
            var queryId = Guid.NewGuid().ToString();

            var request = new RpcRequest(queryId, "Interface", "Method", new[] { "arg" });
            var response = new RpcResponse(queryId, "ReturnValue");

            RpcResponse returnedResponse = null;
            Task sendTask;

            // Call Send() and ensure the request obejct was passed to the underlying message writer
            // before simulating the inbound response.
            using (var requestSent = new ManualResetEvent(false))
            {
                // ReSharper disable once AccessToDisposedClosure
                writer.Stub(w => w.Write(request)).WhenCalled(call => { requestSent.Set(); });
                sendTask = Task.Run(() => { returnedResponse = broker.Send(request); });
                Assert.True(requestSent.WaitOne(2000),
                    "Timed out waiting for Send() to pass the request to the underlying message writer");
            }

            // Broker receives the response...
            broker.HandleInbound(response);

            // ...which should unlock .Send()
            var timedOut = !sendTask.Wait(2000);

            Assert.False(timedOut, "The broker.Send() should have unblocked.");
            Assert.NotNull(returnedResponse, "Send should have returned the RpcResponse");
            Assert.AreEqual(returnedResponse.QueryId, response.QueryId);
            Assert.AreEqual(returnedResponse.ReturnValue, response.ReturnValue);
        }
    }
}
