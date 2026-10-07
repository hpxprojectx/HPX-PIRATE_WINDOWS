using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class PerformanceView : UserControl, IComponentConnector
{

	public PerformanceView()
	{
		InitializeComponent();
		((FrameworkElement)this).DataContext = new PerformanceViewModel(App.Host);
	}

}
