using System.Collections;
using Characters.Ninja;
using Shared;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class PlayerController : MonoBehaviour
    {
        [SerializeField] private int _health;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private float _damageImmuneCD;
        [SerializeField] private GameObject[] _basicAttackPrefabs;
        [SerializeField] private Transform _basicAttackSpawnPoint;
        [SerializeField] private int _basicAttackDamage;
        [SerializeField] private float _basicAttackSpeed;
        [Header("Skill Dash")]
        [SerializeField] private float _dashDistance;
        [SerializeField] private float _dashFreezeTime;
        [SerializeField] private float _dashCD;
        [SerializeField] private SpellIcon _dashSpellIcon;

        protected Animator anim;
        protected bool isAnimationLocked;
        protected static readonly int Casting = Animator.StringToHash("Casting");
        protected static readonly int Damaged = Animator.StringToHash("Damaged");
        
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private float _originalDashCD;
        private float _originalDamagedImmuneCD;
        private Vector3 _lastDestination;
        private NavMeshPath _reusablePath; // Reuse path object to avoid allocations
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Skill_Dash = Animator.StringToHash("Skill_Dash");

        /// *** Unity Events *** ///
        
        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            anim = GetComponent<Animator>();
            _healthText.text = _health.ToString();
            _reusablePath = new NavMeshPath(); // Initialize reusable path
        }

        protected virtual void Start()
        {
            // Initialize all cooldowns
            _originalDashCD = _dashCD;
            _originalDamagedImmuneCD = _damageImmuneCD;
            _dashCD = 0;
            _damageImmuneCD = 0;
        }
        
        private void Update()
        {
            NavMeshAgentPathCheck();
            PlayerInputs();
            PlayerCooldowns();
        }
        
        /// *** Base Methods *** ///
        
        protected virtual void ResetPlayerState(bool includeCoroutines)
        {
            _navMeshAgent.isStopped = true;
            anim.SetBool(Running, false);
            anim.SetBool(Shooting, false);
            anim.SetBool(Casting, false);
            isAnimationLocked = false;
            if (includeCoroutines)
                StopAllCoroutines();
        } 
        
        protected bool GetMouseWorldPoint(out Vector3 worldPoint)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                worldPoint = hit.point;
                return true;
            }
            worldPoint = Vector3.zero;
            return false;
        }

        protected void LookAtMouse()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) 
                return;
            
            Vector3 direction = hit.point - transform.position;
            direction.y = 0; // Flatten to horizontal plane
            
            // Prevent rotation if clicking too close to player (minimum distance check)
            if (direction.sqrMagnitude < 0.1f)
                return;
            
            var targetRotation = Quaternion.LookRotation(direction);
            // Lock X rotation to 0 to prevent tilting
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
        
        protected virtual void PlayerCooldowns()
        {
            if (_dashCD > 0)
                _dashCD -= Time.deltaTime;
            if (_damageImmuneCD > 0)
                _damageImmuneCD -= Time.deltaTime;
        }
        
        // Used in Shoot animation
        public virtual void BasicAttack()
        {
            int basicAttackNumber = Random.Range(0, _basicAttackPrefabs.Length);
            GameObject bullet = Instantiate(_basicAttackPrefabs[basicAttackNumber], _basicAttackSpawnPoint.position, _basicAttackSpawnPoint.rotation);
            Vector3 bulletPosition = bullet.transform.position;
            Vector3 bulletDirection = _basicAttackSpawnPoint.forward;
            anim.SetBool(Shooting, false);
            var bulletScript = bullet.GetComponentInChildren<NinjaBasicAttack>();
            bulletScript.SetAttackDamage(_basicAttackDamage);
            bulletScript.Shoot(_basicAttackSpeed, bulletPosition, bulletDirection);
        }
        
        private void NavMeshAgentPathCheck()
        {
            if (!anim.GetBool(Running) || _navMeshAgent.pathPending) 
                return;
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.25f)
                anim.SetBool(Running, false);
        }
        
        /// *** Inputs *** ///

        private void PlayerInputs()
        {
            MovementAndAttackInput();
            SkillsInput();
        }

        protected virtual void SkillsInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SkillDash();
        }
        
        private void MovementAndAttackInput()
        {
            if (isAnimationLocked || anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
            
            if (Input.GetMouseButton(1))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out var hit)) 
                    return;
                
                // Find nearest valid NavMesh position (reduced search distance for better performance)
                if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
                    return;
                
                // Only set new destination if it's significantly different from the last one
                if (Vector3.Distance(navHit.position, _lastDestination) < 0.5f)
                    return;
                
                _lastDestination = navHit.position;
                
                // Pre-calculate path to reduce hitching
                if (_navMeshAgent.CalculatePath(navHit.position, _reusablePath))
                {
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.SetPath(_reusablePath);
                    anim.SetBool(Running, true);
                    anim.SetBool(Shooting, false);
                }
            }
            
            if (Input.GetMouseButton(0))
            {
                LookAtMouse();
                _navMeshAgent.isStopped = true;
                anim.SetBool(Running, false);
                anim.SetBool(Shooting, !anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"));
            }
        }
        
        /// *** Health Logic *** ///
         
        public virtual void DamagePlayer(int damage)
        {
            if (_damageImmuneCD > 0)
                return;

            // Important to reset player state and coroutines since this method cancels player animations
            ResetPlayerState(true);
            anim.SetTrigger(Damaged);
            isAnimationLocked = true;
            _health -= damage;
            _healthText.text = _health.ToString();
            _damageImmuneCD = _originalDamagedImmuneCD;
        }

        protected void SetHealth(int healthToAdd)
        {
            _health += healthToAdd;
            _healthText.text = _health.ToString();
        }
        
        // Used in Damaged animation
        public void DamageAnimStopped() => isAnimationLocked = false;

        /// *** Dash *** ///
        
        private void SkillDash()
        {
            if (_dashCD > 0)
                return;
            
            // Important to reset player state and coroutines since this method cancels player animations
            ResetPlayerState(true); 
            anim.SetTrigger(Skill_Dash);
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _navMeshAgent.enabled = false;
                Vector3 direction = (hit.point - transform.position).normalized;
                transform.position += direction * _dashDistance;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = lookRotation;
                _navMeshAgent.enabled = true;
            }
            _dashCD = _originalDashCD;
            _dashSpellIcon.StartCooldown(_dashCD);
            StartCoroutine(StartDashFreezeTime(_dashFreezeTime));
        }
        
        // Animation Lock triggered after dashing
        private IEnumerator StartDashFreezeTime(float dashFreezeTime)
        {
            isAnimationLocked = true;
            while (dashFreezeTime > 0)
            {
                dashFreezeTime -= Time.deltaTime;
                yield return null;
            }
            isAnimationLocked = false;
        }
    }
}
