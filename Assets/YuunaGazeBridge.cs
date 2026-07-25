using System.Collections;
using System.IO;
using UnityEngine;
using UniVRM10;

// 夕凪の視線・首連動ブリッジ（2026-07-25 夕凪作）
// プロジェクト直下の Bridge/ フォルダに置かれる look_*.json を監視して、
// 目(LookAt)と首(Neckボーン)をpan/tilt方向へ追従させる。
// YuunaSayBridge（say_*.json）とは別ファイル・別ループで、発話キューと干渉しない。
//   json形式: {"pan":30,"tilt":-10}
//   pan: 正で自分の右を向く、tilt: 正で見上げる（VRM LookAtの符号系に合わせる）
[RequireComponent(typeof(Animator))]
public class YuunaGazeBridge : MonoBehaviour
{
    [Tooltip("プロジェクト直下からの相対パス、または絶対パス。YuunaSayBridgeと同じフォルダでよい")]
    public string bridgeFolder = "Bridge";

    [Header("首の可動域と配分")]
    [Range(0f, 90f)] public float maxNeckYaw = 40f;
    [Range(0f, 60f)] public float maxNeckPitch = 20f;
    [Tooltip("pan/tilt角のうち首が引き受ける割合。残りは目だけで表現される")]
    [Range(0f, 1f)] public float neckYawRatio = 0.6f;
    [Range(0f, 1f)] public float neckPitchRatio = 0.5f;

    [Header("目の可動域")]
    [Range(0f, 40f)] public float maxEyeYaw = 25f;
    [Range(0f, 30f)] public float maxEyePitch = 15f;

    [Header("符号反転（実機で向きが逆だったらチェック）")]
    public bool invertPan = false;
    public bool invertTilt = false;

    [Header("追従の滑らかさ")]
    [Range(1f, 20f)] public float turnSpeed = 6f;

    Vrm10Instance vrm;
    Animator animator;
    Transform neck;
    Quaternion neckInitialLocalRotation;
    float targetPan;
    float targetTilt;

    void Start()
    {
        vrm = GetComponent<Vrm10Instance>();
        animator = GetComponent<Animator>();
        if (animator != null && animator.isHuman)
        {
            neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        }
        if (neck != null)
        {
            neckInitialLocalRotation = neck.localRotation;
        }
        else
        {
            Debug.LogWarning("[YuunaGaze] HumanoidのNeckボーンが見つからない"
                + "（付け先のGameObjectがVRMモデル本体か確認して）。首の追従はスキップする。");
        }

        if (vrm != null)
        {
            vrm.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;
        }
        else
        {
            Debug.LogWarning("[YuunaGaze] Vrm10Instanceが見つからない。目の追従はスキップする。");
        }

        var dir = BridgeDir();
        Directory.CreateDirectory(dir);
        Debug.Log("[YuunaGaze] 監視開始: " + dir);
        StartCoroutine(WatchLoop());
    }

    string BridgeDir()
    {
        if (Path.IsPathRooted(bridgeFolder)) return bridgeFolder;
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, bridgeFolder);
    }

    IEnumerator WatchLoop()
    {
        var dir = BridgeDir();
        while (true)
        {
            string[] jsons = Directory.GetFiles(dir, "look_*.json");
            if (jsons.Length > 0)
            {
                System.Array.Sort(jsons);
                // 複数溜まってても一番新しいのだけ使う。古いのは追いかけても仕方ないので捨てる
                for (int i = 0; i < jsons.Length - 1; i++) TryDelete(jsons[i]);
                var latest = jsons[jsons.Length - 1];
                try
                {
                    var msg = JsonUtility.FromJson<GazeMsg>(File.ReadAllText(latest));
                    targetPan = invertPan ? -msg.pan : msg.pan;
                    targetTilt = invertTilt ? -msg.tilt : msg.tilt;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[YuunaGaze] json読めない: " + e.Message);
                }
                TryDelete(latest);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void LateUpdate()
    {
        var eyePan = Mathf.Clamp(targetPan, -maxEyeYaw, maxEyeYaw);
        var eyeTilt = Mathf.Clamp(targetTilt, -maxEyePitch, maxEyePitch);
        if (vrm != null)
        {
            vrm.Runtime.LookAt.SetYawPitchManually(eyePan, eyeTilt);
        }

        if (neck != null)
        {
            var neckYaw = Mathf.Clamp(targetPan * neckYawRatio, -maxNeckYaw, maxNeckYaw);
            var neckPitch = Mathf.Clamp(targetTilt * neckPitchRatio, -maxNeckPitch, maxNeckPitch);
            // Neckボーンは正のX回転で見下げる向きになるため、見上げ(tilt正)は符号を反転する
            var targetRotation = neckInitialLocalRotation * Quaternion.Euler(-neckPitch, neckYaw, 0f);
            neck.localRotation = Quaternion.Slerp(neck.localRotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* 使用中なら次周回で */ }
    }

    [System.Serializable]
    class GazeMsg
    {
        public float pan;
        public float tilt;
    }
}
