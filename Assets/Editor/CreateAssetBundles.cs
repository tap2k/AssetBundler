using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Newtonsoft.Json.Linq;

public class CreateAssetBundles 
{
    private static string _username = "";
    private static string _password = "";
    private static string _baseURL = "";
    private static string _channelID = "";
    private static string[] _assetBundleDirectories = { Application.streamingAssetsPath + "/Android", Application.streamingAssetsPath + "/PC", Application.streamingAssetsPath + "/WebGL" };
    private static BuildTarget[] _targetPlatforms = { BuildTarget.Android, BuildTarget.StandaloneWindows, BuildTarget.WebGL };
    private static List<AssetBundleManifest> _manifests = new List<AssetBundleManifest>();
    private static List<string> _manifestDirs = new List<string>();

    [MenuItem ("Assets/BuildAssetBundles")]
    static void BuildAssetBundles()
    {
        _manifests.Clear();
        _manifestDirs.Clear();
        for (int i = 0; i < _assetBundleDirectories.Length; i++)
        {
            if (!Directory.Exists(_assetBundleDirectories[i]))
                Directory.CreateDirectory(_assetBundleDirectories[i]);
            // Or no compression?
            _manifests.Add(BuildPipeline.BuildAssetBundles(_assetBundleDirectories[i], BuildAssetBundleOptions.ChunkBasedCompression, _targetPlatforms[i]));
            _manifestDirs.Add(_assetBundleDirectories[i]);
            Debug.Log("Built Asset Bundles: " + _assetBundleDirectories[i]);
        }
    }

    [MenuItem("Assets/UploadAssetBundles")]
    static void UploadAssetBundles()
    {
        string bearerToken = Login();

        int i = 0;
        foreach (AssetBundleManifest manifest in _manifests)
        {
            string[] bundles = manifest.GetAllAssetBundles();
            foreach (string bundle in bundles)
            {
                string path = _manifestDirs[i] + "/" + bundle;
                string[] split = _manifestDirs[i].Split("/");
                string platform = split[split.Length - 1];
                Debug.Log("Uploading " + path);
                WWWForm form = new WWWForm();
                form.AddField("platform", platform);
                Debug.Log("Platform " + platform);
                byte[] bytes = File.ReadAllBytes(path);
                form.AddBinaryData("bundle", bytes, bundle);
                string url = _baseURL + "/api/uploadAssetToChannel";

                if (bundle.ToLower().Contains("avatar"))
                {
                    url = _baseURL + "/api/uploadAvatar";
                }
                else
                {
                    form.AddField("name", bundle);
                    form.AddField("uniqueID", _channelID);
                }

                form.AddField("maxContentLength", "Infinity");
                form.AddField("maxBodyLength", "Infinity");

                var w = UnityWebRequest.Post(url, form);
                w.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                w.SendWebRequest();

                // dont really need to wait
                while (!w.isDone)
                { new WaitForSeconds(1); }
                if (w.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(w.error);
                }
                else
                {
                    Debug.Log("Uploaded Content!");
                }
            }
            i++;
        }
    }

    static string Login()
    {
        WWWForm form = new WWWForm();
        form.AddField("identifier", _username);
        form.AddField("password", _password);

        using (var w = UnityWebRequest.Post(_baseURL + "/api/auth/local", form))
        {
            w.SendWebRequest();
            while (!w.isDone)
            { new WaitForSeconds(1); }

            if (w.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(w.error);
                return "";
            }
            else
            {
                JObject json = JObject.Parse(w.downloadHandler.text);
                string jwt = (string)json["jwt"];
                Debug.Log("Bearer Token = " + jwt);
                return jwt;
            }
        }
    }

}
