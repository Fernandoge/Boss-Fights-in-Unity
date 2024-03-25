using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicAttack : MonoBehaviour
{
    protected Animator animator;
    protected Collider colliderComponent;
    protected Coroutine moveBulletCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        colliderComponent = GetComponent<Collider>();
    }

    protected virtual void OnTriggerEnter(Collider other) => print("Basic attack collides");

    public void Shoot(float bulletSpeed, Vector3 initialPosition, Vector3 direction)
    {
        moveBulletCoroutine = StartCoroutine(MoveBullet(bulletSpeed, initialPosition, direction));
    }

    private IEnumerator MoveBullet(float bulletSpeed, Vector3 initialPosition, Vector3 direction)
    {
        var elapsedTime = 0f;
        var distance = 0f;

        while (distance < bulletSpeed)
        {
            elapsedTime += Time.deltaTime;
            distance = elapsedTime * bulletSpeed;
            transform.position = initialPosition + direction * distance;
            yield return null;
        }
        
        Destroy(transform.parent.gameObject);
    }
}
