using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

public class CollectionAssetBundle
{
    class AssetBundleData
    {
        public string name;             // ab名
        public HashSet<string> assets;  // 包含的资源
        public List<string> parents;    // 父ab
        public List<string> childs;     // 子ab
    }

    static Dictionary<string, AssetBundleData> m_datas = new Dictionary<string, AssetBundleData>();

	// 收集所有的冗余资源
	public static Dictionary<string, HashSet<string>> CollectionRedundance()
	{
		m_datas.Clear();

		CollectAssetBundle();
		//DumpAll("1.txt");
		CollectAssetBundleDependencies();
		//DumpAll("2.txt");
		MergeAssetBundleAsset();
		//DumpAll("3.txt");

		Dictionary<string, HashSet<string>> assets = CollectRedundanceAsset();
		//DumpAll("4.txt", assets);
		m_datas.Clear();

		return assets;
	}

    // 收集所有的ab
    static void CollectAssetBundle()
    {
        string[] names = AssetDatabase.GetAllAssetBundleNames();
        foreach (string name in names)
        {
            if (name.EndsWith(".ab"))
            {
                AssetBundleData data = new AssetBundleData();
                data.name = name;
                data.assets = CollectAssetFromAssetBundle(name);
                data.parents = new List<string>();
                data.childs = new List<string>();

                m_datas.Add(name, data);
            }
        }
    }

    // 收集AssetBundle里的所有资源
    static HashSet<string> CollectAssetFromAssetBundle(string name)
    {
        HashSet<string> files = new HashSet<string>();

        string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(name);
        foreach (string path in paths)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

            Object[] deps = EditorUtility.CollectDependencies(new Object[] { asset });
            foreach (Object dep in deps)
            {
                if (dep is MonoScript)
                {
                    continue;
                }

                string pathDep = AssetDatabase.GetAssetPath(dep);
                if (string.IsNullOrEmpty(pathDep) || pathDep.StartsWith("Library/") || pathDep.StartsWith("Resources/") || pathDep.Contains("/Resources/"))
                {
                    continue;
                }

                if (!files.Contains(pathDep))
                {
                    files.Add(pathDep);
                }
            }
        }

        return files;
    }

    // 收集ab的依赖关系
    static void CollectAssetBundleDependencies()
    {
        var iter = m_datas.GetEnumerator();
        while (iter.MoveNext())
        {
            AssetBundleData current = iter.Current.Value;
            string[] depends = AssetDatabase.GetAssetBundleDependencies(iter.Current.Key, false);
            foreach (string depend in depends)
            {
                AssetBundleData data = m_datas[depend];
                current.childs.Add(data.name);
                data.parents.Add(current.name);
            }
        }
    }

    // 合并资源(如果一个资源在当前ab里,那么将这个资源从父ab中移除掉)
    static void MergeAssetBundleAsset()
    {
        var iter = m_datas.GetEnumerator();
        while (iter.MoveNext())
        {
            MergeAsset(iter.Current.Value);
        }
    }

    // 收集冗余的资源
    static Dictionary<string, HashSet<string>> CollectRedundanceAsset()
    {
        Dictionary<string, HashSet<string>> assets = new Dictionary<string, HashSet<string>>();

        {
            var iter = m_datas.GetEnumerator();
            while (iter.MoveNext())
            {
                CollectAsset(iter.Current.Value, assets);
            }
        }

        {
            // 移除只打包在一个ab的资源
            List<string> removes = new List<string>();
            var iter = assets.GetEnumerator();
            while (iter.MoveNext())
            {
                if (iter.Current.Value.Count <= 1)
                {
                    removes.Add(iter.Current.Key);
                }
            }

            foreach (string remove in removes)
            {
                assets.Remove(remove);
            }
        }

        return assets;
    }

    // 合并资源(如果一个资源在当前ab里,那么将这个资源从父ab中移除掉)
    static void MergeAsset(AssetBundleData data)
    {
        foreach (string asset in data.assets)
        {
            RemoveParentAsset(data, asset);
        }

        foreach (string childname in data.childs)
        {
            AssetBundleData child = m_datas[childname];
            MergeAsset(child);
        }
    }

    static void RemoveParentAsset(AssetBundleData data, string asset)
    {
        foreach (string parentname in data.parents)
        {
            AssetBundleData parent = m_datas[parentname];
            if (parent.assets.Contains(asset))
            {
                parent.assets.Remove(asset);
            }

            RemoveParentAsset(parent, asset);
        }
    }

    // 收集资源包含在哪些ab中
    static void CollectAsset(AssetBundleData data, Dictionary<string, HashSet<string>> assets)
    {
        foreach (string asset in data.assets)
        {
            HashSet<string> abs = null;
            if (!assets.TryGetValue(asset, out abs))
            {
                abs = new HashSet<string>();
                assets.Add(asset, abs);
            }

            if (!abs.Contains(data.name))
            {
                abs.Add(data.name);
            }
        }

        foreach (string childname in data.childs)
        {
            AssetBundleData child = m_datas[childname];
            CollectAsset(child, assets);
        }
    }

    static void DumpAll(string output)
    {
        StringBuilder sb = new StringBuilder();
        
        var iter = m_datas.GetEnumerator();
        while (iter.MoveNext())
        {
            AssetBundleData data = iter.Current.Value;
            sb.AppendLine(string.Format("-{0}", data.name));

            foreach (string file in data.assets)
            {
                sb.AppendLine(string.Format("    {0}", file));
            }

			if (data.childs.Count > 0)
			{
				sb.AppendLine("    -child");
				foreach (string child in data.childs)
				{
					sb.AppendLine(string.Format("        {0}", child));
				}
			}

			if(data.parents.Count > 0)
			{
				sb.AppendLine("    -parent");
				foreach (string parent in data.parents)
				{
					sb.AppendLine(string.Format("        {0}", parent));
				}
			}
		}

        Debug.Log(sb.ToString());
        SaveToFile(output, sb.ToString());
    }

	static void DumpAll(string output, Dictionary<string, HashSet<string>> assets)
	{
		StringBuilder sb = new StringBuilder();

		var iter = assets.GetEnumerator();
		while (iter.MoveNext())
		{
			sb.AppendFormat("-{0}\n", iter.Current.Key);
			foreach(string asset in iter.Current.Value)
			{
				sb.AppendFormat("    {0}\n", iter.Current.Key);
			}
		}

		Debug.Log(sb.ToString());
		SaveToFile(output, sb.ToString());
	}

    static void SaveToFile(string path, string text)
    {
        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            Stream stream = File.Open(path, FileMode.Create);
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}
