using UnityEngine;
using UnityEngine.UI;
using FIMSpace.FProceduralAnimation;
using System.Collections;

public class EnemyHealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float damageMultiplier = 1f; // Hasar çarpanı

    

    [Header("Fall Settings")]
    [SerializeField] private float fallRecoveryTime = 3f; // Düştükten sonra kalkma süresi
    [SerializeField] private bool canRecoverFromFall = true;
    [SerializeField] private float fallDamageThreshold = 30f; // Bu güçten sonra düşer

    [Header("Visual Feedback")]
    [SerializeField] private Slider healthBarUI;
    [SerializeField] private GameObject healthBarCanvas;
    [SerializeField] private float healthBarDisplayTime = 3f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private AudioSource hitAudio;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip deathSound;

    [Header("References")]
    [SerializeField] private Animator animator;

    // Private variables
    private bool isDead = false;
    private bool isFallen = false;
    private float lastDamageTime = 0f;
    private Coroutine healthBarCoroutine;
    private RagdollHandler ragdollHandler;
    // Animation hashes
    private int hitHash = Animator.StringToHash("Hit");
    private int deathHash = Animator.StringToHash("Death");

    private void Start()
    {
        currentHealth = maxHealth;
      

       
        RagdollAnimator2 rag = GetComponent<RagdollAnimator2>();
        ragdollHandler = rag.GetRagdollHandler;
      

        if (healthBarCanvas != null)
            healthBarCanvas.SetActive(false);


        Debug.Log("[EnemyHealthSystem] Start – Enemy initialized.");
    }

  
    public void TakeDamage(float incomingPower, bool canCauseFall, Vector3 impactDirection, bool isUppercut)
    {
        if (isDead) return;


        float actualDamage = incomingPower * damageMultiplier;

        currentHealth = Mathf.Clamp(currentHealth - actualDamage, 0f, maxHealth);
        lastDamageTime = Time.time;

    

        Debug.Log($"[EnemyHealthSystem] TakeDamage – in: {incomingPower}, dmg: {actualDamage:F2}, hp: {currentHealth:F2}");

        ShowDamageEffects(impactDirection);
        //UpdateVisuals();
        //ShowHealthBar();
        //PlayHitSound();

        if (animator != null && !isFallen)
            animator.SetTrigger(hitHash);

        bool shouldFall = canCauseFall && (incomingPower >= fallDamageThreshold);

        if (shouldFall && !isFallen && ragdollHandler != null)
        {
            ApplyRagdollFall(impactDirection, incomingPower, isUppercut);
        }

        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    private void ApplyRagdollFall(Vector3 impactDirection, float power, bool isUppercut)
    {
        isFallen = true;

        WaveManager.instance.OnEnemyDeath();
        Debug.Log($"[EnemyHealthSystem] ApplyRagdollFall – power: {power}");

        ragdollHandler.User_SwitchFallState(RagdollHandler.EAnimatingMode.Falling);

        Vector3 finalDirection = isUppercut
            ? Vector3.Lerp(impactDirection, Vector3.up, 0.7f).normalized
            : impactDirection;

        float impactPower = power * 0.5f;
        ragdollHandler.User_AddAllBonesImpact(finalDirection * impactPower, 0.1f, ForceMode.Impulse);

        Rigidbody nearestBone = ragdollHandler.User_GetNearestRagdollRigidbodyToPosition(
            transform.position + Vector3.up * 1.5f,
            true,
            ERagdollChainType.Core
        );

        if (nearestBone != null)
        {
            ragdollHandler.User_AddRigidbodyImpact(
                nearestBone,
                finalDirection * (impactPower * 1.5f),
                0.05f,
                ForceMode.Impulse
            );
        }
        Destroy(gameObject,5);
       /* if (canRecoverFromFall && !isDead) Tekrar ayağa kalkmamalı düzgünce vurduğumuzda
        {
            StartCoroutine(RecoverFromFall());
        }*/
    }

    private IEnumerator RecoverFromFall()
    {
        Debug.Log("[EnemyHealthSystem] RecoverFromFall – start.");
        yield return new WaitForSeconds(fallRecoveryTime);

        if (!isDead)
        {
            ragdollHandler.User_SwitchFallState(RagdollHandler.EAnimatingMode.Standing);
            isFallen = false;
           

            Debug.Log("[EnemyHealthSystem] RecoverFromFall – completed.");
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[EnemyHealthSystem] Die.");

        if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position + Vector3.up;
            deathParticles.Play();
        }

        if (hitAudio != null && deathSound != null)
        {
            hitAudio.clip = deathSound;
            hitAudio.Play();
        }

        /*if (animator != null)
            animator.SetTrigger(deathHash);*/

        if (ragdollHandler != null && !isFallen)
        {
            ragdollHandler.User_SwitchFallState(RagdollHandler.EAnimatingMode.Falling);
            ragdollHandler.User_SetAllKinematic(false);
        }

        if (healthBarCanvas != null)
            healthBarCanvas.SetActive(false);

       
    }

    private void DestroyEnemy()
    {
        Debug.Log("[EnemyHealthSystem] DestroyEnemy – object destroyed.");
        Destroy(gameObject);
    }

    private void ShowDamageEffects(Vector3 impactDirection)
    {
        if (hitParticles != null)
        {
            hitParticles.transform.position = transform.position + Vector3.up * 1.5f;
            hitParticles.transform.rotation = Quaternion.LookRotation(-impactDirection);
            hitParticles.Play();
        }

      
    }

    
   

    private void ShowHealthBar()
    {
        if (healthBarCanvas == null) return;

        if (healthBarCoroutine != null)
            StopCoroutine(healthBarCoroutine);

        healthBarCanvas.SetActive(true);
        healthBarCoroutine = StartCoroutine(HideHealthBarAfterDelay());
    }

    private IEnumerator HideHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDisplayTime);

        if (healthBarCanvas != null && !isDead)
            healthBarCanvas.SetActive(false);
    }

    private void PlayHitSound()
    {
        if (hitAudio == null || hitSounds == null || hitSounds.Length == 0) return;

        AudioClip randomClip = hitSounds[Random.Range(0, hitSounds.Length)];
        hitAudio.pitch = Random.Range(0.9f, 1.1f);
        hitAudio.PlayOneShot(randomClip);
    }

    // Public getter methods
    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsAlive() => !isDead;
    public bool IsFallen() => isFallen;
}
