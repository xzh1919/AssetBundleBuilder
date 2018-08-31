using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileHelper
{
	// 获取指定文件下的所有文件
	public static List<string> GetAllChildFiles(string path, string suffix = null, List<string> files = null)
	{
		if (files == null)
		{
			files = new List<string>();
		}

		if (!Directory.Exists(path))
		{
			return files;
		}

		AddFiles(path, suffix, files);

		string[] temps = Directory.GetDirectories(path);
		for (int i = 0; i < temps.Length; ++i)
		{
			string dir = temps[i];
			GetAllChildFiles(dir, suffix, files);
		}

		return files;
	}

	private static void AddFiles(string path, string suffix, List<string> files)
	{
		string[] temps = Directory.GetFiles(path);
		for (int i = 0; i < temps.Length; ++i)
		{
			string file = temps[i];
			if (string.IsNullOrEmpty(suffix) || file.ToLower().EndsWith(suffix.ToLower()))
			{
				files.Add(file);
			}
		}
	}
}
