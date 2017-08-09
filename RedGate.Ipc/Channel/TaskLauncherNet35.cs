using System;
using System.Threading;
using RedGate.Ipc.Proxy;

namespace RedGate.Ipc.Channel
{
    public class TaskLauncherNet35 : ITaskLauncher
    {
        ///<remarks>
        /// Serious performance questions to be had here.
        /// ThreadPool can be very slow if jobs block on other jobs in the threadpool, which can happen
        /// in this framework if client and server are calling each other in response.
        /// 
        /// Creating new threads are slower to start up, but RPC requests aren't necessarily coming fast and furious.
        /// A potential drawback with creating a new threads per request is that concurrency is unbounded.
        /// If other protocols are introduced (e.g. file transfer) we could not want one thread per packet
        /// because we could end up with 100's of threads spun up which must then sync their execution order.
        ///
        /// NEW THREADS : if we really need to:
        /// var thread = new Thread(() => m_InboundHandler.Handle(channelMessage))
        /// {
        ///     IsBackground = true
        /// };
        /// thread.Start();
        ///
        /// ThreadPool : This is fine for low concurrent service requests, especially if void proxy methods
        ///              can be <see cref="ProxyNonBlockingAttribute"/>.
        /// ThreadPool.QueueUserWorkItem(o => func());
        ///</remarks>
        public void StartShortTask(Action func)
        {
            ThreadPool.QueueUserWorkItem(o => func());
        }
    }
}