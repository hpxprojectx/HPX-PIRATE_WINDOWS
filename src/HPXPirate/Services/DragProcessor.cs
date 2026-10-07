using System;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class DragProcessor
{
	private double _prevOutX;

	private double _prevOutY;

	private double _prevRawX;

	private double _prevRawY;

	private double _smoothedX;

	private double _smoothedY;

	private double _stabilityAccum = 1.0;

	private double _smoothAccum = 1.0;

	private int _samples;

	public double StabilityScore { get; private set; } = 100.0;


	public double SmoothnessScore { get; private set; } = 100.0;


	public double TotalDistance { get; private set; }

	public double LastSpeed { get; private set; }

	public void Reset()
	{
		_prevOutX = (_prevOutY = (_prevRawX = (_prevRawY = 0.0)));
		_smoothedX = (_smoothedY = 0.0);
		_stabilityAccum = 1.0;
		_smoothAccum = 1.0;
		_samples = 0;
		double stabilityScore = (SmoothnessScore = 100.0);
		StabilityScore = stabilityScore;
		TotalDistance = 0.0;
		LastSpeed = 0.0;
	}

	public DragSample Process(double dx, double dy, EasyDragSettings s, double dtSeconds = 0.016)
	{
		double num = Math.Sqrt(dx * dx + dy * dy);
		double inputDeadzone = s.InputDeadzone;
		if (num < inputDeadzone)
		{
			return new DragSample(dx, dy, 0.0, 0.0, num, 0.0, 0.0, StabilityScore, SmoothnessScore);
		}
		double num2 = Math.Max(1.0, s.MaxDragSpeed);
		double num3 = Math.Min(1.0, num / num2);
		double num4 = Curves.Evaluate(s.DragResponseCurve, num3, s.CustomCurvePoints);
		double num5 = ((num > 0.0) ? (num4 / num3) : 1.0);
		if (double.IsNaN(num5) || double.IsInfinity(num5))
		{
			num5 = 1.0;
		}
		double num6 = dx * num5;
		double num7 = dy * num5;
		double num8 = s.HorizontalDragMultiplier * s.DragSensitivity / Math.Max(0.1, s.DragWeight);
		double num9 = s.VerticalDragMultiplier * s.DragSensitivity / Math.Max(0.1, s.DragWeight);
		num6 *= num8;
		num7 *= num9;
		if (num > 0.0 && Math.Sqrt(_prevRawX * _prevRawX + _prevRawY * _prevRawY) < 0.5)
		{
			num6 *= s.InitialDragResponse;
			num7 *= s.InitialDragResponse;
		}
		if (s.DragAcceleration > 0.0)
		{
			double num10 = 1.0 + s.DragAcceleration * num3;
			num6 *= num10;
			num7 *= num10;
		}
		double num11 = Math.Clamp(s.DragSmoothness + s.InputSmoothing, 0.0, 0.95);
		_smoothedX = _smoothedX * num11 + num6 * (1.0 - num11);
		_smoothedY = _smoothedY * num11 + num7 * (1.0 - num11);
		num6 = _smoothedX;
		num7 = _smoothedY;
		double num12 = s.MicroMovementStabilizer switch
		{
			StabilizerLevel.Off => 0.0, 
			StabilizerLevel.Low => 0.15, 
			StabilizerLevel.Medium => 0.3, 
			StabilizerLevel.High => 0.5, 
			_ => 0.0, 
		};
		if (num12 > 0.0 && num < num2 * 0.15)
		{
			num6 *= 1.0 - num12;
			num7 *= 1.0 - num12;
		}
		double num13 = Math.Sqrt(num6 * num6 + num7 * num7);
		double speed = (LastSpeed = ((dtSeconds > 0.0) ? (num / dtSeconds) : num));
		TotalDistance += num13;
		double num15 = Math.Abs(num13 - Math.Sqrt(_prevOutX * _prevOutX + _prevOutY * _prevOutY));
		double num16 = Math.Min(1.0, num15 / Math.Max(1.0, num2 * 0.5));
		_stabilityAccum = _stabilityAccum * 0.9 + (1.0 - num16) * 0.1;
		double num17 = Math.Sqrt((num6 - _prevOutX) * (num6 - _prevOutX) + (num7 - _prevOutY) * (num7 - _prevOutY));
		double num18 = Math.Min(1.0, num17 / Math.Max(1.0, num2 * 0.25));
		_smoothAccum = _smoothAccum * 0.9 + (1.0 - num18) * 0.1;
		_samples++;
		StabilityScore = Math.Clamp(_stabilityAccum * 100.0, 0.0, 100.0);
		SmoothnessScore = Math.Clamp(_smoothAccum * 100.0, 0.0, 100.0);
		_prevOutX = num6;
		_prevOutY = num7;
		_prevRawX = dx;
		_prevRawY = dy;
		return new DragSample(dx, dy, num6, num7, num, num13, speed, StabilityScore, SmoothnessScore);
	}
}
