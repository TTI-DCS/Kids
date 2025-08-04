using UnityEngine;
using UnityEngine.VFX;

public class InteractiveObject : MonoBehaviour
{
    [Header("Interactive Settings")]
    public string objectName = "Planet";           // オブジェクト名
    public bool isInteractive = true;              // インタラクティブかどうか
    public float glowIntensity = 1.0f;            // 光る強度
    public Color glowColor = Color.white;          // 光る色
    public AudioClip touchSound;                   // タッチ時の音
    public AudioClip releaseSound;                 // リリース時の音
    
    [Header("VFX Settings")]
    public VisualEffect vfxEffect;                 // VFX Graphエフェクト
    public string vfxTriggerParameter = "Trigger"; // VFX Graphのトリガーパラメータ
    public float vfxDuration = 2.0f;              // VFXの持続時間
    
    [Header("Animation Settings")]
    public bool useAnimation = true;               // アニメーションを使用するか
    public Animator animator;                      // アニメーター
    public string touchAnimationTrigger = "Touch"; // タッチ時のアニメーション
    public string releaseAnimationTrigger = "Release"; // リリース時のアニメーション
    
    private Renderer objectRenderer;               // レンダラー
    private Material originalMaterial;             // 元のマテリアル
    private Material glowMaterial;                 // 光るマテリアル
    private AudioSource audioSource;               // オーディオソース
    private bool isTouched = false;                // タッチされているか
    private float touchStartTime;                  // タッチ開始時間
    
    void Start()
    {
        // コンポーネントを取得
        objectRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        // オーディオソースがない場合は追加
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 元のマテリアルを保存
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            
            // 光るマテリアルを作成
            CreateGlowMaterial();
        }
        
        // VFX Graphがない場合は自動で探す
        if (vfxEffect == null)
        {
            vfxEffect = GetComponent<VisualEffect>();
        }
        
        // インタラクティブタグを設定
        if (isInteractive)
        {
            gameObject.tag = "Interactive";
        }
        
        Debug.Log($"Interactive Object '{objectName}' initialized");
    }
    
    void CreateGlowMaterial()
    {
        // 光るマテリアルを作成
        glowMaterial = new Material(originalMaterial);
        glowMaterial.EnableKeyword("_EMISSION");
        glowMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
    }
    
    public void OnTouch()
    {
        if (!isInteractive || isTouched) return;
        
        isTouched = true;
        touchStartTime = Time.time;
        
        Debug.Log($"'{objectName}' touched!");
        
        // マテリアルを光らせる
        if (objectRenderer != null && glowMaterial != null)
        {
            objectRenderer.material = glowMaterial;
        }
        
        // 音を再生
        if (audioSource != null && touchSound != null)
        {
            audioSource.clip = touchSound;
            audioSource.Play();
        }
        
        // VFX Graphエフェクトを開始
        if (vfxEffect != null)
        {
            vfxEffect.SetBool(vfxTriggerParameter, true);
        }
        
        // アニメーションを開始
        if (useAnimation && animator != null)
        {
            animator.SetTrigger(touchAnimationTrigger);
        }
        
        // カスタムイベントを呼び出し
        OnCustomTouch();
    }
    
    public void OnRelease()
    {
        if (!isInteractive || !isTouched) return;
        
        isTouched = false;
        
        Debug.Log($"'{objectName}' released!");
        
        // マテリアルを元に戻す
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
        
        // 音を再生
        if (audioSource != null && releaseSound != null)
        {
            audioSource.clip = releaseSound;
            audioSource.Play();
        }
        
        // VFX Graphエフェクトを停止
        if (vfxEffect != null)
        {
            vfxEffect.SetBool(vfxTriggerParameter, false);
        }
        
        // アニメーションを開始
        if (useAnimation && animator != null)
        {
            animator.SetTrigger(releaseAnimationTrigger);
        }
        
        // カスタムイベントを呼び出し
        OnCustomRelease();
    }
    
    // カスタムタッチイベント（継承先でオーバーライド可能）
    protected virtual void OnCustomTouch()
    {
        // デフォルトの実装
        // 惑星の場合は回転速度を上げるなどの効果を追加
        StartCoroutine(PlanetTouchEffect());
    }
    
    // カスタムリリースイベント（継承先でオーバーライド可能）
    protected virtual void OnCustomRelease()
    {
        // デフォルトの実装
        // 惑星の回転速度を元に戻すなどの効果を追加
        StartCoroutine(PlanetReleaseEffect());
    }
    
    System.Collections.IEnumerator PlanetTouchEffect()
    {
        // 惑星の回転速度を上げる効果
        Transform planetTransform = transform;
        Vector3 originalRotation = planetTransform.eulerAngles;
        float originalRotationSpeed = 10f; // 元の回転速度
        float enhancedRotationSpeed = 50f; // 強化された回転速度
        
        float elapsed = 0f;
        float duration = 1.0f;
        
        while (elapsed < duration && isTouched)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 回転速度を徐々に上げる
            float currentSpeed = Mathf.Lerp(originalRotationSpeed, enhancedRotationSpeed, t);
            planetTransform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
            
            yield return null;
        }
    }
    
    System.Collections.IEnumerator PlanetReleaseEffect()
    {
        // 惑星の回転速度を元に戻す効果
        Transform planetTransform = transform;
        float enhancedRotationSpeed = 50f;
        float originalRotationSpeed = 10f;
        
        float elapsed = 0f;
        float duration = 1.0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 回転速度を徐々に下げる
            float currentSpeed = Mathf.Lerp(enhancedRotationSpeed, originalRotationSpeed, t);
            planetTransform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
            
            yield return null;
        }
    }
    
    // パブリックメソッドで外部から状態を確認
    public bool IsTouched()
    {
        return isTouched;
    }
    
    public float GetTouchDuration()
    {
        if (isTouched)
        {
            return Time.time - touchStartTime;
        }
        return 0f;
    }
    
    // エディタ用のデバッグ表示
    void OnDrawGizmosSelected()
    {
        if (isInteractive)
        {
            Gizmos.color = isTouched ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
} 