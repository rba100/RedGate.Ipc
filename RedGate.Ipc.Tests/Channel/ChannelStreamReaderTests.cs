using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RedGate.Ipc.Channel;
using Rhino.Mocks;

namespace RedGate.Ipc.Tests.Channel
{
    [TestFixture]
    public class ChannelStreamReaderTests
    {
        public static IEnumerable<TestCaseData> NullArgsTestCases()
        {
            var channelMessageStream = MockRepository.GenerateStub<IChannelMessageStream>();
            var channelMessageSerializer = MockRepository.GenerateStub<IChannelMessageSerializer>();
            var channelMessageHandler = MockRepository.GenerateStub<IChannelMessageHandler>();
            var taskLauncher = MockRepository.GenerateStub<ITaskLauncher>();

            yield return new TestCaseData((TestDelegate)(() => new ChannelMessageReader(
               null,
               channelMessageSerializer,
               channelMessageHandler,
               taskLauncher))).SetName("Null channelMessageStream");

            yield return new TestCaseData((TestDelegate)(() => new ChannelMessageReader(
               channelMessageStream,
               null,
               channelMessageHandler,
               taskLauncher))).SetName("Null channelMessageSerializer");

            yield return new TestCaseData((TestDelegate)(() => new ChannelMessageReader(
               channelMessageStream,
               channelMessageSerializer,
               null,
               taskLauncher))).SetName("Null channelMessageHandler");

            yield return new TestCaseData((TestDelegate)(() => new ChannelMessageReader(
               channelMessageStream,
               channelMessageSerializer,
               channelMessageHandler,
               null))).SetName("Null taskLauncher");
        }

        [TestCaseSource(nameof(NullArgsTestCases))]
        public void ConstructorNullArgs(TestDelegate ctor)
        {
            Assert.Throws<ArgumentNullException>(ctor);
        }

        [Test]
        public void DisposePassesThroughOnlyOnce()
        {
            var factory = new TestFactory();
            factory.ChannelMessageStream.Expect(s => s.Dispose()).Repeat.Once();

            var reader = factory.Create();

            reader.Dispose();
            reader.Dispose();

            factory.VerifyAllExpectations();
        }

        [TestCase(typeof(ObjectDisposedException))]
        [TestCase(typeof(ChannelFaultedException))]
        public void StreamDisposedExceptionDisposesReader(Type exceptionType)
        {
            var readCalled = new ManualResetEvent(false);
            var factory = new TestFactory();
            var exception = Activator.CreateInstance(exceptionType, new object[] { "message" });
            factory.ChannelMessageStream.Stub(s => s.Read())
                .Do(new Func<byte[]>(() =>
                {
                    readCalled.Set();
                    throw (Exception) exception;
                }));

            factory.ChannelMessageStream.Expect(s => s.Dispose()).Repeat.Once();

            var reader = factory.Create();
            reader.Start();

            if (!readCalled.WaitOne(2000))
            {
                reader.Dispose();
                Assert.Fail("Test timed out waiting for reader to dispose.");
            }
            else
            {
                factory.VerifyAllExpectations();
            }
        }

        [Test]
        public void ReaderWorkFlowTest()
        {
            // Reader starts thread that:
            //  takes bytes from stream
            //  takes channel message from serialiser
            //  launches task of
            //    handler takes channel message

            var handled = new ManualResetEvent(false);
            var bytes = new byte[] {1, 2, 3, 4};
            var channelMessage = new ChannelMessage(99, new byte[] { 1,2,3,4 });
            var factory = new TestFactory();


            factory.ChannelMessageStream.Stub(s => s.Read()).Return(bytes);
            factory.ChannelMessageSerializer.Stub(s => s.FromBytes(bytes)).Return(channelMessage);
            factory.TaskLauncher.Stub(l => l.StartShortTask(null)).IgnoreArguments().Do(new Action<Action>(action => action.Invoke()));

            var reader = factory.Create();

            factory.ChannelMessageHandler.Expect(h => h.Handle(channelMessage))
                .Do(new Func<ChannelMessage, ChannelMessage>(message =>
                {
                    handled.Set();
                    reader.Dispose(); // To terminate the reader thread.
                    return null;
                }))
                .Repeat.Once();

            reader.Start();

            if (!handled.WaitOne(2000))
            {
                reader.Dispose(); // To terminate the reader thread.
                Assert.Fail("Test timed out waiting for channelMessage to be processed.");
            }
            else
            {
                factory.VerifyAllExpectations();
            }
        }

        private class TestFactory
        {
            public IChannelMessageStream ChannelMessageStream = MockRepository.GenerateMock<IChannelMessageStream>();
            public IChannelMessageSerializer ChannelMessageSerializer = MockRepository.GenerateMock<IChannelMessageSerializer>();
            public IChannelMessageHandler ChannelMessageHandler = MockRepository.GenerateMock<IChannelMessageHandler>();
            public ITaskLauncher TaskLauncher = MockRepository.GenerateMock<ITaskLauncher>();

            public ChannelMessageReader Create()
            {
                return new ChannelMessageReader(ChannelMessageStream, ChannelMessageSerializer, ChannelMessageHandler, TaskLauncher);
            }

            public void VerifyAllExpectations()
            {
                ChannelMessageStream.VerifyAllExpectations();
                ChannelMessageSerializer.VerifyAllExpectations();
                ChannelMessageHandler.VerifyAllExpectations();
                TaskLauncher.VerifyAllExpectations();
            }
        }
    }
}
;