﻿using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Metadata
{
    public interface ITableMetadata : IMetadata
    {
        TableMetadataFlags Flags { get; }
        ITableMetadata MemberOf { get; }
        IReadOnlyList<ITableMetadata> Properties { get; }
        ITableMetadata Item { get; }

        IReadOnlyList<string> TableName { get; }
        string ColumnName { get; }
    }
}
