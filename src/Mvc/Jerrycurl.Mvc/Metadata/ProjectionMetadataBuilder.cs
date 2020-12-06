﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Mvc.Metadata.Annotations;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Collections;

namespace Jerrycurl.Mvc.Metadata
{
    public class ProjectionMetadataBuilder : IMetadataBuilder<IProjectionMetadata>
    {
        public IProjectionMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Relation);

        private IProjectionMetadata GetMetadata(IMetadataBuilderContext context, IRelationMetadata relation)
        {
            IProjectionMetadata parent = context.GetMetadata<IProjectionMetadata>(relation.Parent.Identity.Name) ?? this.GetMetadata(context, relation.Parent);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(relation.Identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(relation.Identity));
        }

        public void Initialize(IMetadataBuilderContext context) => this.CreateAndAddMetadata(context, context.Relation, null);

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private IEnumerable<ProjectionMetadata> CreateProperties(IMetadataBuilderContext context, ProjectionMetadata parent)
        {
            foreach (IRelationMetadata property in parent.Relation.Properties)
                yield return this.CreateAndAddMetadata(context, property, parent);
        }

        private ProjectionMetadata CreateItem(IMetadataBuilderContext context, ProjectionMetadata parent)
        {
            if (parent.Relation.Item != null)
            {
                ProjectionMetadata metadata = this.CreateBaseMetadata(context, parent.Relation.Item);

                metadata.List = parent;

                context.AddMetadata<IProjectionMetadata>(metadata);

                return metadata;
            }

            return null;
        }

        private ProjectionMetadata CreateAndAddMetadata(IMetadataBuilderContext context, IRelationMetadata relation, IProjectionMetadata parent)
        {
            ProjectionMetadata metadata = this.CreateBaseMetadata(context, relation);

            context.AddMetadata<IProjectionMetadata>(metadata);

            metadata.Value = this.CreateValueMetadata(context, metadata, parent);

            return metadata;
        }

        private ProjectionMetadata CreateBaseMetadata(IMetadataBuilderContext context, IRelationMetadata relation)
        {
            ProjectionMetadata metadata = new ProjectionMetadata(relation);

            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Item = this.CreateItem(context, metadata);
            metadata.Flags = this.GetFlags(metadata);

            this.CreateTableMetadata(metadata);

            return metadata;
        }

        private IProjectionMetadata CreateValueMetadata(IMetadataBuilderContext context, IProjectionMetadata metadata, IProjectionMetadata parent)
        {
            if (parent == null)
                return metadata;

            IReferenceMetadata referenceMetadata = parent.Identity.Lookup<IReferenceMetadata>();

            foreach (IReference reference in referenceMetadata.References.Where(r => r.HasFlag(ReferenceFlags.Foreign)))
            {
                int valueIndex = reference.Key.Properties.IndexOf(m => m.Identity.Equals(metadata.Identity));

                if (valueIndex > -1)
                {
                    IReferenceMetadata valueMetadata = reference.Other.Key.Properties[valueIndex];

                    return this.GetMetadata(context, valueMetadata.Relation);
                }
            }

            return metadata;
        }

        private void CreateTableMetadata(ProjectionMetadata metadata)
        {
            ITableMetadata table = metadata.Identity.Lookup<ITableMetadata>();

            if (table != null)
            {
                metadata.Table = table.HasFlag(TableMetadataFlags.Table) ? table : table.Owner;
                metadata.Column = table.HasFlag(TableMetadataFlags.Column) ? table : null;
            }
        }

        private ProjectionMetadataFlags GetFlags(ProjectionMetadata metadata)
        {
            IdAttribute id = metadata.Relation.Annotations?.OfType<IdAttribute>().FirstOrDefault();
            OutAttribute out0 = metadata.Relation.Annotations?.OfType<OutAttribute>().FirstOrDefault();
            InAttribute in0 = metadata.Relation.Annotations?.OfType<InAttribute>().FirstOrDefault();

            IReferenceMetadata reference = metadata.Identity.Lookup<IReferenceMetadata>();
            ProjectionMetadataFlags flags = ProjectionMetadataFlags.None;

            if (id != null)
                flags |= ProjectionMetadataFlags.Identity;

            if (in0 != null || out0 != null)
            {
                flags |= in0 != null ? ProjectionMetadataFlags.Input : ProjectionMetadataFlags.None;
                flags |= out0 != null ? ProjectionMetadataFlags.Output : ProjectionMetadataFlags.None;
            }
            else if (id != null)
                flags |= ProjectionMetadataFlags.Output;
            else if (reference != null && reference.HasAnyFlag(ReferenceMetadataFlags.Key))
                flags |= ProjectionMetadataFlags.Input | ProjectionMetadataFlags.Output;
            else
                flags |= ProjectionMetadataFlags.Input;

            return flags;
        }
    }
}
