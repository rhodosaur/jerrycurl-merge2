﻿using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Relations
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
        public bool IsReadOnly => true;

        IField2 IField2.Model => this;

        public Model2(ISchema schema, object value)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            this.Metadata = schema.Require<IRelationMetadata>();
            this.Identity = new FieldIdentity(this.Metadata.Identity, this.Metadata.Identity.Name);
            this.Snapshot = value;
            this.Data = new FieldData(value);
        }

        public void Commit() { }
        public void Rollback() { }
        public void Update(object model) => throw BindingException2.From(this, "Cannot update model field.");

        public override string ToString() => this.Snapshot != null ? this.Snapshot.ToString() : "<null>";

        #region " Equality "
        public bool Equals(IField2 other) => Equality.Combine(this, other, m => m.Identity, m => m.Snapshot);
        public override bool Equals(object obj) => (obj is IField2 other && this.Equals(other));
        public override int GetHashCode() => this.Identity.GetHashCode();
        #endregion

    }
}
