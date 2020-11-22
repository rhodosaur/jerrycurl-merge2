﻿using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Metadata
{
    public interface IReferenceMetadata : IMetadata
    {
        Type Type { get; }
        IReadOnlyList<IReference> References { get; }
        IReadOnlyList<IReferenceKey> Keys { get; }
        ReferenceMetadataFlags Flags { get; }
        IRelationMetadata Relation { get; }
        IReadOnlyList<IReferenceMetadata> Properties { get; }
        IReferenceMetadata Item { get; }
        IReadOnlyList<Attribute> Annotations { get; }
    }
}
