using System;
using HPXPirate.Models;

namespace HPXPirate.Services;

public static class Curves
{
	public static double Evaluate(ResponseCurve curve, double x, double[]? custom = null)
	{
		x = Math.Clamp(x, 0.0, 1.0);
		return curve switch
		{
			ResponseCurve.Linear => x, 
			ResponseCurve.Smooth => x * x * (3.0 - 2.0 * x), 
			ResponseCurve.FastStart => 1.0 - (1.0 - x) * (1.0 - x), 
			ResponseCurve.SlowStart => x * x, 
			ResponseCurve.Precision => Math.Pow(x, 1.5), 
			ResponseCurve.Custom => Piecewise(custom, x), 
			_ => x, 
		};
	}

	private static double Piecewise(double[]? pts, double x)
	{
		if (pts == null || pts.Length < 2)
		{
			return x;
		}
		int num = pts.Length;
		double num2 = 1.0 / (double)(num - 1);
		int num3 = Math.Min(num - 2, Math.Max(0, (int)(x / num2)));
		double num4 = (x - (double)num3 * num2) / num2;
		return pts[num3] * (1.0 - num4) + pts[num3 + 1] * num4;
	}
}
