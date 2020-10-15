﻿using Jerrycurl.Relations;
using System;
using System.Collections.Concurrent;
#if SQLSERVER_LEGACY
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif


namespace Jerrycurl.Vendors.SqlServer.Internal
{
    internal static class TvpCache
    {
        public static ConcurrentDictionary<RelationHeader, Action<SqlParameter, IRelation2>> Binders { get; } = new ConcurrentDictionary<RelationHeader, Action<SqlParameter, IRelation2>>();
    }
}
