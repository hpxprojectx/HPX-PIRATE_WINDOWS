namespace HPXPirate.Models;

public sealed class EasyDragSettings
{
	public bool Enabled { get; set; }

	public EasyDragPreset Preset { get; set; } = EasyDragPreset.Balanced;


	public double DragWeight { get; set; } = 1.0;


	public double DragSensitivity { get; set; } = 1.0;


	public double DragStability { get; set; } = 0.5;


	public double DragSmoothness { get; set; } = 0.35;


	public double DragAcceleration { get; set; }

	public double VerticalDragMultiplier { get; set; } = 1.0;


	public double HorizontalDragMultiplier { get; set; } = 1.0;


	public double InitialDragResponse { get; set; } = 1.0;


	public ResponseCurve DragResponseCurve { get; set; } = ResponseCurve.Smooth;


	public StabilizerLevel MicroMovementStabilizer { get; set; } = StabilizerLevel.Low;


	public double InputSmoothing { get; set; } = 0.3;


	public double InputDeadzone { get; set; }

	public double MaxDragSpeed { get; set; } = 2000.0;


	public AimFeel AimFeel { get; set; } = AimFeel.Smooth;


	public double[] CustomCurvePoints { get; set; } = new double[5] { 0.0, 0.2, 0.5, 0.8, 1.0 };

}
