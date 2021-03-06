using System;
using Enemy;
using Manger;
using Unity.Mathematics;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour, IDamage
    {
        [Header("Parameters")] public float speed;
        public float jumpForce;
        public bool isHurt;
        [Tooltip("跳跃到空中时的重力")] public float jumpGravityScale;
        public float health;
        public bool isDead;
        public bool god;
        private float _godTime = 2f;

        [Header("GroundCheck")] public LayerMask groundMask;
        public float checkRadius;
        public Transform groundChecker;

        [Header("StatusCheck")] public bool canJump;
        public bool isJump;
        public bool isGround;

        [Header("Component")] private Rigidbody2D _rigidbody2D;

        public Animator animator;

        //悬浮摇杆
        public FloatingJoystick joystick;

        [Header("FX")] public GameObject jumpFX;
        public GameObject landFX;

        [Header("Attack")] public float nextAttackTime;
        public GameObject projectile;
        public float attackCutDown;

        private static readonly int Jump = Animator.StringToHash("jump");
        private static readonly int Speed = Animator.StringToHash("speed");
        private static readonly int VelocityY = Animator.StringToHash("velocityY");
        private static readonly int Ground = Animator.StringToHash("ground");
        private static readonly int GetHit = Animator.StringToHash("GetHit");
        private static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int Resurrection = Animator.StringToHash("Resurrection");

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            //向GameManager注册玩家
            if (GameManager.Instance)
            {
                GameManager.Instance.RegisterPlayer(this);
            }

            joystick = UIManager.Instance.joystick;
            UIManager.Instance.attackButton.onClick.AddListener(AttackByJoystick);
            UIManager.Instance.jumpButton.onClick.AddListener(JumpByJoystick);
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            StatusCheck();
            Attack();
            CheckAnimatorStatus();
            ReduceGodTime();
        }

        private void FixedUpdate()
        {
            if (isDead)
            {
                return;
            }

            CheckGround();

            if (isHurt) return;
            Movement();
            MovementByJoyStick();
            JumpControl();
        }

        #region 移动相关

        private void Movement()
        {
            var axisH = Input.GetAxis("Horizontal"); //获取的键程最大，只有-1与1

            _rigidbody2D.velocity = new Vector2(axisH * speed, _rigidbody2D.velocity.y);

            transform.eulerAngles = axisH switch
            {
                > 0 => Vector3.zero,
                < 0 => Vector3.up * 180,
                _ => transform.eulerAngles
            };
        }

        private void MovementByJoyStick()
        {
            var axisH = joystick.Horizontal; //虚拟摇杆的操控方式
            if (axisH == 0)
            {
                return;
            }

            _rigidbody2D.velocity = new Vector2(axisH * speed, _rigidbody2D.velocity.y);

            transform.eulerAngles = axisH switch
            {
                > 0 => Vector3.zero,
                < 0 => Vector3.up * 180,
                _ => transform.eulerAngles
            };
        }

        /// <summary>
        /// 跳跃的时候将重力增大，可以增加手感
        /// </summary>
        private void JumpControl()
        {
            if (!canJump) return;

            isJump = true;
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, jumpForce);
            _rigidbody2D.gravityScale = jumpGravityScale; //增大重力
            canJump = false;
        }

        private void StatusCheck()
        {
            if (Input.GetButtonDown("Jump") && isGround)
                canJump = true;
        }

        public void JumpByJoystick()
        {
            if (isGround)
                canJump = true;
        }

        private void CheckGround()
        {
            isGround = Physics2D.OverlapCircle(groundChecker.position, checkRadius, groundMask);

            if (isGround)
            {
                _rigidbody2D.gravityScale = 1;
                isJump = false;
                return;
            }

            _rigidbody2D.gravityScale = 4;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(groundChecker.position, checkRadius);
        }

        #endregion

        #region 攻击相关

        private void Attack()
        {
            if (!(Time.time >= nextAttackTime && Input.GetButtonDown("Fire1"))) return;
            Instantiate(projectile, transform.position, transform.rotation);
            nextAttackTime = Time.time + attackCutDown;
        }

        public void AttackByJoystick()
        {
            if (!(Time.time >= nextAttackTime)) return;
            Instantiate(projectile, transform.position, transform.rotation);
            nextAttackTime = Time.time + attackCutDown;
        }

        #endregion

        #region 动画事件

        public void DisableHurt()
        {
            isHurt = false;
        }

        public void ShowJumpFX()
        {
            jumpFX.SetActive(true);
            jumpFX.transform.position = transform.position + new Vector3(0.15f, -0.5f, 0);
        }

        public void ShowLandFX()
        {
            landFX.SetActive(true);
            // landFX.transform.position = transform.position + new Vector3(0, -0.75f, 0);
            landFX.transform.position = transform.position + Vector3.up * -0.75f;
        }

        public void ShowRunFX()
        {
            if (animator.GetCurrentAnimatorStateInfo(1).IsTag("HitLayer"))
            {
                Debug.Log("不需要奔跑");
                return;
            }

            var runFx = ObjectPools.Instance.GetRunFXObject();
            // runFx.transform.parent = transform.parent;
            runFx.transform.eulerAngles = transform.eulerAngles;

            runFx.transform.position = runFx.transform.localEulerAngles.y switch
            {
                <180 => transform.position + new Vector3(-0.5f, -0.75f, 0),
                >=180 => transform.position + new Vector3(0.6f, -0.75f, 0),
                _ => runFx.transform.position
            };
            runFx.SetActive(true);
        }

        #endregion

        #region 动画控制

        private void CheckAnimatorStatus()
        {
            animator.SetBool(Ground, isGround);
            animator.SetFloat(VelocityY, _rigidbody2D.velocity.y);
            animator.SetBool(Jump, isJump);
            animator.SetFloat(Speed, Mathf.Abs(_rigidbody2D.velocity.x));
        }

        #endregion

        #region 受伤或死亡

        public void ReduceGodTime()
        {
            if (!god) return;
            _godTime -= Time.deltaTime;
            if (!(_godTime <= 0)) return;
            _godTime = 2f;
            god = false;
            animator.SetBool(Resurrection, false);
        }

        public void GetDamage(float damage)
        {
            //受伤的过程中是无敌的
            if (isHurt || god)
            {
                return;
            }

            health = Mathf.Max(health - damage, 0);

            UIManager.Instance.UpdateHealth(health);

            if (health == 0)
            {
                Debug.Log("角色死亡");
                isDead = true;
                animator.SetTrigger(Dead);
                //防止死后玩家乱飞
                _rigidbody2D.velocity = Vector2.zero;
                GameManager.Instance.GameOver();
                return;
            }

            animator.SetTrigger(GetHit);
            isHurt = true;
        }

        #endregion
    }
}