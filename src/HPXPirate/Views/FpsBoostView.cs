using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using HPXPirate.ViewModels;

namespace HPXPirate.Views;

public partial class FpsBoostView : UserControl, IComponentConnector
{

	public FpsBoostView()
	{
		InitializeComponent();
		((FrameworkElement)this).DataContext = new FpsBoostViewModel(App.Host);
	}

}
