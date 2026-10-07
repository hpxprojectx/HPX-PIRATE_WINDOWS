using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HPXPirate.Models;
using HPXPirate.Services;

namespace HPXPirate.ViewModels;

public sealed class ControlMapperViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private GameProfile? _selectedProfile;

	private ControlMapping? _selectedMapping;

	private ControlMapping? _capturing;

	private string _status = "Select a control point and press CAPTURE to assign a key.";

	public ObservableCollection<GameProfile> Profiles { get; } = new ObservableCollection<GameProfile>();


	public ObservableCollection<ControlMapping> Mappings { get; } = new ObservableCollection<ControlMapping>();


	public GameProfile? SelectedProfile
	{
		get
		{
			return _selectedProfile;
		}
		set
		{
			if (SetProperty(ref _selectedProfile, value, "SelectedProfile"))
			{
				ReloadMappings();
			}
		}
	}

	public ControlMapping? SelectedMapping
	{
		get
		{
			return _selectedMapping;
		}
		set
		{
			SetProperty(ref _selectedMapping, value, "SelectedMapping");
			((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
			((RelayCommand)CaptureCommand).RaiseCanExecuteChanged();
		}
	}

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

	public bool CaptureActive => _capturing != null;

	public ICommand AddKeyCommand { get; }

	public ICommand AddMouseCommand { get; }

	public ICommand RemoveCommand { get; }

	public ICommand CaptureCommand { get; }

	public ICommand SaveCommand { get; }

	public ControlMapperViewModel(AppHost host)
	{
		_host = host;
		AddKeyCommand = new RelayCommand((Action)delegate
		{
			AddMapping(MappingInputType.Keyboard);
		}, (Func<bool>?)null);
		AddMouseCommand = new RelayCommand((Action)delegate
		{
			AddMapping(MappingInputType.MouseButton);
		}, (Func<bool>?)null);
		RemoveCommand = new RelayCommand(RemoveMapping, () => SelectedMapping != null);
		CaptureCommand = new RelayCommand(delegate
		{
			StartCapture(SelectedMapping);
		}, () => SelectedMapping != null);
		SaveCommand = new RelayCommand(SaveProfile, () => SelectedProfile != null);
		_host.Input.KeyDown += OnKeyDown;
		_host.Input.MouseButtonDown += OnMouseButtonDown;
		Reload();
	}

	private void Reload()
	{
		Profiles.Clear();
		foreach (GameProfile profile in _host.Profiles.Profiles)
		{
			Profiles.Add(profile);
		}
		SelectedProfile = Profiles.FirstOrDefault();
	}

	private void ReloadMappings()
	{
		Mappings.Clear();
		if (SelectedProfile == null)
		{
			return;
		}
		foreach (ControlMapping item in SelectedProfile.KeyboardMapping)
		{
			Mappings.Add(item);
		}
		foreach (ControlMapping item2 in SelectedProfile.MouseMapping)
		{
			Mappings.Add(item2);
		}
	}

	private void AddMapping(MappingInputType type)
	{
		if (SelectedProfile != null)
		{
			ControlMapping controlMapping = new ControlMapping
			{
				InputType = type,
				Input = ((type == MappingInputType.Keyboard) ? "W" : "MouseLeft"),
				Action = ((type == MappingInputType.Keyboard) ? "New Action" : "Mouse Action")
			};
			if (type == MappingInputType.Keyboard)
			{
				SelectedProfile.KeyboardMapping.Add(controlMapping);
			}
			else
			{
				SelectedProfile.MouseMapping.Add(controlMapping);
			}
			Mappings.Add(controlMapping);
			SelectedMapping = controlMapping;
		}
	}

	private void RemoveMapping()
	{
		if (SelectedProfile != null && SelectedMapping != null)
		{
			SelectedProfile.KeyboardMapping.Remove(SelectedMapping);
			SelectedProfile.MouseMapping.Remove(SelectedMapping);
			Mappings.Remove(SelectedMapping);
			SelectedMapping = null;
		}
	}

	private void StartCapture(ControlMapping mapping)
	{
		_capturing = mapping;
		_host.Input.CaptureEnabled = true;
		Status = "Press any key or mouse button to assign...";
		OnPropertyChanged("CaptureActive");
	}

	private void OnKeyDown(string key)
	{
		string key2 = key;
		if (_capturing == null)
		{
			return;
		}
		Application current = Application.Current;
		if (current != null)
		{
			((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
			{
				_capturing.Input = key2;
				_capturing.InputType = MappingInputType.Keyboard;
				FinishCapture();
			});
		}
	}

	private void OnMouseButtonDown(string button)
	{
		string button2 = button;
		if (_capturing == null)
		{
			return;
		}
		Application current = Application.Current;
		if (current != null)
		{
			((DispatcherObject)current).Dispatcher.Invoke((Action)delegate
			{
				_capturing.Input = button2;
				_capturing.InputType = MappingInputType.MouseButton;
				FinishCapture();
			});
		}
	}

	private void FinishCapture()
	{
		_capturing = null;
		_host.Input.CaptureEnabled = false;
		Status = "Mapping updated. Remember to SAVE.";
		OnPropertyChanged("CaptureActive");
		ControlMapping selectedMapping = SelectedMapping;
		if (selectedMapping != null)
		{
			int num = Mappings.IndexOf(selectedMapping);
			if (num >= 0)
			{
				Mappings.RemoveAt(num);
				Mappings.Insert(num, selectedMapping);
			}
			SelectedMapping = selectedMapping;
		}
	}

	private void SaveProfile()
	{
		if (SelectedProfile != null)
		{
			_host.Profiles.Save(SelectedProfile);
			Status = "Profile saved.";
		}
	}
}
