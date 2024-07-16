using Manager.GameManager;
using UnityEngine;

namespace Bosses.First_Boss
{
    public class Orbs : MonoBehaviour
    {
        [SerializeField] private GameObject _sparksPrefab;
        [SerializeField] private GameObject _soakIndicator;
        [SerializeField] private GameObject _explosion;

        private Animator _animator;
        private float _originalAnimatorSpeed;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _originalAnimatorSpeed = _animator.speed;
        }

        private void OnEnable()
        {
            _soakIndicator.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) 
                return;
            
            _sparksPrefab.SetActive(true);
            _animator.speed = 4f;
        }
    
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) 
                return;
            
            _sparksPrefab.SetActive(false);
            _animator.speed = _originalAnimatorSpeed;
        }

        public void Explode()
        {
            _explosion.SetActive(true);
            _soakIndicator.SetActive(false);
            GameManager.Instance.player.DamagePlayer(GameManager.Instance.firstBoss.orbsDamage);
        }

    }
}
