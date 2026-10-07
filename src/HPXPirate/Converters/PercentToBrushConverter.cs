using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HPXPirate.Converters;

public sealed class PercentToBrushConverter : IValueConverter
{
	private static readonly SolidColorBrush Ok = new SolidColorBrush(Color.FromRgb((byte)53, (byte)224, (byte)138));

	private static readonly SolidColorBrush Warn = new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)176, (byte)32));

	private static readonly SolidColorBrush Bad = new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)77, (byte)109));

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		double num4 = ((value is double num) ? num : ((value is float num2) ? ((double)num2) : ((!(value is int num3)) ? 0.0 : ((double)num3))));
		double num5 = num4;
		if (num5 >= 90.0)
		{
			return Bad;
		}
		if (num5 >= 70.0)
		{
			return Warn;
		}
		return Ok;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
