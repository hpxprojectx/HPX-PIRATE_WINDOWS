using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using HPXPirate.Interop;
using HPXPirate.Models;

namespace HPXPirate.Views;

public partial class CrosshairOverlayWindow : Window, IComponentConnector
{
	private CrosshairSettings _settings = new CrosshairSettings();



	public CrosshairOverlayWindow()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		InitializeComponent();
		((Window)this).Left = 0.0;
		((Window)this).Top = 0.0;
		((FrameworkElement)this).Width = SystemParameters.PrimaryScreenWidth;
		((FrameworkElement)this).Height = SystemParameters.PrimaryScreenHeight;
		((Window)this).SourceInitialized += delegate
		{
			MakeClickThrough();
		};
		((FrameworkElement)this).SizeChanged += (SizeChangedEventHandler)delegate
		{
			Render();
		};
	}

	private void MakeClickThrough()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			IntPtr handle = new WindowInteropHelper((Window)(object)this).Handle;
			int windowLong = NativeMethods.GetWindowLong(handle, -20);
			windowLong |= 0x80800A0;
			NativeMethods.SetWindowLong(handle, -20, windowLong);
		}
		catch
		{
		}
	}

	public void Apply(CrosshairSettings settings)
	{
		_settings = settings;
		Render();
	}

	private void Render()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Expected O, but got Unknown
		//IL_031b: Expected O, but got Unknown
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_02ba: Expected O, but got Unknown
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Expected O, but got Unknown
		//IL_0459: Expected O, but got Unknown
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Expected O, but got Unknown
		//IL_0400: Expected O, but got Unknown
		((Panel)Root).Children.Clear();
		if (!_settings.Enabled)
		{
			return;
		}
		double num = ((FrameworkElement)this).Width / 2.0 + _settings.OffsetX;
		double num2 = ((FrameworkElement)this).Height / 2.0 + _settings.OffsetY;
		Color val = ParseColor(_settings.Color);
		SolidColorBrush brush = new SolidColorBrush(val)
		{
			Opacity = Math.Clamp(_settings.Opacity, 0.0, 1.0)
		};
		SolidColorBrush outlineBrush = new SolidColorBrush(Colors.Black)
		{
			Opacity = Math.Clamp(_settings.Opacity * 0.8, 0.0, 1.0)
		};
		double num3 = Math.Max(2.0, _settings.Size);
		double num4 = Math.Max(1.0, _settings.Thickness);
		double num5 = Math.Max(0.0, _settings.Gap);
		string text = _settings.Shape ?? "Cross";
		if ((text == "Cross" || text == "CrossDot") ? true : false)
		{
			AddLine(num - num4 / 2.0, num2 - num5 - num3, num4, num3);
			AddLine(num - num4 / 2.0, num2 + num5, num4, num3);
			AddLine(num - num5 - num3, num2 - num4 / 2.0, num3, num4);
			AddLine(num + num5, num2 - num4 / 2.0, num3, num4);
		}
		if (text == "Circle")
		{
			double num6 = (num5 + num3) * 2.0;
			if (_settings.Outline)
			{
				((Panel)Root).Children.Add((UIElement)new Ellipse
				{
					Width = num6 + 2.0,
					Height = num6 + 2.0,
					Stroke = (Brush)(object)outlineBrush,
					StrokeThickness = num4 + 2.0,
					RenderTransform = (Transform)new TranslateTransform(num - num6 / 2.0 - 1.0, num2 - num6 / 2.0 - 1.0)
				});
			}
			((Panel)Root).Children.Add((UIElement)new Ellipse
			{
				Width = num6,
				Height = num6,
				Stroke = (Brush)(object)brush,
				StrokeThickness = num4,
				RenderTransform = (Transform)new TranslateTransform(num - num6 / 2.0, num2 - num6 / 2.0)
			});
		}
		bool flag = ((text == "Dot" || text == "CrossDot") ? true : false);
		if (flag || _settings.CenterDot)
		{
			double num7 = Math.Max(2.0, num4 + 1.0);
			if (_settings.Outline)
			{
				((Panel)Root).Children.Add((UIElement)new Ellipse
				{
					Width = num7 + 2.0,
					Height = num7 + 2.0,
					Fill = (Brush)(object)outlineBrush,
					RenderTransform = (Transform)new TranslateTransform(num - num7 / 2.0 - 1.0, num2 - num7 / 2.0 - 1.0)
				});
			}
			((Panel)Root).Children.Add((UIElement)new Ellipse
			{
				Width = num7,
				Height = num7,
				Fill = (Brush)(object)brush,
				RenderTransform = (Transform)new TranslateTransform(num - num7 / 2.0, num2 - num7 / 2.0)
			});
		}
		void AddLine(double x, double y, double w, double h)
		{
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Expected O, but got Unknown
			//IL_00f1: Expected O, but got Unknown
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Expected O, but got Unknown
			//IL_0094: Expected O, but got Unknown
			if (_settings.Outline)
			{
				((Panel)Root).Children.Add((UIElement)new Rectangle
				{
					Width = w + 2.0,
					Height = h + 2.0,
					Fill = (Brush)(object)outlineBrush,
					RadiusX = 1.0,
					RadiusY = 1.0,
					RenderTransform = (Transform)new TranslateTransform(x - 1.0, y - 1.0)
				});
			}
			((Panel)Root).Children.Add((UIElement)new Rectangle
			{
				Width = w,
				Height = h,
				Fill = (Brush)(object)brush,
				RadiusX = 1.0,
				RadiusY = 1.0,
				RenderTransform = (Transform)new TranslateTransform(x, y)
			});
		}
	}

	private static Color ParseColor(string hex)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return (Color)ColorConverter.ConvertFromString(hex);
		}
		catch
		{
			return Color.FromRgb(byte.MaxValue, (byte)43, (byte)214);
		}
	}

}
