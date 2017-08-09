using System;

namespace RedGate.Ipc.Channel
{
    internal interface ITaskLauncher
    {
        void StartShortTask(Action func);
    }
}