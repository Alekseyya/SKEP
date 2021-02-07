using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace RMX.RPCS.UnitTests.Data
{
    public class RPCSContextTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly DbContextOptionsBuilder<RPCSContext> _obPostgresContext;
        public RPCSContextTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            var obPostgresContext = new DbContextOptionsBuilder<RPCSContext>();
            obPostgresContext.UseNpgsql("");
            _obPostgresContext = obPostgresContext;
        }


        [Fact]
        public void RPCSContext_GetSequence()
        {
            using (var db = new RPCSContext(_obPostgresContext.Options))
            {
                var properties = db.GetType().GetProperties().ToList().Select(x => x.PropertyType).ToList();
                try
                {
                    foreach (var property in properties)
                    {
                        var tableName = property.GetGenericArguments()[0].Name;
                        _outputHelper.WriteLine($"SELECT setval(pg_get_serial_sequence('\"{tableName}\"', 'ID'), coalesce(max(\"{tableName}\".\"ID\"),0) + 1, false) FROM \"{tableName}\"" + ";");
                    }
                }
                catch (IndexOutOfRangeException e)
                {
                }
            }
        }
    }
}
