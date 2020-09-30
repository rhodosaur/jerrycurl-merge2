﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Jerrycurl.Relations.Internal.V11.Caching;
using Jerrycurl.Relations.Internal.V11.Compilation;
using Jerrycurl.Relations.Internal.V11.Enumerators;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.V11
{
    public class RelationReader : IRelationReader
    {
        public IRelation3 Relation => this.enumerator.Current;
        public int Degree { get; private set; }
        internal RelationBuffer Buffer { get; private set; }

        int ITuple2.Degree => this.Degree;
        int IReadOnlyCollection<IField>.Count => this.Degree;

        IField IReadOnlyList<IField>.this[int index] => null;

        private readonly IEnumerator<IRelation3> enumerator;
        private int currentIndex;
        private Func<bool> readFactory;
        

        public RelationReader(IEnumerable<IRelation3> relations)
        {
            this.enumerator = relations.GetEnumerator();
            this.NextResult();
        }

        public RelationReader(IRelation3 relation)
            : this(new[] { relation })
        {
            
        }

        public bool NextResult()
        {
            if (this.enumerator.MoveNext())
            {
                this.currentIndex = 0;
                this.readFactory = this.ReadFirst;
                this.Degree = this.enumerator.Current.Header.Attributes.Count;

                return true;
            }

            this.readFactory = this.ReadEnd;

            return false;
        }

        public void CopyTo(IField2[] target, int sourceIndex, int targetIndex, int length)
            => Array.Copy(this.Buffer.Fields, sourceIndex, target, targetIndex, length);


        public void CopyTo(IField2[] target, int length)
            => Array.Copy(this.Buffer.Fields, target, length);

        public IField2 this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Degree)
                    throw new IndexOutOfRangeException();

                return this.Buffer.Fields[index];
            }
        }

        public void Dispose()
        {
            this.enumerator.Dispose();

            if (this.Buffer == null)
                return;

            for (int i = 0; i < this.Buffer.Queues.Length; i++)
            {
                try
                {
                    if (this.Buffer.Queues[i] is IDisposable disposable)
                        disposable.Dispose();

                    this.Buffer.Queues[i] = null;
                }
                catch { }
            }
        }

        private bool ReadEnd() => false;
        private bool ReadFirst()
        {
            this.Buffer = RelationCache.CreateBuffer2(this.Relation);
            this.Buffer.Writer.Initializer(this.Buffer);

            this.currentIndex = 0;

            return (this.readFactory = this.ReadNext)();
        }

        private bool ReadNext()
        {
            Action<RelationBuffer>[] writers = this.Buffer.Writer.Queues;
            IRelationQueue[] queues = this.Buffer.Queues;

            while (this.currentIndex >= 0)
            {
                if (this.currentIndex == writers.Length)
                {
                    this.currentIndex--;

                    return true;
                }
                else if (this.ReadOrThrow(queues[this.currentIndex], null))
                {
                    writers[this.currentIndex](this.Buffer);

                    this.currentIndex++;
                }
                else
                    this.currentIndex--;
            }

            return false;
        }

        public bool Read() => this.readFactory();

        private bool ReadOrThrow(IRelationQueue queue, MetadataIdentity identity)
        {
            try
            {
                return queue.Read();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException();
                //throw RelationException.FromRelation(this.Relation, $"Cannot move enumerator for '{identity}': {ex.Message}", ex);
            }
        }

        public IEnumerator<IField> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
