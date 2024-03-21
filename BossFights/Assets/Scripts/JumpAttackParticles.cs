using Manager.GameManager;
using UnityEngine;
using UnityEngine.AI;

public class JumpAttackParticles : MonoBehaviour
{
    private ParticleSystem.MainModule _parentParticlesMain;
    private float originalSimulationSpeed;
    
    private void Awake()
    {
        _parentParticlesMain = transform.parent.GetComponent<ParticleSystem>().main;
        originalSimulationSpeed = _parentParticlesMain.simulationSpeed;
    }

    private void OnDisable() => _parentParticlesMain.simulationSpeed = originalSimulationSpeed;

    private void OnParticleTrigger()
    {
        GameManager.Instance.player.DamagePlayer(1);
        print("Player hit");
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!other.GetComponent<NavMeshObstacle>()) 
            return;
        
        _parentParticlesMain.simulationSpeed = 0f;
    }
}
