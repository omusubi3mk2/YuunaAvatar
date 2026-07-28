using System.Collections;
using System.IO;
using UnityEngine;

// 夕凪のボディジェスチャー（2026-07-25 夕凪作）
// Bridge/フォルダのgesture_*.json({"gesture":"thinking","duration":3})を監視して、
// 一定時間だけ腕・頭のポーズを変える。声なし・表情なしでも「考え中」感を出す用途。
// IdlePose/IdleSwayと同じボーン(右腕・頭)を触るので、DefaultExecutionOrderで
// それらより後に実行して上書きする（先に実行されると毎フレーム打ち消されてしまう）。
[DefaultExecutionOrder(100)]
public class YuunaGestureBridge : MonoBehaviour
{
    [Tooltip("プロジェクト直下からの相対パス、または絶対パス")]
    public string bridgeFolder = "Bridge";

    [Header("「考え中(thinking)」ポーズの角度")]
    [Tooltip("右上腕を持ち上げる角度。逆に下がったらマイナスに")]
    [Range(-90f, 90f)] public float thinkingUpperArmAngle = -15f;
    [Tooltip("右肘を曲げる角度")]
    [Range(-120f, 120f)] public float thinkingLowerArmAngle = -30f;
    [Tooltip("頭を傾ける角度")]
    [Range(-30f, 30f)] public float thinkingHeadTilt = 8f;

    [Range(1f, 10f)] public float blendSpeed = 4f;

    Animator animator;
    Transform rightUpperArm, rightLowerArm, head, hips;
    float restHipsY;

    // ジェスチャー開始の瞬間の実際の姿勢(IdlePose/IdleSway適用後)を基準にする。
    // Startで一度だけ取れるT-poseの生値を基準にすると、非アクティブ時に
    // IdlePoseの「腕を下げた姿勢」と毎フレーム綱引きしてジッタるため。
    Quaternion restUpperArm, restLowerArm, restHead;
    // 自前で進捗を保持するブレンド値。ボーンの現在値(IdlePoseが毎フレーム
    // 上書きしてしまう)を起点にSlerpすると、その場で足踏みするだけで
    // 目標まで進まないため、こちらを起点にする。
    Quaternion blendUpperArm, blendLowerArm, blendHead;
    bool wasActive;

    string currentGesture;
    float gestureEndTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("[YuunaGesture] HumanoidのAnimatorが見つからない。ジェスチャーはスキップする。");
            enabled = false;
            return;
        }

        // マスコットはその場に立っているだけでルートモーションは不要。
        // Waveなど重心移動の大きいMixamoクリップがバストアップ画角でフレームアウトする対策
        // （TODO.md「Wave発火時、夕凪が上に移動して画面外に消える」）。
        animator.applyRootMotion = false;

        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);

        var dir = BridgeDir();
        Directory.CreateDirectory(dir);
        Debug.Log("[YuunaGesture] 監視開始: " + dir);
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
            string[] jsons = Directory.GetFiles(dir, "gesture_*.json");
            if (jsons.Length > 0)
            {
                System.Array.Sort(jsons);
                for (int i = 0; i < jsons.Length - 1; i++) TryDelete(jsons[i]);
                var latest = jsons[jsons.Length - 1];
                try
                {
                    var msg = JsonUtility.FromJson<GestureMsg>(File.ReadAllText(latest));
                    currentGesture = msg.gesture;
                    gestureEndTime = Time.time + (msg.duration > 0f ? msg.duration : 3f);
                    Debug.Log("[YuunaGesture] 発火: " + currentGesture + " (" + msg.duration + "s)");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[YuunaGesture] json読めない: " + e.Message);
                }
                TryDelete(latest);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Mixamoクリップなど、AnimatorController側のTriggerに任せるジェスチャー名。
    // ここに載っている名前は毎フレームのボーン直接操作をせず、Animatorの
    // 通常評価（Update）にそのまま任せる。LateUpdateでは上書きしない。
    static readonly System.Collections.Generic.HashSet<string> AnimatorDrivenGestures =
        new System.Collections.Generic.HashSet<string> { "wave" };

    // YuunaIdlePoseへの合図。Animator駆動のジェスチャー中は、IdlePoseが
    // 毎フレーム腕を「下ろした姿勢」に固定する処理をスキップしてもらう
    // （このスクリプトは実行順序100でIdlePose(順序0)より後に走るため、
    // 実際に反映されるのは1フレーム遅れるが体感では気にならない）。
    public static bool AnimatorGestureActive { get; private set; }

    void LateUpdate()
    {
        // Startで一度falseにしても、VRMの非同期初期化など何か別の経路で
        // trueへ戻される可能性があるため、念のため毎フレーム強制する。
        animator.applyRootMotion = false;

        bool active = !string.IsNullOrEmpty(currentGesture) && Time.time < gestureEndTime;
        AnimatorGestureActive = active && AnimatorDrivenGestures.Contains(currentGesture);

        if (active && !wasActive)
        {
            if (AnimatorDrivenGestures.Contains(currentGesture))
            {
                // Triggerは一度だけ発火。あとの再生・待機時間経過での復帰は
                // AnimatorController側のTransition（Has Exit Time）に任せる
                animator.SetTrigger(currentGesture == "wave" ? "Wave" : currentGesture);
                // 発火直前(まだ遷移前)の、今立っている実際の高さを基準に記録する
                if (hips != null) restHipsY = hips.position.y;
            }
            else
            {
                // 立ち上がりの瞬間だけ、その時点の実際の姿勢を基準として記録し、
                // ブレンド進捗もそこからスタートする
                if (rightUpperArm != null) restUpperArm = blendUpperArm = rightUpperArm.localRotation;
                if (rightLowerArm != null) restLowerArm = blendLowerArm = rightLowerArm.localRotation;
                if (head != null) restHead = blendHead = head.localRotation;
            }
        }
        wasActive = active;

        // Hipsの高さ固定は「Wave」の実際のAnimator再生状況（本体のクリップ長・
        // Exit Timeの遷移）で判定する。外部のduration(既定3秒)で先に区切ると、
        // クリップ本体がまだ再生中/遷移中でも固定が先に切れてしまい、
        // 「手を振った後にジャンプする」形で症状が残ってしまうため。
        if (hips != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool waveStatePlaying = stateInfo.IsName("Wave")
                || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Wave"));
            if (waveStatePlaying)
            {
                var p = hips.position;
                p.y = restHipsY;
                hips.position = p;
            }
        }

        if (!active)
        {
            // 非アクティブ時はボーンに一切触らない。IdlePose/IdleSwayに任せる
            currentGesture = null;
            return;
        }

        if (AnimatorDrivenGestures.Contains(currentGesture))
        {
            // Animatorが全身のボーンを駆動する。Hipsの高さ固定は上ですでに処理済み
            return;
        }

        var targetUpperArm = restUpperArm;
        var targetLowerArm = restLowerArm;
        var targetHead = restHead;

        if (currentGesture == "thinking")
        {
            targetUpperArm = restUpperArm * Quaternion.Euler(0f, 0f, thinkingUpperArmAngle);
            targetLowerArm = restLowerArm * Quaternion.Euler(0f, 0f, thinkingLowerArmAngle);
            targetHead = restHead * Quaternion.Euler(0f, 0f, thinkingHeadTilt);
        }

        float t = Time.deltaTime * blendSpeed;
        blendUpperArm = Quaternion.Slerp(blendUpperArm, targetUpperArm, t);
        blendLowerArm = Quaternion.Slerp(blendLowerArm, targetLowerArm, t);
        blendHead = Quaternion.Slerp(blendHead, targetHead, t);

        if (rightUpperArm != null) rightUpperArm.localRotation = blendUpperArm;
        if (rightLowerArm != null) rightLowerArm.localRotation = blendLowerArm;
        if (head != null) head.localRotation = blendHead;
    }

    static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* 使用中なら次周回で */ }
    }

    [System.Serializable]
    class GestureMsg
    {
        public string gesture;
        public float duration;
    }
}
