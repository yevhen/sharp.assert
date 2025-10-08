namespace SharpAssert.Demo.Rendering;

/// <summary>
/// Interface for rendering demo output in different formats.
/// </summary>
interface IDemoRenderer
{
    /// <summary>
    /// Render the document header.
    /// </summary>
    void RenderHeader(string title);

    /// <summary>
    /// Render a category section.
    /// </summary>
    void RenderCategory(DemoCategory category);

    /// <summary>
    /// Render a single demo within a category.
    /// </summary>
    void RenderDemo(DemoDefinition demo, DemoResult result);

    /// <summary>
    /// Render the document footer with summary statistics.
    /// </summary>
    void RenderFooter(int categoryCount, int totalDemos);

    /// <summary>
    /// Complete rendering and perform any finalization.
    /// </summary>
    void Complete();
}

/// <summary>
/// Interface for renderers that can save output to a file.
/// </summary>
interface ISaveableRenderer
{
    /// <summary>
    /// Save the rendered output to the configured file.
    /// </summary>
    void Save();
}
