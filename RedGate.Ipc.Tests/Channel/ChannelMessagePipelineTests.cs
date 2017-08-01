using System;

using RedGate.Ipc.Channel;

using NUnit.Framework;
using Rhino.Mocks;

namespace RedGate.Ipc.Tests.Channel
{
    [TestFixture]
    public class ChannelMessagePipelineTests
    {
        [Test]
        public void CannotConstrustWithNullHandlers()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new ChannelMessagePipeline(null));
        }

        [Test]
        public void PipelineTerminatesWhenAHandlerReturnsNull()
        {
            var channelMessage = new ChannelMessage(0, new byte[0]);

            var handler1 = MockRepository.GenerateStub<IChannelMessageHandler>();
            handler1.Stub(h => h.Handle(channelMessage)).Return(null);

            var handler2 = MockRepository.GenerateStrictMock<IChannelMessageHandler>();
            handler2.Expect(h => h.Handle(channelMessage)).IgnoreArguments().Repeat.Never();

            var handlers = new[]
            {
                handler1, handler2
            };

            var pipeline = new ChannelMessagePipeline(handlers);

            var returnedMessage = pipeline.Handle(channelMessage);

            handler2.VerifyAllExpectations();

            Assert.Null(returnedMessage);
        }

        [Test]
        public void PipelineFeedsSuccessiveResultsToHandlersAndThenReturnsLast()
        {
            var channelMessage1 = new ChannelMessage(0, new byte[0]);
            var channelMessage2 = new ChannelMessage(1, new byte[1]);
            var channelMessage3 = new ChannelMessage(2, new byte[2]);

            var handler1 = MockRepository.GenerateStrictMock<IChannelMessageHandler>();
            handler1.Expect(h => h.Handle(channelMessage1)).Return(channelMessage2).Repeat.Once();

            var handler2 = MockRepository.GenerateStrictMock<IChannelMessageHandler>();
            handler2.Expect(h => h.Handle(channelMessage2)).Return(channelMessage3).Repeat.Once();

            var handlers = new[]
            {
                handler1, handler2
            };

            var pipeline = new ChannelMessagePipeline(handlers);

            var returnedMesage = pipeline.Handle(channelMessage1);

            handler1.VerifyAllExpectations();
            handler2.VerifyAllExpectations();

            Assert.AreSame(channelMessage3, returnedMesage);
        }
    }
}
