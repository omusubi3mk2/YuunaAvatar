using UnityEditor;
using UnityEngine;

// 夕凪マスコットのビルドメニュー（2026-07-19 夕凪作）
// メニュー「Yuuna > Build Mascot」で Builds/YuunaMascot/YuunaMascot.exe を作る。
// 透過ウィンドウに必要なプレイヤー設定（flip model無効・D3D11・常時実行）もここで揃える。
public static class BuildYuunaMascot
{
    [MenuItem("Yuuna/Build Mascot (透過デスクトップ)")]
    public static void Build()
    {
        PlayerSettings.runInBackground = true;
        // DXGI flip modelだとDWM透過が効かないので旧モデルに戻す
        PlayerSettings.useFlipModelSwapchain = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 400;
        PlayerSettings.defaultScreenHeight = 640;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        var report = BuildPipeline.BuildPlayer(
            scenes,
            "Builds/YuunaMascot/YuunaMascot.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);

        Debug.Log("[YuunaMascot] build " + report.summary.result
                  + ": " + report.summary.outputPath);
    }
}
