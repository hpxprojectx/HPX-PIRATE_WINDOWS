using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class CrosshairView : UserControl, IComponentConnector
{
	private readonly CrosshairViewModel _vm;



	public CrosshairView()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		InitializeComponent();
		_vm = new CrosshairViewModel(App.Host);
		((FrameworkElement)this).DataContext = _vm;
		_vm.PropertyChanged += delegate
		{
			RenderPreview();
		};
		((FrameworkElement)PreviewCanvas).SizeChanged += (SizeChangedEventHandler)delegate
		{
			RenderPreview();
		};
		((FrameworkElement)this).Loaded += (RoutedEventHandler)delegate
		{
			RenderPreview();
		};
	}

	private void RenderPreview()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Expected O, but got Unknown
		//IL_0349: Expected O, but got Unknown
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Expected O, but got Unknown
		//IL_02e7: Expected O, but got Unknown
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0484: Expected O, but got Unknown
		//IL_0489: Expected O, but got Unknown
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Expected O, but got Unknown
		//IL_042f: Expected O, but got Unknown
		if (PreviewCanvas == null)
		{
			return;
		}
		((Panel)PreviewCanvas).Children.Clear();
		double actualWidth = ((FrameworkElement)PreviewCanvas).ActualWidth;
		double actualHeight = ((FrameworkElement)PreviewCanvas).ActualHeight;
		if (actualWidth < 2.0 || actualHeight < 2.0)
		{
			return;
		}
		double num = actualWidth / 2.0 + _vm.OffsetX;
		double num2 = actualHeight / 2.0 + _vm.OffsetY;
		Color val = ParseColor(_vm.Color);
		SolidColorBrush brush = new SolidColorBrush(val)
		{
			Opacity = Math.Clamp(_vm.Opacity, 0.0, 1.0)
		};
		SolidColorBrush outlineBrush = new SolidColorBrush(Colors.Black)
		{
			Opacity = Math.Clamp(_vm.Opacity * 0.8, 0.0, 1.0)
		};
		double num3 = Math.Max(2.0, _vm.Size);
		double num4 = Math.Max(1.0, _vm.Thickness);
		double num5 = Math.Max(0.0, _vm.Gap);
		string text = _vm.Shape ?? "Cross";
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
			if (_vm.Outline)
			{
				((Panel)PreviewCanvas).Children.Add((UIElement)new Ellipse
				{
					Width = num6 + 2.0,
					Height = num6 + 2.0,
					Stroke = (Brush)(object)outlineBrush,
					StrokeThickness = num4 + 2.0,
					RenderTransform = (Transform)new TranslateTransform(num - num6 / 2.0 - 1.0, num2 - num6 / 2.0 - 1.0)
				});
			}
			((Panel)PreviewCanvas).Children.Add((UIElement)new Ellipse
			{
				Width = num6,
				Height = num6,
				Stroke = (Brush)(object)brush,
				StrokeThickness = num4,
				RenderTransform = (Transform)new TranslateTransform(num - num6 / 2.0, num2 - num6 / 2.0)
			});
		}
		bool flag = ((text == "Dot" || text == "CrossDot") ? true : false);
		if (flag || _vm.CenterDot)
		{
			double num7 = Math.Max(2.0, num4 + 1.0);
			if (_vm.Outline)
			{
				((Panel)PreviewCanvas).Children.Add((UIElement)new Ellipse
				{
					Width = num7 + 2.0,
					Height = num7 + 2.0,
					Fill = (Brush)(object)outlineBrush,
					RenderTransform = (Transform)new TranslateTransform(num - num7 / 2.0 - 1.0, num2 - num7 / 2.0 - 1.0)
				});
			}
			((Panel)PreviewCanvas).Children.Add((UIElement)new Ellipse
			{
				Width = num7,
				Height = num7,
				Fill = (Brush)(object)brush,
				RenderTransform = (Transform)new TranslateTransform(num - num7 / 2.0, num2 - num7 / 2.0)
			});
		}
		void AddLine(double x, double y, double lw, double lh)
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
			if (_vm.Outline)
			{
				((Panel)PreviewCanvas).Children.Add((UIElement)new Rectangle
				{
					Width = lw + 2.0,
					Height = lh + 2.0,
					Fill = (Brush)(object)outlineBrush,
					RadiusX = 1.0,
					RadiusY = 1.0,
					RenderTransform = (Transform)new TranslateTransform(x - 1.0, y - 1.0)
				});
			}
			((Panel)PreviewCanvas).Children.Add((UIElement)new Rectangle
			{
				Width = lw,
				Height = lh,
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
