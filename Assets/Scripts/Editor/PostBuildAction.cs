using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Editor
{
	public class PostBuildAction : IPostprocessBuildWithReport
	{
		public int callbackOrder { get; }

		private readonly string[] copyAssets = { "data", "licenses" };
		private readonly string[] removeAfter = { "data/settings.tsv", "Magic_BurstDebugInformation_DoNotShip" };
		
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

			foreach (var removePath in removeAfter)
			{
				var path = Path.Combine(directory, removePath);
				
				if (!File.Exists(path) && !Directory.Exists(path))
					continue;
				
				var attributes = File.GetAttributes(path);
				if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
				{
					Directory.Delete(path, true);
				}
				else
				{
					File.Delete(path);
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