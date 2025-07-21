using System.Collections;
using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using FIMSpace.RagdollAnimatorDemo;
using UnityEngine;
[DefaultExecutionOrder( 10 )]
public class Player:MonoBehaviour
{
    public float damage;
    //[AddComponentMenu( "FImpossible Creations/Demos/Fimpossible Demo Hero 1" )]
    
        public FBasic_RigidbodyMover Mover;
        public Animator Mecanim;
        public RagdollAnimator2 Ragdoll;

        public LayerMask HittableLayermask;

        [Space( 6 )]
        public float PunchPower = 10f;

        public float UppercutPower = 10f;
        public float PushForcePower = 50f;
   
        public Transform Hand;
        public AudioSource HitAudio;

        [Header( "References" )]
        public RA2MagnetPoint CatchMagnet;

        public RA2MagnetPoint GripMagnet;

        [Header( "Input" )]
        public KeyCode PunchKey = KeyCode.None;
        public KeyCode PunchUppercutKey = KeyCode.None;
      

        private int actionHash = Animator.StringToHash( "Action" );
        private bool InAction => Mecanim.GetBool( actionHash );
        private bool Action
        { get { return Mecanim.GetBool( actionHash ); } set { Mecanim.SetBool( actionHash, value ); } }

        private List<Collider> toIgnore = new List<Collider>();

        private void Start()
        {
            Collider[] myCol = Mover.GetComponentsInChildren<Collider>();
            foreach( var col in myCol ) toIgnore.Add( col );
            if( Ragdoll ) foreach( var col in Ragdoll.Settings.User_GetAllDummyColliders() ) toIgnore.Add( col );

            if( GripMagnet ) GripMagnet.transform.SetParent( null );
        }

        private void LateUpdate()
        {
            Vector2 moveDirectionLocal = Vector2.zero;

            if( InAction == false )
            {
                if( Input.GetKey( KeyCode.A ) || Input.GetKey( KeyCode.LeftArrow ) ) moveDirectionLocal += Vector2.left;
                else if( Input.GetKey( KeyCode.D ) || Input.GetKey( KeyCode.RightArrow ) ) moveDirectionLocal += Vector2.right;

                if( Input.GetKey( KeyCode.W ) || Input.GetKey( KeyCode.UpArrow ) ) moveDirectionLocal += Vector2.up;
                else if( Input.GetKey( KeyCode.S ) || Input.GetKey( KeyCode.DownArrow ) ) moveDirectionLocal += Vector2.down;

                if( Input.GetKeyDown( PunchKey ) ) { StartCharge( PunchKey ); }
                else if( Input.GetKeyDown( PunchUppercutKey ) ) { StartCharge( PunchUppercutKey ); }
                else if( Input.GetKeyDown( KeyCode.Space ) ) DoJump();
            }
            else
            {
                if( chargeKey != KeyCode.None )
                {
                    if( Input.GetKeyUp( chargeKey ) )
                    {
                        float offset = Mathf.Clamp( chargeAmount, 0f, 0.125f );

                        if( chargeKey == PunchKey ) DoPunchF( offset );
                        else if( chargeKey == PunchUppercutKey ) DoPunchU( offset );
                        chargeKey = KeyCode.None;
                    }
                    else
                    {
                        rotated += Time.deltaTime * ( 110f + Mathf.Clamp( chargeAmount * 75f, 0f, 100f ) ) * 10f;
                        chargeAmount += Time.deltaTime;
                        chargedScale = 1f + ( Mathf.Clamp( chargeAmount * 0.5f, 0f, 0.8f ) );
                    }
                }
            }

            if( chargeKey == KeyCode.None )
            {
                chargedScale = Mathf.Lerp( chargedScale, 1f, Time.deltaTime * 4f );
            }

            if( Hand ) Hand.localScale = new Vector3( chargedScale, chargedScale, chargedScale );
            //if( UpperArm ) if( chargeKey != KeyCode.None ) UpperArm.rotation = Quaternion.AngleAxis( rotated * Mathf.Clamp01( chargeAmount * 2f ), Mover.transform.forward ) * ( UpperArm.parent.rotation * UpperArm.localRotation );

           
            Mover.moveDirectionLocal = moveDirectionLocal;
        }

        private void StartCharge( KeyCode key )
        {
            Action = true;
            chargeKey = key;
            chargeAmount = -.2f;
            rotated = 0;
            PlayClip( "Punch Charge" );
        }

        private KeyCode chargeKey = KeyCode.None;
        private float chargedScale = 1f;
        private float chargeAmount = -1f;
        private float rotated = 0f;

        // Hero Actions -------------------------

        public void DoPunchF( float timeOffset = 0f )
        {
            PlayClip( "Punch F", timeOffset );
        }

        public void DoPunchU( float timeOffset = 0f )
        {
            PlayClip( "Punch U", timeOffset );
        }

        private RagdollHandler gripped = null;

        public void DoJump()
        {
            if( Mover.isGrounded == false ) return;
            Mover.jumpRequest = Mover.JumpPower;
        }

        // Holding Up

        private RagdollHandler isHoldingUp = null;
        private bool updateUpperBodyLayer = false;

        private float _sd_layer = 0f;

      
        // Utilities ---------------------

        public void PlayClip( string state, float timeOffset = 0f )
        {
            Mecanim.CrossFadeInFixedTime( state, 0.145f, 0, timeOffset );
        }

        // Animation Events -------------------

       
        public void EPunchForward()
        {
            CastCloseBox( 1f, 0.3f, 0.25f, 1.1f );
            RagdollHandler rag = FindRagdollIn( close, closeCount );

            if( rag != null )
            {
                if( HitAudio ) HitAudio.Play();

                Vector3 impactDirection = transform.forward + new Vector3( 0f, 0.33f, 0f );
                var rigidbody = rag.User_GetNearestRagdollRigidbodyToPosition( transform.TransformPoint( new Vector3( 0f, 1.45f, 0.2f ) ), true, ERagdollChainType.Core );
                if( rigidbody == null ) return;

                rag.User_SwitchFallState();
                float chargeMul = 1f + chargeAmount * 0.4f;
                rag.User_AddAllBonesImpact( impactDirection * ( PunchPower * 0.5f * chargeMul ), 0.05f, ForceMode.Impulse );
                rag.User_AddRigidbodyImpact( rigidbody, impactDirection * ( PunchPower * 1.5f * chargeMul ), 0.0f, ForceMode.Impulse );
                Debug.Log("chargeAmount :"+chargeAmount);
                Debug.Log("chargeMul :"+chargeMul);
            }
        }

        public void EPunchUp()
        {
            CastCloseBox( 1f, 0.05f, 0.25f, .9f );
            RagdollHandler rag = FindRagdollIn( close, closeCount );

            if( rag != null )
            {
                if( HitAudio ) HitAudio.Play();

                Vector3 impactDirection = Vector3.up;
                var rigidbody = rag.User_GetNearestRagdollRigidbodyToPosition( transform.TransformPoint( new Vector3( 0f, 1.45f, 0.2f ) ), true, ERagdollChainType.Core );

                if( rigidbody == null ) return;

                rag.User_SwitchFallState();

                float chargeMul = 1f + chargeAmount * 0.3f;
                rag.User_AddAllBonesImpact( impactDirection * ( UppercutPower * 0.55f * chargeMul ), 0f, ForceMode.VelocityChange );
                rag.User_AddRigidbodyImpact( rigidbody, impactDirection * ( UppercutPower * 2.1f * chargeMul ), 0f, ForceMode.Impulse, 0.05f );
                Debug.Log("chargeAmount :"+chargeAmount);
                Debug.Log("chargeMul :"+chargeMul);
            }
        }

        private int surroundCount = 0;
        private Collider[] far = new Collider[32];
        private int farCount = 0;
        private Collider[] mid = new Collider[32];
        private int midCount = 0;
        private Collider[] close = new Collider[16];
        private int closeCount = 0;

       
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
    
      
    }


