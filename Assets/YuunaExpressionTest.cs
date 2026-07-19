using UnityEngine;
using UniVRM10;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 夕凪の表情テスト（2026-07-19 夕凪作）
// Yuuna_v01 にアタッチして Play:
//   1=happy 2=sad 3=angry 4=surprised 5=relaxed 0=neutral
// まばたきは自動。
public class YuunaExpressionTest : MonoBehaviour
{
    Vrm10Instance vrm;
    ExpressionKey currentKey;
    bool hasEmotion;
    float blinkTimer;
    float blinkPhase = -1f; // -1: 待機中, 0〜1: まばたき進行中

    void Start()
    {
        vrm = GetComponent<Vrm10Instance>();
        if (vrm == null)
        {
            Debug.LogError("[Yuuna] Vrm10Instance が見つからないよ。Yuuna_v01 本体にアタッチしてね。");
        }
        ResetBlinkTimer();
    }

    void Update()
    {
        if (vrm == null) return;

        if (Pressed(1)) SetEmotion(ExpressionPreset.happy);
        if (Pressed(2)) SetEmotion(ExpressionPreset.sad);
        if (Pressed(3)) SetEmotion(ExpressionPreset.angry);
        if (Pressed(4)) SetEmotion(ExpressionPreset.surprised);
        if (Pressed(5)) SetEmotion(ExpressionPreset.relaxed);
        if (Pressed(0)) ClearEmotion();

        UpdateBlink();
    }

    void SetEmotion(ExpressionPreset preset)
    {
        ClearEmotion();
        currentKey = ExpressionKey.CreateFromPreset(preset);
        vrm.Runtime.Expression.SetWeight(currentKey, 1f);
        hasEmotion = true;
        Debug.Log("[Yuuna] expression: " + preset);
    }

    void ClearEmotion()
    {
        if (hasEmotion)
        {
            vrm.Runtime.Expression.SetWeight(currentKey, 0f);
            hasEmotion = false;
        }
    }

    void UpdateBlink()
    {
        var blinkKey = ExpressionKey.CreateFromPreset(ExpressionPreset.blink);

        if (blinkPhase < 0f)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f) blinkPhase = 0f;
            return;
        }

        blinkPhase += Time.deltaTime / 0.15f; // 0.15秒で一回のまばたき
        float w = Mathf.Sin(Mathf.Clamp01(blinkPhase) * Mathf.PI);
        vrm.Runtime.Expression.SetWeight(blinkKey, w);

        if (blinkPhase >= 1f)
        {
            vrm.Runtime.Expression.SetWeight(blinkKey, 0f);
            blinkPhase = -1f;
            ResetBlinkTimer();
        }
    }

    void ResetBlinkTimer()
    {
        blinkTimer = Random.Range(2.0f, 5.0f);
    }

    bool Pressed(int n)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        switch (n)
        {
            case 0: return kb.digit0Key.wasPressedThisFrame;
            case 1: return kb.digit1Key.wasPressedThisFrame;
            case 2: return kb.digit2Key.wasPressedThisFrame;
            case 3: return kb.digit3Key.wasPressedThisFrame;
            case 4: return kb.digit4Key.wasPressedThisFrame;
            case 5: return kb.digit5Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(n.ToString());
#endif
    }
}
