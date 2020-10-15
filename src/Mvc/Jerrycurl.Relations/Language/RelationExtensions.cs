﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language
{
    public static class RelationExtensions
    {
        public static RelationHeader Select(this ISchema schema, IEnumerable<string> header)
        {
            if (header == null)
                throw new ArgumentException(nameof(header));

            IEnumerable<IRelationMetadata> metadata = header.Select(a => schema.Require<IRelationMetadata>(a)).ToList();
            IReadOnlyList<RelationAttribute> attributes = metadata.Select(m => new RelationAttribute(m)).ToList();

            return new RelationHeader(schema, attributes);
        }

        public static RelationHeader Select(this ISchema schema, params string[] header)
            => schema.Select((IEnumerable<string>)header);

        public static IRelation2 Select(this IField2 source, IEnumerable<string> header)
            => new Relation2(source, source.Identity.Schema.Select(header));

        public static IRelation2 Select(this IField2 source, params string[] header)
            => new Relation2(source, source.Identity.Schema.Select(header));

        public static ITuple2 Lookup(this IField2 source, IEnumerable<string> header)
            => source.Select(header).Body.FirstOrDefault();

        public static ITuple2 Lookup(this IField2 source, params string[] header)
            => source.Select(header).Body.FirstOrDefault();

        public static IField2 Lookup(this IField2 source, string attributeName)
        {
            IRelation2 relation = source.Select(attributeName);

            using IRelationReader reader = relation.GetReader();

            if (reader.Read())
                return reader[0];

            return null;
        }

        public static IRelation2 From(this RelationHeader header, object model)
            => header.From(new Model2(header.Schema, model));

        public static IRelation2 From(this RelationHeader header, IField2 source)
            => new Relation2(source, header);

        public static IField2 From<TModel>(this ISchemaStore store, TModel model)
            => new Model2(store.GetSchema(typeof(TModel)), model);

        public static RelationHeader<TModel> As<TModel>(this ISchema schema)
            => new RelationHeader<TModel>(schema);

        public static ISchema GetSchema<TModel>(this ISchemaStore store)
            => store.GetSchema(typeof(TModel));

        public static RelationHeader<TModel> For<TModel>(this ISchemaStore store)
            => new RelationHeader<TModel>(store.GetSchema(typeof(TModel)));

        public static void Update<T>(this IField2 field, Func<T, T> valueFactory)
            => field.Update(valueFactory((T)field.Snapshot));

        public static void Update<T>(this IField2 field, T value)
            => field.Update(value);
    }
}