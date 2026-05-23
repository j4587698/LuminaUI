using Avalonia.Collections;

namespace LuminaUI.Controls;

public class LuminaCascaderPanel
{
	public AvaloniaList<LuminaCascaderNode> Nodes { get; }

	public double Width { get; }

	public double MaxHeight { get; }

	public LuminaCascaderPanel(AvaloniaList<LuminaCascaderNode> nodes, double width, double maxHeight)
	{
		Nodes = nodes;
		Width = width;
		MaxHeight = maxHeight;
	}
}
