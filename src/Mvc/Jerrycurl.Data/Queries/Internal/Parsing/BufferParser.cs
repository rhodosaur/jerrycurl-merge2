﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Data.Queries.Internal.Binding;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class BufferParser
    {
        public ISchema Schema => this.Buffer.Schema;
        public BufferCache Buffer { get; }
        public QueryType Type { get; }

        public BufferParser(QueryType type, BufferCache cache)
        {
            this.Type = type;
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
        }

        public BufferTree Parse(IEnumerable<ColumnName> valueNames)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, valueNames);
            BufferTree tree = new BufferTree()
            {
                QueryType = this.Type,
                Schema = this.Schema,
            };

            this.MoveManyToOneNodes(nodeTree);

            this.AddWriters(tree, nodeTree, valueNames);
            this.AddAggregates(tree, nodeTree, valueNames);

            return tree;
        }

        private void MoveManyToOneNodes(NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Nodes.Where(n => this.HasOneAttribute(n.Metadata)))
            {
                Node parentNode = nodeTree.FindNode(node.Metadata.Parent);

                parentNode.Properties.Remove(node);

                nodeTree.Items.Add(node);
            }
        }

        private bool HasOneAttribute(IBindingMetadata metadata) => metadata.Annotations.OfType<OneAttribute>().Any();
        private bool HasOneAttribute(IReference reference) => reference.Metadata.Annotations.OfType<OneAttribute>().Any();

        private void AddAggregates(BufferTree tree, NodeTree nodeTree, IEnumerable<ColumnName> valueNames)
        {
            if (this.Type != QueryType.Aggregate)
                return;

            foreach (Node node in nodeTree.Nodes.Where(n => n.Depth == 1))
            {
                ColumnBinder binder = BindingHelper.FindValue(node, valueNames);

                if (binder != null)
                {
                    AggregateWriter writer = new AggregateWriter()
                    {
                        BufferIndex = this.Buffer.GetAggregateIndex(node.Identity),
                        Data = binder,
                    };

                    AggregateName x = new AggregateName(binder.Metadata.Identity.Name, isPrincipal: false);

                    tree.Aggregates.Add(writer);
                    tree.AggregateNames.Add(x);
                }
            }
        }

        private void AddWriters(BufferTree tree, NodeTree nodeTree, IEnumerable<ColumnName> valueNames)
        {
            IEnumerable<Node> itemNodes = this.Type == QueryType.List ? nodeTree.Items : nodeTree.Items.Where(n => n.Depth > 1);

            foreach (Node itemNode in itemNodes)
            {
                ListWriter writer = new ListWriter()
                {
                    Metadata = itemNode.Metadata.Parent,
                    Item = this.GetBinder(tree, itemNode, valueNames),
                };

                if (writer.Item is NewBinder newBinder)
                {
                    writer.PrimaryKey = newBinder.PrimaryKey;
                    newBinder.PrimaryKey = null;
                }

                if (!this.IsPrincipalList(itemNode.Metadata))
                    this.AddChildKey(writer);
                else if (this.Type == QueryType.Aggregate)
                    tree.AggregateNames.Add(new AggregateName(writer.Metadata.Identity.Name, isPrincipal: true));

                writer.Slot = this.AddSlot(tree, writer.Metadata, writer.JoinKey);

                if (writer.JoinKey != null)
                {
                    writer.BufferIndex = this.Buffer.GetChildIndex(writer.JoinKey.Metadata);
                    writer.IsOneToMany = this.HasOneAttribute(writer.JoinKey.Metadata);
                }
                    

                tree.Lists.Add(writer);
            }
        }

        private bool IsPrincipalList(IBindingMetadata metadata)
        {
            bool isPrincipal = metadata.Relation.Depth == (this.Type == QueryType.Aggregate ? 2 : 1);

            return (isPrincipal && !this.HasOneAttribute(metadata));
        }

        private ParameterExpression AddSlot(BufferTree tree, IBindingMetadata metadata, KeyBinder joinKey)
        {
            int slotIndex;
            Type variableType;
            if (joinKey == null)
            {
                slotIndex = metadata.Relation.Depth == 0 ? this.Buffer.GetResultIndex() : this.Buffer.GetListIndex(metadata.Identity);
                variableType = metadata.Composition.Construct.Type;
            }
            else
            {
                slotIndex = this.Buffer.GetParentIndex(joinKey.Metadata);
                variableType = this.GetDictionaryType(joinKey);
            }

            SlotWriter slotWriter = tree.Slots.FirstOrDefault(w => w.BufferIndex == slotIndex);

            if (slotWriter == null)
            {
                slotWriter = new SlotWriter()
                {
                    BufferIndex = slotIndex,
                    Variable = BindingHelper.Variable(variableType, metadata.Identity),
                    Metadata = metadata,
                    KeyType = joinKey?.KeyType,
                };

                tree.Slots.Add(slotWriter);
            }

            if (joinKey != null)
                joinKey.Slot = slotWriter.Variable;

            return slotWriter.Variable;
        }

        private void AddHelper(BufferTree tree, ColumnBinder binder)
        {
            if (binder.Metadata.Helper != null)
            {
                HelperWriter writer = new HelperWriter()
                {
                    Object = binder.Metadata.Helper.Object,
                    BufferIndex = tree.Helpers.Count,
                    Variable = BindingHelper.Variable(binder.Metadata.Helper.Type, binder),
                };

                binder.Helper = writer.Variable;

                tree.Helpers.Add(writer);
            }
        }

        private NodeBinder GetBinder(BufferTree tree, Node node, IEnumerable<ColumnName> valueNames)
        {
            ColumnBinder columnBinder = BindingHelper.FindValue(node, valueNames);

            if (columnBinder != null)
            {
                this.AddHelper(tree, columnBinder);

                return columnBinder;
            }
                
            NewBinder binder = new NewBinder()
            {
                Metadata = node.Metadata,
                Properties = node.Properties.Select(n => this.GetBinder(tree, n, valueNames)).ToList(),
                JoinKeys = new List<KeyBinder>(),
            };

            BindingHelper.AddPrimaryKey(binder);

            this.AddParentKeys(tree, binder);

            return binder;
        }

        private void InitializeKeyVariables(KeyBinder key)
        {
            foreach (ValueBinder value in key.Values)
            {
                value.IsDbNull ??= BindingHelper.Variable(typeof(bool), value);
                value.Variable ??= BindingHelper.Variable(value.KeyType, value);
            }
        }

        private void AddParentKeys(BufferTree tree, NewBinder binder)
        {
            IReferenceMetadata metadata = binder.Metadata.Identity.GetMetadata<IReferenceMetadata>();

            foreach (IReference reference in metadata.References.Where(this.IsValidReference))
            {
                KeyBinder key = BindingHelper.FindParentKey(binder, reference);

                if (key != null)
                {
                    IBindingMetadata childMetadata = reference.Other.Metadata.To<IBindingMetadata>();

                    JoinBinder joinBinder = new JoinBinder()
                    {
                        Metadata = childMetadata,
                        Array = key.Array ??= BindingHelper.Variable(typeof(ElasticArray), childMetadata.Identity),
                        ArrayIndex = this.Buffer.GetChildIndex(reference),
                        IsManyToOne = this.HasOneAttribute(childMetadata),
                    };

                    if (!joinBinder.IsManyToOne)
                        joinBinder.Metadata = joinBinder.Metadata.Parent;

                    binder.JoinKeys.Add(key);
                    binder.Properties.Add(joinBinder);

                    this.InitializeKeyVariables(key);
                    this.AddSlot(tree, childMetadata, key);
                }
            }
        }

        private void AddChildKey(ListWriter writer)
        {
            if (writer.Item is NewBinder binder)
            {
                IReferenceMetadata metadata = writer.Item.Metadata.To<IReferenceMetadata>();
                IReference reference = metadata.References.FirstOrDefault(this.IsValidReference);

                writer.JoinKey = BindingHelper.FindChildKey(binder, reference);

                if (writer.JoinKey != null)
                {
                    this.InitializeKeyVariables(writer.JoinKey);

                    writer.JoinKey.Array ??= BindingHelper.Variable(typeof(ElasticArray), writer.JoinKey.Metadata.Metadata.Identity);
                }
            }
        }

        private bool IsValidReference(IReference reference)
        {
            IReference childReference = reference.HasFlag(ReferenceFlags.Child) ? reference : reference.Other;

            return (childReference.HasFlag(ReferenceFlags.Many) || this.HasOneAttribute(childReference));
        }

        private Type GetDictionaryType(KeyBinder key)
            => typeof(Dictionary<,>).MakeGenericType(key.KeyType, typeof(ElasticArray));
    }
}
