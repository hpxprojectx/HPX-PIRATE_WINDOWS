using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HPXPirate.Models;
using HPXPirate.Services;
using Microsoft.Win32;

namespace HPXPirate.ViewModels;

public sealed class GameProfilesViewModel : ViewModelBase
{
	private readonly AppHost _host;

	private GameProfile? _selected;

	private string _status = "";

	public ObservableCollection<GameProfile> Profiles { get; } = new ObservableCollection<GameProfile>();


	public GameProfile? SelectedProfile
	{
		get
		{
			return _selected;
		}
		set
		{
			if (SetProperty(ref _selected, value, "SelectedProfile"))
			{
				((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
			}
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

	public bool AutoLoad
	{
		get
		{
			return _host.AppSettings.AutoLoadProfiles;
		}
		set
		{
			_host.AppSettings.AutoLoadProfiles = value;
			OnPropertyChanged("AutoLoad");
			_host.Settings.Save();
		}
	}

	public ICommand NewCommand { get; }

	public ICommand SaveCommand { get; }

	public ICommand DeleteCommand { get; }

	public ICommand DuplicateCommand { get; }

	public ICommand ResetCommand { get; }

	public ICommand ApplyCommand { get; }

	public ICommand ImportCommand { get; }

	public ICommand ExportCommand { get; }

	public ICommand BackupCommand { get; }

	public ICommand RestoreCommand { get; }

	public GameProfilesViewModel(AppHost host)
	{
		_host = host;
		NewCommand = new RelayCommand(NewProfile);
		SaveCommand = new RelayCommand(SaveProfile, () => SelectedProfile != null);
		DeleteCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null);
		DuplicateCommand = new RelayCommand(DuplicateProfile, () => SelectedProfile != null);
		ResetCommand = new RelayCommand(ResetProfile, () => SelectedProfile != null);
		ApplyCommand = new RelayCommand(ApplyProfile, () => SelectedProfile != null);
		ImportCommand = new RelayCommand(ImportProfile);
		ExportCommand = new RelayCommand(ExportProfile, () => SelectedProfile != null);
		BackupCommand = new RelayCommand(BackupProfile, () => SelectedProfile != null);
		RestoreCommand = new RelayCommand(RestoreProfile);
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

	private void NewProfile()
	{
		GameProfile gameProfile = GameProfile.CreateDefault("New Game");
		_host.Profiles.Save(gameProfile);
		Profiles.Add(gameProfile);
		SelectedProfile = gameProfile;
		Status = "New profile created.";
	}

	private void SaveProfile()
	{
		if (SelectedProfile != null)
		{
			_host.Profiles.Save(SelectedProfile);
			Status = "Saved '" + SelectedProfile.Game + "'.";
		}
	}

	private void DeleteProfile()
	{
		if (SelectedProfile != null)
		{
			string game = SelectedProfile.Game;
			_host.Profiles.Delete(SelectedProfile);
			Profiles.Remove(SelectedProfile);
			SelectedProfile = Profiles.FirstOrDefault();
			Status = "Deleted '" + game + "'.";
		}
	}

	private void DuplicateProfile()
	{
		if (SelectedProfile != null)
		{
			GameProfile gameProfile = JsonHelper.Deserialize<GameProfile>(JsonHelper.Serialize(SelectedProfile));
			gameProfile.Id = Guid.NewGuid().ToString("N");
			gameProfile.Game = SelectedProfile.Game + " (copy)";
			_host.Profiles.Save(gameProfile);
			Profiles.Add(gameProfile);
			SelectedProfile = gameProfile;
			Status = "Profile duplicated.";
		}
	}

	private void ResetProfile()
	{
		if (SelectedProfile != null)
		{
			GameProfile gameProfile = _host.Profiles.Reset(SelectedProfile);
			int num = Profiles.IndexOf(SelectedProfile);
			if (num >= 0)
			{
				Profiles[num] = gameProfile;
			}
			SelectedProfile = gameProfile;
			Status = "Profile reset to defaults.";
		}
	}

	private void ApplyProfile()
	{
		if (SelectedProfile != null)
		{
			GameProfile selectedProfile = SelectedProfile;
			AppSettings appSettings = _host.AppSettings;
			appSettings.Mouse = selectedProfile.Mouse;
			appSettings.DragAssist = selectedProfile.DragAssist;
			appSettings.EasyDrag = selectedProfile.EasyDrag;
			appSettings.Crosshair = selectedProfile.Crosshair;
			appSettings.DefaultBoostMode = selectedProfile.PerformanceMode;
			_host.Settings.Save();
			_host.Crosshair.ApplySettings(appSettings.Crosshair);
			Status = "Applied profile '" + selectedProfile.Game + "'.";
		}
	}

	private void ImportProfile()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		OpenFileDialog val = new OpenFileDialog
		{
			Filter = "HPX Profile (*.json)|*.json|All files (*.*)|*.*"
		};
		if (!((CommonDialog)val).ShowDialog().GetValueOrDefault())
		{
			return;
		}
		try
		{
			GameProfile gameProfile = _host.Profiles.Import(((FileDialog)val).FileName);
			Profiles.Add(gameProfile);
			SelectedProfile = gameProfile;
			Status = "Imported '" + gameProfile.Game + "'.";
		}
		catch (Exception ex)
		{
			Status = "Import failed: " + ex.Message;
		}
	}

	private void ExportProfile()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		if (SelectedProfile == null)
		{
			return;
		}
		SaveFileDialog val = new SaveFileDialog
		{
			Filter = "HPX Profile (*.json)|*.json",
			FileName = SelectedProfile.Game.Replace(' ', '_') + ".json"
		};
		if (!((CommonDialog)val).ShowDialog().GetValueOrDefault())
		{
			return;
		}
		try
		{
			_host.Profiles.Export(SelectedProfile, ((FileDialog)val).FileName);
			Status = "Exported to " + ((FileDialog)val).FileName;
		}
		catch (Exception ex)
		{
			Status = "Export failed: " + ex.Message;
		}
	}

	private void BackupProfile()
	{
		if (SelectedProfile == null)
		{
			return;
		}
		try
		{
			string text = _host.Profiles.Backup(SelectedProfile);
			Status = "Backup created: " + text;
		}
		catch (Exception ex)
		{
			Status = "Backup failed: " + ex.Message;
		}
	}

	private void RestoreProfile()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		OpenFileDialog val = new OpenFileDialog
		{
			Filter = "HPX Profile Backup (*.json)|*.json|All files (*.*)|*.*",
			InitialDirectory = AppPaths.Backups
		};
		if (!((CommonDialog)val).ShowDialog().GetValueOrDefault())
		{
			return;
		}
		try
		{
			GameProfile gameProfile = _host.Profiles.Restore(((FileDialog)val).FileName);
			if (!Profiles.Contains(gameProfile))
			{
				Profiles.Add(gameProfile);
			}
			SelectedProfile = gameProfile;
			Status = "Restored '" + gameProfile.Game + "'.";
		}
		catch (Exception ex)
		{
			Status = "Restore failed: " + ex.Message;
		}
	}
}
