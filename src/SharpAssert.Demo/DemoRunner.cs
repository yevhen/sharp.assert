using SharpAssert.Demo.Rendering;

namespace SharpAssert.Demo;

sealed class DemoRunner(IDemoRenderer renderer)
{
    public async Task RunAllAsync(string title, IEnumerable<DemoCategory> categories)
    {
        renderer.RenderHeader(title);

        var categoryList = categories.ToList();
        var totalDemos = 0;

        foreach (var category in categoryList)
        {
            renderer.RenderCategory(category);

            foreach (var demo in category.Demos)
            {
                var result = await demo.ExecuteAsync();
                renderer.RenderDemo(demo, result);

                totalDemos++;
            }
        }

        renderer.RenderFooter(categoryList.Count, totalDemos);
        renderer.Complete();
        SaveRendererIfPossible();
    }

    void SaveRendererIfPossible()
    {
        if (renderer is ISaveableRenderer saveable)
            saveable.Save();
    }

    public async Task RunCategoryAsync(string categoryName, IEnumerable<DemoCategory> categories)
    {
        var category = FindCategoryByName(categories, categoryName);
        if (category == null)
        {
            Console.WriteLine($"Category '{categoryName}' not found.");
            return;
        }

        renderer.RenderHeader($"SharpAssert Demo - {category.Name}");
        renderer.RenderCategory(category);

        foreach (var demo in category.Demos)
        {
            var result = await demo.ExecuteAsync();
            renderer.RenderDemo(demo, result);
        }

        renderer.RenderFooter(1, category.Demos.Count);
        renderer.Complete();
    }

    static DemoCategory? FindCategoryByName(IEnumerable<DemoCategory> categories, string name)
    {
        return categories.FirstOrDefault(c =>
            c.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
}
