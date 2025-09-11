using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;

namespace SharpAssert.Tests;

[TestFixture]
public class UnsupportedFeatureDetectorFixture
{
    [Test]
    public void Should_detect_collection_contains_as_unsupported()
    {
        var list = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list.Contains(2);
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeTrue("LINQ Contains operations are not implemented");
    }
    
    [Test]
    public void Should_detect_collection_any_as_unsupported()
    {
        var list = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list.Any(x => x > 1);
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeTrue("LINQ Any operations are not implemented");
    }
    
    [Test]
    public void Should_detect_collection_all_as_unsupported()
    {
        var list = new[] { 1, 2, 3 };
        Expression<Func<bool>> expr = () => list.All(x => x > 0);
        
        var detector = new UnsupportedFeatureDetector();
        detector.Visit(expr);
        
        detector.HasUnsupported.Should().BeTrue("LINQ All operations are not implemented");
    }
    
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
}