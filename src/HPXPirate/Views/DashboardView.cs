using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class DashboardView : UserControl, IComponentConnector
{

	public DashboardView()
	{
		InitializeComponent();
		((FrameworkElement)this).DataContext = new DashboardViewModel(App.Host);
	}

}
