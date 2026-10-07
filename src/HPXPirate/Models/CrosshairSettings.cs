namespace HPXPirate.Models;

public sealed class CrosshairSettings
{
	public bool Enabled { get; set; }

	public string Shape { get; set; } = "Cross";


	public double Size { get; set; } = 12.0;


	public double Thickness { get; set; } = 2.0;


	public double Gap { get; set; } = 4.0;


	public double Opacity { get; set; } = 0.9;


	public string Color { get; set; } = "#FF2BD6";


	public bool CenterDot { get; set; } = true;


	public bool Outline { get; set; } = true;


	public string ToggleHotkey { get; set; } = "F8";


	public double OffsetX { get; set; }

	public double OffsetY { get; set; }
}
