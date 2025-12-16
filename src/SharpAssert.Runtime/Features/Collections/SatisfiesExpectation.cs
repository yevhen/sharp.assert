// ABOUTME: Expectation for verifying collection elements can satisfy predicates with 1:1 matching.
// ABOUTME: Uses bipartite matching algorithm to find valid element-to-predicate assignments.

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that collection elements can satisfy predicates with unique 1:1 matching.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// <para>
/// This expectation uses a maximum bipartite matching algorithm to determine if each predicate
/// can be satisfied by a different element. Unlike <c>All()</c> which requires every element
/// to satisfy a condition, this verifies that there exists an assignment where each predicate
/// is matched to exactly one distinct element.
/// </para>
/// <para>
/// Example: For collection [1, 2, 3] and predicates [x => x > 0, x => x > 1], both predicates
/// can be satisfied by different elements (e.g., 1 satisfies "x > 0", 2 satisfies "x > 1").
/// </para>
/// </remarks>
public sealed class SatisfiesExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly Func<T, bool>[] predicates;

    internal SatisfiesExpectation(IEnumerable<T> collection, params Func<T, bool>[] predicates)
    {
        this.collection = collection;
        this.predicates = predicates;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        if (predicates.Length == 0)
            return ExpectationResults.Pass(context.Expression);

        var elements = collection.ToList();
        if (elements.Count < predicates.Length)
            return Fail(context);

        var matchingSize = BipartiteMatching.FindMaximumMatching(elements, predicates);

        return matchingSize == predicates.Length
            ? ExpectationResults.Pass(context.Expression)
            : Fail(context);
    }

    static EvaluationResult Fail(ExpectationContext context) =>
        ExpectationResults.Fail(context.Expression,
            "Expected collection to satisfy all predicates with unique elements, but no valid matching exists.");
}

/// <summary>Bipartite matching algorithm using DFS-based augmenting paths.</summary>
internal static class BipartiteMatching
{
    /// <summary>
    /// Finds the maximum number of predicates that can be matched to unique elements.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="elements">Left side of bipartite graph (elements).</param>
    /// <param name="predicates">Right side of bipartite graph (predicates).</param>
    /// <returns>The size of the maximum matching.</returns>
    public static int FindMaximumMatching<T>(List<T> elements, Func<T, bool>[] predicates)
    {
        var elementCount = elements.Count;
        var predicateCount = predicates.Length;

        // Build adjacency: for each element, which predicates it satisfies
        var adjacency = new List<int>[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            adjacency[i] = new List<int>();
            for (var j = 0; j < predicateCount; j++)
            {
                if (predicates[j](elements[i]))
                    adjacency[i].Add(j);
            }
        }

        // predicateMatch[j] = index of element matched to predicate j, or -1 if unmatched
        var predicateMatch = new int[predicateCount];
        Array.Fill(predicateMatch, -1);

        var matchingSize = 0;

        // Try to find augmenting path from each element
        for (var element = 0; element < elementCount; element++)
        {
            var visited = new bool[predicateCount];
            if (TryAugment(element, adjacency, predicateMatch, visited))
                matchingSize++;

            // Early exit if we've matched all predicates
            if (matchingSize == predicateCount)
                break;
        }

        return matchingSize;
    }

    /// <summary>
    /// DFS to find an augmenting path starting from the given element.
    /// </summary>
    static bool TryAugment(int element, List<int>[] adjacency, int[] predicateMatch, bool[] visited)
    {
        foreach (var predicate in adjacency[element])
        {
            if (visited[predicate])
                continue;

            visited[predicate] = true;

            // If predicate is unmatched or we can find an alternate path for its current match
            if (predicateMatch[predicate] == -1 ||
                TryAugment(predicateMatch[predicate], adjacency, predicateMatch, visited))
            {
                predicateMatch[predicate] = element;
                return true;
            }
        }

        return false;
    }
}
