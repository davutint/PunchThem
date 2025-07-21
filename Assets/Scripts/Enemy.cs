using FIMSpace.FProceduralAnimation;
using UnityEngine;

public abstract class Enemy:MonoBehaviour
{
    protected LayerMask DetectHitsOn = 0 << 0;
    
    protected SkinnedMeshRenderer Skin;
    protected float FallImpactPower = 1f;
    protected float FallImpactDuration = 0.1f;
    protected float DamageAtVelocity = 4f;
    internal float lastImpulse = 0f;

    private int HP = 3;

    /// <summary> For Collisions Culldown </summary>
    protected float hitTime = 0f;
    protected int Health;

    public abstract void EnemyTakeDamage(int damage);


}
