using System;
using System.Collections.Generic;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class DragAssistViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private string _status = "";

	private List<double> _curve = new List<double>();

	private DragAssistSettings S => _host.AppSettings.DragAssist;

	public string Status
	{
		get
		{
			return _status;
		}
		private set
		{
			SetProperty(ref _status, value, "Status");
		}
	}

	public bool Enabled
	{
		get
		{
			return S.Enabled;
		}
		set
		{
			S.Enabled = value;
			OnPropertyChanged("Enabled");
		}
	}

	public double DragSensitivity
	{
		get
		{
			return S.DragSensitivity;
		}
		set
		{
			S.DragSensitivity = value;
			OnPropertyChanged("DragSensitivity");
			UpdateCurve();
		}
	}

	public double DragAcceleration
	{
		get
		{
			return S.DragAcceleration;
		}
		set
		{
			S.DragAcceleration = value;
			OnPropertyChanged("DragAcceleration");
		}
	}

	public double DragSmoothing
	{
		get
		{
			return S.DragSmoothing;
		}
		set
		{
			S.DragSmoothing = value;
			OnPropertyChanged("DragSmoothing");
		}
	}

	public double StartZoneSensitivity
	{
		get
		{
			return S.StartZoneSensitivity;
		}
		set
		{
			S.StartZoneSensitivity = value;
			OnPropertyChanged("StartZoneSensitivity");
		}
	}

	public double VerticalMultiplier
	{
		get
		{
			return S.VerticalMultiplier;
		}
		set
		{
			S.VerticalMultiplier = value;
			OnPropertyChanged("VerticalMultiplier");
		}
	}

	public double HorizontalMultiplier
	{
		get
		{
			return S.HorizontalMultiplier;
		}
		set
		{
			S.HorizontalMultiplier = value;
			OnPropertyChanged("HorizontalMultiplier");
		}
	}

	public double DragDistance
	{
		get
		{
			return S.DragDistance;
		}
		set
		{
			S.DragDistance = value;
			OnPropertyChanged("DragDistance");
		}
	}

	public double DragSpeed
	{
		get
		{
			return S.DragSpeed;
		}
		set
		{
			S.DragSpeed = value;
			OnPropertyChanged("DragSpeed");
		}
	}

	public List<double> Curve
	{
		get
		{
			return _curve;
		}
		private set
		{
			SetProperty(ref _curve, value, "Curve");
		}
	}

	public string CurveName => S.ResponseCurve.ToString();

	public ICommand SaveCommand { get; }

	public ICommand LinearCommand { get; }

	public ICommand SmoothCommand { get; }

	public ICommand FastCommand { get; }

	public ICommand SlowCommand { get; }

	public ICommand PrecisionCommand { get; }

	public ICommand CustomCommand { get; }

	public DragAssistViewModel(AppHost host)
	{
		_host = host;
		SaveCommand = new RelayCommand((Action)delegate
		{
			_host.Settings.Save();
			Status = "Drag Assist settings saved.";
		}, (Func<bool>?)null);
		LinearCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.Linear);
		}, (Func<bool>?)null);
		SmoothCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.Smooth);
		}, (Func<bool>?)null);
		FastCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.FastStart);
		}, (Func<bool>?)null);
		SlowCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.SlowStart);
		}, (Func<bool>?)null);
		PrecisionCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.Precision);
		}, (Func<bool>?)null);
		CustomCommand = new RelayCommand((Action)delegate
		{
			SetCurve(ResponseCurve.Custom);
		}, (Func<bool>?)null);
		UpdateCurve();
	}

	private void SetCurve(ResponseCurve curve)
	{
		S.ResponseCurve = curve;
		OnPropertyChanged("CurveName");
		UpdateCurve();
	}

	private void UpdateCurve()
	{
		List<double> list = new List<double>();
		for (int i = 0; i <= 50; i++)
		{
			double x = (double)i / 50.0;
			list.Add(Curves.Evaluate(S.ResponseCurve, x, S.CustomCurvePoints) * 100.0);
		}
		Curve = list;
	}
}
