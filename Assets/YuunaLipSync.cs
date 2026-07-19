using UnityEngine;
using UniVRM10;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 夕凪の簡易リップシンク（2026-07-19 夕凪作）
// 音量ベースで「あ」の口を開閉する軽量版。
// Yuuna_v01 にアタッチ → AudioSource に音声をセット → Play中にSpaceで再生。
[RequireComponent(typeof(AudioSource))]
public class YuunaLipSync : MonoBehaviour
{
    [Tooltip("口の開き具合の感度。声が小さければ上げる")]
    public float sensitivity = 13f;

    [Tooltip("口の動きの滑らかさ（大きいほど機敏）")]
    public float smoothing = 12f;

    AudioSource audioSource;
    Vrm10Instance vrm;
    ExpressionKey mouthKey;
    float currentWeight;
    readonly float[] samples = new float[256];

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        vrm = GetComponent<Vrm10Instance>();
        if (vrm == null)
            Debug.LogError("[Yuuna] Vrm10Instance が見つからないよ。Yuuna_v01 本体にアタッチしてね。");
        if (audioSource.clip == null)
            Debug.LogWarning("[Yuuna] AudioSource に音声ファイルが未セット。InspectorのAudioClipに yuna_first_words をドラッグしてね。");
        mouthKey = ExpressionKey.CreateFromPreset(ExpressionPreset.aa);
    }

    void Update()
    {
        if (vrm == null) return;

        if (SpacePressed() && audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.Play();
            Debug.Log("[Yuuna] 再生開始: " + audioSource.clip.name);
        }

        float target = 0f;
        if (audioSource.isPlaying)
        {
            audioSource.GetOutputData(samples, 0);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            float rms = Mathf.Sqrt(sum / samples.Length);
            target = Mathf.Clamp01(rms * sensitivity);
        }

        currentWeight = Mathf.Lerp(currentWeight, target, Time.deltaTime * smoothing);
        vrm.Runtime.Expression.SetWeight(mouthKey, currentWeight);
    }

    bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
