using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy.AllEnemy
{
    public class Enemy : MonoBehaviour, IDamage
    {
        [Header("Parameters")] public float health;
        public bool isDead;

        [Header("Movement")] public float speed;
        public Transform pointA;
        public Transform pointB;

        [Header("Attack")] public Transform targetPoint;
        public List<GameObject> targets = new List<GameObject>();
        public float attackCutDown;
        private float _nextAttackTime;
        public float skillRange;
        public float attackRange;

        [Header("State")] protected EnemyBaseState CurrentState;
        public PatrolState patrolState = new PatrolState();
        public AttackState AttackState = new AttackState();

        [Header("Component")] public Animator animator;
        private GameObject _alarmSign;
        private Animator _alarmAnimator;

        public readonly int AnimState =
            Animator.StringToHash("animState");

        public readonly int Attack =
            Animator.StringToHash("Attack");

        public readonly int Skill =
            Animator.StringToHash("Skill");

        private static readonly int GetHit =
            Animator.StringToHash("GetHit");

        private static readonly int Dead =
            Animator.StringToHash("Dead");


        protected virtual void Init()
        {
            animator = GetComponent<Animator>();
            _alarmSign = transform.GetChild(0).gameObject;
            _alarmAnimator = _alarmSign.GetComponent<Animator>();
        }

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            TransitionState(patrolState);
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            CurrentState.OnUpdate(this);
        }


        #region 移动相关

        public void MoveToTarget()
        {
            // Debug.Log("巡逻...Move");
            transform.position =
                Vector2.MoveTowards(
                    transform.position,
                    targetPoint.position,
                    speed * Time.deltaTime);
            FlipDirection();
        }

        public void FlipDirection()
        {
            //目标在右边
            if (transform.position.x < targetPoint.position.x)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                return;
            }

            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        public void SwitchPoint()
        {
            if (Mathf.Abs(transform.position.x - pointA.position.x) >
                Mathf.Abs(transform.position.x - pointB.position.x))
            {
                targetPoint = pointA;
                return;
            }

            targetPoint = pointB;
        }

        #endregion

        #region 攻击相关

        public virtual void AttackAction()
        {
            //距离够近且冷却已经结束才能进行攻击
            if (!(Vector2.Distance(transform.position,
                targetPoint.position) <= attackRange) || !(Time.time >= _nextAttackTime)) return;

            animator.SetTrigger(Attack);
            _nextAttackTime = Time.time + attackCutDown;
        }

        public virtual bool SkillAction()
        {
            //距离够近且冷却已经结束才能进行攻击
            if (!(Vector2.Distance(transform.position,
                targetPoint.position) <= skillRange) || !(Time.time >= _nextAttackTime)) return false;

            animator.SetTrigger(Skill);
            _nextAttackTime = Time.time + attackCutDown;
            return true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!targets.Contains(other.gameObject))
            {
                targets.Add(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (targets.Contains(other.gameObject))
            {
                targets.Remove(other.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Bomb"))
            {
                StartCoroutine(ShowAlarm());
            }
        }

        private IEnumerator ShowAlarm()
        {
            _alarmSign.SetActive(true);
            yield return new WaitForSeconds(_alarmAnimator.
                GetCurrentAnimatorClipInfo(0)[0].clip.length);
            _alarmSign.SetActive(false);
        }

        #endregion

        #region 状态机

        public void TransitionState(EnemyBaseState state)
        {
            CurrentState = state;
            state.EnterState(this);
        }

        #endregion

        #region 受伤或死亡

        public void GetDamage(float damage)
        {
            health = Mathf.Max(health - damage, 0);

            if (health == 0)
            {
                Debug.Log($"{gameObject.name}死亡");
                isDead = true;
                animator.SetTrigger(Dead);
                return;
            }

            animator.SetTrigger(GetHit);
        }

        #endregion
    }
}