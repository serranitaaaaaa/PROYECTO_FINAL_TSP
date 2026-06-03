using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

public class EnemyAI : MonoBehaviour
{
    [Header("Detección")]
    public float detectionRange = 2f;
    public float viewDistance = 12f;
    public float viewAngle = 120f;
    public float sphereCastRadius = 0.1f;
    public LayerMask obstacleMask;

    [Header("IA y Memoria")]
    public float patrolRadius = 10f;
    public float waitTimeAtPoint = 2f;
    public float memoryTime = 4f;

    [Header("Velocidades")]
    public float chaseSpeed = 3.5f;
    public float patrolSpeed = 1.8f;

    [Header("Audio del Minotauro")]
    public AudioSource bocinaMinotauro;
    public AudioClip sonidoQuieto;
    public AudioClip sonidoCaminando;
    public AudioClip sonidoPersecucion;
    public AudioClip sonidoAtaque;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform targetPlayer;

    private float waitTimer;
    private float memoryTimer;
    private bool isCatching = false;
    private AudioClip sonidoActual; // Para evitar que el audio tartamudee

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;
    }

    void Update()
    {
        if (GameManager.Instance.gameWon || isCatching || targetPlayer == null) return;

        if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        //Atrapando al jugador
        if (distanceToPlayer < detectionRange)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            // Solo te atrapa si no hay una pared (obstacleMask) entre ustedes
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                CatchPlayer();
                return;
            }
        }

        // Persiguiendo
        if (CanSeePlayer())
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(targetPlayer.position);
            memoryTimer = memoryTime;
            GestionarSonido(sonidoPersecucion);
        }
        // Buscando
        else if (memoryTimer > 0)
        {
            memoryTimer -= Time.deltaTime;
            if (!agent.pathPending && agent.remainingDistance < 1.5f) memoryTimer = 0;
            GestionarSonido(sonidoPersecucion);
        }
        // Patrullando
        else
        {
            PerformPatrol();
            if (agent.velocity.magnitude < 0.1f) GestionarSonido(sonidoQuieto);
            else GestionarSonido(sonidoCaminando);
        }
    }

    private void GestionarSonido(AudioClip clipDeseado)
    {
        if (bocinaMinotauro != null && clipDeseado != null)
        {
            
            if (sonidoActual != clipDeseado)
            {
                sonidoActual = clipDeseado;
                bocinaMinotauro.clip = clipDeseado;
                bocinaMinotauro.Play();
            }
        }
    }

    private void CatchPlayer()
    {
        isCatching = true;
        agent.isStopped = true; 
        agent.ResetPath();

        Vector3 lookDir = targetPlayer.position - transform.position;
        lookDir.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDir);

        // Animación
        if (anim != null) anim.SetTrigger("Attack");

        GestionarSonido(sonidoAtaque);

        
        PlayerManager pm = targetPlayer.GetComponent<PlayerManager>();
        if (pm != null) pm.IniciarJumpscare(transform);
    }

    [Header("Generación Aleatoria")]
    public float radioDelLaberinto = 100f; 
    public float distanciaSegura = 20f;    

    public void ResetPositionRandom()
    {
        agent.enabled = false;

        Vector3 puntoFinal = transform.position; // Fallback por si acaso
        bool puntoEncontrado = false;

        // Intentamos hasta 10 veces encontrar un punto aleatorio
        for (int i = 0; i < 10; i++)
        {
            
            Vector3 randomPos = Random.insideUnitSphere * radioDelLaberinto;
            randomPos.y = transform.position.y; 

            
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radioDelLaberinto, NavMesh.AllAreas))
            {
                
                if (Vector3.Distance(hit.position, targetPlayer.position) > distanciaSegura)
                {
                    puntoFinal = hit.position;
                    puntoEncontrado = true;
                    break; 
                }
            }
        }

        if (puntoEncontrado)
        {
            transform.position = puntoFinal;
        }
        else
        {
            Debug.LogWarning("No se encontró un punto lejano, se queda en su última posición.");
        }

        agent.enabled = true;
        agent.isStopped = false; 
        agent.ResetPath();
        agent.speed = patrolSpeed;
        memoryTimer = 0;
        isCatching = false;
        sonidoActual = null;

        if (anim != null) anim.Play("Idle");
    }

    private void PerformPatrol()
    {
        agent.speed = patrolSpeed;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                Vector3 randomPos = Random.insideUnitSphere * patrolRadius;
                randomPos += transform.position;
                NavMesh.SamplePosition(randomPos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas);
                agent.SetDestination(hit.position);
                waitTimer = 0;
            }
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = targetPlayer.position + Vector3.up * 1.0f;
        Vector3 dirToPlayer = (targetPos - eyePosition).normalized;
        float distToPlayer = Vector3.Distance(eyePosition, targetPos);

        if (distToPlayer < viewDistance)
        {
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                if (!Physics.SphereCast(eyePosition, sphereCastRadius, dirToPlayer, out RaycastHit hit, distToPlayer, obstacleMask))
                {
                    return true;
                }
            }
        }
        return false;
    }
}