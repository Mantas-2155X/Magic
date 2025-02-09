using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using AI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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
		private readonly Dictionary<string, Tuple<InputAction, int>> keybinds = new ();

		private CancellationTokenSource cancellationToken = new ();
		
		#region Manage

		public bool AddSetting(string key, string name, string description, ESettingType type, object value, UnityAction<object, object> changed)
		{
			if (settings.ContainsKey(key))
			{
				Debug.LogWarning("[SettingsManager] Setting with the same key already exists");
				return false;
			}

			settings.Add(key, new Setting(name, description, type, value, value, changed));
			saveSettings();

			return true;
		}
		
		public bool AddSetting(string key, string name, string description, InputAction inputAction, int bindingIndex, object value, UnityAction<object, object> changed)
		{
			if (!AddSetting(key, name, description, ESettingType.String, value, changed))
				return false;

			if (keybinds.ContainsKey(key))
			{
				Debug.LogWarning("[SettingsManager] Keybind with the same setting already exists");
				return false;
			}
			
			keybinds.Add(key, new Tuple<InputAction, int>(inputAction, bindingIndex));
			return true;
		}

		public void RemoveSetting(string key)
		{
			settings.Remove(key);
			saveSettings();
		}

		public Dictionary<string, Setting> GetSettings()
		{
			return settings;
		}
		
		public void ClearSettings()
		{
			settings.Clear();
			saveSettings();
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
			
			saveSettings();
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
		
		public void ResetSettings()
		{
			foreach (var pair in settings)
				DefaultSetting(pair.Key);
		}

		#endregion

		#region Get

		public Setting GetSetting(string key)
		{
			return settings.GetValueOrDefault(key);
		}

		public Tuple<InputAction, int> GetKeybind(string key)
		{
			return keybinds.GetValueOrDefault(key);
		}
		
		public string GetString(string key)
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
			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();

			saveSettingsDelayed(cancellationToken.Token).Forget();
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
					try
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
					catch (Exception e)
					{
						Debug.LogError($"[SettingsManager] Failed loading setting {key}, {e}");
					}
				}
			}
		}
		
		private void setupSettings()
		{
			#region Video

			var defaultResolution = "1920x1080";

			try
			{
				var displays = Display.displays;
				for (var i = 0; i < displays.Length; i++)
				{
					var display = displays[i];
					if (!display.active)
						continue;

					defaultResolution = $"{display.systemWidth}x{display.systemHeight}";
					break;
				}

				if (!RenderManager.Instance.Resolutions.Contains(defaultResolution))
				{
					defaultResolution = "1920x1080";
					Debug.LogWarning("[SettingsManager] Native resolution unsupported, defaulting");
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[SettingsManager] Failed grabbing native resolution, defaulting, {e}");
			}
			
			AddSetting("video-resolution", "SETTINGS_VIDEO_RESOLUTION", "SETTINGS_VIDEO_RESOLUTION_DESC", ESettingType.String, defaultResolution, (previousValue, newValue) =>
			{
				var setting = newValue.ToString();
				if (string.IsNullOrEmpty(setting))
				{
					Debug.LogWarning("[SettingsManager] Invalid resolution provided, skipping");
					return;
				}

				var resolutions = RenderManager.Instance.Resolutions;
				if (resolutions == null)
				{
					Debug.LogWarning("[SettingsManager] No valid resolutions found, skipping");
					return;
				}

				if (!resolutions.Contains(setting))
				{
					Debug.LogWarning("[SettingsManager] Unsupported resolution provided, skipping");
					return;
				}

				var split = setting.Split("x");
				
				var width = Convert.ToInt32(split[0]);
				var height = Convert.ToInt32(split[1]);
				
				Screen.SetResolution(width, height, GetBool("video-fullscreen")!.Value);
			});

			AddSetting("video-fullscreen", "SETTINGS_VIDEO_FULLSCREEN", "SETTINGS_VIDEO_FULLSCREEN_DESC", ESettingType.Bool, true, (previousValue, newValue) =>
			{
				Screen.fullScreen = Convert.ToBoolean(newValue);
			});
			
			AddSetting("video-vsync", "SETTINGS_VIDEO_VSYNC", "SETTINGS_VIDEO_VSYNC_DESC", ESettingType.Bool, true, (previousValue, newValue) =>
			{
				QualitySettings.vSyncCount = Convert.ToBoolean(newValue) ? 1 : 0;
			});

			AddSetting("video-renderscale", "SETTINGS_VIDEO_RENDERSCALE", "SETTINGS_VIDEO_RENDERSCALE_DESC", ESettingType.Float, 1f, (previousValue, newValue) =>
			{
				var setting = Convert.ToSingle(newValue);
				if (setting is < 0.1f or > 2f)
				{
					Debug.LogWarning("[SettingsManager] Invalid render scale provided, skipping");
					return;
				}
				
				var renderAsset = RenderManager.Instance.RenderAsset;
				if (renderAsset == null)
				{
					Debug.LogError("[SettingsManager] Failed to get render asset");
					return;
				}
				
				renderAsset.upscalingFilter = Mathf.Approximately(setting, 1f) ? UpscalingFilterSelection.Auto : UpscalingFilterSelection.FSR;
				renderAsset.renderScale = setting;
			});
			
			AddSetting("video-fpslimit", "SETTINGS_VIDEO_FPSLIMIT", "SETTINGS_VIDEO_FPSLIMIT_DESC", ESettingType.Int, 1000, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting < 15)
				{
					Debug.LogWarning("[SettingsManager] Invalid FPS limit provided, skipping");
					return;
				}
				
				Application.targetFrameRate = setting;
			});
				
			#endregion
			
			#region Graphics

			AddSetting("graphics-shadowquality", "SETTINGS_GRAPHICS_SHADOWQUALITY", "SETTINGS_GRAPHICS_SHADOWQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2 or 3))
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
				
				var softShadowsQuality = renderAsset.GetType().GetProperty("softShadowQuality", BindingFlags.NonPublic | BindingFlags.Instance);
				if (softShadowsQuality == null)
				{
					Debug.LogError("[SettingsManager] Failed to get softShadowQuality");
					return;
				}

				switch (setting)
				{
					case 0:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.Low);
						renderAsset.mainLightShadowmapResolution = 1024;
						renderAsset.additionalLightsShadowmapResolution = 1024;
						renderAsset.shadowDistance = 25f;
						break;
					case 1:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.Medium);
						renderAsset.mainLightShadowmapResolution = 2048;
						renderAsset.additionalLightsShadowmapResolution = 2048;
						renderAsset.shadowDistance = 40f;
						break;
					case 2:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.High);
						renderAsset.mainLightShadowmapResolution = 4096;
						renderAsset.additionalLightsShadowmapResolution = 4096;
						renderAsset.shadowDistance = 50f;
						break;
					case 3:
						softShadowsQuality.SetValue(renderAsset, SoftShadowQuality.High);
						renderAsset.mainLightShadowmapResolution = 8192;
						renderAsset.additionalLightsShadowmapResolution = 8192;
						renderAsset.shadowDistance = 75f;
						break;
				}
			});

			AddSetting("graphics-texturequality", "SETTINGS_GRAPHICS_TEXTUREQUALITY", "SETTINGS_GRAPHICS_TEXTUREQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
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
			
			AddSetting("graphics-modelquality", "SETTINGS_GRAPHICS_MODELQUALITY", "SETTINGS_GRAPHICS_MODELQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2 or 3))
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
						QualitySettings.lodBias = 1f;
						break;
					case 2:
						QualitySettings.lodBias = 2f;
						break;
					case 3:
						QualitySettings.lodBias = 3f;
						break;
				}
			});
			
			AddSetting("graphics-shaderquality", "SETTINGS_GRAPHICS_SHADERQUALITY", "SETTINGS_GRAPHICS_SHADERQUALITY_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2 or 3))
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
						useBloom = false;
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
					case 3:
						useBloom = true;
						useVignette = true;
						useChromaticAberration = true;
						useFilmGrain = true;
						useDepthOfField = true;
						break;
				}

				var components = profile.components;
				for (var i = 0; i < components.Count; i++)
				{
					var component = components[i];
					switch (component)
					{
						case Bloom:
							component.active = useBloom;
							break;
						case Vignette:
							component.active = useVignette;
							break;
						case ChromaticAberration:
							component.active = useChromaticAberration;
							break;
						case FilmGrain:
							component.active = useFilmGrain;
							break;
						case DepthOfField:
							component.active = useDepthOfField;
							break;
					}
				}

				VolumeManager.instance.OnVolumeProfileChanged(profile);

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
				var falloff = type.GetField("Falloff", BindingFlags.NonPublic | BindingFlags.Instance)!;

				switch (setting)
				{
					case 1:
						downsample.SetValue(featureSettings, true);
						samples.SetValue(featureSettings, 1);
						blurQuality.SetValue(featureSettings, 1);
						falloff.SetValue(featureSettings, 35);
						break;
					case 2:
						downsample.SetValue(featureSettings, true);
						samples.SetValue(featureSettings, 0);
						blurQuality.SetValue(featureSettings, 0);
						falloff.SetValue(featureSettings, 75);
						break;
					case 3:
						downsample.SetValue(featureSettings, false);
						samples.SetValue(featureSettings, 0);
						blurQuality.SetValue(featureSettings, 0);
						falloff.SetValue(featureSettings, 150);
						break;
				}
			});
			
			AddSetting("graphics-antialiasing", "SETTINGS_GRAPHICS_ANTIALIASING", "SETTINGS_GRAPHICS_ANTIALIASING_DESC", ESettingType.Int, 2, (previousValue, newValue) =>
			{
				var setting = Convert.ToInt32(newValue);
				if (setting is not (0 or 1 or 2 or 3))
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

				switch (setting)
				{
					case 0:
						renderAsset.msaaSampleCount = 1;
						break;
					case 1:
						renderAsset.msaaSampleCount = 2;
						break;
					case 2:
						renderAsset.msaaSampleCount = 4;
						break;
					case 3:
						renderAsset.msaaSampleCount = 8;
						break;
				}
			});

			AddSetting("graphics-shatterobjects", "SETTINGS_GRAPHICS_SHATTEROBJECTS", "SETTINGS_GRAPHICS_SHATTEROBJECTS_DESC", ESettingType.Bool, true, (previousValue, newValue) =>
			{
				
			});
			
			#endregion

			#region Controls

			AddSetting("controls-sensitivity-mouse", "SETTINGS_CONTROLS_SENSITIVITY", "SETTINGS_CONTROLS_SENSITIVITY_DESC", ESettingType.Float, 1f, (previousValue, newValue) =>
			{
				Player.MouseSensitivity = Convert.ToSingle(newValue);
			});
			
			AddSetting("controls-sensitivity-controller", "SETTINGS_CONTROLS_SENSITIVITY", "SETTINGS_CONTROLS_SENSITIVITY_DESC", ESettingType.Float, 1f, (previousValue, newValue) =>
			{
				Player.ControllerSensitivity = Convert.ToSingle(newValue);
			});
			
			AddSetting("controls-allowhotbarscrolling", "SETTINGS_CONTROLS_ALLOWHOTBARSCROLLING", "SETTINGS_CONTROLS_ALLOWHOTBARSCROLLING_DESC", ESettingType.Bool, true, (previousValue, newValue) =>
			{
				Player.AllowHotbarScrolling = Convert.ToBoolean(newValue);
			});
			
			AddSetting("controls-showselection", "SETTINGS_CONTROLS_SHOWSELECTION", "SETTINGS_CONTROLS_SHOWSELECTION_DESC", ESettingType.Bool, true, (previousValue, newValue) =>
			{
				SelectionManager.ShowIndicator = Convert.ToBoolean(newValue);
			});

			#endregion

			#region Keybinds

			var actions = InputSystem.actions;
			
			var playerMap = actions.FindActionMap("Player");
			var titleMap = actions.FindActionMap("Title");
			
			AddSetting("keybinds-movement-forward", "SETTINGS_KEYBINDS_MOVEMENT_FORWARD", "SETTINGS_KEYBINDS_MOVEMENT_FORWARD_DESC", playerMap.FindAction("Move"), 2, "<Keyboard>/w", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-forward"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-backward", "SETTINGS_KEYBINDS_MOVEMENT_BACKWARD", "SETTINGS_KEYBINDS_MOVEMENT_BACKWARD_DESC", playerMap.FindAction("Move"), 3, "<Keyboard>/s", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-backward"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-left", "SETTINGS_KEYBINDS_MOVEMENT_LEFT", "SETTINGS_KEYBINDS_MOVEMENT_LEFT_DESC", playerMap.FindAction("Move"), 4, "<Keyboard>/a", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-left"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-right", "SETTINGS_KEYBINDS_MOVEMENT_RIGHT", "SETTINGS_KEYBINDS_MOVEMENT_RIGHT_DESC", playerMap.FindAction("Move"), 5, "<Keyboard>/d", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-right"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-jump", "SETTINGS_KEYBINDS_MOVEMENT_JUMP", "SETTINGS_KEYBINDS_MOVEMENT_JUMP_DESC", playerMap.FindAction("Jump"), 0, "<Keyboard>/space", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-jump"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-sprint", "SETTINGS_KEYBINDS_MOVEMENT_SPRINT", "SETTINGS_KEYBINDS_MOVEMENT_SPRINT_DESC", playerMap.FindAction("Sprint"), 0, "<Keyboard>/leftShift", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-sprint"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-movement-fall", "SETTINGS_KEYBINDS_MOVEMENT_FALL", "SETTINGS_KEYBINDS_MOVEMENT_FALL_DESC", playerMap.FindAction("Fall"), 0, "<Keyboard>/leftCtrl", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-movement-fall"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-debug-noclip", "SETTINGS_KEYBINDS_DEBUG_NOCLIP", "SETTINGS_KEYBINDS_DEBUG_NOCLIP_DESC", playerMap.FindAction("Noclip"), 0, "<Keyboard>/v", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-debug-noclip"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-attack", "SETTINGS_KEYBINDS_GAMEPLAY_ATTACK", "SETTINGS_KEYBINDS_GAMEPLAY_ATTACK_DESC", playerMap.FindAction("Attack"), 0, "<Mouse>/leftButton", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-attack"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-interact", "SETTINGS_KEYBINDS_GAMEPLAY_INTERACT", "SETTINGS_KEYBINDS_GAMEPLAY_INTERACT_DESC", playerMap.FindAction("Interact"), 0, "<Keyboard>/e", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-interact"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-grab", "SETTINGS_KEYBINDS_GAMEPLAY_GRAB", "SETTINGS_KEYBINDS_GAMEPLAY_GRAB_DESC", playerMap.FindAction("Grab"), 0, "<Mouse>/rightButton", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-grab"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-light", "SETTINGS_KEYBINDS_GAMEPLAY_LIGHT", "SETTINGS_KEYBINDS_GAMEPLAY_LIGHT_DESC", playerMap.FindAction("Light"), 0, "<Keyboard>/f", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-light"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-spellbook", "SETTINGS_KEYBINDS_GAMEPLAY_SPELLBOOK", "SETTINGS_KEYBINDS_GAMEPLAY_SPELLBOOK_DESC", playerMap.FindAction("Spellbook"), 0, "<Keyboard>/i", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-spellbook"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar1", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR1", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR1_DESC", playerMap.FindAction("Hotbar1"), 0, "<Keyboard>/1", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar1"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar2", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR2", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR2_DESC", playerMap.FindAction("Hotbar2"), 0, "<Keyboard>/2", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar2"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar3", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR3", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR3_DESC", playerMap.FindAction("Hotbar3"), 0, "<Keyboard>/3", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar3"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar4", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR4", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR4_DESC", playerMap.FindAction("Hotbar4"), 0, "<Keyboard>/4", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar4"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar5", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR5", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR5_DESC", playerMap.FindAction("Hotbar5"), 0, "<Keyboard>/5", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar5"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar6", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR6", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR6_DESC", playerMap.FindAction("Hotbar6"), 0, "<Keyboard>/6", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar6"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-gameplay-hotbar7", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR7", "SETTINGS_KEYBINDS_GAMEPLAY_HOTBAR7_DESC", playerMap.FindAction("Hotbar7"), 0, "<Keyboard>/7", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-gameplay-hotbar7"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			AddSetting("keybinds-debug-console", "SETTINGS_KEYBINDS_DEBUG_CONSOLE", "SETTINGS_KEYBINDS_DEBUG_CONSOLE_DESC", titleMap.FindAction("Console"), 0, "<Keyboard>/backquote", (previousValue, newValue) =>
			{
				var keybind = keybinds["keybinds-debug-console"];
				keybind.Item1.ApplyBindingOverride(keybind.Item2, (string)newValue);
			});
			
			#endregion
			
			ResetSettings();
			loadSettings();
			saveSettings();
		}

		private async UniTask saveSettingsDelayed(CancellationToken token)
		{
			await UniTask.WaitForSeconds(2.5f, cancellationToken: token);

			if (token.IsCancellationRequested)
				return;

			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);
			
			var builder = new StringBuilder();

			foreach (var (key, setting) in settings)
			{
				try
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
				catch (Exception e)
				{
					Debug.LogError($"[SettingsManager] Failed saving setting {key}, {e}");
				}
			}
			
			await File.WriteAllTextAsync(System.IO.Path.Combine(Path, Name), builder.ToString(), token);
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