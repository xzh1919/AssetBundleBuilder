using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MeMenuItem
{
    [MenuItem("Me/BuildAssetBundle")]
    static void OnBuildAssetBundle()
    {
		AssetBundleBuilder.Build();
    }
}
