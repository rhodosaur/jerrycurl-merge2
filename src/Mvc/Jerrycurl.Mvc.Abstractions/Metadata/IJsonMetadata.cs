﻿using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc.Metadata
{
    public interface IJsonMetadata : IMetadata
    {
        string Path { get; }
        bool IsRoot { get; }
        IJsonMetadata MemberOf { get; }
        IRelationMetadata Relation { get; }
    }
}
