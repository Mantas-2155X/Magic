using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public class ModdingTool : EditorWindow
	{
		[SerializeField]
		public string Author = "MyName";
		
		[SerializeField]
		public string Name = "ModName";

		[SerializeField]
		public string Version = "1.0.0";

		[SerializeField]
		public List<Data> Objects = new ();
		
		private Vector2 scrollPosition;
		
		private readonly Regex fileFilter = new (@"\W|_");
		private readonly Regex versionFilter = new (@"^(0|[1-9]\d*)(\.(0|[1-9]\d*)){0,3}$");
		
		private readonly string exportPath = "data/mods";
		
		private readonly List<Type> allowedModdedDatas = new ()
		{
			typeof(AttackData), 
			typeof(CastData), 
			typeof(DecalData), 
			typeof(ProjectileData), 
			typeof(SpellData), 
			typeof(WearableData)
		};
		
		private readonly BuildTarget[] buildTargets = { BuildTarget.StandaloneLinux64, BuildTarget.StandaloneWindows64 };
		
		[MenuItem("Modding/Modding Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<ModdingTool>(true);
			window.minSize = new Vector2(300, 300);
			window.Show();
		}

		public void OnGUI()
		{
			Author = EditorGUILayout.TextField("Author", fileFilter.Replace(Author, ""));
			Name = EditorGUILayout.TextField("Name", fileFilter.Replace(Name, ""));
			Version = EditorGUILayout.TextField("Version", versionFilter.Match(Version).Value);

			GUILayout.Space(5);
			
			Objects ??= new List<Data>();
			
			GUILayout.BeginHorizontal();
			
			EditorGUILayout.LabelField($"Objects ({Objects.Count})");
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				Objects.Clear();
				return;
			}
			
			if (GUILayout.Button("+", GUILayout.Width(25)))
				Objects.Add(null);
			
			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			var validObjects = 0;
			
			for (var i = 0; i < Objects.Count; i++)
			{
				GUILayout.BeginHorizontal();
				
				Objects[i] = (Data)EditorGUILayout.ObjectField(Objects[i], typeof(Data), false);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					Objects.RemoveAt(i);
					return;
				}

				if (Objects[i] != null)
				{
					var type = Objects[i].GetType();
					var invalid = false;
					
					if (!allowedModdedDatas.Contains(type))
					{
						invalid = true;
						Objects[i] = null;
						Debug.LogWarning($"[ModdingTools] Objects of data {type} are not supported");
					}

					if (!invalid)
						validObjects++;
				}
				
				GUILayout.EndHorizontal();
			}
			
			GUILayout.EndScrollView();
			
			GUILayout.FlexibleSpace();

			GUI.enabled = Author != "" && Name != "" && Version != "" && validObjects > 0;
			var shouldBuild = GUILayout.Button("Build Mod");
			GUI.enabled = true;
			
			if (shouldBuild)
			{
				if (!Directory.Exists(exportPath))
					Directory.CreateDirectory(exportPath);

				var directory = $"{Author}.{Name}-{Version}";
				var path = Path.Combine(exportPath, directory);
				
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				var builder = new StringBuilder();
				builder.AppendLine($"Author\t{Author}");
				builder.AppendLine($"Name\t{Name}");
				builder.AppendLine($"Version\t{Version}");
				
				builder.AppendLine();

				for (var i = 0; i < Objects.Count; i++)
				{
					var obj = Objects[i];
					if (obj == null)
						continue;
					
					builder.AppendLine($"{obj.GetType().Name}\t{obj.Name}");
				}
				
				File.WriteAllText(Path.Combine(path, "info.tsv"), builder.ToString());

				for (var i = 0; i < buildTargets.Length; i++)
				{
					var buildTarget = buildTargets[i];
					var bundlePath = Path.Combine(path, buildTarget.ToString());
					
					if (!Directory.Exists(bundlePath))
						Directory.CreateDirectory(bundlePath);

					var assetBundleBuild = new AssetBundleBuild
					{
						assetBundleName = directory.ToLower(),
						assetNames = new string[validObjects]
					};

					var index = 0;
					for (var k = 0; k < Objects.Count; k++)
					{
						var obj = Objects[k];
						if (obj == null)
							continue;

						assetBundleBuild.assetNames[index] = AssetDatabase.GetAssetPath(obj);
						index++;
					}
					
					BuildPipeline.BuildAssetBundles(bundlePath, new [] {assetBundleBuild}, BuildAssetBundleOptions.None, buildTarget);
				}
			}
		}
	}
}