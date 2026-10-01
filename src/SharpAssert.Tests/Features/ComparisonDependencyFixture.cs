using System.Data;
using KellermanSoftware.CompareNetObjects;

namespace SharpAssert.Features;

[TestFixture]
public class ComparisonDependencyFixture
{
    [Test]
    public void Comparison_engine_has_no_System_Drawing_dependency()
    {
        var dependencies = typeof(CompareLogic).Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        NUnit.Framework.Assert.That(dependencies, Does.Not.Contain("System.Drawing.Common"));
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void Comparison_engine_compares_dictionary_values(int value, bool equivalent)
    {
        var actual = new Dictionary<string, int> { ["key"] = 1 };
        var expected = new Dictionary<string, int> { ["key"] = value };

        var result = new CompareLogic().Compare(actual, expected);

        NUnit.Framework.Assert.That(result.AreEqual, Is.EqualTo(equivalent));
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void Comparison_engine_compares_DateOnly_values(int day, bool equivalent)
    {
        var actual = new DateOnly(2025, 1, 1);
        var expected = new DateOnly(2025, 1, day);

        var result = new CompareLogic().Compare(actual, expected);

        NUnit.Framework.Assert.That(result.AreEqual, Is.EqualTo(equivalent));
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void Comparison_engine_compares_TimeOnly_values(int hour, bool equivalent)
    {
        var actual = new TimeOnly(1, 0);
        var expected = new TimeOnly(hour, 0);

        var result = new CompareLogic().Compare(actual, expected);

        NUnit.Framework.Assert.That(result.AreEqual, Is.EqualTo(equivalent));
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void Comparison_engine_compares_DataTable_values(int value, bool equivalent)
    {
        var actual = CreateTable(1);
        var expected = CreateTable(value);

        var result = new CompareLogic().Compare(actual, expected);

        NUnit.Framework.Assert.That(result.AreEqual, Is.EqualTo(equivalent));
    }

    static DataTable CreateTable(int value)
    {
        var table = new DataTable("Items");
        table.Columns.Add("Value", typeof(int));
        table.Rows.Add(value);
        return table;
    }
}
