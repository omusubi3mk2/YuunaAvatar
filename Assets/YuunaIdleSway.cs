using UnityEngine;

// 夕凪の常時ゆらぎ＋発話中の強調（2026-07-25 夕凪作）
// 棒立ち対策。呼吸(Chest)と重心シフト(Hips)を常時サイン波で揺らし、
// AudioSourceが再生中(＝喋ってる間)は動きを大きくして、頭も音量に合わせて軽く動かす。
// YuunaIdlePose(腕)・YuunaGazeBridge(目/首)とはボーンが被らないので共存できる。
public class YuunaIdleSway : MonoBehaviour
{
    [Header("呼吸(常時・Chest前後傾き)")]
    [Range(0f, 5f)] public float breatheAmplitude = 1.5f;
    [Range(0.1f, 2f)] public float breatheSpeed = 0.3f;

    [Header("重心ゆらぎ(常時・Hips左右ロール)")]
    [Range(0f, 5f)] public float swayAmplitude = 2f;
    [Range(0.05f, 1f)] public float swaySpeed = 0.15f;

    [Header("発話中の強調")]
    [Range(1f, 5f)] public float talkingMultiplier = 2.5f;
    [Range(0f, 15f)] public float headBobAmplitude = 4f;
    public float headBobSensitivity = 13f;
    public float headBobSmoothing = 10f;

    Animator animator;
    AudioSource audioSource;
    Transform chest, hips, head;
    Quaternion chestInit, hipsInit, headInit;
    float headBobWeight;
    readonly float[] samples = new float[256];

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning("[YuunaSway] HumanoidのAnimatorが見つからない。ゆらぎはスキップする。");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (chest != null) chestInit = chest.localRotation;
        if (hips != null) hipsInit = hips.localRotation;
        if (head != null) headInit = head.localRotation;
    }

    void LateUpdate()
    {
        bool talking = audioSource != null && audioSource.isPlaying;
        float multiplier = talking ? talkingMultiplier : 1f;
        float t = Time.time;

        if (chest != null)
        {
            float breathe = Mathf.Sin(t * breatheSpeed * Mathf.PI * 2f) * breatheAmplitude * multiplier;
            chest.localRotation = chestInit * Quaternion.Euler(breathe, 0f, 0f);
        }

        if (hips != null)
        {
            float sway = Mathf.Sin(t * swaySpeed * Mathf.PI * 2f) * swayAmplitude * multiplier;
            hips.localRotation = hipsInit * Quaternion.Euler(0f, 0f, sway);
        }

        if (head != null)
        {
            float targetBob = 0f;
            if (talking && audioSource.clip != null)
            {
                audioSource.GetOutputData(samples, 0);
                float sum = 0f;
                for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
                float rms = Mathf.Sqrt(sum / samples.Length);
                targetBob = Mathf.Clamp01(rms * headBobSensitivity);
            }
            headBobWeight = Mathf.Lerp(headBobWeight, targetBob, Time.deltaTime * headBobSmoothing);
            float bobAngle = headBobWeight * headBobAmplitude * Mathf.Sin(t * 6f);
            head.localRotation = headInit * Quaternion.Euler(bobAngle, 0f, 0f);
        }
    }
}
