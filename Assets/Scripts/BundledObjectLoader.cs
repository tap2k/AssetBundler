using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class BundledObjectLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        string bundleName = "mybundle";
        AssetBundle localAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath + "/PC/", bundleName));
        if (localAssetBundle == null)
        {
            Debug.Log("Failed to Load Asset Bundle");
            return;
        }

        Object[] assets = localAssetBundle.LoadAllAssets();
        foreach (Object asset in assets)
        {
            Instantiate(asset);
        }

        localAssetBundle.Unload(false);

        //StartCoroutine(GetAssetBundle(url));
    }

    IEnumerator GetAssetBundle(string url)
    {
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            Object[] assets = bundle.LoadAllAssets();
            foreach (Object asset in assets)
            {
                Debug.Log("Name = " + asset.name);
                GameObject o = (GameObject)Instantiate(asset);
            }

            bundle.Unload(false);
        }
    }

}
