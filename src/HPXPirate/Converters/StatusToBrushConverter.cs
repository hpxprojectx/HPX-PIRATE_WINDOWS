using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HPXPirate.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		string text = (value?.ToString() ?? "").ToLowerInvariant();
		if (!text.Contains("critical") && !text.Contains("bottleneck") && !text.Contains("pressure") && !text.Contains("instab") && !text.Contains("bad"))
		{
			if (!text.Contains("warn") && !text.Contains("moderate") && !text.Contains("elevated"))
			{
				return (object)new SolidColorBrush(Color.FromRgb((byte)53, (byte)224, (byte)138));
			}
			return (object)new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)176, (byte)32));
		}
		return (object)new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)77, (byte)109));
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
