using System.Collections;
using Bosses;
using Characters.Ninja;
using Manager.GameManager;
using Shared;
using UI;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class PlayerController : MonoBehaviour
    {
        [SerializeField] private int _health;
        [SerializeField] private PlayerHealthUI _playerHealthUI;
        [SerializeField] private float _damageImmuneCD;
        [SerializeField] private float _gameOverDelay = 1.8f;
        [SerializeField] private GameObject[] _basicAttackPrefabs;
        [SerializeField] private Transform _basicAttackSpawnPoint;
        [SerializeField] private int _basicAttackDamage;
        [SerializeField] private float _basicAttackSpeed;
        [Header("Skill Dash")]
        [SerializeField] private float _dashDistance;
        [SerializeField] private float _dashFreezeTime;
        [SerializeField] private float _dashCD;
        [SerializeField] private SpellIcon _dashSpellIcon;
        [SerializeField] private GameObject _dashSmokePrefab;

        protected Animator anim;
        protected bool isAnimationLocked;
        protected static readonly int Casting = Animator.StringToHash("Casting");
        protected static readonly int Damaged = Animator.StringToHash("Damaged");
        protected const KeyCode DashKey = KeyCode.Space;
        private const KeyCode PauseKey = KeyCode.Escape;

        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private int _maxHealth;
        private float _originalDashCD;
        private float _originalDamagedImmuneCD;
        private float _invulnerableTime;
        private Vector3 _lastDestination;
        private GameMenuScreen _pauseScreen;
        private NavMeshPath _reusablePath; // Reuse path object to avoid allocations
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Skill_Dash = Animator.StringToHash("Skill_Dash");
        private static readonly int Dead = Animator.StringToHash("Dead");

        public bool DebugInvulnerable { get; set; }
        public bool IsDead { get; private set; }

        /// *** Unity Events *** ///
        
        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            anim = GetComponent<Animator>();
            _reusablePath = new NavMeshPath(); // Initialize reusable path
        }

        protected virtual void Start()
        {
            // Initialize all cooldowns
            _originalDashCD = _dashCD;
            _originalDamagedImmuneCD = _damageImmuneCD;
            _dashCD = 0;
            _damageImmuneCD = 0;
            _maxHealth = _health;
            _playerHealthUI.UpdateHearts(_health);
            _dashSpellIcon.SetKeyLabel(DashKey);
        }
        
        private void Update()
        {
            if (IsDead)
                return;

            // The victory screen freezes the game like the pause screen, and Escape must not open a pause screen on top of it
            if (GameMenuScreen.IsOpen && !_pauseScreen)
                return;

            if (Input.GetKeyDown(PauseKey))
                TogglePause();

            // The game is frozen while the pause screen is open, but Update still runs, so the inputs are skipped here
            if (_pauseScreen)
                return;

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

        // The bosses sit on the Ignore Raycast layer, so the normal mouse ray goes through them and lands on the ground behind; this one also hits them (and ignores triggers)
        protected bool GetMouseAimPoint(out Vector3 aimPoint)
        {
            if (GetMouseAimHit(out RaycastHit hit))
            {
                aimPoint = hit.point;
                return true;
            }
            aimPoint = Vector3.zero;
            return false;
        }

        protected bool GetMouseAimHit(out RaycastHit hit) => Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);

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
            if (_invulnerableTime > 0)
                _invulnerableTime -= Time.deltaTime;
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
        
        // Shoots a basic attack projectile from any point in any direction (skills that throw kunai on their own)
        protected void ShootBasicAttack(Vector3 position, Vector3 direction, int damage)
        {
            int basicAttackNumber = Random.Range(0, _basicAttackPrefabs.Length);
            Quaternion rotation = Quaternion.FromToRotation(_basicAttackSpawnPoint.forward, direction) * _basicAttackSpawnPoint.rotation;
            GameObject bullet = Instantiate(_basicAttackPrefabs[basicAttackNumber], position, rotation);
            NinjaBasicAttack bulletScript = bullet.GetComponentInChildren<NinjaBasicAttack>();
            bulletScript.SetAttackDamage(damage);
            bulletScript.Shoot(_basicAttackSpeed, bullet.transform.position, direction);
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
            if (Input.GetKeyDown(DashKey))
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
         
        // An unavoidable hit ignores the hit cooldown and skill immunity (a dodge), for attacks the fight cannot continue without
        public virtual void DamagePlayer(int damage, bool isUnavoidable = false)
        {
            if (IsDead || DebugInvulnerable || (!isUnavoidable && (_damageImmuneCD > 0 || _invulnerableTime > 0)))
                return;

            // Once the boss is beaten, what it left on the field can no longer hurt the player
            BossController activeBoss = GameManager.Instance.ActiveBoss;
            if (activeBoss && activeBoss.IsDead)
                return;

            // Important to reset player state and coroutines since this method cancels player animations
            ResetPlayerState(true);
            _health -= damage;
            _playerHealthUI.UpdateHearts(_health);
            _damageImmuneCD = _originalDamagedImmuneCD;

            if (_health <= 0)
            {
                Die();
                return;
            }

            anim.SetTrigger(Damaged);
            isAnimationLocked = true;
        }

        protected void SetHealth(int healthToAdd)
        {
            _health = Mathf.Min(_health + healthToAdd, _maxHealth);
            _playerHealthUI.UpdateHearts(_health);
        }
        
        protected void SetInvulnerable(float duration) => _invulnerableTime = duration;

        // Used in Damaged animation
        public void DamageAnimStopped() => isAnimationLocked = false;

        private void Die()
        {
            IsDead = true;
            isAnimationLocked = true;
            anim.SetTrigger(Dead);
            GameMenuScreen.ShowGameOver(_gameOverDelay);
        }

        private void TogglePause()
        {
            if (_pauseScreen)
                _pauseScreen.Close();
            else
                _pauseScreen = GameMenuScreen.ShowPaused();
        }

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
                // A smoke puff where the player was, so the dash reads as a vanish and not just a teleport
                if (_dashSmokePrefab)
                    Instantiate(_dashSmokePrefab, transform.position + Vector3.up * 0.9f, Quaternion.identity);

                _navMeshAgent.enabled = false;
                Vector3 direction = hit.point - transform.position;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

                // Stop at the first wall or edge of the walkable floor instead of landing inside it
                Vector3 destination = transform.position + direction * _dashDistance;
                if (NavMesh.Raycast(transform.position, destination, out NavMeshHit edge, NavMesh.AllAreas))
                    destination = edge.position;

                transform.position = destination;
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
