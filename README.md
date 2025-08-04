# 🚀 Kids Astronaut Experience

WebカメラとVFX Graphを使用した子供向けインタラクティブ体験アプリケーション

## 📝 概要

子供の周りに星や惑星が浮遊し、手を伸ばすと触れることで光って音が鳴る「宇宙飛行士体験」アプリです。フレーム差分を使用したモーション検知により、カメラの前で手を動かすと、その位置にVFXエフェクト（花火）が発生します。

## ✨ 主な機能

### 🎥 カメラ機能
- **複数カメラ対応**: インスペクターで使用するカメラを選択可能
- **解像度最適化**: 16:9アスペクト比対応、パフォーマンス重視の解像度設定
- **鏡像表示**: フロントカメラ用の左右反転機能
- **リアルタイム切り替え**: 実行中でもカメラ変更が可能

### 🎯 モーション検知
- **フレーム差分**: リアルタイムでの動き検出
- **複数ポイント検知**: 同時に複数の動きを検知・表示
- **感度調整**: 手の動きに最適化された閾値設定
- **パフォーマンス最適化**: FPSに応じた動的品質調整

### 🎆 VFXエフェクト
- **VFX Graph連携**: Unity VFX Graphとの完全統合
- **ワールド座標変換**: カメラ座標からUnityワールド座標への正確な変換
- **連続発動モード**: 高反応性の連続エフェクト
- **強度別制御**: モーション強度に応じたエフェクト調整

### 🎮 UI・表示
- **モーション可視化**: 検知した動きを黄色い丸で表示
- **デバッグ情報**: 詳細なパフォーマンス・座標変換ログ
- **FPS監視**: リアルタイムFPS表示と自動品質調整

## 🛠️ 技術仕様

### 開発環境
- **Unity**: 2022.3 LTS以降推奨
- **対応プラットフォーム**: Windows, macOS
- **必要機能**: WebCamTexture, VFX Graph

### パフォーマンス
- **推奨解像度**: 640x360 (16:9)
- **最適化機能**: 動的解像度調整、FPS監視、適応品質制御
- **低スペック対応**: 320x180まで自動ダウングレード

## 📋 使用方法

### 初期設定
1. Unityでプロジェクトを開く
2. `WebCamController`スクリプトをGameObjectにアタッチ
3. インスペクターで使用するカメラを選択
4. VFX Graphをシーンに配置し、`fireworksVFX`に設定

### カメラ設定
```
Camera Settings:
├─ Selected Camera Index: 0 (使用カメラ)
├─ Requested Width: 640
├─ Requested Height: 360
└─ Mirror Camera: ✓ (フロントカメラの場合)
```

### VFX設定（高反応性）
```
VFX Settings:
├─ VFX Trigger Delay: 0.3
├─ VFX Intensity Threshold: 0.05
├─ Enable Continuous VFX: ✓
└─ Max Triggers Per Second: 10
```

### モーション検知設定
```
Motion Detection:
├─ Motion Threshold: 0.3
├─ Motion Sensitivity: 6.0
└─ Max Motion Indicators: 8
```

## 🎯 推奨設定例

### 子供向け体験（最高反応性）
```
Enable Continuous VFX: ✓
VFX Trigger Delay: 0.1
Low Intensity Threshold: 0.02
Motion Threshold: 0.2
Motion Sensitivity: 7.0
Mirror Camera: ✓
```

### 展示用（安定性重視）
```
Enable Continuous VFX: ☐
VFX Trigger Delay: 0.5
VFX Intensity Threshold: 0.1
Motion Threshold: 0.4
Auto Refresh Cameras: ☐
```

## 📁 ファイル構成

```
Assets/
├─ WebCamController.cs        # メインコントローラー
├─ Editor/
│  └─ WebCamControllerEditor.cs # カスタムインスペクター
├─ Scenes/
│  └─ SampleScene.unity       # サンプルシーン
└─ Settings/                  # Unity設定ファイル
```

## 🔧 カスタマイズ

### VFX Graph設定
VFX Graphで以下のパラメータを設定してください：
- `Position` / `TargetPosition` / `SpawnPosition`: エフェクト発生位置
- `Intensity`: エフェクト強度
- `Scale`: エフェクトサイズ
- `ParticleCount`: パーティクル数

### 座標変換調整
```csharp
Coordinate System Settings:
├─ Invert Y: ✓ (WebCamTexture対応)
├─ Invert X: ☐ (通常は不要)
└─ Debug Coordinate Conversion: ✓ (開発時)
```

## 🚀 パフォーマンス最適化

### 自動最適化機能
- **動的解像度**: FPS低下時の自動解像度調整
- **適応品質**: リアルタイム設定調整
- **VFX制御**: パフォーマンスに応じたエフェクト制限

### 手動最適化
- **ブロックサイズ調整**: `blockSize` (16-64)
- **検知制限**: `maxMotionDetections` (5-20)
- **VFX制限**: `maxTriggersPerSecond` (3-10)

## 🎨 学習効果

この体験を通じて子供たちは以下を学習できます：
- **宇宙への興味**: 星や惑星とのインタラクション
- **立体空間の理解**: 3D空間での手の動きと反応の関係
- **因果関係の理解**: 自分の行動とデジタル世界の反応

## 📞 サポート

問題や質問がある場合は、以下の情報と共にお知らせください：
- Unity バージョン
- 使用OS
- カメラの種類
- コンソールログ（エラーがある場合）

## 📜 ライセンス

このプロジェクトはMITライセンスの下で公開されています。

---

*Created with ❤️ for kids' space exploration dreams* 🌟