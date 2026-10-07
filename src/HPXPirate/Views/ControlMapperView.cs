using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using HPXPirate.Models;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class ControlMapperView : UserControl, IComponentConnector
{
	private readonly ControlMapperViewModel _vm;

	private ControlMapping? _dragging;

	private Point _dragOffset;



	public ControlMapperView()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		InitializeComponent();
		_vm = new ControlMapperViewModel(App.Host);
		((FrameworkElement)this).DataContext = _vm;
		_vm.Mappings.CollectionChanged += delegate
		{
			RebuildCanvas();
		};
		_vm.PropertyChanged += delegate(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "SelectedProfile")
			{
				RebuildCanvas();
			}
		};
		((FrameworkElement)MapCanvas).SizeChanged += (SizeChangedEventHandler)delegate
		{
			RebuildCanvas();
		};
		((FrameworkElement)this).Loaded += (RoutedEventHandler)delegate
		{
			RebuildCanvas();
		};
	}

	private void RebuildCanvas()
	{
		if (MapCanvas == null)
		{
			return;
		}
		((Panel)MapCanvas).Children.Clear();
		double actualWidth = ((FrameworkElement)MapCanvas).ActualWidth;
		double actualHeight = ((FrameworkElement)MapCanvas).ActualHeight;
		if (actualWidth < 2.0 || actualHeight < 2.0)
		{
			return;
		}
		foreach (ControlMapping mapping in _vm.Mappings)
		{
			FrameworkElement val = CreatePoint(mapping);
			Canvas.SetLeft((UIElement)(object)val, mapping.X * actualWidth - val.Width / 2.0);
			Canvas.SetTop((UIElement)(object)val, mapping.Y * actualHeight - val.Height / 2.0);
			((Panel)MapCanvas).Children.Add((UIElement)(object)val);
		}
	}

	private FrameworkElement CreatePoint(ControlMapping m)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected O, but got Unknown
		//IL_013b: Expected O, but got Unknown
		Brush borderBrush = (Brush)((FrameworkElement)this).FindResource((object)"Brush.Accent.Magenta");
		Border val = new Border
		{
			Width = 48.0,
			Height = 48.0,
			CornerRadius = new CornerRadius(24.0),
			Background = (Brush)new SolidColorBrush(Color.FromArgb((byte)70, byte.MaxValue, (byte)43, (byte)214)),
			BorderBrush = borderBrush,
			BorderThickness = new Thickness(2.0),
			Cursor = Cursors.SizeAll,
			Tag = m,
			ToolTip = m.Action,
			Child = (UIElement)new TextBlock
			{
				Text = m.Input,
				Foreground = (Brush)(object)Brushes.White,
				FontWeight = FontWeights.Bold,
				FontSize = 11.0,
				HorizontalAlignment = (HorizontalAlignment)1,
				VerticalAlignment = (VerticalAlignment)1,
				TextTrimming = (TextTrimming)1,
				Margin = new Thickness(2.0)
			}
		};
		((UIElement)val).MouseLeftButtonDown += new MouseButtonEventHandler(Point_MouseDown);
		((UIElement)val).MouseMove += new MouseEventHandler(Point_MouseMove);
		((UIElement)val).MouseLeftButtonUp += new MouseButtonEventHandler(Point_MouseUp);
		return (FrameworkElement)val;
	}

	private void Point_MouseDown(object sender, MouseButtonEventArgs e)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		FrameworkElement val = (FrameworkElement)((sender is FrameworkElement) ? sender : null);
		if (val != null && val.Tag is ControlMapping dragging)
		{
			_dragging = dragging;
			Point position = ((MouseEventArgs)e).GetPosition((IInputElement)(object)MapCanvas);
			_dragOffset = new Point(((Point)(position)).X - Canvas.GetLeft((UIElement)(object)val), ((Point)(position)).Y - Canvas.GetTop((UIElement)(object)val));
			((UIElement)val).CaptureMouse();
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void Point_MouseMove(object sender, MouseEventArgs e)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (_dragging == null)
		{
			return;
		}
		FrameworkElement val = (FrameworkElement)((sender is FrameworkElement) ? sender : null);
		if (val != null && (int)e.LeftButton == 1)
		{
			Point position = e.GetPosition((IInputElement)(object)MapCanvas);
			double actualWidth = ((FrameworkElement)MapCanvas).ActualWidth;
			double actualHeight = ((FrameworkElement)MapCanvas).ActualHeight;
			if (!(actualWidth < 2.0) && !(actualHeight < 2.0))
			{
				double num = Math.Clamp(((Point)(position)).X - ((Point)(_dragOffset)).X, 0.0, actualWidth - val.Width);
				double num2 = Math.Clamp(((Point)(position)).Y - ((Point)(_dragOffset)).Y, 0.0, actualHeight - val.Height);
				Canvas.SetLeft((UIElement)(object)val, num);
				Canvas.SetTop((UIElement)(object)val, num2);
				_dragging.X = Math.Clamp((num + val.Width / 2.0) / actualWidth, 0.0, 1.0);
				_dragging.Y = Math.Clamp((num2 + val.Height / 2.0) / actualHeight, 0.0, 1.0);
				((RoutedEventArgs)e).Handled = true;
			}
		}
	}

	private void Point_MouseUp(object sender, MouseButtonEventArgs e)
	{
		FrameworkElement val = (FrameworkElement)((sender is FrameworkElement) ? sender : null);
		if (val != null)
		{
			((UIElement)val).ReleaseMouseCapture();
		}
		_dragging = null;
		((RoutedEventArgs)e).Handled = true;
	}

}
