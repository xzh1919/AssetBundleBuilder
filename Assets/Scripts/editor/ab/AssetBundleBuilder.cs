using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class AssetBundleBuilder
{
    // 设置指定资源的ab名
    static void Reimport(string assetPath, string abName)
    {
        var importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            Debug.LogErrorFormat("importer failed: {0}", assetPath);
            return;
        }

        abName = abName.ToLower();

        if (importer.assetBundleName != abName || importer.assetBundleVariant != "ab")
        {
            importer.assetBundleName = abName;
            importer.assetBundleVariant = "ab";

            importer.SaveAndReimport();
        }
    }

	// 每个文件夹下的文件各自打成一个ab,ab名用文件名命名
	static void Reimport(string inPath, string outPath, string suffix)
	{
		if (!Directory.Exists(inPath))
		{
			return;
		}
		List<string> files = new List<string>();
		FileHelper.GetAllChildFiles(inPath, suffix, files);
		for (int i = 0; i < files.Count; ++i)
		{
			string fullpath = files[i];

			string name = fullpath.Replace("\\", "/").Replace(inPath, "").Replace(string.Format(".{0}", suffix), "");
			name = string.Format("{0}{1}", outPath, name.ToLower());

			Reimport(fullpath, name);
		}
	}

	// 基本资源
	static void ReimportBase()
	{
		Reimport("Assets/Data/test", "test", "prefab");
	}

	// 冗余资源
	static void ReimportRedundance()
	{
		// 收集冗余资源
		Dictionary<string, HashSet<string>> assets = CollectionAssetBundle.CollectionRedundance();

		Dictionary<string, List<string>> paths = new Dictionary<string, List<string>>();
		{
			var iter = assets.GetEnumerator();
			while (iter.MoveNext())
			{
				string name = iter.Current.Key;
				if (name.EndsWith(".shader"))
				{
					continue;
				}

				string path = name.Substring(0, name.LastIndexOf("/"));
				List<string> files = null;
				if (!paths.TryGetValue(path, out files))
				{
					files = new List<string>();
					paths.Add(path, files);
				}

				files.Add(name);
			}
		}

		{
			var iter = paths.GetEnumerator();
			while (iter.MoveNext())
			{
				foreach (string file in iter.Current.Value)
				{
					string abName = iter.Current.Key.Replace("Assets/", "");
					Reimport(file, abName);
				}
			}
		}
	}

	public static void Reimport()
	{
		ReimportBase();
		ReimportRedundance();
	}

	public static void Build()
	{
		Reimport();

		string path = "Assets/StreamingAssets/data/";
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}

		BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle;
		BuildPipeline.BuildAssetBundles(path, options, EditorUserBuildSettings.activeBuildTarget);

		AssetDatabase.Refresh();
	}
}
