﻿using UnityEngine;
using Random = UnityEngine.Random;

namespace Characters.Ninja
{
    public class NinjaClone : MonoBehaviour
    {
        [SerializeField] private GameObject[] _basicAttackPrefabs;
        [SerializeField] private Transform _basicAttackSpawnPoint;
        [SerializeField] private int _basicAttackDamage;
        [SerializeField] private float _basicAttackSpeed;

        private Animator _anim;
        private bool _isAttacking;
        private float _lifetime;
        private Vector3 _targetShootDirection;
        private Quaternion _targetRotation;

        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Shoot_State = Animator.StringToHash("Shoot");

        /// *** Unity Events *** ///

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _targetRotation = transform.rotation;
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0)
                StopClone();
        }

        private void LateUpdate()
        {
            if (_isAttacking)
                transform.rotation = _targetRotation;
        }

        /// *** Public Methods *** ///

        public void StartAttacking() => _isAttacking = true;

        public void SetLifetime(float duration) => _lifetime = duration;

        public void StopClone()
        {
            _isAttacking = false;
            Destroy(gameObject);
        }

        // Called by NinjaController.BasicAttack to sync the shoot animation
        public void TriggerShoot(float normalizedTime, Vector3 mouseWorldPoint)
        {
            if (!_isAttacking)
                return;

            Vector3 direction = mouseWorldPoint - transform.position;
            direction.y = 0;
            _targetShootDirection = direction.sqrMagnitude >= 0.1f ? direction.normalized : transform.forward;

            // Rotate clone to face the shoot direction
            _targetRotation = Quaternion.LookRotation(_targetShootDirection);

            _anim.SetBool(Shooting, true);
            _anim.Play(Shoot_State, 0, normalizedTime);
        }

        // Used in Shoot animation event (mirrors PlayerController.BasicAttack)
        public void BasicAttack()
        {
            if (!_isAttacking)
                return;

            int basicAttackNumber = Random.Range(0, _basicAttackPrefabs.Length);
            GameObject bullet = Instantiate(_basicAttackPrefabs[basicAttackNumber],
                _basicAttackSpawnPoint.position, Quaternion.LookRotation(_targetShootDirection));
            Vector3 bulletPosition = bullet.transform.position;
            Vector3 bulletDirection = _targetShootDirection;
            _anim.SetBool(Shooting, false);
            NinjaBasicAttack bulletScript = bullet.GetComponentInChildren<NinjaBasicAttack>();
            bulletScript.SetAttackDamage(_basicAttackDamage);
            bulletScript.Shoot(_basicAttackSpeed, bulletPosition, bulletDirection);
        }
    }
}
