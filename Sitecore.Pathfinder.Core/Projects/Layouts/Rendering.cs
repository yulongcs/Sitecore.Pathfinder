namespace Sitecore.Pathfinder.Projects.Layouts
{
  using Sitecore.Pathfinder.Diagnostics;
  using Sitecore.Pathfinder.Projects.Files;
  using Sitecore.Pathfinder.Projects.Items;
  using Sitecore.Pathfinder.TreeNodes;

  public class Rendering : ContentFile
  {
    public Rendering([NotNull] IProject project, [NotNull] ITextSpan textSpan, [NotNull] Item item) : base(project, textSpan)
    {
      this.Item = item;
    }

    [NotNull]
    public Item Item { get; }
  }
}