using UnityEngine;
using System.Collections.Generic;

// 夕凪の待機ポーズ v2（2026-07-19 夕凪作）
// v1は回転を毎フレーム加算していて腕風車になったので、
// 「初期姿勢＋指定角度」に固定する方式に修正。
public class YuunaIdlePose : MonoBehaviour
{
    [Tooltip("腕を下ろす角度。逆に上がったらマイナスに")]
    [Range(-80f, 80f)]
    public float armDownAngle = 70f;

    [Tooltip("ひじの軽い曲げ")]
    [Range(-30f, 30f)]
    public float elbowBend = 8f;

    Animator animator;
    readonly Dictionary<HumanBodyBones, Quaternion> initialRotations = new Dictionary<HumanBodyBones, Quaternion>();

    static readonly HumanBodyBones[] targetBones =
    {
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightLowerArm,
    };

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[Yuuna] Animator が見つからないよ。Yuuna_v01 本体にアタッチしてね。");
            return;
        }
        foreach (var bone in targetBones)
        {
            var t = animator.GetBoneTransform(bone);
            if (t != null) initialRotations[bone] = t.localRotation;
        }
    }

    void LateUpdate()
    {
        if (animator == null) return;
        Apply(HumanBodyBones.LeftUpperArm, -armDownAngle);
        Apply(HumanBodyBones.RightUpperArm, armDownAngle);
        Apply(HumanBodyBones.LeftLowerArm, -elbowBend);
        Apply(HumanBodyBones.RightLowerArm, elbowBend);
    }

    void Apply(HumanBodyBones bone, float zAngle)
    {
        var t = animator.GetBoneTransform(bone);
        if (t == null || !initialRotations.ContainsKey(bone)) return;
        t.localRotation = initialRotations[bone] * Quaternion.Euler(0f, 0f, zAngle);
    }
}
