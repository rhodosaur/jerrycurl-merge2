﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using System.Reflection;
using Jerrycurl.Data.Queries.Internal.IO.Writers;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal abstract class BaseParser
    {
        public ISchema Schema { get; set; }

        public BaseParser(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        protected bool IsResultNode(Node node) => node.Metadata.HasFlag(BindingMetadataFlags.Model);
        protected bool IsResultListNode(Node node) => (node.Metadata.Parent != null && node.Metadata.Parent.HasFlag(BindingMetadataFlags.Model));

        protected T FindData<T>(Node node, IEnumerable<T> header)
            where T : DataAttribute
        {
            foreach (T attribute in header)
            {
                MetadataIdentity metadata = new MetadataIdentity(node.Metadata.Identity.Schema, attribute.Name);

                if (metadata.Equals(node.Identity))
                    return attribute;
            }

            return null;
        }

        protected virtual DataReader CreateDataReader(BaseResult result, Node node)
        {
            if (node == null)
                return null;

            if (node.Data is ColumnAttribute column)
            {
                ColumnReader reader = new ColumnReader(node)
                {
                    Column = new ColumnMetadata(column.Name, column.Type, column.TypeName, column.Index),
                };

                this.AddHelper(result, reader);

                return reader;
            }
            else if (node.Data is AggregateAttribute aggregate)
            {
                return new AggregateReader(node)
                {
                    Attribute = aggregate,
                };
            }

            return null;
        }

        protected virtual BaseReader CreateReader(BaseResult result, Node node)
        {
            if (node == null)
                return null;
            else if (node.Data != null)
                return this.CreateDataReader(result, node);
            else if (node.Metadata.HasFlag(BindingMetadataFlags.Dynamic))
            {
                return new DynamicReader(node)
                {
                    Properties = node.Properties.Select(n => this.CreateReader(result, n)).ToList(),
                };
            }
            else
            {
                NewReader reader = new NewReader(node.Metadata)
                {
                    Properties = node.Properties.Select(n => this.CreateReader(result, n)).ToList(),
                };

                this.AddPrimaryKey(reader);

                return reader;
            }
        }

        private void InitializePrimaryKey(KeyReader primaryKey)
        {
            int index = 0;

            foreach (DataReader valueReader in primaryKey.Values)
            {
                valueReader.CanBeDbNull = false;
                valueReader.IsDbNull ??= Expression.Variable(typeof(bool), $"key_{index++}_isnull");
                valueReader.Variable ??= Expression.Variable(valueReader.Metadata.Type, $"key_{index}");
            }
        }

        private void AddPrimaryKey(NewReader binder)
        {
            IReferenceMetadata metadata = binder.Metadata.Identity.Lookup<IReferenceMetadata>();
            IEnumerable<IReferenceKey> primaryKeys = metadata?.Keys.Where(k => k.HasFlag(ReferenceKeyFlags.Primary)).ToList();
            IEnumerable<KeyReader> keys = primaryKeys?.Select(k => this.FindPrimaryKey(binder, k)).ToList();

            binder.PrimaryKey = keys?.NotNull().FirstOrDefault();

            if (binder.PrimaryKey != null)
                this.InitializePrimaryKey(binder.PrimaryKey);
        }

        protected KeyReader FindChildKey(NewReader binder, IReference reference) => this.FindKey(binder, reference.FindChildKey(), reference);
        protected KeyReader FindParentKey(NewReader binder, IReference reference) => this.FindKey(binder, reference.FindParentKey(), reference);
        protected KeyReader FindPrimaryKey(NewReader binder, IReferenceKey primaryKey) => this.FindKey(binder, primaryKey, null);

        private KeyReader FindKey(NewReader binder, IReferenceKey referenceKey, IReference reference)
        {
            if (referenceKey == null)
                return null;

            List<DataReader> values = new List<DataReader>();

            foreach (MetadataIdentity identity in referenceKey.Properties.Select(m => m.Identity))
            {
                DataReader value = binder.Properties.FirstOfType<DataReader>(m => m.Metadata.Identity.Equals(identity));

                values.Add(value);
            }

            if (values.All(v => v != null))
                return new KeyReader() { Values = values };

            return null;
        }

        private void AddHelper(BaseResult result, ColumnReader reader)
        {
            IBindingHelperContract helper = reader.Metadata.Helper;

            if (helper != null)
            {
                HelperWriter writer = new HelperWriter(reader.Metadata)
                {
                    Object = helper.Object,
                    BufferIndex = result.Helpers.Count,
                    Variable = Expression.Variable(helper.Type, $"helper_{result.Helpers.Count}"),
                };

                reader.Helper = writer.Variable;
                result.Helpers.Add(writer);
            }
        }
    }
}