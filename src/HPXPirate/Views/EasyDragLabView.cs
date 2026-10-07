using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class EasyDragLabView : UserControl, IComponentConnector
{
	private readonly EasyDragLabViewModel _vm;

	private bool _dragging;

	private Point _lastPoint;

	private DateTime _lastTime;





	public EasyDragLabView()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		InitializeComponent();
		_vm = new EasyDragLabViewModel(App.Host);
		((FrameworkElement)this).DataContext = _vm;
		((UIElement)DragPad).MouseLeftButtonDown += new MouseButtonEventHandler(OnPadDown);
		((UIElement)DragPad).MouseMove += new MouseEventHandler(OnPadMove);
		((UIElement)DragPad).MouseLeftButtonUp += new MouseButtonEventHandler(OnPadUp);
		((UIElement)DragPad).MouseLeave += new MouseEventHandler(OnPadLeave);
	}

	private void OnPadDown(object sender, MouseButtonEventArgs e)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		_dragging = true;
		_lastPoint = ((MouseEventArgs)e).GetPosition((IInputElement)(object)PadCanvas);
		_lastTime = DateTime.UtcNow;
		MoveDot(_lastPoint);
		((UIElement)DragPad).CaptureMouse();
		((RoutedEventArgs)e).Handled = true;
	}

	private void OnPadMove(object sender, MouseEventArgs e)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if (_dragging)
		{
			Point position = e.GetPosition((IInputElement)(object)PadCanvas);
			DateTime utcNow = DateTime.UtcNow;
			double num = Math.Max(0.001, (utcNow - _lastTime).TotalSeconds);
			if (num > 0.2)
			{
				num = 0.016;
			}
			double dx = ((Point)(position)).X - ((Point)(_lastPoint)).X;
			double dy = ((Point)(position)).Y - ((Point)(_lastPoint)).Y;
			_vm.ProcessDrag(dx, dy, num);
			MoveDot(position);
			_lastPoint = position;
			_lastTime = utcNow;
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void OnPadUp(object sender, MouseButtonEventArgs e)
	{
		_dragging = false;
		((UIElement)DragPad).ReleaseMouseCapture();
		((RoutedEventArgs)e).Handled = true;
	}

	private void OnPadLeave(object sender, MouseEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.LeftButton != 1)
		{
			_dragging = false;
			((UIElement)DragPad).ReleaseMouseCapture();
		}
	}

	private void MoveDot(Point p)
	{
		double actualWidth = ((FrameworkElement)PadCanvas).ActualWidth;
		double actualHeight = ((FrameworkElement)PadCanvas).ActualHeight;
		double num = Math.Clamp(((Point)(p)).X, 0.0, Math.Max(0.0, actualWidth));
		double num2 = Math.Clamp(((Point)(p)).Y, 0.0, Math.Max(0.0, actualHeight));
		Canvas.SetLeft((UIElement)(object)PadDot, num - ((FrameworkElement)PadDot).Width / 2.0);
		Canvas.SetTop((UIElement)(object)PadDot, num2 - ((FrameworkElement)PadDot).Height / 2.0);
	}

}
