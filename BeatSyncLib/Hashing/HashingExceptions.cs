using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace BeatSyncLib.Hashing
{
    public class HashingException : Exception
    {
        public HashingException()
        {
        }

        public HashingException(string message) : base(message)
        {
        }

        public HashingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HashingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class HashingTargetNotFoundException : HashingException
    {
        public HashingTargetNotFoundException()
        {
        }

        public HashingTargetNotFoundException(string message) : base(message)
        {
        }

        public HashingTargetNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HashingTargetNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
