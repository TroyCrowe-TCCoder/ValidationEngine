namespace ValidationEngine.Reporting;

/// <summary>
/// Contract for rendering a <see cref="ValidationEngine.Models.ValidationReport"/> as human-readable Markdown.
/// Declared in ValidationEngine.Models (not the core engine or the default Reporting plug-in)
/// so any consumer app can supply its own implementation without depending on either project:
/// each app configures/plugs in its own reporting engine, no layer assumes a particular
/// reporting implementation is "the" source.
/// </summary>
public interface IMarkdownReportRenderer
{
    string Render(ValidationEngine.Models.ValidationReport report);
}
