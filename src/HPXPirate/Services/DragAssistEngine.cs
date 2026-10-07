using System;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class DragAssistEngine
{
	private readonly DragProcessor _processor = new DragProcessor();

	public DragProcessor Processor => _processor;

	public DragSample Process(double dx, double dy, DragAssistSettings s, double dt = 0.016)
	{
		EasyDragSettings s2 = new EasyDragSettings
		{
			DragSensitivity = s.DragSensitivity,
			DragSmoothness = s.DragSmoothing,
			DragAcceleration = s.DragAcceleration,
			VerticalDragMultiplier = s.VerticalMultiplier,
			HorizontalDragMultiplier = s.HorizontalMultiplier,
			InitialDragResponse = s.StartZoneSensitivity,
			DragResponseCurve = s.ResponseCurve,
			MicroMovementStabilizer = StabilizerLevel.Low,
			InputSmoothing = s.DragSmoothing,
			InputDeadzone = 0.0,
			MaxDragSpeed = Math.Max(1.0, s.DragSpeed * 2000.0),
			CustomCurvePoints = s.CustomCurvePoints
		};
		return _processor.Process(dx, dy, s2, dt);
	}

	public void Reset()
	{
		_processor.Reset();
	}
}
