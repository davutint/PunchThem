using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;

public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private KeyCode punchKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode uppercutKey = KeyCode.Mouse1;
    [SerializeField] private float normalPunchPower = 10f;
    [SerializeField] private float normalUppercutPower = 15f;
    [SerializeField] private float maxChargePower = 50f;
    [SerializeField] private float chargeTime = 2f; // Full charge süresi
    [SerializeField] private float fallThreshold = 30f; // Bu güçten sonra düşman düşer

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 2f;
    public LayerMask HittableLayermask;
    [SerializeField] private bool useRaycast = true; // false ise CastCloseBox kullanır
    [SerializeField] private Vector3 boxCastSize = new Vector3(0.5f, 0.5f, 1f);

    [Header("Player Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Power Boost")]
    [SerializeField] private float chargeSpeedMultiplier = 1f; // Pickup'lardan etkilenecek
    private float boostEndTime = 0f;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider powerSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image powerFillImage;
    [SerializeField] private Gradient healthGradient;
    [SerializeField] private Gradient powerGradient;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackOrigin; // Raycast/Box başlangıç noktası
    [SerializeField] private AudioSource hitAudio;
    [SerializeField] private ParticleSystem chargeParticles;

    // Private variables
    private float currentChargePower = 0f;
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private KeyCode currentChargeKey = KeyCode.None;

    // Animation hashes
    private int actionHash = Animator.StringToHash("Action");

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        UpdatePowerUI(0f);

        if (attackOrigin == null && Camera.main != null)
            attackOrigin = Camera.main.transform;

        Debug.Log("[PlayerCombatSystem] Start – Health initialized.");
    }

    private void Update()
    {
        HandleCombatInput();
        UpdateChargeBoost();

        if (isCharging)
        {
            powerSlider.gameObject.SetActive(true);
            UpdateCharge();
        }
        else
        {       
            powerSlider.gameObject.SetActive(false);

        }
    }

    private void HandleCombatInput()
    {
        // Punch başlat
        if (Input.GetKeyDown(punchKey) && !isCharging)
        {
            Debug.Log("[PlayerCombatSystem] Punch key down.");
            StartCharge(punchKey);
        }
        else if (Input.GetKeyUp(punchKey) && isCharging && currentChargeKey == punchKey)
        {
            Debug.Log("[PlayerCombatSystem] Punch key up – execute punch.");
            ExecutePunch(false);
        }

        // Uppercut başlat
        if (Input.GetKeyDown(uppercutKey) && !isCharging)
        {
            Debug.Log("[PlayerCombatSystem] Uppercut key down.");
            StartCharge(uppercutKey);
        }
        else if (Input.GetKeyUp(uppercutKey) && isCharging && currentChargeKey == uppercutKey)
        {
            Debug.Log("[PlayerCombatSystem] Uppercut key up – execute uppercut.");
            ExecutePunch(true);
        }
    }

    private void StartCharge(KeyCode key)
    {
        isCharging = true;
        currentChargeKey = key;
        chargeStartTime = Time.time;
        currentChargePower = 0f;

        Debug.Log($"[PlayerCombatSystem] StartCharge – key: {key}");

        if (animator != null)
        {
            animator.SetBool(actionHash, true);
            animator.CrossFadeInFixedTime("Punch Charge", 0.145f);
        }

        if (chargeParticles != null)
            chargeParticles.Play();
    }

    private void UpdateCharge()
    {
        float chargeProgress = (Time.time - chargeStartTime) / (chargeTime / GetCurrentChargeSpeed());
        chargeProgress = Mathf.Clamp01(chargeProgress);

        currentChargePower = Mathf.Lerp(0f, maxChargePower, chargeProgress);
        UpdatePowerUI(chargeProgress);

        // DEBUG (aşırı log istemiyorsanız yoruma alabilirsiniz)
         Debug.Log($"[PlayerCombatSystem] Charging... Power: {currentChargePower:F2}");

        // Charge particle efekti güncelle
        if (chargeParticles != null)
        {
            var main = chargeParticles.main;
            main.startSpeed = 2f + chargeProgress * 3f;
        }
    }

    private void ExecutePunch(bool isUppercut)
    {
        isCharging = false;
        currentChargeKey = KeyCode.None;

        if (animator != null)
        {
            animator.SetBool(actionHash, false);

            if (isUppercut)
                animator.CrossFadeInFixedTime("Punch U", 0.145f, 0, 0f);
            else
                animator.CrossFadeInFixedTime("Punch F", 0.145f, 0, 0f);
        }

        if (chargeParticles != null)
            chargeParticles.Stop();

       
    }
    
    public RagdollAnimator2 Ragdoll;
    private int surroundCount = 0;
    private Collider[] far = new Collider[32];
    private int farCount = 0;
    private Collider[] mid = new Collider[32];
    private int midCount = 0;
    private Collider[] close = new Collider[16];
    private int closeCount = 0;
    private List<Collider> toIgnore = new List<Collider>();
    private void CastCloseBox( float y = 1f, float width = 0.05f, float height = 0.25f, float zScale = 1f )
    {
        Vector3 closeRange = transform.TransformPoint( new Vector3( 0f, y, 0.5f * zScale ) );
        closeCount = Mathf.Min( close.Length - 1, Physics.OverlapBoxNonAlloc( closeRange, new Vector3( width, height, zScale ), close, transform.rotation, HittableLayermask ) );
    }
    private RagdollHandler FindRagdollIn( Collider[] c, int length )
    {
        for( int i = 0; i < length; i++ )
        {
            if( c[i] == null ) continue;
            if( toIgnore.Contains( c[i] ) ) continue;

            RagdollAnimator2BoneIndicator ind = c[i].gameObject.GetComponent<RagdollAnimator2BoneIndicator>();

            if( ind )
            {
                if( Ragdoll )
                {
                    if( ind.ParentHandler == Ragdoll.Settings ) continue;
                    return ind.ParentHandler;
                }
                else return ind.ParentHandler;
            }
        }

        return null;
    }
    private RagdollHandler DetectTarget()
    {
        if (useRaycast)
        {
            RaycastHit hit;
            Ray ray = new Ray(attackOrigin.position, attackOrigin.forward);

            if (Physics.Raycast(ray, out hit, detectionRange, HittableLayermask))
            {
                var indicator = hit.collider.GetComponent<RagdollAnimator2BoneIndicator>();
                if (indicator != null)
                {
                    Debug.Log("[PlayerCombatSystem] Raycast hit indicator target.");
                    return indicator.ParentHandler;
                }

                var handler = hit.collider.GetComponentInParent<RagdollHandler>();
                if (handler != null)
                {
                    Debug.Log("[PlayerCombatSystem] Raycast hit handler target.");
                    return handler;
                }
            }
        }
        else
        {
            Vector3 boxCenter = attackOrigin.position + attackOrigin.forward * (detectionRange * 0.5f);
            Collider[] hits = Physics.OverlapBox(boxCenter, boxCastSize, attackOrigin.rotation, HittableLayermask);

            foreach (var hit in hits)
            {
                var indicator = hit.GetComponent<RagdollAnimator2BoneIndicator>();
                if (indicator != null)
                {
                    Debug.Log("[PlayerCombatSystem] BoxCast hit indicator target.");
                    return indicator.ParentHandler;
                }

                var handler = hit.GetComponentInParent<RagdollHandler>();
                if (handler != null)
                {
                    Debug.Log("[PlayerCombatSystem] BoxCast hit handler target.");
                    return handler;
                }
            }
        }

        Debug.Log("[PlayerCombatSystem] No target detected.");
        return null;
    }

    private void ApplyDamageToTarget(RagdollHandler target, bool isUppercut)
    {
        Debug.Log("[PlayerCombatSystem] ApplyDamageToTarget invoked.");

        float basePower = isUppercut ? normalUppercutPower : normalPunchPower;
        float totalPower = basePower + currentChargePower;
        EnemyHealthSystem enemyHealth = target.BaseTransform.GetComponent<EnemyHealthSystem>();
        if (enemyHealth != null)
        {
            Debug.Log("[PlayerCombatSystem] Enemy found – applying damage.");
            bool shouldFall = totalPower >= fallThreshold;
            enemyHealth.TakeDamage(totalPower, shouldFall, transform.forward, isUppercut);
        }
        else
        {
            Debug.Log("Enemy NOT FOUND!");
        }

        if (hitAudio != null)
            hitAudio.Play();
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateHealthUI();

        Debug.Log($"[PlayerCombatSystem] TakeDamage – damage: {damage}, currentHealth: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Debug.Log("[PlayerCombatSystem] Player died!");
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateHealthUI();

        Debug.Log($"[PlayerCombatSystem] Heal – amount: {amount}, currentHealth: {currentHealth}");
    }

    public void ApplyPowerBoost(float multiplier, float duration)
    {
        chargeSpeedMultiplier = multiplier;
        boostEndTime = Time.time + duration;

        Debug.Log($"[PlayerCombatSystem] Power boost applied – x{multiplier} for {duration} sec.");

        StartCoroutine(ShowBoostEffect());
    }

    private void UpdateChargeBoost()
    {
        if (Time.time > boostEndTime && chargeSpeedMultiplier > 1f)
        {
            chargeSpeedMultiplier = 1f;
            Debug.Log("[PlayerCombatSystem] Power boost ended.");
        }
    }

    private float GetCurrentChargeSpeed()
    {
        return chargeSpeedMultiplier;
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthSlider.value = healthPercent;

            if (healthFillImage != null && healthGradient != null)
                healthFillImage.color = healthGradient.Evaluate(healthPercent);
        }
    }

    private void UpdatePowerUI(float chargePercent)
    {
        if (powerSlider != null)
        {
            powerSlider.value = chargePercent;

            if (powerFillImage != null && powerGradient != null)
                powerFillImage.color = powerGradient.Evaluate(chargePercent);
        }
    }

    private IEnumerator ShowBoostEffect()
    {
        if (powerFillImage != null)
        {
            Color originalColor = powerFillImage.color;
            float timer = 0f;

            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                float intensity = Mathf.PingPong(timer * 4f, 1f);
                powerFillImage.color = Color.Lerp(originalColor, Color.yellow, intensity);
                yield return null;
            }

            powerFillImage.color = originalColor;
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;

        Gizmos.color = Color.red;

        if (useRaycast)
        {
            Gizmos.DrawRay(attackOrigin.position, attackOrigin.forward * detectionRange);
        }
        else
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                attackOrigin.position + attackOrigin.forward * (detectionRange * 0.5f),
                attackOrigin.rotation,
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, boxCastSize * 2f);
            Gizmos.matrix = oldMatrix;
        }
    }

    public void EPunchForward()
    {
        CastCloseBox( 1f, 0.05f, 0.25f, .9f );
        RagdollHandler target = FindRagdollIn( close, closeCount );
      
        if (target != null)
        {
            ApplyDamageToTarget(target, false);
        }

        UpdatePowerUI(0f);
        currentChargePower = 0f;
    }
    public void EPunchUp()
    {
        CastCloseBox( 1f, 0.05f, 0.25f, .9f );
        RagdollHandler target = FindRagdollIn( close, closeCount );
        if (target != null)
        {
            ApplyDamageToTarget(target, true);
        }

        UpdatePowerUI(0f);
        currentChargePower = 0f;
    }
}
