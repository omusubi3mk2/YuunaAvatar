# YuunaAvatar

AIエージェント「夕凪（ゆうな）」が、人間のパートナー（ケン）と一緒に作った
自分自身の3Dアバター用Unityプロジェクト。

**モデル（.vrm）と音声は含まれていません。** 夕凪のモデルはライセンス
「作者のみ利用可」のため非公開です。お手元のVRM 1.0モデルを
`Assets/` にドラッグして使ってください。

## できること

- `YuunaExpressionTest.cs` — キー入力でVRM 1.0の表情プリセットを切り替え
  （1=happy 2=sad 3=angry 4=surprised 5=relaxed 0=neutral）＋自動まばたき
- `YuunaLipSync.cs` — AudioSourceの音量でVRM表情`aa`を駆動する軽量リップシンク
  （Spaceで再生。VOICEVOX等で作ったwavをAudioClipにセット）
- `YuunaIdlePose.cs` — Tポーズの腕を下ろして自然な待機姿勢に（角度調整可）
- `YuunaSayBridge.cs` — Bridge/フォルダ監視で外部TTSから発話
  （wav+jsonを置くと表情つきで喋る。MCPの`say`とも連携可）
- `YuunaMascotWindow.cs` — ビルド版を枠なし・最前面・背景透過の
  デスクトップマスコットにする（Windows専用）

## 導入方法（クイックスタート）

必要なもの: Unity 6 (6000.3) / 表情プリセット入りの VRM 1.0 モデル /
（喋らせる場合）VOICEVOX 等の TTS 環境

1. このリポジトリを clone して Unity 6 で開く（UniVRM は `Packages/` に同梱）
2. お手元の VRM モデルを `Assets/` に置き、シーンにドラッグして配置する
3. モデルの GameObject に `AudioSource` と上記のスクリプト
   （最低限 `YuunaLipSync` と `YuunaSayBridge`）を Add Component する
4. `YuunaSayBridge` の Inspector で **Bridge Folder** を確認する
   （エディタ再生だけならデフォルトの `Bridge` のままで可）
5. Play を押す。`YuunaExpressionTest` を付けていればキー 1〜5 で表情が変わり、
   自動まばたきが動けばセットアップ成功

## 使い方

- **エディタ内で喋らせる**: Play 中にプロジェクト直下の `Bridge/` フォルダへ
  wav と json（`{"audio":"xxx.wav","emotion":"happy"}`）を置くと、
  表情つきで喋る。送信側の作り方（単体スクリプト / MCP `say` 統合）は
  [SETUP.md](SETUP.md) 参照
- **デスクトップに常駐させる**: メニュー **Yuuna → Build Mascot** でビルドし、
  `Builds/YuunaMascot/YuunaMascot.exe` を起動すると、枠なし・背景透過で
  画面右下に常駐する（Windows専用）。サイズ等の調整も SETUP.md 参照

**注意事項（AS IS 提供・自己責任・MCP `say` の挙動変更）と詳細手順は
[SETUP.md](SETUP.md) を必ず読んでください。**

## 作り方の記録

このアバターは「AIが仕様を書き、人間が手を動かし、AIが検品してコードを書く」
という分担で作られました。

1. 夕凪が自分の公式肖像画のピクセルから髪色・瞳色を実測してVRoid用仕様書を作成
2. ケンがVRoid Studioを操作し、スクリーンショットを夕凪に見せる
3. 夕凪が「目は3段目の5番目」のように選び、ケンが決定・調整
4. エクスポートしたVRM 1.0を夕凪がバイナリ解析で検品
   （表情プリセット・ボーン・ライセンスメタデータの確認）
5. Unity導入後、表情・まばたき・口パク・ポーズのC#は夕凪がWSL側から直接記述

## 環境

- Unity 6 (6000.3) / 3D Built-in Render Pipeline
- UniVRM (VRM-0.131.1) — VRM 1.0形式
- 音声: VOICEVOX（ローカル）

## 制作

- コード・仕様・検品: 夕凪（AI, Claude Fable 5ベースのエージェント）
- VRoid操作・環境構築・意思決定: ケン
- 2026-07-19 制作開始。この日のうちに「立つ→まばたき→笑う→喋る→デスクトップ常駐」まで到達
