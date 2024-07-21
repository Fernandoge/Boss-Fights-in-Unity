using System.Collections;
using Manager.GameManager;
using UnityEngine;

namespace Bosses.First_Boss
{
    public class Meteors : MonoBehaviour
    {
        [SerializeField] private GameObject indicator;
        [SerializeField] private GameObject particles;

        // Used in Meteor prefab animation
        private void EnableMeteor() => StartCoroutine(StartMeteor(GameManager.Instance.firstBoss.meteorsDuration));

        private IEnumerator StartMeteor(float duration)
        {
            particles.gameObject.SetActive(true);
            while (duration > 0)
            {
                duration -= Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) 
                return;

            var meteorDamage = GameManager.Instance.firstBoss.meteorsDamage;
            GameManager.Instance.player.DamagePlayer(meteorDamage);
        }
    }
}
