﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Relations;
using Jerrycurl.Data.Commands;
using Shouldly;
using Jerrycurl.Data.Test.Models;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;

namespace Jerrycurl.Data.Test
{
    public class CommandTests
    {
        public async Task Test_Execute_WithCaseInsensitiveColumns()
        {
            IList<int> personIds = new List<int>() { 0 };

            IField2 field = DatabaseHelper.Default.Relation(personIds, "Item").Scalar();

            Command command = new Command()
            {
                CommandText = "SELECT 1 AS b1",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(field, "B1"),
                },
            }; ;

            DatabaseHelper.Default.Execute(command);

            personIds.ShouldBe(new[] { 1 });

            personIds[0] = 0;

            await DatabaseHelper.Default.ExecuteAsync(command);

            personIds.ShouldBe(new[] { 1 });
        }

        public async Task Test_Execute_WithParameterPropagationBetweenCommands()
        {
            IList<int> personIds = new List<int>() { 0, 0 };

            IField2[] fields = DatabaseHelper.Default.Relation(personIds, "Item").Column().ToArray();

            Command[] commands = new Command[]
            {
                new Command()
                {
                    CommandText = "SELECT 1 AS B1",
                    Bindings = new IUpdateBinding[]
                    {
                        new ColumnBinding(fields[0], "B1"),
                    },
                    Parameters = new IParameter[]
                    {
                        new Parameter("P1", fields[0]),
                    }
                },
                new Command()
                {
                    CommandText = "SELECT @P1 * 2 AS B2",
                    Bindings = new IUpdateBinding[]
                    {
                        new ColumnBinding(fields[1], "B2"),
                    },
                    Parameters = new IParameter[]
                    {
                        new Parameter("P1", fields[0]),
                    }
                }
            };

            DatabaseHelper.Default.Execute(commands);

            personIds.ShouldBe(new[] { 1, 2 });

            personIds[0] = 0;
            personIds[1] = 0;

            await DatabaseHelper.Default.ExecuteAsync(commands);

            personIds.ShouldBe(new[] { 1, 2 });
        }

        public async Task Test_Execute_WithColumnBindingToMissingValue_Throws()
        {
            BigModel model = new BigModel();

            IField2 field = DatabaseHelper.Default.Relation(model, "OneToOne.Value").Scalar();

            Command command = new Command()
            {
                CommandText = @"SELECT 12 AS B1",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(field, "B1"),
                }
            };

            Should.Throw<Relations.BindingException2>(() => DatabaseHelper.Default.Execute(command));
            await Should.ThrowAsync<Relations.BindingException2>(async () => await DatabaseHelper.Default.ExecuteAsync(command));
        }

        public async Task Test_Execute_WithColumnBindingToProperty()
        {
            BigModel model1 = new BigModel() { Value = 1, Value2 = "banana" };
            BigModel model2 = new BigModel() { Value = 1, Value2 = "banana" };

            ITuple2 tuple1 = DatabaseHelper.Default.Relation(model1, "Value", "Value2").Row();
            ITuple2 tuple2 = DatabaseHelper.Default.Relation(model2, "Value", "Value2").Row();

            Command command1 = new Command()
            {
                CommandText = @"SELECT 2 AS B1, 'apple' AS B2;",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(tuple1[0], "B1"),
                    new ColumnBinding(tuple1[1], "B2"),
                }
            };
            Command command2 = new Command()
            {
                CommandText = @"SELECT 2 AS B1, 'apple' AS B2;",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(tuple2[0], "B1"),
                    new ColumnBinding(tuple2[1], "B2"),
                }
            };

            DatabaseHelper.Default.Execute(command1);
            await DatabaseHelper.Default.ExecuteAsync(command2);

            tuple1[0].Snapshot.ShouldBe(2);
            tuple2[0].Snapshot.ShouldBe(2);

            model1.Value.ShouldBe(2);
            model2.Value.ShouldBe(2);

            tuple1[1].Snapshot.ShouldBe("apple");
            tuple2[1].Snapshot.ShouldBe("apple");

            model1.Value2.ShouldBe("apple");
            model2.Value2.ShouldBe("apple");
        }

        public async Task Test_Execute_WithColumnBindingToIndexer()
        {
            IList<int> model1 = new List<int>() { 0, 0 };
            IList<int> model2 = new List<int>() { 0, 0 };

            IField2[] fields1 = DatabaseHelper.Default.Relation(model1, "Item").Column().ToArray();
            IField2[] fields2 = DatabaseHelper.Default.Relation(model2, "Item").Column().ToArray();

            Command command1 = new Command()
            {
                CommandText = @"SELECT 1 AS B1;
                                SELECT 2 AS B2;",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(fields1[0], "B1"),
                    new ColumnBinding(fields1[1], "B2"),
                }
            };
            Command command2 = new Command()
            {
                CommandText = @"SELECT 1 AS B1;
                                SELECT 2 AS B2;",
                Bindings = new IUpdateBinding[]
                {
                    new ColumnBinding(fields2[0], "B1"),
                    new ColumnBinding(fields2[1], "B2"),
                }
            };

            DatabaseHelper.Default.Execute(command1);
            await DatabaseHelper.Default.ExecuteAsync(command2);

            fields1.Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 2 });
            fields2.Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 2 });

            model1.ShouldBe(new[] { 1, 2 });
            model2.ShouldBe(new[] { 1, 2 });
        }
    }
}
