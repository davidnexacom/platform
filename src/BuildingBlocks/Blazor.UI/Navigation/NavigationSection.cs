namespace FSH.BuildingBlocks.Blazor.UI.Navigation;

/// <summary>
/// Represents a navigation section that groups related menu items.
/// </summary>
public sealed class NavigationSection
{
    /// <summary>
    /// Unique identifier for this section.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display title for the section.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Display order (lower numbers appear first).
    /// </summary>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Whether this section is visible.
    /// </summary>
    public bool IsVisible { get; init; } = true;
}
