﻿using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations.Metadata
{
    [Serializable]
    public class MetadataBuilderException : Exception
    {
        public MetadataBuilderException()
        {

        }

        public MetadataBuilderException(string message)
            : base(message)
        {

        }

        public MetadataBuilderException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected MetadataBuilderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        internal static MetadataBuilderException InvalidContract(IRelationMetadata metadata, string message)
            => new MetadataBuilderException($"Invalid contract for {metadata.Identity}: {message}");
    }
}
