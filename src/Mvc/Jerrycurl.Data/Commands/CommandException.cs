﻿using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Data.Commands
{
    [Serializable]
    public class CommandException : Exception
    {
        public CommandException()
        {

        }

        public CommandException(string message)
            : base(message)
        {

        }

        public CommandException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected CommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        internal static InvalidOperationException NoSchemaStoreAttached()
            => new InvalidOperationException("No schema store attached; use the CommandBuffer(ISchemaStore) constructor to use language features.");
    }
}
