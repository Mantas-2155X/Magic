using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using File = System.IO.File;

namespace Editor
{
	public class PostBuildAction : IPostprocessBuildWithReport
	{
		public int callbackOrder { get; }

		private readonly string[] copyAssets = { "data" };
		
		public void OnPostprocessBuild(BuildReport report)
		{
			var executable = new FileInfo(report.summary.outputPath);
			if (!executable.Exists || executable.Directory == null)
				return;
			
			var directory = executable.Directory.FullName;

			foreach (var copyAsset in copyAssets)
			{
				if (!File.Exists(copyAsset) && !Directory.Exists(copyAsset))
					continue;

				var attributes = File.GetAttributes(copyAsset);
				if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
				{
					Copy(copyAsset, Path.Combine(directory, copyAsset));
				}
				else
				{
					File.Copy(copyAsset, Path.Combine(directory, copyAsset));
				}
			}
		}
		
		public static void Copy(string source, string target)
		{
			var sourceDirectory = new DirectoryInfo(source);
			var targetDirectory = new DirectoryInfo(target);

			CopyAll(sourceDirectory, targetDirectory);
		}

		public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
		{
			Directory.CreateDirectory(target.FullName);

			foreach (var file in source.GetFiles())
			{
				if (file.Name.StartsWith("."))
					continue;
				
				Console.WriteLine($"Copying {target.FullName}\\{file.Name}");
				file.CopyTo(Path.Combine(target.FullName, file.Name), true);
			}

			foreach (var sourceSubdirectory in source.GetDirectories())
			{
				var targetSubdirectory = target.CreateSubdirectory(sourceSubdirectory.Name);
				CopyAll(sourceSubdirectory, targetSubdirectory);
			}
		}
	}
}