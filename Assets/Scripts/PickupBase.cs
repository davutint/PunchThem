using UnityEngine;
using System.Collections;

public abstract class PickupBase : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] protected string pickupName = "Pickup";
    [SerializeField] protected float respawnTime = 30f; // 0 = respawn yok
    [SerializeField] protected bool destroyOnPickup = true;
    [SerializeField] protected LayerMask playerLayer = 1 << 0; // Default layer
    
    [Header("Visual Settings")]
    [SerializeField] protected float rotationSpeed = 50f;
    [SerializeField] protected float bobSpeed = 2f;
    [SerializeField] protected float bobHeight = 0.5f;
    [SerializeField] protected bool enableRotation = true;
    [SerializeField] protected bool enableBobbing = true;
    
    [Header("Effects")]
    [SerializeField] protected GameObject pickupEffect;
    [SerializeField] protected AudioClip pickupSound;
    [SerializeField] protected float effectDuration = 1f;
    
    [Header("UI Feedback")]
    [SerializeField] protected bool showPickupText = true;
    [SerializeField] protected float textDisplayTime = 2f;
    [SerializeField] protected Color textColor = Color.white;
    
    // Components
    protected AudioSource audioSource;
    protected Collider pickupCollider;
    protected Renderer[] renderers;
    
    // State
    protected bool isActive = true;
    protected Vector3 startPosition;
    protected float bobOffset = 0f;
    
    protected virtual void Start()
    {
        // Component referansları
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D ses
        }
        
        pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
        {
            // Collider yoksa sphere collider ekle
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 1f;
            pickupCollider = sphere;
        }
        else
        {
            // Trigger olduğundan emin ol
            pickupCollider.isTrigger = true;
        }
        
        renderers = GetComponentsInChildren<Renderer>();
        startPosition = transform.position;
        
        // Random başlangıç offseti
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    protected virtual void Update()
    {
        if (!isActive) return;
        
        // Dönme efekti
        if (enableRotation)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
        
        // Yukarı-aşağı hareket
        if (enableBobbing)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        // Player kontrolü
        if (!IsInLayerMask(other.gameObject.layer, playerLayer)) return;
        
        // Player component kontrolü
        PlayerCombatSystem player = other.GetComponent<PlayerCombatSystem>();
        if (player == null)
            player = other.GetComponentInParent<PlayerCombatSystem>();
            
        if (player != null)
        {
            // Pickup efektini uygula
            ApplyPickupEffect(player);
            
            // Görsel ve ses efektleri
            PlayPickupEffects();
            
            // UI feedback
            if (showPickupText)
                ShowPickupMessage();
            
            // Pickup'ı deaktive et veya yok et
            HandlePostPickup();
        }
    }
    
    /// <summary>
    /// Alt sınıflar tarafından implement edilmeli - Pickup'ın etkisini uygular
    /// </summary>
    protected abstract void ApplyPickupEffect(PlayerCombatSystem player);
    
    /// <summary>
    /// Alt sınıflar tarafından override edilebilir - Pickup mesajını döndürür
    /// </summary>
    protected virtual string GetPickupMessage()
    {
        return $"{pickupName} alındı!";
    }
    
    protected virtual void PlayPickupEffects()
    {
        // Ses efekti
        if (pickupSound != null && audioSource != null)
        {
            // Ses kaynağını parent'tan ayır ki yok olmasın
            audioSource.transform.SetParent(null);
            audioSource.PlayOneShot(pickupSound);
            
            // Ses bittikten sonra yok et
            Destroy(audioSource.gameObject, pickupSound.length + 0.1f);
        }
        
        // Görsel efekt
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }
    
    protected virtual void HandlePostPickup()
    {
        isActive = false;
        
        // Görsel elementleri gizle
        SetRenderersEnabled(false);
        pickupCollider.enabled = false;
        
        if (destroyOnPickup)
        {
            Destroy(gameObject, 0.1f);
        }
        else if (respawnTime > 0f)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }
    
    protected virtual IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);
        
        // Respawn efekti
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Yeniden aktif et
        isActive = true;
        SetRenderersEnabled(true);
        pickupCollider.enabled = true;
        
        // Pozisyonu sıfırla
        transform.position = startPosition;
    }
    
    protected virtual void ShowPickupMessage()
    {
        // Basit bir debug log - Gerçek oyunda UI sistemi ile entegre edilmeli
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{GetPickupMessage()}</color>");
        
        // TODO: Gerçek UI sistemine bağlanmalı
        // Örnek: UIManager.Instance.ShowPickupText(GetPickupMessage(), textColor, textDisplayTime);
    }
    
    protected void SetRenderersEnabled(bool enabled)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }
    
    protected bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
    
    // Gizmos for debugging
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        if (pickupCollider != null)
        {
            if (pickupCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius);
            }
            else if (pickupCollider is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}