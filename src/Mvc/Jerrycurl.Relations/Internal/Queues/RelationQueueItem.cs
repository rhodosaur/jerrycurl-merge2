﻿using System.Collections.Generic;
using Jerrycurl.Collections;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    public class RelationQueueItem<TList> : NameBuffer
    {
        public TList List { get; }
        public List<FieldArray> Cache { get; set; }

        public RelationQueueItem(TList list, string namePart, DotNotation notation)
            : base(namePart, notation)
        {
            this.List = list;
        }
    }
}
