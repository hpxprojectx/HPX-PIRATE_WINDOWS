using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HPXPirate.Controls;

public sealed class LineGraph : FrameworkElement
{
	public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register("Values", typeof(IEnumerable<double>), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)null, (FrameworkPropertyMetadataOptions)16));

	public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)Brushes.Magenta, (FrameworkPropertyMetadataOptions)16));

	public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)null, (FrameworkPropertyMetadataOptions)16));

	public static readonly DependencyProperty AutoScaleProperty = DependencyProperty.Register("AutoScale", typeof(bool), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)true, (FrameworkPropertyMetadataOptions)16));

	public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)100.0, (FrameworkPropertyMetadataOptions)16));

	public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register("ShowGrid", typeof(bool), typeof(LineGraph), (PropertyMetadata)new FrameworkPropertyMetadata((object)true, (FrameworkPropertyMetadataOptions)16));

	public IEnumerable<double>? Values
	{
		get
		{
			return (IEnumerable<double>)((DependencyObject)this).GetValue(ValuesProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ValuesProperty, (object)value);
		}
	}

	public Brush Stroke
	{
		get
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			return (Brush)((DependencyObject)this).GetValue(StrokeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(StrokeProperty, (object)value);
		}
	}

	public Brush? Fill
	{
		get
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			return (Brush)((DependencyObject)this).GetValue(FillProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(FillProperty, (object)value);
		}
	}

	public bool AutoScale
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(AutoScaleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(AutoScaleProperty, (object)value);
		}
	}

	public double MaxValue
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(MaxValueProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(MaxValueProperty, (object)value);
		}
	}

	public bool ShowGrid
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(ShowGridProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ShowGridProperty, (object)value);
		}
	}

	public LineGraph()
	{
		((UIElement)this).SnapsToDevicePixels = true;
	}

	protected override void OnRender(DrawingContext dc)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Expected O, but got Unknown
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		double actualWidth = ((FrameworkElement)this).ActualWidth;
		double actualHeight = ((FrameworkElement)this).ActualHeight;
		if (actualWidth <= 1.0 || actualHeight <= 1.0)
		{
			return;
		}
		dc.DrawRectangle((Brush)new SolidColorBrush(Color.FromArgb((byte)20, byte.MaxValue, byte.MaxValue, byte.MaxValue)), (Pen)null, new Rect(0.0, 0.0, actualWidth, actualHeight));
		if (ShowGrid)
		{
			Pen val = new Pen((Brush)new SolidColorBrush(Color.FromArgb((byte)24, byte.MaxValue, byte.MaxValue, byte.MaxValue)), 1.0);
			for (int i = 1; i < 4; i++)
			{
				double num = actualHeight * (double)i / 4.0;
				dc.DrawLine(val, new Point(0.0, num), new Point(actualWidth, num));
			}
			for (int j = 1; j < 6; j++)
			{
				double num2 = actualWidth * (double)j / 6.0;
				dc.DrawLine(val, new Point(num2, 0.0), new Point(num2, actualHeight));
			}
		}
		double[] array = Values?.ToArray();
		if (array == null || array.Length < 2)
		{
			return;
		}
		double num3 = (AutoScale ? Math.Max(1.0, array.Max() * 1.15) : Math.Max(1.0, MaxValue));
		double num4 = actualWidth / (double)(array.Length - 1);
		Point[] array2 = (Point[])(object)new Point[array.Length];
		for (int k = 0; k < array.Length; k++)
		{
			double num5 = Math.Clamp(array[k], 0.0, num3);
			double num6 = actualHeight - num5 / num3 * actualHeight;
			array2[k] = new Point((double)k * num4, num6);
		}
		if (Fill != null)
		{
			StreamGeometry val2 = new StreamGeometry();
			StreamGeometryContext val3 = val2.Open();
			try
			{
				val3.BeginFigure(new Point(0.0, actualHeight), true, true);
				val3.PolyLineTo((IList<Point>)array2, true, false);
				val3.LineTo(new Point(actualWidth, actualHeight), true, false);
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
			((Freezable)val2).Freeze();
			dc.DrawGeometry(Fill, (Pen)null, (Geometry)(object)val2);
		}
		Pen val4 = new Pen(Stroke, 2.0)
		{
			LineJoin = (PenLineJoin)2
		};
		((Freezable)val4).Freeze();
		for (int l = 1; l < array2.Length; l++)
		{
			dc.DrawLine(val4, array2[l - 1], array2[l]);
		}
	}
}
