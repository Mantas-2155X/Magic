using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace Managers
{
	public class SettingsManager
	{
		private static SettingsManager instance;
		public static SettingsManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new SettingsManager();
				instance.setupSettings();
				return instance;
			}
		}

		public string Path { get; private set; } = "data/";
		public string Name { get; private set; } = "settings.tsv";

		private readonly Dictionary<string, Setting> settings = new ();

		#region Manage

		public void AddSetting(string key, string name, string description, ESettingType type, object value, UnityAction<object, object> changed)
		{
			if (settings.ContainsKey(key))
			{
				Debug.LogWarning("[SettingsManager] Setting with the same key already exists");
				return;
			}

			settings.Add(key, new Setting(name, description, type, value, value, changed));
			// todo: timer this saveSettings();
		}

		public void RemoveSetting(string key)
		{
			settings.Remove(key);
			// todo: timer this saveSettings();
		}

		public void ClearSettings()
		{
			settings.Clear();
			// todo: timer this saveSettings();
		}

		public void SetSetting(string key, object value)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return;
			}

			var previousValue = setting.Value;
			setting.Value = value;
			setting.Changed?.Invoke(previousValue, value);
			
			// todo: timer this saveSettings();
		}

		public void DefaultSetting(string key)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return;
			}
			
			SetSetting(key, setting.DefaultValue);
		}
		
		#endregion

		#region Get
		
		public string? GetString(string key)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return null;
			}

			if (setting.Type != ESettingType.String)
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} is not a string type");
				return null;
			}

			return setting.Value?.ToString();
		}
		
		public int? GetInt(string key)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return null;
			}

			if (setting.Type != ESettingType.Int)
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} is not an int type");
				return null;
			}

			if (setting.Value == null || !int.TryParse(setting.Value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
				return null;

			return intValue;
		}
		
		public float? GetFloat(string key)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return null;
			}

			if (setting.Type != ESettingType.Float)
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} is not a float type");
				return null;
			}

			if (setting.Value == null || !float.TryParse(setting.Value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
				return null;

			return floatValue;
		}
		
		public bool? GetBool(string key)
		{
			if (!settings.TryGetValue(key, out var setting))
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} does not exist");
				return null;
			}

			if (setting.Type != ESettingType.Bool)
			{
				Debug.LogWarning($"[SettingsManager] Setting with the key {key} is not a bool type");
				return null;
			}

			if (setting.Value == null || !bool.TryParse(setting.Value.ToString(), out var boolValue))
				return null;

			return boolValue;
		}
		
		#endregion

		#region Internal

		private void saveSettings()
		{
			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);
			
			var builder = new StringBuilder();

			foreach (var (key, setting) in settings)
			{
				string value;

				switch (setting.Type)
				{
					case ESettingType.String:
						value = GetString(key);
						break;
					case ESettingType.Int:
						value = GetInt(key)?.ToString();
						break;
					case ESettingType.Float:
						value = GetFloat(key)?.ToString(CultureInfo.InvariantCulture);
						break;
					case ESettingType.Bool:
						value = GetBool(key)?.ToString();
						break;
					default:
						throw new NotImplementedException();
				}

				value ??= "";
				
				builder.AppendLine($"{key}\t{value}");
			}
			
			File.WriteAllText(System.IO.Path.Combine(Path, Name), builder.ToString());
		}

		private void loadSettings()
		{
			var filePath = System.IO.Path.Combine(Path, Name);
			if (!File.Exists(filePath))
				return;
			
			var lines = File.ReadAllLines(filePath);
			if (lines.Length == 0)
				return;

			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				if (string.IsNullOrEmpty(line))
					continue;
				
				var split = line.Split('\t');
				if (split.Length != 2)
				{
					Debug.LogWarning($"[SettingsManager] Setting at line {i} is the wrong length");
					continue;
				}
				
				var key = split[0];
				if (string.IsNullOrEmpty(key))
				{
					Debug.LogWarning($"[SettingsManager] Setting at line {i} key is invalid");
					continue;
				}
				
				var valueStr = split[1];
				if (string.IsNullOrEmpty(valueStr))
				{
					Debug.LogWarning($"[SettingsManager] Setting at line {i} value is invalid");
					continue;
				}

				if (settings.TryGetValue(key, out var setting))
				{
					object value;

					switch (setting.Type)
					{
						case ESettingType.String:
							value = valueStr;
							break;
						case ESettingType.Int:
							if (!int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
							{
								Debug.LogWarning($"[SettingsManager] Setting at line {i} failed to parse int value");
								continue;
							}
							value = intValue;
							break;
						case ESettingType.Float:
							if (!float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
							{
								Debug.LogWarning($"[SettingsManager] Setting at line {i} failed to parse float value");
								continue;
							}
							value = floatValue;
							break;
						case ESettingType.Bool:
							if (!bool.TryParse(valueStr, out var boolValue))
							{
								Debug.LogWarning($"[SettingsManager] Setting at line {i} failed to parse bool value");
								continue;
							}
							value = boolValue;
							break;
						default:
							throw new NotImplementedException();
					}

					SetSetting(key, value);
				}
			}
		}

		private void setupSettings()
		{
			AddSetting("video-vsync", "SETTING_VIDEO_VSYNC", "SETTING_VIDEO_VSYNC_DESC", ESettingType.Bool, false, (previousValue, newValue) =>
			{
				var setting = Convert.ToBoolean(newValue);
				QualitySettings.vSyncCount = setting ? 1 : 0;
			});
			
			AddSetting("graphics-aa", "SETTING_GRAPHICS_AA", "SETTING_GRAPHICS_AA_DESC", ESettingType.Int, 8, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 2 or 4 or 8))
				{
					Debug.LogWarning("[SettingsManager] Invalid antialiasing mode provided, skipping");
					return;
				}

				var renderAsset = RenderManager.Instance.RenderAsset;
				if (renderAsset == null)
				{
					Debug.LogError("[SettingsManager] Failed to get render asset");
					return;
				}

				// Why is "None" set at 1?
				if (setting == 0)
					setting = 1;
				
				renderAsset.msaaSampleCount = setting;
			});

			AddSetting("graphics-shadowquality", "SETTING_GRAPHICS_SHADOWQUALITY", "SETTING_GRAPHICS_SHADOWQUALITY_DESC", ESettingType.Int, 4096, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (1024 or 2048 or 4096))
				{
					Debug.LogWarning("[SettingsManager] Invalid shadow quality mode provided, skipping");
					return;
				}

				var renderAsset = RenderManager.Instance.RenderAsset;
				if (renderAsset == null)
				{
					Debug.LogError("[SettingsManager] Failed to get render asset");
					return;
				}
				
				renderAsset.mainLightShadowmapResolution = setting;
				renderAsset.additionalLightsShadowmapResolution = setting;

				var softShadowsQuality = renderAsset.GetType().GetProperty("softShadowQuality", BindingFlags.NonPublic | BindingFlags.Instance);
				if (softShadowsQuality == null)
				{
					Debug.LogError("[SettingsManager] Failed to get softShadowQuality");
					return;
				}

				switch (setting)
				{
					case 1024:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.Low);
						break;
					case 2048:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.Medium);
						break;
					case 4096:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.High);
						break;
				}
			});

			AddSetting("graphics-texturequality", "SETTING_GRAPHICS_TEXTUREQUALITY", "SETTING_GRAPHICS_TEXTUREQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2))
				{
					Debug.LogWarning("[SettingsManager] Invalid texture quality mode provided, skipping");
					return;
				}
				
				switch (setting)
				{
					case 0:
						QualitySettings.globalTextureMipmapLimit = 2;
						QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
						break;
					case 1:
						QualitySettings.globalTextureMipmapLimit = 1;
						QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
						break;
					case 2:
						QualitySettings.globalTextureMipmapLimit = 0;
						QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
						break;
				}
			});
			
			AddSetting("graphics-modelquality", "SETTING_GRAPHICS_MODELQUALITY", "SETTING_GRAPHICS_MODELQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2))
				{
					Debug.LogWarning("[SettingsManager] Invalid model quality mode provided, skipping");
					return;
				}
				
				switch (setting)
				{
					case 0:
						QualitySettings.lodBias = 0.5f;
						break;
					case 1:
						QualitySettings.lodBias = 1;
						break;
					case 2:
						QualitySettings.lodBias = 1.5f;
						break;
				}
			});
			
			AddSetting("graphics-shaderquality", "SETTING_GRAPHICS_SHADERQUALITY", "SETTING_GRAPHICS_SHADERQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2))
				{
					Debug.LogWarning("[SettingsManager] Invalid shader quality mode provided, skipping");
					return;
				}
				
				var renderAsset = RenderManager.Instance.RenderAsset;
				if (renderAsset == null)
				{
					Debug.LogError("[SettingsManager] Failed to get render asset");
					return;
				}

				var profile = renderAsset.volumeProfile;
				if (profile == null)
				{
					Debug.LogError("[SettingsManager] Failed to get volume profile");
					return;
				}
				
				var useBloom = false;
				var useVignette = false;
				var useChromaticAberration = false;
				var useFilmGrain = false;
				var useDepthOfField = false;

				switch (setting)
				{
					case 0:
						useBloom = true;
						useVignette = false;
						useChromaticAberration = false;
						useFilmGrain = false;
						useDepthOfField = false;
						break;
					case 1:
						useBloom = true;
						useVignette = true;
						useChromaticAberration = false;
						useFilmGrain = true;
						useDepthOfField = false;
						break;
					case 2:
						useBloom = true;
						useVignette = true;
						useChromaticAberration = true;
						useFilmGrain = true;
						useDepthOfField = true;
						break;
				}

				if (profile.TryGet<Bloom>(out var bloom))
					bloom.active = useBloom;

				if (profile.TryGet<Vignette>(out var vignette))
					vignette.active = useVignette;

				if (profile.TryGet<ChromaticAberration>(out var chromaticAberration))
					chromaticAberration.active = useChromaticAberration;

				if (profile.TryGet<FilmGrain>(out var filmGrain))
					filmGrain.active = useFilmGrain;

				if (profile.TryGet<DepthOfField>(out var depthOfField))
					depthOfField.active = useDepthOfField;

				var ssao = RenderManager.Instance.SsaoFeature;
				if (ssao == null)
				{
					Debug.LogError("[SettingsManager] Failed to get SSAO feature");
					return;
				}

				ssao.SetActive(setting != 0);
				
				// SSAO disabled at low shader quality
				if (setting == 0)
					return;
				
				var settingsField = ssao.GetType().GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
				if (settingsField == null)
				{
					Debug.LogError("[SettingsManager] Failed to get m_Settings");
					return;
				}

				var featureSettings = settingsField.GetValue(ssao);
				if (featureSettings == null)
				{
					Debug.LogError("[SettingsManager] Failed to get SSAO settings");
					return;
				}

				var type = featureSettings.GetType();

				var downsample = type.GetField("Downsample", BindingFlags.NonPublic | BindingFlags.Instance)!;
				var samples = type.GetField("Samples", BindingFlags.NonPublic | BindingFlags.Instance)!;
				var blurQuality = type.GetField("BlurQuality", BindingFlags.NonPublic | BindingFlags.Instance)!;

				switch (setting)
				{
					case 1:
						downsample.SetValue(featureSettings, true);
						samples.SetValue(featureSettings, 1);
						blurQuality.SetValue(featureSettings, 1);
						break;
					case 2:
						downsample.SetValue(featureSettings, false);
						samples.SetValue(featureSettings, 0);
						blurQuality.SetValue(featureSettings, 0);
						break;
				}
			});
			
			loadSettings();
			saveSettings();
		}
		
		#endregion

		public class Setting
		{
			public string Name;
			public string Description;
		
			public ESettingType Type;

			public object DefaultValue;
			public object Value;

			public readonly UnityAction<object, object> Changed;

			public Setting(string name, string description, ESettingType type, object defaultValue, object value, UnityAction<object, object> changed)
			{
				Name = name;
				Description = description;
				Type = type;
				DefaultValue = defaultValue;
				Value = value;
				Changed = changed;
			}
		}
		
		public enum ESettingType
		{
			String,
			Int,
			Float,
			Bool
		}
	}
}