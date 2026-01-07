namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents a named layout containing camera positions.
/// </summary>
public class CameraLayout
{
    /// <summary>
    /// Gets or sets the unique identifier for the layout.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the layout.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of camera positions in this layout.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
    [SuppressMessage("Design", "MA0016:Prefer using collection abstraction instead of implementation", Justification = "Required for JSON deserialization")]
    public List<CameraLayoutItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the date and time when the layout was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the layout was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Returns the name of the layout.
    /// </summary>
    /// <returns>The layout name.</returns>
    public override string ToString()
        => Name;
}