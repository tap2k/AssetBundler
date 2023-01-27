using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Newtonsoft.Json.Linq;

public class CreateAssetBundles 
{
    private static string _username = "tsp53";
    private static string _password = "abcdef1234";
    private static string _channelID = "tsp53";
    private static string _baseURL = "https://remaking.represent.org/strapi";
    
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
            Debug.Log("Built asset bundles: " + _assetBundleDirectories[i]);
        }
        Debug.Log("Finished building all the bundles!");
    }

    [MenuItem("Assets/UploadAssetBundles")]
    static void UploadAssetBundles()
    {
        string bearerToken = Login();

        if (!bearerToken)
        {
            Debug.error("Couldn't login to server");
            return;
        }

        int i = 0;
        foreach (AssetBundleManifest manifest in _manifests)
        {
            string[] bundles = manifest.GetAllAssetBundles();
            foreach (string bundle in bundles)
            {
                string path = _manifestDirs[i] + "/" + bundle;
                string[] split = _manifestDirs[i].Split("/");
                string platform = split[split.Length - 1];
                Debug.Log("Uploading " + bundle);
                WWWForm form = new WWWForm();
                form.AddField("platform", platform);
                byte[] bytes = File.ReadAllBytes(path);
                form.AddBinaryData("bundle", bytes, bundle);
                string url = _baseURL + "/api/uploadAssetToChannel";

                if (bundle.ToLower().Contains("avatar"))
                    url = _baseURL + "/api/uploadAvatar";
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
                    new WaitForSeconds(1);

                if (w.result != UnityWebRequest.Result.Success)
                    Debug.error(w.error);
                else
                    Debug.Log("Finished uploading " + bundle + " for " + platform);
            }
            i++;
        }
        Debug.Log("Uploaded all the content!");
    }

    static string Login()
    {
        WWWForm form = new WWWForm();
        form.AddField("identifier", _username);
        form.AddField("password", _password);
        Debug.Log("Logging in as " + identifier);

        using (var w = UnityWebRequest.Post(_baseURL + "/api/auth/local", form))
        {
            w.SendWebRequest();
            while (!w.isDone)
            { new WaitForSeconds(1); }

            if (w.result != UnityWebRequest.Result.Success)
            {
                Debug.error(w.error);
                return "";
            }
            else
            {
                JObject json = JObject.Parse(w.downloadHandler.text);
                string jwt = (string)json["jwt"];
                Debug.log("Successfully logged in!");
                return jwt;
            }
        }
    }

}
