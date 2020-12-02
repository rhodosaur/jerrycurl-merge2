﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Language
{
    public static class SchemaExtensions
    {
        public static ISchemaStore Use(this ISchemaStore store, IBindingContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            BindingMetadataBuilder builder = store.Builders.FirstOfType<BindingMetadataBuilder>();

            builder?.Add(resolver);

            return store;
        }

        public static ISchemaStore Use(this ISchemaStore store, ITableContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            TableMetadataBuilder builder = store.Builders.FirstOfType<TableMetadataBuilder>();

            builder?.Add(resolver);

            return store;
        }
    }
}
