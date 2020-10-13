﻿using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;
using Jerrycurl.Relations.V11.Language;

namespace Jerrycurl.Relations.V11
{
    [DebuggerDisplay("{Identity.Name}: {ToString(),nq}")]
    public class Model2 : IField2
    {
        public FieldIdentity Identity { get; }
        public object Snapshot { get; }
        public FieldType2 Type { get; } = FieldType2.Model;
        public IRelationMetadata Metadata { get; }
        public bool HasChanged => false;
        public IFieldData Data { get; }

        IField2 IField2.Model => this;

        public Model2(ISchema schema, object value)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            this.Identity = new FieldIdentity(new MetadataIdentity(schema, schema.Notation.Model()), schema.Notation.Model());
            this.Snapshot = value;
            this.Metadata = schema.Require<IRelationMetadata>();
            this.Data = new ModelData(value);
        }

        public void Commit() { }
        public void Rollback() { }
        public void Update(object model) => throw BindingException.FromField(this, "Cannot update model field.");

        public bool Equals(IField2 other) => Equality.Combine(this, other, m => m.Identity, m => m.Snapshot);
        public override bool Equals(object obj) => (obj is IField2 other && this.Equals(other));
        public override int GetHashCode() => this.Identity.GetHashCode();

        public override string ToString() => this.Snapshot != null ? this.Snapshot.ToString() : "<null>";

    }
}
