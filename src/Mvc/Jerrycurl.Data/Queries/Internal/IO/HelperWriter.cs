﻿using System.Linq.Expressions;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class HelperWriter
    {
        public int BufferIndex { get; set; }
        public object Object { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
