using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPXPirate.Models;

namespace HPXPirate.Services;

public sealed class ProfileManager
{
	private readonly ILogger _log;

	private readonly List<GameProfile> _profiles = new List<GameProfile>();

	public IReadOnlyList<GameProfile> Profiles => _profiles;

	public event Action? ProfilesChanged;

	public ProfileManager(ILogger log)
	{
		_log = log;
	}

	public void LoadAll()
	{
		_profiles.Clear();
		try
		{
			AppPaths.EnsureCreated();
			foreach (string item in Directory.EnumerateFiles(AppPaths.Profiles, "*.json"))
			{
				try
				{
					GameProfile gameProfile = JsonHelper.Deserialize<GameProfile>(File.ReadAllText(item));
					if (gameProfile != null)
					{
						_profiles.Add(gameProfile);
					}
				}
				catch (Exception ex)
				{
					_log.Warn("Skipping unreadable profile '" + Path.GetFileName(item) + "': " + ex.Message);
				}
			}
			if (_profiles.Count == 0)
			{
				SeedDefaults();
			}
			_log.Info($"Loaded {_profiles.Count} game profile(s).");
		}
		catch (Exception ex2)
		{
			_log.Error("Failed to load profiles.", ex2);
		}
		this.ProfilesChanged?.Invoke();
	}

	private void SeedDefaults()
	{
		string[] array = new string[4] { "Free Fire", "PUBG Mobile", "COD Mobile", "Game Custom" };
		for (int i = 0; i < array.Length; i++)
		{
			GameProfile gameProfile = GameProfile.CreateDefault(array[i]);
			_profiles.Add(gameProfile);
			Save(gameProfile);
		}
		_log.Info("Seeded default game profiles.");
	}

	public void Save(GameProfile profile)
	{
		try
		{
			AppPaths.EnsureCreated();
			profile.Modified = DateTime.Now;
			File.WriteAllText(Path.Combine(AppPaths.Profiles, SafeName(profile.Id) + ".json"), JsonHelper.Serialize(profile));
			if (!_profiles.Contains(profile))
			{
				_profiles.Add(profile);
			}
			this.ProfilesChanged?.Invoke();
		}
		catch (Exception ex)
		{
			_log.Error("Failed to save profile '" + profile.Game + "'.", ex);
		}
	}

	public void Delete(GameProfile profile)
	{
		try
		{
			string path = Path.Combine(AppPaths.Profiles, SafeName(profile.Id) + ".json");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			_profiles.Remove(profile);
			this.ProfilesChanged?.Invoke();
			_log.Info("Deleted profile '" + profile.Game + "'.");
		}
		catch (Exception ex)
		{
			_log.Error("Failed to delete profile '" + profile.Game + "'.", ex);
		}
	}

	public GameProfile Reset(GameProfile profile)
	{
		GameProfile gameProfile = GameProfile.CreateDefault(profile.Game);
		gameProfile.Id = profile.Id;
		gameProfile.ExecutablePath = profile.ExecutablePath;
		Save(gameProfile);
		return gameProfile;
	}

	public string Export(GameProfile profile, string targetPath)
	{
		File.WriteAllText(targetPath, JsonHelper.Serialize(profile));
		_log.Info($"Exported profile '{profile.Game}' to {targetPath}.");
		return targetPath;
	}

	public GameProfile Import(string sourcePath)
	{
		GameProfile gameProfile = JsonHelper.Deserialize<GameProfile>(File.ReadAllText(sourcePath)) ?? throw new InvalidDataException("Profile file is not valid JSON.");
		gameProfile.Id = Guid.NewGuid().ToString("N");
		Save(gameProfile);
		_log.Info("Imported profile '" + gameProfile.Game + "'.");
		return gameProfile;
	}

	public string Backup(GameProfile profile)
	{
		AppPaths.EnsureCreated();
		string text = Path.Combine(AppPaths.Backups, $"{SafeName(profile.Game)}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
		File.WriteAllText(text, JsonHelper.Serialize(profile));
		_log.Info("Backed up profile '" + profile.Game + "'.");
		return text;
	}

	public GameProfile Restore(string backupPath)
	{
		GameProfile gameProfile = JsonHelper.Deserialize<GameProfile>(File.ReadAllText(backupPath)) ?? throw new InvalidDataException("Backup file is not valid JSON.");
		Save(gameProfile);
		_log.Info("Restored profile '" + gameProfile.Game + "' from backup.");
		return gameProfile;
	}

	public GameProfile? FindByExecutable(string exePathOrName)
	{
		string exePathOrName2 = exePathOrName;
		if (string.IsNullOrWhiteSpace(exePathOrName2))
		{
			return null;
		}
		string name = Path.GetFileNameWithoutExtension(exePathOrName2).ToLowerInvariant();
		return _profiles.FirstOrDefault(delegate(GameProfile p)
		{
			if (string.IsNullOrWhiteSpace(p.ExecutablePath))
			{
				return false;
			}
			return Path.GetFileNameWithoutExtension(p.ExecutablePath).ToLowerInvariant() == name || p.ExecutablePath.ToLowerInvariant() == exePathOrName2.ToLowerInvariant();
		});
	}

	private static string SafeName(string s)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			s = s.Replace(oldChar, '_');
		}
		return s;
	}
}
