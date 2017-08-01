using System;
using System.IO;
using NUnit.Framework;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;
using Rhino.Mocks;

namespace RedGate.Ipc.Tests.Channel
{
    [TestFixture]
    public class ChannelStreamTests
    {
        [Test]
        public void DisconnectedEventFiresImmediatelyIfDisposed()
        {
            var stream = MockRepository.GenerateStub<Stream>();
            var channelStream = new ChannelStream(stream);
            channelStream.Dispose();
            bool called = false;
            var eventHandler = new DisconnectedEventHandler(() => called = true);
            channelStream.Disconnected += eventHandler;
            Assert.True(called, "Expected event to have been fired");
        }

        [Test]
        public void DisconnectedEventFiresAfterDispose()
        {
            var stream = MockRepository.GenerateStub<Stream>();
            var channelStream = new ChannelStream(stream);
            bool called = false;
            var eventHandler = new DisconnectedEventHandler(() => called = true);
            channelStream.Disconnected += eventHandler;
            Assert.False(called, "Disconnected should not have fired before Dispose() was called.");
            channelStream.Dispose();
            Assert.True(called, "Expected event to have been fired");
        }

        [Test]
        public void DisconnectedEventHandlerCanBeRemoved()
        {
            var stream = MockRepository.GenerateStub<Stream>();
            var channelStream = new ChannelStream(stream);
            bool called = false;
            var eventHandler = new DisconnectedEventHandler(() => called = true);
            channelStream.Disconnected += eventHandler;
            channelStream.Disconnected -= eventHandler;
            channelStream.Dispose();
            Assert.False(called, "Event handler should not have been called.");
        }

        [TestCase(typeof(IOException))]
        [TestCase(typeof(ObjectDisposedException))]
        public void ReadFailureThrowsChannelFaultedException(Type exceptionType)
        {
            var stream = MockRepository.GenerateStub<Stream>();
            stream.Stub(s => s.Read(null, 0, 0))
                .IgnoreArguments()
                .Throw(Activator.CreateInstance(exceptionType, string.Empty) as Exception);

            var channelStream = new ChannelStream(stream);
            var buffer = new byte[1];

            Assert.That(() => { channelStream.Read(buffer, 0, 1); },
                Throws.Exception.TypeOf<ChannelFaultedException>());
        }

        [TestCase(typeof(IOException))]
        [TestCase(typeof(ObjectDisposedException))]
        public void WriteFailureThrowsChannelFaultedException(Type exceptionType)
        {
            var stream = MockRepository.GenerateStub<Stream>();
            stream.Stub(s => s.Write(null, 0, 0))
                .IgnoreArguments()
                .Throw(Activator.CreateInstance(exceptionType, string.Empty) as Exception);

            var channelStream = new ChannelStream(stream);
            var buffer = new byte[1];

            Assert.That(() => { channelStream.Write(buffer, 0, 1); },
                Throws.Exception.TypeOf<ChannelFaultedException>());
        }

        [Test]
        public void ReadPassThrough()
        {
            var stream = MockRepository.GenerateStrictMock<Stream>();
            byte[] buffer = new byte[1];
            stream.Expect(s => s.Read(buffer, 0, 1)).Repeat.Once().Return(42);
            var channelStream = new ChannelStream(stream);
            var returned = channelStream.Read(buffer, 0, 1);
            Assert.AreEqual(42, returned);
            stream.VerifyAllExpectations();
        }

        [Test]
        public void WritePassThrough()
        {
            var stream = MockRepository.GenerateMock<Stream>();
            byte[] buffer = new byte[1];
            stream.Expect(s => s.Write(buffer, 0, 1)).Repeat.Once();
            var channelStream = new ChannelStream(stream);
            channelStream.Write(buffer, 0, 1);
            stream.VerifyAllExpectations();
        }

        [Test]
        public void DisposePassThrough()
        {
            var stream = MockRepository.GenerateStrictMock<Stream>();
            stream.Expect(s => s.Dispose()).Repeat.Once();
            var channelStream = new ChannelStream(stream);
            channelStream.Dispose();
            stream.VerifyAllExpectations();
        }

        [Test]
        public void DisposeDoesNotPassThroughIfCustomDisposeAction()
        {
            var stream = MockRepository.GenerateStrictMock<Stream>();
            stream.Expect(s => s.Dispose()).Repeat.Never();
            var channelStream = new ChannelStream(stream, () => { });
            channelStream.Dispose();
            stream.VerifyAllExpectations();
        }

        [Test]
        public void DisposeCustomDisposeActionGetsCalledOnDispose()
        {
            var stream = MockRepository.GenerateStub<Stream>();
            var called = false;
            var channelStream = new ChannelStream(stream, () => { called = true; });
            Assert.False(called, "Custom dispose action should not have been called yet.");
            channelStream.Dispose();
            Assert.True(called, "Custom dispose action should have been called.");
        }
    }
}
