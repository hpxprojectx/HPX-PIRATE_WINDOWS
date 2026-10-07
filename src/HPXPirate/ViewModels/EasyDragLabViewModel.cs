using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class EasyDragLabViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private readonly DragProcessor _rawProcessor = new DragProcessor();

	private readonly DragProcessor _engineProcessor = new DragProcessor();

	private string _status = "Drag inside the test area to measure your input.";

	private List<double> _curve = new List<double>();

	private double _rawMagnitude;

	private double _processedMagnitude;

	private double _distance;

	private double _speed;

	private double _stability = 100.0;

	private double _smoothness = 100.0;

	private double _beforeStability = 100.0;

	private double _beforeSmoothness = 100.0;

	private string _responseLabel = "—";

	private List<double> _rawSeries = new List<double>();

	private List<double> _processedSeries = new List<double>();

	private EasyDragSettings S => _host.AppSettings.EasyDrag;

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

	public double DragWeight
	{
		get
		{
			return S.DragWeight;
		}
		set
		{
			S.DragWeight = value;
			OnPropertyChanged("DragWeight");
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

	public double DragStability
	{
		get
		{
			return S.DragStability;
		}
		set
		{
			S.DragStability = value;
			OnPropertyChanged("DragStability");
		}
	}

	public double DragSmoothness
	{
		get
		{
			return S.DragSmoothness;
		}
		set
		{
			S.DragSmoothness = value;
			OnPropertyChanged("DragSmoothness");
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

	public double VerticalDragMultiplier
	{
		get
		{
			return S.VerticalDragMultiplier;
		}
		set
		{
			S.VerticalDragMultiplier = value;
			OnPropertyChanged("VerticalDragMultiplier");
		}
	}

	public double HorizontalDragMultiplier
	{
		get
		{
			return S.HorizontalDragMultiplier;
		}
		set
		{
			S.HorizontalDragMultiplier = value;
			OnPropertyChanged("HorizontalDragMultiplier");
		}
	}

	public double InitialDragResponse
	{
		get
		{
			return S.InitialDragResponse;
		}
		set
		{
			S.InitialDragResponse = value;
			OnPropertyChanged("InitialDragResponse");
		}
	}

	public double InputSmoothing
	{
		get
		{
			return S.InputSmoothing;
		}
		set
		{
			S.InputSmoothing = value;
			OnPropertyChanged("InputSmoothing");
		}
	}

	public double InputDeadzone
	{
		get
		{
			return S.InputDeadzone;
		}
		set
		{
			S.InputDeadzone = value;
			OnPropertyChanged("InputDeadzone");
		}
	}

	public double MaxDragSpeed
	{
		get
		{
			return S.MaxDragSpeed;
		}
		set
		{
			S.MaxDragSpeed = value;
			OnPropertyChanged("MaxDragSpeed");
		}
	}

	public StabilizerLevel MicroMovementStabilizer
	{
		get
		{
			return S.MicroMovementStabilizer;
		}
		set
		{
			S.MicroMovementStabilizer = value;
			OnPropertyChanged("MicroMovementStabilizer");
		}
	}

	public string PresetName => S.Preset.ToString();

	public string AimFeelName => S.AimFeel.ToString();

	public string CurveName => S.DragResponseCurve.ToString();

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

	public double RawMagnitude
	{
		get
		{
			return _rawMagnitude;
		}
		private set
		{
			SetProperty(ref _rawMagnitude, value, "RawMagnitude");
		}
	}

	public double ProcessedMagnitude
	{
		get
		{
			return _processedMagnitude;
		}
		private set
		{
			SetProperty(ref _processedMagnitude, value, "ProcessedMagnitude");
		}
	}

	public double Distance
	{
		get
		{
			return _distance;
		}
		private set
		{
			SetProperty(ref _distance, value, "Distance");
		}
	}

	public double Speed
	{
		get
		{
			return _speed;
		}
		private set
		{
			SetProperty(ref _speed, value, "Speed");
		}
	}

	public double Stability
	{
		get
		{
			return _stability;
		}
		private set
		{
			SetProperty(ref _stability, value, "Stability");
		}
	}

	public double Smoothness
	{
		get
		{
			return _smoothness;
		}
		private set
		{
			SetProperty(ref _smoothness, value, "Smoothness");
		}
	}

	public double BeforeStability
	{
		get
		{
			return _beforeStability;
		}
		private set
		{
			SetProperty(ref _beforeStability, value, "BeforeStability");
		}
	}

	public double BeforeSmoothness
	{
		get
		{
			return _beforeSmoothness;
		}
		private set
		{
			SetProperty(ref _beforeSmoothness, value, "BeforeSmoothness");
		}
	}

	public string ResponseLabel
	{
		get
		{
			return _responseLabel;
		}
		private set
		{
			SetProperty(ref _responseLabel, value, "ResponseLabel");
		}
	}

	public List<double> RawSeries
	{
		get
		{
			return _rawSeries;
		}
		private set
		{
			SetProperty(ref _rawSeries, value, "RawSeries");
		}
	}

	public List<double> ProcessedSeries
	{
		get
		{
			return _processedSeries;
		}
		private set
		{
			SetProperty(ref _processedSeries, value, "ProcessedSeries");
		}
	}

	public ICommand ApplyCommand { get; }

	public ICommand ResetLabCommand { get; }

	public ICommand LightCommand { get; }

	public ICommand BalancedCommand { get; }

	public ICommand StableCommand { get; }

	public ICommand FastDragCommand { get; }

	public ICommand PrecisionCommand { get; }

	public ICommand CustomCommand { get; }

	public ICommand AimLightCommand { get; }

	public ICommand AimSmoothCommand { get; }

	public ICommand AimStableCommand { get; }

	public ICommand AimResponsiveCommand { get; }

	public ICommand AimPrecisionCommand { get; }

	public EasyDragLabViewModel(AppHost host)
	{
		_host = host;
		ApplyCommand = new RelayCommand((Action)delegate
		{
			_host.Settings.Save();
			_host.EasyDrag.Reset();
			Status = "Easy Drag settings applied.";
		}, (Func<bool>?)null);
		ResetLabCommand = new RelayCommand(ResetLab);
		LightCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.Light);
		}, (Func<bool>?)null);
		BalancedCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.Balanced);
		}, (Func<bool>?)null);
		StableCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.Stable);
		}, (Func<bool>?)null);
		FastDragCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.FastDrag);
		}, (Func<bool>?)null);
		PrecisionCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.Precision);
		}, (Func<bool>?)null);
		CustomCommand = new RelayCommand((Action)delegate
		{
			Preset(EasyDragPreset.Custom);
		}, (Func<bool>?)null);
		AimLightCommand = new RelayCommand((Action)delegate
		{
			Aim(AimFeel.Light);
		}, (Func<bool>?)null);
		AimSmoothCommand = new RelayCommand((Action)delegate
		{
			Aim(AimFeel.Smooth);
		}, (Func<bool>?)null);
		AimStableCommand = new RelayCommand((Action)delegate
		{
			Aim(AimFeel.Stable);
		}, (Func<bool>?)null);
		AimResponsiveCommand = new RelayCommand((Action)delegate
		{
			Aim(AimFeel.Responsive);
		}, (Func<bool>?)null);
		AimPrecisionCommand = new RelayCommand((Action)delegate
		{
			Aim(AimFeel.Precision);
		}, (Func<bool>?)null);
		UpdateCurve();
	}

	public void ProcessDrag(double dx, double dy, double dt = 0.016)
	{
		EasyDragSettings s = new EasyDragSettings
		{
			DragSensitivity = 1.0,
			DragWeight = 1.0,
			DragSmoothness = 0.0,
			InputSmoothing = 0.0,
			DragAcceleration = 0.0,
			InitialDragResponse = 1.0,
			MicroMovementStabilizer = StabilizerLevel.Off,
			InputDeadzone = 0.0,
			DragResponseCurve = ResponseCurve.Linear,
			MaxDragSpeed = 1000000000.0
		};
		DragSample dragSample = _rawProcessor.Process(dx, dy, s, dt);
		DragSample dragSample2 = _engineProcessor.Process(dx, dy, S, dt);
		RawMagnitude = dragSample.RawMagnitude;
		ProcessedMagnitude = dragSample2.OutMagnitude;
		Distance = _engineProcessor.TotalDistance;
		Speed = dragSample2.Speed;
		Stability = dragSample2.StabilityScore;
		Smoothness = dragSample2.SmoothnessScore;
		BeforeStability = dragSample.StabilityScore;
		BeforeSmoothness = dragSample.SmoothnessScore;
		ResponseLabel = ((dragSample2.OutMagnitude > dragSample2.RawMagnitude * 1.05) ? "FAST" : ((dragSample2.OutMagnitude < dragSample2.RawMagnitude * 0.95) ? "PRECISE" : "BALANCED"));
		List<double> list = new List<double>(RawSeries) { Math.Min(100.0, dragSample.RawMagnitude) };
		List<double> list2 = new List<double>(ProcessedSeries) { Math.Min(100.0, dragSample2.OutMagnitude) };
		if (list.Count > 120)
		{
			list.RemoveAt(0);
		}
		if (list2.Count > 120)
		{
			list2.RemoveAt(0);
		}
		RawSeries = list;
		ProcessedSeries = list2;
	}

	private void ResetLab()
	{
		_rawProcessor.Reset();
		_engineProcessor.Reset();
		RawSeries = new List<double>();
		ProcessedSeries = new List<double>();
		double num2 = (Speed = 0.0);
		double num4 = (Distance = num2);
		double rawMagnitude = (ProcessedMagnitude = num4);
		RawMagnitude = rawMagnitude;
		num2 = (BeforeSmoothness = 100.0);
		num4 = (BeforeStability = num2);
		rawMagnitude = (Smoothness = num4);
		Stability = rawMagnitude;
		ResponseLabel = "—";
		Status = "Lab reset.";
	}

	private void Preset(EasyDragPreset preset)
	{
		EasyDragEngine.ApplyPreset(S, preset);
		RefreshAll();
		Status = $"Preset '{preset}' applied.";
	}

	private void Aim(AimFeel feel)
	{
		EasyDragEngine.ApplyAimFeel(S, feel);
		RefreshAll();
		Status = $"Aim Feel '{feel}' applied (mouse feel / input consistency).";
	}

	private void RefreshAll()
	{
		OnPropertyChanged("PresetName");
		OnPropertyChanged("AimFeelName");
		OnPropertyChanged("CurveName");
		PropertyInfo[] properties = GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			bool flag;
			switch (propertyInfo.Name)
			{
			case "DragStability":
			case "InputDeadzone":
			case "DragSmoothness":
			case "InputSmoothing":
			case "DragWeight":
			case "DragSensitivity":
			case "DragAcceleration":
			case "VerticalDragMultiplier":
			case "HorizontalDragMultiplier":
			case "InitialDragResponse":
			case "MaxDragSpeed":
			case "MicroMovementStabilizer":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				OnPropertyChanged(propertyInfo.Name);
			}
		}
		UpdateCurve();
	}

	private void UpdateCurve()
	{
		List<double> list = new List<double>();
		for (int i = 0; i <= 50; i++)
		{
			list.Add(Curves.Evaluate(S.DragResponseCurve, (double)i / 50.0, S.CustomCurvePoints) * 100.0);
		}
		Curve = list;
	}
}
