using FIMSpace.FProceduralAnimation;
using Unity;
using UnityEngine;
public class Skeleton : Enemy ,IRagdollAnimator2Receiver
{
    
    
    public void RagdollAnimator2_OnCollisionEnterEvent( RA2BoneCollisionHandler hitted, Collision mainCollision )
    {
        if( Time.fixedTime - hitTime < 0.25f ) return;
        if( RagdollHandlerUtilities.LayerMaskContains( DetectHitsOn, mainCollision.collider.gameObject.layer ) == false ) return;

        float hitImpulsePower = mainCollision.impulse.magnitude;
        lastImpulse = hitImpulsePower;

        if( hitImpulsePower < DamageAtVelocity ) return;

        hitTime = Time.fixedTime;
       Player Player=mainCollision.gameObject.GetComponent<Player>();
       EnemyTakeDamage((int)Player.damage);
        
        if( Health == 0 )
        {
            var ragdollHandler = hitted.ParentHandler;
            ragdollHandler.User_SwitchFallState( RagdollHandler.EAnimatingMode.Falling );

            Vector3 impactDirection = mainCollision.relativeVelocity.normalized;

            // Push whole ragdoll with some force
            ragdollHandler.User_AddAllBonesImpact( impactDirection * FallImpactPower, FallImpactDuration, ForceMode.Acceleration );

            // Empathise hitted limb with impact
            ragdollHandler.User_AddRigidbodyImpact( hitted.DummyBoneRigidbody, impactDirection * FallImpactPower, FallImpactDuration, ForceMode.VelocityChange );
        }
    }

    public override void EnemyTakeDamage(int damage)
    {
        Health-= damage;
       
    }
}
