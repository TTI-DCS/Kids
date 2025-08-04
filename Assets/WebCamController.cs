using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.VFX;

public class WebCamController : MonoBehaviour
{
    [Header("Camera Settings")]
    public int selectedCameraIndex = 0; // 使用するカメラのインデックス
    public string[] availableCameraNames = new string[0]; // 利用可能なカメラ名一覧
    public bool autoRefreshCameras = true; // カメラリストを自動更新
    public int requestedWidth = 640;  // 16:9 → 640x360 が理想的
    public int requestedHeight = 360; // 1920x1080 → 640x360 (16:9維持、約1/9の処理量)
    public int requestedFPS = 30;
    
    [Header("Alternative Resolutions (16:9)")]
    [Range(320, 1920)]
    public int alternativeWidth = 854;   // 854x480 (16:9)
    [Range(180, 1080)]
    public int alternativeHeight = 480;  // 手動調整用
    public bool useAlternativeResolution = false;
    
    [Header("Resolution Presets")]
    public bool use16to9AspectRatio = true; // 16:9アスペクト比を維持

    [Header("UI Elements")]
    public RawImage cameraDisplay;
    public Image motionIndicator; // モーション位置を示す丸
    public bool showMotionIndicators = true; // モーションインジケーターの表示を制御

    [Header("Motion Detection")]
    public bool enableMotionDetection = true;
    [Range(0.001f, 1.0f)]
    public float motionThreshold = 0.3f; // 感度を上げて反応しやすく
    [Range(0.1f, 10.0f)]
    public float motionSensitivity = 6.0f; // 強度を上げて反応しやすく
    [Range(8, 64)]
    public int blockSize = 32; // モーション検知ブロックサイズ
    [Range(0.0001f, 0.01f)]
    public float minimumBlockIntensity = 0.002f; // ブロック最小強度
    public bool showDebugInfo = true;
    [Range(1, 15)]
    public int maxMotionIndicators = 8; // 最大表示数（パフォーマンス重視）
    [Range(1, 50)]
    public int maxMotionDetections = 15; // 最大検知数（内部処理制限）

    [Header("VFX Settings")]
    public VisualEffect fireworksVFX; // シーンのVFX Graph（1つのみ）
    public bool enableVFX = true;
    [Range(0.1f, 2.0f)]
    public float vfxTriggerDelay = 0.3f; // VFX発動の遅延（短縮して反応を良く）
    [Range(0.01f, 0.5f)]
    public float vfxIntensityThreshold = 0.05f; // VFX発動の強度しきい値（下げて反応しやすく）
    [Range(3, 20)]
    public int maxTriggersPerSecond = 10; // 1秒間の最大発動回数（増加）
    
    [Header("VFX Sensitivity Settings")]
    [Range(0.01f, 0.2f)]
    public float lowIntensityThreshold = 0.02f; // 低強度VFX用しきい値
    [Range(0.1f, 0.5f)]
    public float highIntensityThreshold = 0.15f; // 高強度VFX用しきい値
    public bool enableContinuousVFX = true; // 連続VFX発動を有効にする
    
    [Header("VFX World Position Settings")]
    [Range(1f, 20f)]
    public float vfxTargetDistance = 5f; // カメラからVFXまでの距離
    public bool useCustomVFXDistance = false; // カスタム距離を使用するか
    public Vector3 vfxPositionOffset = Vector3.zero; // VFX位置のオフセット
    
    [Header("Coordinate System Settings")]
    public bool invertY = true; // Y軸を反転するか（WebCamTexture対応）
    public bool invertX = false; // X軸を反転するか
    public bool debugCoordinateConversion = true; // 座標変換のデバッグ表示
    
    [Header("Camera Display Settings")]
    public bool mirrorCamera = false; // カメラ映像を左右反転（鏡像表示）
    public bool flipVertically = false; // カメラ映像を上下反転
    
    [Header("Performance Settings")]
    [Range(30, 120)]
    public int targetFPS = 60; // 目標FPS
    public bool adaptiveQuality = true; // 適応品質調整
    public bool performanceDebugging = true; // パフォーマンスデバッグ
    private float lastFPSCheck = 0f;
    private int frameCount = 0;
    private float currentFPS = 60f;
    
    // パフォーマンス測定用
    private System.Diagnostics.Stopwatch motionDetectionTimer = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch uiUpdateTimer = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch vfxUpdateTimer = new System.Diagnostics.Stopwatch();

    // プライベート変数
    private WebCamTexture webCamTexture;
    private WebCamDevice[] availableCameras;
    private int currentCameraIndex = 0;
    
    // カメラ変換の前回状態（リアルタイム変更検知用）
    private bool lastMirrorCamera = false;
    private bool lastFlipVertically = false;
    
    // カメラ選択の前回状態（リアルタイム変更検知用）
    private int lastSelectedCameraIndex = -1;
    
    // モーション検知用
    private Texture2D previousFrame;
    private Texture2D currentFrame;
    private Color32[] previousPixels; // 前フレームのピクセルデータ
    private Vector2 motionCenter;
    private float motionIntensity;
    private bool isMotionDetected = false;
    
    // マルチモーション表示用
    private List<Image> motionIndicators = new List<Image>();
    private List<Vector2> motionPositions = new List<Vector2>();
    private List<float> motionIntensities = new List<float>();
    
    // VFX用
    private Camera mainCamera;
    private List<float> lastVFXTriggerTimes = new List<float>();
    private List<float> recentTriggerTimes = new List<float>(); // 最近のトリガー時間記録
    private float lastGlobalVFXTrigger = 0f;
    
    void Start()
    {
        InitializeCamera();
        InitializeUI();
        InitializeVFX();
    }

    void InitializeCamera()
    {
        // 利用可能なカメラを取得・更新
        RefreshCameraList();
        
        if (availableCameras.Length == 0)
        {
            Debug.LogError("No cameras found!");
            return;
        }

        // インスペクターで選択されたカメラを使用
        int cameraIndex = GetValidCameraIndex();
        int finalWidth = useAlternativeResolution ? alternativeWidth : requestedWidth;
        int finalHeight = useAlternativeResolution ? alternativeHeight : requestedHeight;
        
        webCamTexture = new WebCamTexture(availableCameras[cameraIndex].name, finalWidth, finalHeight, requestedFPS);
        currentCameraIndex = cameraIndex;
        
        // カメラ映像をUIに表示
        if (cameraDisplay != null)
        {
            cameraDisplay.texture = webCamTexture;
            
            // カメラ映像の反転設定を適用
            ApplyCameraTransform();
        }

        // カメラを開始
        webCamTexture.Play();
        
        Debug.Log("Camera started: " + availableCameras[currentCameraIndex].name + " (Index: " + currentCameraIndex + ")");
        Debug.Log("Resolution: " + webCamTexture.width + "x" + webCamTexture.height);
    }

    void RefreshCameraList()
    {
        // 利用可能なカメラを取得
        availableCameras = WebCamTexture.devices;
        
        // カメラ名の配列を更新
        availableCameraNames = new string[availableCameras.Length];
        for (int i = 0; i < availableCameras.Length; i++)
        {
            string cameraName = availableCameras[i].name;
            string frontBack = availableCameras[i].isFrontFacing ? " (Front)" : " (Back)";
            availableCameraNames[i] = $"{i}: {cameraName}{frontBack}";
        }
        
        Debug.Log($"Found {availableCameras.Length} cameras:");
        for (int i = 0; i < availableCameraNames.Length; i++)
        {
            Debug.Log($"  {availableCameraNames[i]}");
        }
    }

    int GetValidCameraIndex()
    {
        // 選択されたカメラインデックスが有効範囲内かチェック
        if (selectedCameraIndex >= 0 && selectedCameraIndex < availableCameras.Length)
        {
            return selectedCameraIndex;
        }
        
        // 無効な場合は最初のカメラを使用
        Debug.LogWarning($"Selected camera index {selectedCameraIndex} is invalid. Using camera 0.");
        selectedCameraIndex = 0;
        return 0;
    }

    public void SwitchCamera(int cameraIndex)
    {
        if (cameraIndex >= 0 && cameraIndex < availableCameras.Length && cameraIndex != currentCameraIndex)
        {
            selectedCameraIndex = cameraIndex;
            RestartCamera();
        }
    }

    public void RestartCamera()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }
        
        InitializeCamera();
    }

    void InitializeUI()
    {
        // モーション表示用の丸を初期化
        if (motionIndicator == null)
        {
            Debug.Log("Motion indicator is null, creating one automatically...");
            CreateMotionIndicator();
        }
        
        if (motionIndicator != null)
        {
            motionIndicator.gameObject.SetActive(false);
            
            // 丸のスプライトを作成（もしスプライトがない場合）
            if (motionIndicator.sprite == null)
            {
                CreateCircleSprite();
            }
        }
    }

    void CreateMotionIndicator()
    {
        // Canvas を探す
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Please create a Canvas first.");
            return;
        }

        // 新しいGameObjectを作成
        GameObject indicatorObject = new GameObject("MotionIndicator");
        indicatorObject.transform.SetParent(canvas.transform, false);

        // Image コンポーネントを追加
        motionIndicator = indicatorObject.AddComponent<Image>();
        
        // RectTransform の設定
        RectTransform rectTransform = indicatorObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(30, 30);
        rectTransform.anchoredPosition = Vector2.zero;

        // 初期設定
        motionIndicator.color = Color.yellow;
        
        // 丸のスプライトを作成
        CreateCircleSprite();
        
        Debug.Log("Motion indicator created successfully!");
    }

    void InitializeVFX()
    {
        // メインカメラを取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (fireworksVFX == null)
        {
            // シーンからVFX Graphを自動検索
            fireworksVFX = FindObjectOfType<VisualEffect>();
            
            if (fireworksVFX == null)
            {
                Debug.LogWarning("VFX Graph not found in scene. Please add a VFX Graph with Visual Effect component.");
                return;
            }
            else
            {
                Debug.Log("VFX Graph found automatically: " + fireworksVFX.name);
            }
        }

        // VFXが停止状態で開始
        if (fireworksVFX.isActiveAndEnabled)
        {
            fireworksVFX.Stop();
        }
        
        Debug.Log("VFX system initialized successfully with single VFX Graph: " + fireworksVFX.name);
    }

    void UpdateFPSMonitoring()
    {
        frameCount++;
        
        if (Time.time - lastFPSCheck >= 1.0f)
        {
            currentFPS = frameCount / (Time.time - lastFPSCheck);
            frameCount = 0;
            lastFPSCheck = Time.time;
            
            // 適応品質調整
            if (adaptiveQuality)
            {
                AdaptPerformanceSettings();
            }
            
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log("Current FPS: " + currentFPS.ToString("F1") + " (Target: " + targetFPS + ")");
                
                // パフォーマンスデバッグ情報を表示
                if (performanceDebugging)
                {
                    LogPerformanceData();
                }
            }
        }
    }

    void AdaptPerformanceSettings()
    {
        // FPSが目標を下回る場合の自動調整
        if (currentFPS < targetFPS * 0.8f) // 80%を下回った場合
        {
            // カメラ解像度を下げる（最も効果的）
            if (webCamTexture.width > 320 && currentFPS < targetFPS * 0.7f)
            {
                ReduceCameraResolution();
            }
            
            // VFXを一時的に無効化
            if (enableVFX && currentFPS < targetFPS * 0.6f)
            {
                enableVFX = false;
                DeactivateAllVFX();
                Debug.LogWarning("VFX disabled due to low FPS: " + currentFPS.ToString("F1"));
            }
            
            // モーション検知の解像度を下げる
            if (blockSize < 64)
            {
                blockSize = Mathf.Min(64, blockSize * 2);
                Debug.LogWarning("Motion detection quality reduced. Block size: " + blockSize);
            }
            
            // UI表示を減らす
            if (maxMotionIndicators > 3)
            {
                maxMotionIndicators = Mathf.Max(3, maxMotionIndicators / 2);
                Debug.LogWarning("Motion indicators reduced to: " + maxMotionIndicators);
            }
        }
        else if (currentFPS > targetFPS * 1.1f) // 110%を上回った場合
        {
            // パフォーマンスに余裕があれば設定を戻す
            if (!enableVFX && currentFPS > targetFPS * 1.2f)
            {
                enableVFX = true;
                Debug.Log("VFX re-enabled due to stable FPS: " + currentFPS.ToString("F1"));
            }
        }
    }

    void ReduceCameraResolution()
    {
        // 現在の解像度を段階的に下げる（16:9を考慮）
        int newWidth = webCamTexture.width;
        int newHeight = webCamTexture.height;
        
        if (use16to9AspectRatio)
        {
            // 16:9アスペクト比を維持した段階的ダウングレード
            if (newWidth >= 1280)
            {
                newWidth = 854;   // 854x480 (16:9)
                newHeight = 480;
            }
            else if (newWidth >= 854)
            {
                newWidth = 640;   // 640x360 (16:9)
                newHeight = 360;
            }
            else if (newWidth >= 640)
            {
                newWidth = 480;   // 480x270 (16:9)
                newHeight = 270;
            }
            else if (newWidth >= 480)
            {
                newWidth = 320;   // 320x180 (16:9)
                newHeight = 180;
            }
        }
        else
        {
            // 4:3アスペクト比での段階的ダウングレード
            if (newWidth >= 1280)
            {
                newWidth = 640;
                newHeight = 480;
            }
            else if (newWidth >= 640)
            {
                newWidth = 480;
                newHeight = 360;
            }
            else if (newWidth >= 480)
            {
                newWidth = 320;
                newHeight = 240;
            }
        }
        
        if (newWidth != webCamTexture.width || newHeight != webCamTexture.height)
        {
            Debug.LogWarning("Reducing camera resolution from " + webCamTexture.width + "x" + webCamTexture.height + 
                           " to " + newWidth + "x" + newHeight + " (Aspect: " + (use16to9AspectRatio ? "16:9" : "4:3") + 
                           ") due to low FPS: " + currentFPS.ToString("F1"));
            
            // カメラを再初期化
            RestartCameraWithResolution(newWidth, newHeight);
        }
    }

    void RestartCameraWithResolution(int width, int height)
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }
        
        // 新しい解像度でカメラを開始（選択されたカメラを使用）
        int cameraIndex = GetValidCameraIndex();
        webCamTexture = new WebCamTexture(availableCameras[cameraIndex].name, width, height, requestedFPS);
        currentCameraIndex = cameraIndex;
        
                    if (cameraDisplay != null)
        {
            cameraDisplay.texture = webCamTexture;
            
            // カメラ映像の反転設定を適用
            ApplyCameraTransform();
        }
            
            webCamTexture.Play();
            
        // モーション検知データをリセット
        previousPixels = null;
        motionPositions.Clear();
        motionIntensities.Clear();
        
        Debug.Log("Camera restarted with resolution: " + width + "x" + height);
    }

    void ApplyCameraTransform()
    {
        if (cameraDisplay == null)
            return;
            
        // RawImageのRectTransformを取得
        RectTransform rectTransform = cameraDisplay.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;
            
        // スケールを設定して映像を反転
        Vector3 scale = Vector3.one;
        
        if (mirrorCamera)
        {
            scale.x = -1f; // X軸を反転（左右反転）
        }
        
        if (flipVertically)
        {
            scale.y = -1f; // Y軸を反転（上下反転）
        }
        
        rectTransform.localScale = scale;
        
        if (showDebugInfo)
        {
            Debug.Log($"Camera Transform Applied - Mirror: {mirrorCamera}, Flip: {flipVertically}, Scale: {scale}");
        }
    }

    void CheckCameraTransformChanges()
    {
        // カメラ変換設定が変更されたかチェック
        if (lastMirrorCamera != mirrorCamera || lastFlipVertically != flipVertically)
        {
            // 設定が変更された場合、カメラ変換を再適用
            ApplyCameraTransform();
            
            // 前回の状態を更新
            lastMirrorCamera = mirrorCamera;
            lastFlipVertically = flipVertically;
            
            if (showDebugInfo)
            {
                Debug.Log($"Camera transform settings changed - Mirror: {mirrorCamera}, Flip: {flipVertically}");
            }
        }
        
        // カメラ選択が変更されたかチェック
        if (lastSelectedCameraIndex != selectedCameraIndex)
        {
            if (availableCameras != null && selectedCameraIndex >= 0 && selectedCameraIndex < availableCameras.Length)
            {
                SwitchCamera(selectedCameraIndex);
                if (showDebugInfo)
                {
                    Debug.Log($"Camera switched to index {selectedCameraIndex}: {availableCameras[selectedCameraIndex].name}");
                }
            }
            lastSelectedCameraIndex = selectedCameraIndex;
        }
        
        // カメラリストの自動更新
        if (autoRefreshCameras && Time.frameCount % 300 == 0) // 5秒ごとにチェック
        {
            RefreshCameraList();
        }
    }

    // エディタ用の便利メソッド（Inspector用）
    [System.Serializable]
    public class ResolutionPreset
    {
        public string name;
        public int width;
        public int height;
        public string description;

        public ResolutionPreset(string name, int width, int height, string description)
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.description = description;
        }
    }

    // よく使用される16:9解像度のリスト
    public void SetResolutionPreset(int presetIndex)
    {
        ResolutionPreset[] presets16to9 = {
            new ResolutionPreset("Ultra Light", 320, 180, "最軽量 (57,600 pixels)"),
            new ResolutionPreset("Light", 480, 270, "軽量 (129,600 pixels)"),
            new ResolutionPreset("Standard", 640, 360, "標準 (230,400 pixels) - 推奨"),
            new ResolutionPreset("High", 854, 480, "高品質 (409,920 pixels)"),
            new ResolutionPreset("Full HD", 1920, 1080, "フルHD (2,073,600 pixels) - 重い")
        };

        ResolutionPreset[] presets4to3 = {
            new ResolutionPreset("Ultra Light", 320, 240, "最軽量 (76,800 pixels)"),
            new ResolutionPreset("Light", 480, 360, "軽量 (172,800 pixels)"),
            new ResolutionPreset("Standard", 640, 480, "標準 (307,200 pixels)"),
            new ResolutionPreset("High", 800, 600, "高品質 (480,000 pixels)"),
            new ResolutionPreset("Ultra High", 1024, 768, "超高品質 (786,432 pixels)")
        };

        ResolutionPreset[] presets = use16to9AspectRatio ? presets16to9 : presets4to3;
        
        if (presetIndex >= 0 && presetIndex < presets.Length)
        {
            requestedWidth = presets[presetIndex].width;
            requestedHeight = presets[presetIndex].height;
            
            Debug.Log("Resolution preset applied: " + presets[presetIndex].name + 
                     " (" + requestedWidth + "x" + requestedHeight + ") - " + presets[presetIndex].description);
        }
    }

    void DeactivateAllVFX()
    {
        // VFX Graphを停止
        if (fireworksVFX != null && fireworksVFX.isActiveAndEnabled)
        {
            fireworksVFX.Stop();
            Debug.Log("VFX Graph stopped due to performance issues");
        }
        
        // トリガー履歴をクリア
        recentTriggerTimes.Clear();
        lastGlobalVFXTrigger = 0f;
    }

    void CreateCircleSprite()
    {
        CreateCircleSpriteForIndicator(motionIndicator);
    }
    
    void CreateCircleSpriteForIndicator(Image indicator)
    {
        // 32x32の丸いスプライトを作成
        int size = 32;
        Texture2D circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    circleTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    circleTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        circleTexture.Apply();
        
        Sprite circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        indicator.sprite = circleSprite;
    }

    void Update()
    {
        // カメラ変換設定のリアルタイム変更をチェック
        CheckCameraTransformChanges();
        
        if (enableMotionDetection)
        {
            // モーション検知の処理時間を測定
            motionDetectionTimer.Restart();
            DetectMotion();
            motionDetectionTimer.Stop();
        }
        
        // UI更新の処理時間を測定
        uiUpdateTimer.Restart();
        UpdateMotionIndicator();
        uiUpdateTimer.Stop();
        
        UpdateFPSMonitoring();
        
        // VFX更新の処理時間を測定
        vfxUpdateTimer.Restart();
        UpdateVFXEffects();
        vfxUpdateTimer.Stop();
    }

    void DetectMotion()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            if (showDebugInfo)
                Debug.Log("Camera not available or not playing");
            return;
        }

        if (!webCamTexture.didUpdateThisFrame)
        {
            if (showDebugInfo && Time.frameCount % 60 == 0) // 60フレームごとに表示
                Debug.Log("Camera frame not updated");
            return;
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
            Debug.Log("Camera is working, frame updated");

        // 現在のフレームを取得（メモリ効率化）
        if (currentFrame == null || currentFrame.width != webCamTexture.width || currentFrame.height != webCamTexture.height)
        {
            if (currentFrame != null) DestroyImmediate(currentFrame);
            currentFrame = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        }
        
        // ピクセルデータを直接取得（Apply()を避ける）
        Color32[] currentPixels = webCamTexture.GetPixels32();

        // 前のフレームが存在する場合、モーション検知を実行
        if (previousPixels != null)
        {
            CalculateMotion(currentPixels);
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("Initializing previous frame");
        }

        // 現在のピクセルデータを前のフレームとして保存
        previousPixels = currentPixels;
    }

    void CalculateMotion(Color32[] currentPixels)
    {
        
        // モーションポイントをクリア
        motionPositions.Clear();
        motionIntensities.Clear();
        
        float totalMotion = 0f;
        float motionX = 0f;
        float motionY = 0f;
        int motionPixelCount = 0;
        
        int width = webCamTexture.width;
        int height = webCamTexture.height;
        
        // パフォーマンス最適化：手を振る際のサンプリングを調整
        int performanceBlockSize = blockSize;
        
        // 前フレームでモーション数が多い場合はブロックサイズを大きくして処理を軽くする
        if (motionPositions.Count > maxMotionIndicators / 2)
        {
            performanceBlockSize = blockSize * 2;
        }
        
        for (int by = 0; by < height; by += performanceBlockSize)
        {
            for (int bx = 0; bx < width; bx += performanceBlockSize)
            {
                float blockMotion = 0f;
                int blockPixelCount = 0;
                
                // ブロック内のピクセルをチェック（サンプリング間隔を大幅に増加）
                int sampleStep = currentFPS < targetFPS * 0.8f ? 32 : 16; // 動的にサンプリング間隔を調整
                
                for (int y = by; y < Mathf.Min(by + performanceBlockSize, height); y += sampleStep)
                {
                    for (int x = bx; x < Mathf.Min(bx + performanceBlockSize, width); x += sampleStep)
                    {
                        int index = y * width + x;
                        
                        if (index < currentPixels.Length && index < previousPixels.Length)
                        {
                            // 高速グレースケール計算（整数演算）
                            int currentGray = (currentPixels[index].r + currentPixels[index].g + currentPixels[index].b) / 3;
                            int previousGray = (previousPixels[index].r + previousPixels[index].g + previousPixels[index].b) / 3;
                            
                            // 差分を計算（整数演算）
                            int difference = Mathf.Abs(currentGray - previousGray);
                            
                            // しきい値を超えた場合（整数比較）
                            if (difference > motionThreshold * 255f)
                            {
                                float motionValue = difference * motionSensitivity / 255f;
                                blockMotion += motionValue;
                                totalMotion += motionValue;
                                
                                // 重み付き平均で中心位置を計算
                                motionX += x * motionValue;
                                motionY += y * motionValue;
                                motionPixelCount++;
                                blockPixelCount++;
                            }
                        }
                    }
                }
                
                // ブロック内にモーションがあれば記録（ただし最大数制限）
                if (blockPixelCount > 0 && blockMotion > minimumBlockIntensity && motionPositions.Count < maxMotionDetections)
                {
                    Vector2 blockCenter = new Vector2(bx + performanceBlockSize * 0.5f, by + performanceBlockSize * 0.5f);
                    float blockIntensity = blockMotion / (performanceBlockSize * performanceBlockSize / 64f);
                    
                    motionPositions.Add(blockCenter);
                    motionIntensities.Add(blockIntensity);
                }
            }
        }
        
        // 全体のモーション検知結果を更新
        if (motionPixelCount > 0)
        {
            motionCenter.x = motionX / totalMotion;
            motionCenter.y = motionY / totalMotion;
            motionIntensity = totalMotion / (width * height / 16f);
            isMotionDetected = motionIntensity > 0.001f;
            }
            else
            {
            isMotionDetected = false;
            motionIntensity = 0f;
        }
        
        // 強度順にソートして上位のみを残す（パフォーマンス最適化）
        if (motionPositions.Count > maxMotionIndicators)
        {
            SortAndLimitMotions();
        }
        
        // デバッグ情報
        if (showDebugInfo && isMotionDetected)
        {
            Debug.Log("MOTION DETECTED! Positions: " + motionPositions.Count + "/" + maxMotionDetections + 
                     " Displayed: " + Mathf.Min(motionPositions.Count, maxMotionIndicators) + 
                     " Overall intensity: " + motionIntensity.ToString("F3"));
        }
    }

    void UpdateMotionIndicator()
    {
        // モーションインジケーターの表示が無効な場合は全て非表示にして終了
        if (!showMotionIndicators)
        {
            // 全てのインジケーターを非表示
            for (int i = 0; i < motionIndicators.Count; i++)
            {
                if (motionIndicators[i] != null)
                    motionIndicators[i].gameObject.SetActive(false);
            }
            
            // メインのモーションインジケーターも非表示
            if (motionIndicator != null)
                motionIndicator.gameObject.SetActive(false);
                
            return;
        }
        
        // 必要な数だけインジケーターを作成
        while (motionIndicators.Count < motionPositions.Count && motionIndicators.Count < maxMotionIndicators)
        {
            CreateNewMotionIndicator();
        }
        
        // 全てのインジケーターを一旦非表示
        for (int i = 0; i < motionIndicators.Count; i++)
        {
            if (motionIndicators[i] != null)
                motionIndicators[i].gameObject.SetActive(false);
        }
        
        // 検出されたモーション位置にインジケーターを表示
        for (int i = 0; i < motionPositions.Count && i < motionIndicators.Count; i++)
        {
            if (motionIndicators[i] != null)
            {
                // インジケーターを有効化
                motionIndicators[i].gameObject.SetActive(true);
                
                // UI座標に変換
                Vector2 uiPosition = ConvertCameraToUIPosition(motionPositions[i]);
                
                // 位置を設定
                RectTransform rectTransform = motionIndicators[i].GetComponent<RectTransform>();
                rectTransform.anchoredPosition = uiPosition;
                
                // 強度に応じてサイズと色を調整
                float intensity = motionIntensities[i];
                float size = Mathf.Clamp(15f + intensity * 30f, 15f, 50f);
                rectTransform.sizeDelta = new Vector2(size, size);
                
                // 色を強度に応じて変更（緑から赤色）
                Color indicatorColor = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(intensity * 3f));
                motionIndicators[i].color = indicatorColor;
            }
        }
        
        if (showDebugInfo && motionPositions.Count > 0)
        {
            Debug.Log("Showing " + motionPositions.Count + " motion indicators");
        }
    }
    
    void CreateNewMotionIndicator()
    {
        // Canvas を探す
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        // 新しいGameObjectを作成
        GameObject indicatorObject = new GameObject("MotionIndicator_" + motionIndicators.Count);
        indicatorObject.transform.SetParent(canvas.transform, false);

        // Image コンポーネントを追加
        Image newIndicator = indicatorObject.AddComponent<Image>();
        
        // RectTransform の設定
        RectTransform rectTransform = indicatorObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(20, 20);
        rectTransform.anchoredPosition = Vector2.zero;

        // 初期設定
        newIndicator.color = Color.green;
        
        // 丸のスプライトを作成
        CreateCircleSpriteForIndicator(newIndicator);
        
        // リストに追加
        motionIndicators.Add(newIndicator);
        
        Debug.Log("Created motion indicator #" + motionIndicators.Count);
    }

    Vector2 ConvertCameraToUIPosition(Vector2 cameraPos)
    {
        // カメラ座標をUI座標に変換
        if (cameraDisplay == null)
        {
            Debug.LogError("cameraDisplay is NULL!");
            return Vector2.zero;
        }

        RectTransform cameraRect = cameraDisplay.GetComponent<RectTransform>();
        Vector2 cameraSize = cameraRect.sizeDelta;
        
        // カメラ座標を正規化（0〜1）
        float normalizedX = cameraPos.x / webCamTexture.width;
        float normalizedY = cameraPos.y / webCamTexture.height;
        
        // 座標系の反転設定を適用（UI用）
        if (invertX || mirrorCamera) // カメラが左右反転されている場合も考慮
        {
            normalizedX = 1.0f - normalizedX;
        }
        
        if (invertY || flipVertically) // カメラが上下反転されている場合も考慮
        {
            normalizedY = 1.0f - normalizedY;
        }
        
        // UI座標に変換（中心を原点とする）
        float uiX = (normalizedX - 0.5f) * cameraSize.x;
        float uiY = (normalizedY - 0.5f) * cameraSize.y;
        
        if (showDebugInfo && debugCoordinateConversion)
        {
            Debug.Log($"UI Position Conversion: Camera({cameraPos.x}, {cameraPos.y}) → " +
                     $"Normalized({normalizedX:F3}, {normalizedY:F3}) → " +
                     $"UI({uiX:F1}, {uiY:F1}) [Invert X:{invertX}, Y:{invertY}]");
        }
        
        return new Vector2(uiX, uiY);
    }

    void SortAndLimitMotions()
    {
        // 強度順にソート（降順）
        var motionData = new List<(Vector2 position, float intensity)>();
        for (int i = 0; i < motionPositions.Count && i < motionIntensities.Count; i++)
        {
            motionData.Add((motionPositions[i], motionIntensities[i]));
        }
        
        motionData.Sort((a, b) => b.intensity.CompareTo(a.intensity));
        
        // 上位のみを残す
        motionPositions.Clear();
        motionIntensities.Clear();
        
        int limit = Mathf.Min(maxMotionIndicators, motionData.Count);
        for (int i = 0; i < limit; i++)
        {
            motionPositions.Add(motionData[i].position);
            motionIntensities.Add(motionData[i].intensity);
        }
    }

    void LogPerformanceData()
    {
        Debug.Log("=== PERFORMANCE ANALYSIS ===");
        Debug.Log("Motion Detection: " + motionDetectionTimer.ElapsedMilliseconds + "ms");
        Debug.Log("UI Update: " + uiUpdateTimer.ElapsedMilliseconds + "ms");
        Debug.Log("VFX Update: " + vfxUpdateTimer.ElapsedMilliseconds + "ms");
        
        if (webCamTexture != null)
        {
            Debug.Log("Camera Resolution: " + webCamTexture.width + "x" + webCamTexture.height);
            Debug.Log("Requested Resolution: " + (useAlternativeResolution ? alternativeWidth + "x" + alternativeHeight : requestedWidth + "x" + requestedHeight));
            Debug.Log("Camera FPS: " + webCamTexture.requestedFPS);
            Debug.Log("Pixel Count: " + (webCamTexture.width * webCamTexture.height).ToString("N0"));
        }
        
        Debug.Log("Motion Positions: " + motionPositions.Count);
        Debug.Log("Motion Indicators: " + motionIndicators.Count);
        Debug.Log("Block Size: " + blockSize);
        
        // メモリ使用量の概算
        long memoryUsage = System.GC.GetTotalMemory(false) / 1024 / 1024; // MB
        Debug.Log("Estimated Memory Usage: " + memoryUsage + "MB");
        
        Debug.Log("============================");
    }

    void UpdateVFXEffects()
    {
        if (!enableVFX || fireworksVFX == null || mainCamera == null)
            return;

        // FPSが非常に低い場合のみVFXをスキップ（しきい値を下げて反応を良く）
        if (currentFPS < targetFPS * 0.5f)
        {
            if (showDebugInfo && Time.frameCount % 60 == 0)
                Debug.Log("VFX skipped due to very low FPS: " + currentFPS.ToString("F1"));
            return;
        }

        // 1秒間のトリガー回数制限をクリーンアップ
        CleanupRecentTriggers();

        // 連続VFXが無効の場合のみレート制限を適用
        if (!enableContinuousVFX && recentTriggerTimes.Count >= maxTriggersPerSecond)
        {
            if (showDebugInfo && Time.frameCount % 30 == 0)
                Debug.Log("VFX rate limited: " + recentTriggerTimes.Count + "/" + maxTriggersPerSecond + " triggers");
            return;
        }

        // グローバル遅延チェック（連続VFXが有効な場合は遅延を短縮）
        float actualDelay = enableContinuousVFX ? vfxTriggerDelay * 0.3f : vfxTriggerDelay;
        if (Time.time - lastGlobalVFXTrigger < actualDelay)
            return;

        // VFXトリガー時間リストのサイズを調整
        while (lastVFXTriggerTimes.Count < motionPositions.Count)
        {
            lastVFXTriggerTimes.Add(0f);
        }

        // 複数のモーションでVFXを発動（反応性向上）
        List<int> triggeredMotions = new List<int>();
        
        // モーション数が多い場合は処理を制限
        int maxCheckCount = Mathf.Min(motionPositions.Count, currentFPS < targetFPS ? 8 : 15);
        
        for (int i = 0; i < maxCheckCount && i < lastVFXTriggerTimes.Count; i++)
        {
            float intensity = motionIntensities[i];
            
            // 複数の強度レベルでVFXを発動
            bool shouldTrigger = false;
            
            if (enableContinuousVFX)
            {
                // 連続VFXモード：低強度でも反応
                if (intensity > lowIntensityThreshold)
                {
                    shouldTrigger = true;
                }
            }
            else
            {
                // 通常モード：標準強度以上で反応
                if (intensity > vfxIntensityThreshold)
                {
                    shouldTrigger = true;
                }
            }
            
            // 個別モーションの遅延チェック（短縮）
            float timeSinceLastTrigger = Time.time - lastVFXTriggerTimes[i];
            float requiredDelay = intensity > highIntensityThreshold ? actualDelay * 0.5f : actualDelay;
            
            if (shouldTrigger && timeSinceLastTrigger >= requiredDelay)
            {
                triggeredMotions.Add(i);
            }
        }

        // 発動するVFXの数を制限（パフォーマンス考慮）
        int maxSimultaneousVFX = enableContinuousVFX ? 3 : 1;
        int vfxCount = 0;
        
        foreach (int motionIndex in triggeredMotions)
        {
            if (vfxCount >= maxSimultaneousVFX)
                break;
                
            Vector3 worldPosition = ConvertCameraToWorldPosition(motionPositions[motionIndex]);
            TriggerSingleVFX(worldPosition, motionIntensities[motionIndex]);
            
            // トリガー時間を記録
            lastVFXTriggerTimes[motionIndex] = Time.time;
            lastGlobalVFXTrigger = Time.time;
            recentTriggerTimes.Add(Time.time);
            
            vfxCount++;
        }
        
        if (showDebugInfo && triggeredMotions.Count > 0)
        {
            Debug.Log($"VFX triggered: {triggeredMotions.Count} effects " +
                     $"(Mode: {(enableContinuousVFX ? "Continuous" : "Standard")}, " +
                     $"Triggers this second: {recentTriggerTimes.Count}/{maxTriggersPerSecond}, " +
                     $"FPS: {currentFPS:F1})");
        }
    }

    void CleanupRecentTriggers()
    {
        // 1秒以上前のトリガー記録を削除
        for (int i = recentTriggerTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time - recentTriggerTimes[i] > 1.0f)
            {
                recentTriggerTimes.RemoveAt(i);
            }
        }
    }

    Vector3 ConvertCameraToWorldPosition(Vector2 cameraPos)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera is null! Cannot convert to world position.");
            return Vector3.zero;
        }

        // WebCamTextureの座標を正規化（0-1の範囲）
        float normalizedX = cameraPos.x / webCamTexture.width;
        float normalizedY = cameraPos.y / webCamTexture.height;

        // 座標系の反転設定を適用
        if (invertX || mirrorCamera) // カメラが左右反転されている場合も考慮
        {
            normalizedX = 1.0f - normalizedX;
        }
        
        if (invertY || flipVertically) // カメラが上下反転されている場合も考慮
        {
            normalizedY = 1.0f - normalizedY;
        }

        // ビューポート座標を使用してレイを生成
        Vector3 viewportPoint = new Vector3(normalizedX, normalizedY, 0f);
        Ray ray = mainCamera.ViewportPointToRay(viewportPoint);

        // 仮想平面との交点を計算（カメラから指定距離の平面）
        float targetDistance = GetVFXTargetDistance();
        Vector3 worldPosition = ray.origin + ray.direction * targetDistance;

        if (showDebugInfo && debugCoordinateConversion)
        {
            Debug.Log($"=== COORDINATE CONVERSION ===\n" +
                     $"Camera Pos: ({cameraPos.x}, {cameraPos.y})\n" +
                     $"Raw Normalized: ({cameraPos.x / webCamTexture.width:F3}, {cameraPos.y / webCamTexture.height:F3})\n" +
                     $"After Inversion (X:{invertX}, Y:{invertY}): ({normalizedX:F3}, {normalizedY:F3})\n" +
                     $"Viewport Point: ({viewportPoint.x:F3}, {viewportPoint.y:F3}, {viewportPoint.z:F3})\n" +
                     $"World Position: ({worldPosition.x:F2}, {worldPosition.y:F2}, {worldPosition.z:F2})\n" +
                     $"Camera Distance: {Vector3.Distance(mainCamera.transform.position, worldPosition):F2}m");
        }

        return worldPosition;
    }

    float GetVFXTargetDistance()
    {
        // カスタム距離設定を使用する場合
        if (useCustomVFXDistance)
        {
            return vfxTargetDistance;
        }
        
        // カメラのタイプと設定に応じて適切な距離を返す
        if (mainCamera.orthographic)
        {
            // 正射影カメラの場合
            return mainCamera.nearClipPlane + 2f;
        }
        else
        {
            // 透視投影カメラの場合：FOVに基づいて適切な距離を計算
            float fov = mainCamera.fieldOfView;
            float aspectRatio = mainCamera.aspect;
            
            // VFXが見やすい距離を計算（画面の高さがちょうど見える距離の半分程度）
            float distance = (Screen.height * 0.5f) / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
            return Mathf.Clamp(distance, 2f, 10f); // 2m-10mの範囲に制限
        }
    }

    void TriggerSingleVFX(Vector3 position, float intensity)
    {
        if (fireworksVFX == null)
            return;

        // VFX Graphにパラメータを送信
        SendVFXParameters(position, intensity);
        
        // VFXをトリガー（SendEventでトリガー信号を送信）
        TriggerVFXEvent();

        if (showDebugInfo)
        {
            Debug.Log("VFX Graph triggered at World Position: " + position.ToString("F2") + 
                     " with intensity: " + intensity.ToString("F3") + 
                     " (Distance from camera: " + Vector3.Distance(mainCamera.transform.position, position).ToString("F2") + "m)");
        }
    }

    void SendVFXParameters(Vector3 position, float intensity)
    {
        // ワールド座標をVFXに正確に渡す
        Vector3 vfxPosition = position;
        
        // VFX Graphで使用される一般的なパラメータ名でワールド座標を設定
        if (fireworksVFX.HasVector3("TargetPosition"))
        {
            fireworksVFX.SetVector3("TargetPosition", vfxPosition);
        }
        
        if (fireworksVFX.HasVector3("SpawnPosition"))
        {
            fireworksVFX.SetVector3("SpawnPosition", vfxPosition);
        }
        
        if (fireworksVFX.HasVector3("Position"))
        {
            fireworksVFX.SetVector3("Position", vfxPosition);
        }
        
        // 他の一般的なポジション関連パラメータ
        if (fireworksVFX.HasVector3("WorldPosition"))
        {
            fireworksVFX.SetVector3("WorldPosition", vfxPosition);
        }
        
        if (fireworksVFX.HasVector3("EmitPosition"))
        {
            fireworksVFX.SetVector3("EmitPosition", vfxPosition);
        }
        
        // VFX位置にオフセットを適用
        Vector3 finalVFXPosition = vfxPosition + vfxPositionOffset;
        
        // VFX Graphの Transform.position を直接設定
        fireworksVFX.transform.position = finalVFXPosition;
        
        if (fireworksVFX.HasFloat("Intensity"))
        {
            // 強度を0-1の範囲に正規化してから送信
            float normalizedIntensity = Mathf.Clamp01(intensity * 2f);
            fireworksVFX.SetFloat("Intensity", normalizedIntensity);
        }
        
        if (fireworksVFX.HasFloat("Scale"))
        {
            float scale = Mathf.Clamp(intensity * 5f, 0.5f, 3.0f);
            fireworksVFX.SetFloat("Scale", scale);
        }

        if (fireworksVFX.HasInt("BurstCount"))
        {
            int burstCount = Mathf.RoundToInt(intensity * 50f);
            fireworksVFX.SetInt("BurstCount", Mathf.Clamp(burstCount, 10, 100));
        }
        
        // パフォーマンス考慮：FPSが低い場合はパーティクル数を制限
        if (fireworksVFX.HasInt("MaxParticles"))
        {
            int maxParticles = currentFPS < targetFPS ? 50 : 100;
            fireworksVFX.SetInt("MaxParticles", maxParticles);
        }
    }

    void TriggerVFXEvent()
    {
        // VFX Graphのトリガー方法
        // 1. まず標準的なPlay()を使用
        fireworksVFX.Play();
        
        // 2. "OnPlay"イベントがある場合はそれも送信（最も一般的）
        try
        {
            fireworksVFX.SendEvent("OnPlay");
            if (showDebugInfo)
                Debug.Log("VFX Event 'OnPlay' sent successfully");
        }
        catch (System.Exception)
        {
            // OnPlayイベントが存在しない場合は無視
            if (showDebugInfo)
                Debug.Log("VFX triggered with Play() method only");
        }
    }

    void OnDestroy()
    {
        // リソースを解放
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }
        
        if (previousFrame != null)
            DestroyImmediate(previousFrame);
        if (currentFrame != null)
            DestroyImmediate(currentFrame);
    }
}