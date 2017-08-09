using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;

using RedGate.Ipc.Channel;

using static System.IO.Pipes.PipeAccessRights;
using static System.Security.AccessControl.AccessControlType;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipeEndpoint : IEndpoint
    {
        public event ChannelConnectedEventHandler ChannelConnected = delegate { };

        private readonly string m_PipeName;
        private Thread m_Worker;
        private bool m_Disposed;
        private NamedPipeServerStream m_CurrentListener;
        private readonly List<IChannelStream> m_ActiveConnections = new List<IChannelStream>();

        public NamedPipeEndpoint(
            string pipeName)
        {
            m_PipeName = pipeName;
        }

        public void Start()
        {
            if (m_Disposed) throw new ObjectDisposedException(typeof(NamedPipeEndpoint).FullName);
            if (m_Worker == null)
            {
                m_Worker = new Thread(Worker)
                {
                    Name = "NamedPipeEndpoint Listener",
                    IsBackground = true
                };
                m_Worker.Start();
            }
        }

        private void Worker()
        {
            var pipeSecurity = GetPipeSecurity();

            try
            {
                while (!m_Disposed)
                {
                    m_CurrentListener = new NamedPipeServerStream(
                        m_PipeName, 
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        2048,
                        2048,
                        pipeSecurity);

                    m_CurrentListener.WaitForConnection();
                    var channelStream = new ChannelStream(m_CurrentListener);
                    m_ActiveConnections.Add(channelStream);
                    channelStream.Disconnected += () => m_ActiveConnections.Remove(channelStream);
                    ChannelConnected(new ChannelConnectedEventArgs(Guid.NewGuid().ToString(),
                        channelStream));
                }
            }
            catch (IOException)
            {

            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException)
            {

            }
        }

        public void Dispose()
        {
            m_Disposed = true;
            try
            {
                m_CurrentListener?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                
            }
            var activeConnections = m_ActiveConnections.ToArray();
            foreach (var activeConnection in activeConnections)
            {
                try
                {
                    activeConnection.Dispose();
                }
                catch
                {
                    //
                }
            }
        }

        private static PipeSecurity GetPipeSecurity()
        {
            var me = WindowsIdentity.GetCurrent().Owner;
            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

            var anyoneCanReadAndWrite = new PipeAccessRule(everyone, ReadWrite, Allow);
            var iHaveFullControl = new PipeAccessRule(me, FullControl, Allow);

            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(anyoneCanReadAndWrite);
            pipeSecurity.AddAccessRule(iHaveFullControl);
            return pipeSecurity;
        }
    }
}
