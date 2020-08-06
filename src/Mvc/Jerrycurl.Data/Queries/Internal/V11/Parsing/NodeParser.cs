﻿using System;
using System.Collections.Generic;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.V11.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.V11.Parsers
{
    internal static class NodeParser
    {
        public static NodeTree Parse(ISchema schema, IEnumerable<ICacheValue> values)
        {
            NodeTree tree = new NodeTree();

            foreach (ICacheValue value in values)
                AddNode(tree, new MetadataIdentity(schema, value.Name));

            return tree;
        }

        private static void AddNode(NodeTree tree, MetadataIdentity identity)
        {
            IBindingMetadata metadata = identity.GetMetadata<IBindingMetadata>() ?? FindDynamicMetadata(identity);

            if (IsValidMetadata(metadata))
            {
                if (metadata.HasFlag(BindingMetadataFlags.Dynamic))
                    AddDynamicNode(tree, identity, metadata);
                else
                    AddStaticNode(tree, metadata);
            }
        }

        private static Node AddDynamicNode(NodeTree tree, MetadataIdentity identity, IBindingMetadata metadata)
        {
            AddStaticNode(tree, metadata);

            Node thisNode = tree.FindNode(identity);
            MetadataIdentity parentIdentity = identity.Pop();

            if (thisNode != null)
                return thisNode;
            else if (parentIdentity != null)
            {
                Node parentNode = tree.FindNode(parentIdentity) ?? AddDynamicNode(tree, parentIdentity, metadata);

                if (parentNode != null)
                {
                    thisNode = new Node(identity, metadata)
                    {
                        Flags = NodeFlags.Dynamic,
                    };

                    parentNode.Properties.Add(thisNode);
                    tree.Nodes.Add(thisNode);
                }
            }

            return thisNode;
        }

        private static Node AddStaticNode(NodeTree tree, IBindingMetadata metadata)
        {
            Node thisNode = tree.FindNode(metadata);

            if (thisNode != null)
                return thisNode;
            else if (metadata.HasFlag(BindingMetadataFlags.Item))
            {
                thisNode = new Node(metadata)
                {
                    Flags = NodeFlags.Item,
                };

                if (metadata.HasFlag(BindingMetadataFlags.Dynamic))
                    thisNode.Flags |= NodeFlags.Dynamic;

                if (metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                    thisNode.Flags |= NodeFlags.Result;

                tree.Nodes.Add(thisNode);
                tree.Items.Add(thisNode);
            }
            else
            {
                Node parentNode = tree.FindNode(metadata.Parent) ?? AddStaticNode(tree, metadata.Parent);

                if (parentNode != null)
                {
                    thisNode = new Node(metadata)
                    {
                        Flags = metadata.HasFlag(BindingMetadataFlags.Dynamic) ? NodeFlags.Dynamic : NodeFlags.None,
                    };

                    parentNode.Properties.Add(thisNode);
                    tree.Nodes.Add(thisNode);
                }
            }

            return thisNode;
        }

        private static bool IsValidMetadata(IBindingMetadata metadata) => (metadata != null && !metadata.MemberOf.HasFlag(BindingMetadataFlags.Model));
        private static IBindingMetadata FindDynamicMetadata(MetadataIdentity identity)
        {
            IBindingMetadata metadata = identity.GetMetadata<IBindingMetadata>();

            while (metadata == null && (identity = identity.Pop()) != null)
                metadata = identity.GetMetadata<IBindingMetadata>();

            if (metadata != null && metadata.HasFlag(BindingMetadataFlags.Dynamic))
                return metadata;

            return null;
        }
    }
}
