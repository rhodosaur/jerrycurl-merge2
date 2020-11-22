﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Metadata
{
    internal class BindingMetadata : IBindingMetadata
    {
        public MetadataIdentity Identity { get; }
        public Type Type { get; }
        public MemberInfo Member { get; }
        public IRelationMetadata Relation { get; }

        public BindingMetadataFlags Flags { get; set; }
        public BindingMetadata Parent { get; set; }
        public Lazy<IReadOnlyList<BindingMetadata>> Properties { get; set; }
        public IReadOnlyList<Attribute> CustomAttributes { get; set; }
        public BindingMetadata Item { get; set; }
        public BindingMetadata MemberOf { get; set; }
        public IReadOnlyList<Attribute> Annotations => this.Relation.Annotations;

        public IBindingParameterContract Parameter { get; set; }
        public IBindingCompositionContract Composition { get; set; }
        public IBindingValueContract Value { get; set; }
        public IBindingHelperContract Helper { get; set; }

        IBindingMetadata IBindingMetadata.Parent => this.Parent;
        IReadOnlyList<IBindingMetadata> IBindingMetadata.Properties => this.Properties.Value;
        IBindingMetadata IBindingMetadata.Item => this.Item;
        IBindingMetadata IBindingMetadata.MemberOf => this.MemberOf;

        public BindingMetadata(IRelationMetadata relation)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            this.Identity = relation.Identity;
            this.Type = relation.Type;
            this.Member = relation.Member;
            this.Relation = relation;
        }

        public override string ToString() => $"IBindingMetadata: {this.Identity}";
    }
}
