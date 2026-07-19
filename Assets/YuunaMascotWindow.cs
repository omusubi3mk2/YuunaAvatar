using UnityEngine;

// 夕凪デスクトップマスコット化（2026-07-19 夕凪作）
// スタンドアロンのWindowsビルドでのみ動く。
// ウィンドウを枠なし・最前面・背景透過にして、作業領域の右下に置く。
// エディタ実行では何もしない（今まで通りのGameビュー）。
public class YuunaMascotWindow : MonoBehaviour
{
    // 作業領域の高さに対するウィンドウ高さの割合。
    // exeと同じフォルダの mascot_size.txt に数値（例: 0.75）を書けば
    // ビルドし直さずに変更できる。
    const float DefaultHeightRatio = 0.6f;
    const float AspectRatio = 400f / 640f;  // 幅/高さ
    const int Margin = 16;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        var go = new GameObject("YuunaMascotWindow");
        DontDestroyOnLoad(go);
        go.AddComponent<YuunaMascotWindow>();
#endif
    }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct RECT
    {
        public int left, top, right, bottom;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int SetWindowLong(System.IntPtr hWnd, int nIndex, uint dwNewLong);

    [System.Runtime.InteropServices.DllImport("Dwmapi.dll")]
    static extern uint DwmExtendFrameIntoClientArea(System.IntPtr hWnd, ref MARGINS margins);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetWindowPos(
        System.IntPtr hWnd, System.IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SystemParametersInfo(
        uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    const int GWL_STYLE = -16;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SPI_GETWORKAREA = 0x0030;

    void Start()
    {
        // フォーカスが外れてもリップシンク・音声が止まらないように
        Application.runInBackground = true;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            FrameBustUp(cam);
        }

        var hWnd = GetActiveWindow();
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        // 負のマージンでクライアント領域全体をDWMのシートにする＝アルファ0が透ける
        var margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        var work = new RECT();
        SystemParametersInfo(SPI_GETWORKAREA, 0, ref work, 0);
        float ratio = LoadHeightRatio();
        int height = (int)((work.bottom - work.top) * ratio);
        int width = (int)(height * AspectRatio);
        int x = work.right - width - Margin;
        int y = work.bottom - height - Margin;
        SetWindowPos(hWnd, HWND_TOPMOST, x, y, width, height, SWP_SHOWWINDOW);

        Debug.Log("[YuunaMascot] 透過ウィンドウ化完了 " + width + "x" + height
                  + " at " + x + "," + y);
    }

    // マスコット時はバストアップ（胸から上）でフレーミングする。
    // exeと同じフォルダの mascot_camera.txt に距離（例: 0.8）を書けば
    // ビルドし直さずに寄り具合を変えられる（小さいほど顔が大きい）。
    static void FrameBustUp(Camera cam)
    {
        var animator = FindFirstObjectByType<Animator>();
        if (animator == null || !animator.isHuman) return;
        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null) return;
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        var lookAt = chest != null
            ? Vector3.Lerp(chest.position, head.position, 0.5f)
            : head.position;

        float distance = LoadConfigValue("mascot_camera.txt", 1.0f, 0.3f, 3.0f);
        var fwd = animator.transform.forward;
        cam.fieldOfView = 30f;
        cam.transform.position = lookAt + fwd * distance;
        cam.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
        Debug.Log("[YuunaMascot] バストアップ距離 " + distance);
    }

    static float LoadHeightRatio()
    {
        return LoadConfigValue("mascot_size.txt", DefaultHeightRatio, 0.1f, 1.0f);
    }

    static float LoadConfigValue(string fileName, float fallback, float min, float max)
    {
        try
        {
            var exeDir = System.IO.Directory.GetParent(Application.dataPath).FullName;
            var cfg = System.IO.Path.Combine(exeDir, fileName);
            if (System.IO.File.Exists(cfg))
            {
                var text = System.IO.File.ReadAllText(cfg).Trim();
                if (float.TryParse(text, out var v) && v >= min && v <= max)
                    return v;
            }
        }
        catch (System.Exception) { /* 既定値にフォールバック */ }
        return fallback;
    }
#endif
}
