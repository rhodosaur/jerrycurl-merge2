﻿using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model
{
    public class JsonBlog
    {
        [Json]
        public Blog Blog { get; set; }
    }
}