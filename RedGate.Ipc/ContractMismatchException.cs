using System;
using System.Runtime.Serialization;

namespace RedGate.Ipc
{
    [Serializable]
    public class ContractMismatchException : Exception
    {
        public ContractMismatchException()
        {
        }

        public ContractMismatchException(string message) : base(message)
        {
        }

        public ContractMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ContractMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
