using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;

namespace SharpAssert.Tests;

[TestFixture]
public class UnsupportedFeatureDetectorFixture
{
    [Test]
    public void Should_detect_sequence_equal_as_unsupported()
    {
        var list1 = new[] { 1, 2, 3 };
        var list2 = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list1.SequenceEqual(list2);
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeTrue("LINQ SequenceEqual operations are not implemented");
    }

    [Test]
    public void Should_not_detect_basic_int_comparisons_as_unsupported()
    {
        Expression<Func<bool>> expr = () => 5 == 10;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("basic int comparisons are supported");
    }
    
    [Test]
    public void Should_not_detect_logical_operators_as_unsupported()
    {
        Expression<Func<bool>> expr = () => true && false;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("logical operators are supported");
    }
    
    [Test]
    public void Should_not_detect_simple_boolean_expressions_as_unsupported()
    {
        var flag = true;
        Expression<Func<bool>> expr = () => flag;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("simple boolean expressions are supported");
    }
    
    [Test]
    public void Should_not_detect_string_comparisons_as_unsupported()
    {
        var str1 = "hello";
        var str2 = "world";
        Expression<Func<bool>> expr = () => str1 == str2;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("string comparisons are supported");
    }
    
    [Test]
    public void Should_not_detect_collection_comparisons_as_unsupported()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list1 == list2;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("collection comparisons are supported");
    }
    
    [Test]
    public void Should_not_detect_object_comparisons_as_unsupported()
    {
        var obj1 = new Person("Alice", 30);
        var obj2 = new Person("Bob", 25);
        Expression<Func<bool>> expr = () => obj1 == obj2;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("object comparisons are supported");
    }
    
    [Test]
    public void Should_not_detect_complex_expressions_as_unsupported()
    {
        var x = 5;
        var y = 10;
        var name = "test";
        Expression<Func<bool>> expr = () => x < y && name.Length > 0 || x == 0;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("complex expressions with logical operators are supported");
    }
    
    [Test]
    public void Should_not_detect_property_access_as_unsupported()
    {
        var person = new Person("Alice", 30);
        Expression<Func<bool>> expr = () => person.Name == "Alice";
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("property access is supported");
    }
    
    [Test]
    public void Should_not_detect_supported_method_calls_as_unsupported()
    {
        var text = "hello world";
        Expression<Func<bool>> expr = () => text.Contains("hello");
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("supported method calls are supported");
    }
    
    [Test]
    public void Should_not_detect_array_length_as_unsupported()
    {
        var array = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => array.Length == 3;
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeFalse("array length access is supported");
    }
}

// Test helper classes
public record Person(string Name, int Age);