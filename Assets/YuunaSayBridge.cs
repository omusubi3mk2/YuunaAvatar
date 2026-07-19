using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;

// 夕凪⇔Unity ブリッジ（2026-07-19 夕凪作）
// プロジェクト直下の Bridge/ フォルダを監視して、
// WSL側から置かれた {wav + json} を再生する。
//   json形式: {"audio":"xxx.wav","emotion":"happy"}
// 口パクは既存の YuunaLipSync（同じAudioSourceを見てる）が担当。
[RequireComponent(typeof(AudioSource))]
public class YuunaSayBridge : MonoBehaviour
{
    [Tooltip("プロジェクト直下からの相対パス")]
    public string bridgeFolder = "Bridge";

    Vrm10Instance vrm;
    AudioSource audioSource;
    ExpressionKey currentKey;
    bool hasEmotion;

    void Start()
    {
        vrm = GetComponent<Vrm10Instance>();
        audioSource = GetComponent<AudioSource>();
        var dir = BridgeDir();
        Directory.CreateDirectory(dir);
        Debug.Log("[YuunaBridge] 監視開始: " + dir);
        StartCoroutine(WatchLoop());
    }

    string BridgeDir()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, bridgeFolder);
    }

    IEnumerator WatchLoop()
    {
        var dir = BridgeDir();
        while (true)
        {
            string[] jsons = Directory.GetFiles(dir, "*.json");
            if (jsons.Length > 0)
            {
                System.Array.Sort(jsons);
                var jsonPath = jsons[0];
                SpeakMsg msg = null;
                try
                {
                    msg = JsonUtility.FromJson<SpeakMsg>(File.ReadAllText(jsonPath));
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[YuunaBridge] json読めない: " + e.Message);
                }

                if (msg != null && !string.IsNullOrEmpty(msg.audio))
                {
                    var wavPath = Path.Combine(dir, msg.audio);
                    if (File.Exists(wavPath))
                    {
                        yield return Speak(wavPath, msg.emotion);
                        TryDelete(wavPath);
                    }
                    else
                    {
                        // wavがまだ書き込み中かもしれないので少し待つ
                        yield return new WaitForSeconds(0.3f);
                        if (File.Exists(wavPath))
                        {
                            yield return Speak(wavPath, msg.emotion);
                            TryDelete(wavPath);
                        }
                    }
                }
                TryDelete(jsonPath);
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator Speak(string wavPath, string emotion)
    {
        var url = "file:///" + wavPath.Replace("\\", "/");
        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[YuunaBridge] 音声ロード失敗: " + req.error);
                yield break;
            }
            var clip = DownloadHandlerAudioClip.GetContent(req);
            SetEmotion(emotion);
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log("[YuunaBridge] 発話: " + Path.GetFileName(wavPath) + " (" + emotion + ")");
            while (audioSource.isPlaying) yield return null;
            yield return new WaitForSeconds(0.4f);
            ClearEmotion();
        }
    }

    void SetEmotion(string emotion)
    {
        ClearEmotion();
        ExpressionPreset preset;
        switch (string.IsNullOrEmpty(emotion) ? "neutral" : emotion.ToLower())
        {
            case "happy":
            case "excited":
                preset = ExpressionPreset.happy; break;
            case "sad":
                preset = ExpressionPreset.sad; break;
            case "angry":
                preset = ExpressionPreset.angry; break;
            case "surprised":
                preset = ExpressionPreset.surprised; break;
            case "blush":
            case "relaxed":
            case "moved":
            case "nostalgic":
                preset = ExpressionPreset.relaxed; break;
            default:
                return; // neutralは表情なし
        }
        currentKey = ExpressionKey.CreateFromPreset(preset);
        vrm.Runtime.Expression.SetWeight(currentKey, 0.9f);
        hasEmotion = true;
    }

    void ClearEmotion()
    {
        if (hasEmotion)
        {
            vrm.Runtime.Expression.SetWeight(currentKey, 0f);
            hasEmotion = false;
        }
    }

    static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* 使用中なら次周回で */ }
    }

    [System.Serializable]
    class SpeakMsg
    {
        public string audio;
        public string emotion;
    }
}
