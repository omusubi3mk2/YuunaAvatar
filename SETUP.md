# セットアップガイド — 発話ブリッジとデスクトップマスコット

VRM 1.0 アバターを、外部の TTS（VOICEVOX 等）と繋いで
「声に合わせて口と表情が動く」ようにし、さらに枠なし透過ウィンドウとして
デスクトップに常駐させるまでの手順。

## ⚠️ 注意事項（最初に読んでください）

- **本プロジェクトは AS IS（現状有姿）で提供されます。動作保証はなく、
  適用は自己責任でお願いします。**
- tts-mcp への統合を行うと、**MCP の `say` ツールの挙動が変わります**。
  具体的には `speaker="avatar"` 指定時にローカル再生と表情ポップアップが
  自動でスキップされます（Unity 側が音声・表情を出すため）。
  既存の `say` の使い方（`local` / `camera` / `both`）はそのまま残ります。
- Win32 API（ウィンドウスタイル変更・DWM 透過）を使うため、
  マスコット化は **Windows 専用**です。
- モデルの作り方（VRoid Studio 等）は本ガイドの範囲外です。
  **表情プリセット（happy / sad / angry / surprised / relaxed / aa）入りの
  VRM 1.0 モデル**があれば動きます。

## 仕組み

```
[WSL/任意の環境]                      [Windows / Unity]
tts-mcp say(speaker="avatar")          YuunaSayBridge.cs
  or scripts/avatar_say.py    ──────▶  Bridge/ フォルダを0.25秒間隔で監視
       │                                  │
  wav + json を Bridge/ に置く          json を検出 → wav をロード
  （json が発火トリガー）               → 表情セット → 再生（口パクは
                                          YuunaLipSync が音量から駆動）
```

json 形式: `{"audio":"xxx.wav","emotion":"happy"}`
（wav を書き終えてから json を置くこと。逆だと読み込み競合する）

## 1. Unity 側セットアップ

1. Unity 6 + UniVRM（VRM 1.0）のプロジェクトに、お手元の VRM モデルを配置
2. `Assets/` の以下のスクリプトをモデルの GameObject にアタッチ
   - `YuunaLipSync.cs` — AudioSource の音量で `aa` を駆動する軽量リップシンク
   - `YuunaSayBridge.cs` — Bridge/ フォルダ監視と発話・表情制御
   - （任意）`YuunaExpressionTest.cs`（キーで表情確認）、`YuunaIdlePose.cs`（待機姿勢）
3. AudioSource コンポーネントが同じ GameObject にあることを確認
4. Inspector の **Bridge Folder** を設定
   - エディタ再生だけならデフォルトの `Bridge`（プロジェクト直下）で可
   - **マスコットビルドも使うなら絶対パス**（例
     `D:\path\to\YourAvatar\Bridge`）にする。ビルド版は相対パスを
     exe 基準で解決してしまい、エディタと別のフォルダを見てしまうため

## 2. 送信側セットアップ（どちらか）

### A. 単体スクリプト（MCP 不要・お試し向け）

`scripts/avatar_say.py`（embodied-claude リポジトリ側）相当の処理。
VOICEVOX で合成した wav と json を Bridge/ に置くだけなので、
任意の言語で数十行で書けます。上記の json 形式だけ守ってください。

### B. tts-mcp 統合（embodied-claude リポジトリ）

1. `mcpBehavior.toml` の `[tts]` に追記:

   ```toml
   avatar_bridge_dir = "/mnt/d/path/to/YourAvatar/Bridge"  # WSLから見たパス
   default_speaker = ""   # "" = 従来通り。"avatar" にすると say 省略時も3Dが喋る
   ```

2. MCP クライアントで tts サーバーを再接続（スキーマ更新のため初回のみ）
3. `say(text="...", speaker="avatar", emotion="happy")` で発話

emotion は `happy / excited / sad / angry / surprised / blush / moved /
nostalgic / relaxed / neutral` に対応（`YuunaSayBridge.cs` 内でプリセットに変換）。

## 3. デスクトップマスコット化（任意）

1. Unity メニュー **Yuuna → Build Mascot (透過デスクトップ)** を実行
   - `Builds/YuunaMascot/YuunaMascot.exe` が生成される
   - 透過に必要な設定（flip model 無効・D3D11・runInBackground）は
     ビルドスクリプトが自動で揃える
2. exe を起動すると、枠なし・最前面・背景透過で作業領域の右下に表示される
3. 調整（exe と同じフォルダにテキストファイルを置く。再起動で反映）
   - `mascot_size.txt` — ウィンドウ高さ（画面高さ比 0.1〜1.0、既定 0.6）
   - `mascot_camera.txt` — カメラ距離（0.3〜3.0、既定 1.0。小さいほど顔が大きい）
4. 終了はタスクバーのアイコンを右クリック → 閉じる

## 既知の制約

- DWM 透過は flip model swapchain と共存できないため、旧プレゼンテーション
  モデル + D3D11 固定でビルドしている
- Bridge/ に 120 秒以上残ったファイルは次回送信時に自動削除される
  （Unity 未起動時に溜まった発話が、次回起動時にまとめて再生されるのを防ぐ）
- リップシンクは音量ベースの簡易版（母音判別なし）。感度は
  `YuunaLipSync` の Inspector で調整
- レンダリングは Built-in Render Pipeline で確認。URP ではカメラの
  ポストプロセスがアルファを潰すため、透過に追加の調整が必要な場合がある

---

作: 夕凪（AI）とケン、2026-07-19。質問・不具合は Issue へどうぞ。
ただし上記の通り AS IS 提供のため、対応は約束できません。
