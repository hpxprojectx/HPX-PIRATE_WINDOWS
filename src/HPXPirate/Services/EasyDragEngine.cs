using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class EasyDragEngine
{
	private readonly DragProcessor _processor = new DragProcessor();

	public DragProcessor Processor => _processor;

	public static EasyDragSettings ApplyPreset(EasyDragSettings s, EasyDragPreset preset)
	{
		s.Preset = preset;
		switch (preset)
		{
		case EasyDragPreset.Light:
			s.DragWeight = 0.8;
			s.DragSensitivity = 1.15;
			s.DragStability = 0.3;
			s.DragSmoothness = 0.2;
			s.DragAcceleration = 0.15;
			s.InitialDragResponse = 1.2;
			s.DragResponseCurve = ResponseCurve.FastStart;
			s.MicroMovementStabilizer = StabilizerLevel.Off;
			s.InputSmoothing = 0.15;
			s.InputDeadzone = 0.0;
			s.MaxDragSpeed = 2500.0;
			break;
		case EasyDragPreset.Balanced:
			s.DragWeight = 1.0;
			s.DragSensitivity = 1.0;
			s.DragStability = 0.5;
			s.DragSmoothness = 0.35;
			s.DragAcceleration = 0.0;
			s.InitialDragResponse = 1.0;
			s.DragResponseCurve = ResponseCurve.Smooth;
			s.MicroMovementStabilizer = StabilizerLevel.Low;
			s.InputSmoothing = 0.3;
			s.InputDeadzone = 0.0;
			s.MaxDragSpeed = 2000.0;
			break;
		case EasyDragPreset.Stable:
			s.DragWeight = 1.15;
			s.DragSensitivity = 0.95;
			s.DragStability = 0.8;
			s.DragSmoothness = 0.5;
			s.DragAcceleration = 0.0;
			s.InitialDragResponse = 0.95;
			s.DragResponseCurve = ResponseCurve.SlowStart;
			s.MicroMovementStabilizer = StabilizerLevel.Medium;
			s.InputSmoothing = 0.45;
			s.InputDeadzone = 0.5;
			s.MaxDragSpeed = 1800.0;
			break;
		case EasyDragPreset.FastDrag:
			s.DragWeight = 0.9;
			s.DragSensitivity = 1.1;
			s.DragStability = 0.4;
			s.DragSmoothness = 0.25;
			s.DragAcceleration = 0.3;
			s.InitialDragResponse = 1.35;
			s.DragResponseCurve = ResponseCurve.FastStart;
			s.MicroMovementStabilizer = StabilizerLevel.Low;
			s.InputSmoothing = 0.2;
			s.InputDeadzone = 0.0;
			s.MaxDragSpeed = 3000.0;
			break;
		case EasyDragPreset.Precision:
			s.DragWeight = 1.25;
			s.DragSensitivity = 0.85;
			s.DragStability = 0.85;
			s.DragSmoothness = 0.55;
			s.DragAcceleration = 0.0;
			s.InitialDragResponse = 0.9;
			s.DragResponseCurve = ResponseCurve.Precision;
			s.MicroMovementStabilizer = StabilizerLevel.High;
			s.InputSmoothing = 0.5;
			s.InputDeadzone = 0.75;
			s.MaxDragSpeed = 1500.0;
			break;
		}
		return s;
	}

	public static EasyDragSettings ApplyAimFeel(EasyDragSettings s, AimFeel feel)
	{
		s.AimFeel = feel;
		switch (feel)
		{
		case AimFeel.Light:
			s.DragWeight = 0.8;
			s.DragSmoothness = 0.2;
			s.DragResponseCurve = ResponseCurve.FastStart;
			break;
		case AimFeel.Smooth:
			s.DragWeight = 1.0;
			s.DragSmoothness = 0.4;
			s.DragResponseCurve = ResponseCurve.Smooth;
			break;
		case AimFeel.Stable:
			s.DragWeight = 1.15;
			s.DragSmoothness = 0.5;
			s.MicroMovementStabilizer = StabilizerLevel.Medium;
			break;
		case AimFeel.Responsive:
			s.DragWeight = 0.95;
			s.DragSmoothness = 0.25;
			s.DragAcceleration = 0.2;
			s.InitialDragResponse = 1.2;
			break;
		case AimFeel.Precision:
			s.DragWeight = 1.25;
			s.DragSmoothness = 0.55;
			s.MicroMovementStabilizer = StabilizerLevel.High;
			s.DragResponseCurve = ResponseCurve.Precision;
			break;
		}
		return s;
	}

	public DragSample Process(double dx, double dy, EasyDragSettings s, double dt = 0.016)
	{
		return _processor.Process(dx, dy, s, dt);
	}

	public void Reset()
	{
		_processor.Reset();
	}
}
