namespace HPXPirate.Models;

public sealed class MouseSettings
{
	public double HorizontalSensitivity { get; set; } = 1.0;


	public double VerticalSensitivity { get; set; } = 1.0;


	public double AdsSensitivity { get; set; } = 0.8;


	public double GeneralSensitivity { get; set; } = 1.0;


	public bool MouseAcceleration { get; set; }

	public double MouseSmoothing { get; set; }

	public ResponseCurve ResponseCurve { get; set; }

	public double DpiMultiplier { get; set; } = 1.0;


	public int PollingRateHz { get; set; } = 1000;

}
