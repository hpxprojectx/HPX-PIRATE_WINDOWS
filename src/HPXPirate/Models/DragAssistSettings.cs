namespace HPXPirate.Models;

public sealed class DragAssistSettings
{
	public bool Enabled { get; set; }

	public double DragSensitivity { get; set; } = 1.0;


	public double DragAcceleration { get; set; }

	public double DragSmoothing { get; set; } = 0.25;


	public ResponseCurve ResponseCurve { get; set; } = ResponseCurve.Smooth;


	public double StartZoneSensitivity { get; set; } = 1.0;


	public double VerticalMultiplier { get; set; } = 1.0;


	public double HorizontalMultiplier { get; set; } = 1.0;


	public double DragDistance { get; set; } = 100.0;


	public double DragSpeed { get; set; } = 1.0;


	public double[] CustomCurvePoints { get; set; } = new double[5] { 0.0, 0.25, 0.5, 0.75, 1.0 };

}
