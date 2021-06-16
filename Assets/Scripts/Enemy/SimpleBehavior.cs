using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class SimpleBehavior : MonoBehaviour
{
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private LayerMask _layerMask;

    private int _currentDestination = 0;
    private int _getNextDestination
	{
		get {
            _currentDestination++;
            if (_patrolPoints.Length > _currentDestination)
                return _currentDestination;
            else
            {
                _currentDestination = 0;
                return _currentDestination;
            }
        }
    }
    private Transform _viewPosition;
    private float _startingSpeed;

    NavMeshAgent _navMeshAgent;
    private AIStates _currentState;
    private enum AIStates
	{
        patrol,
        attack,
        blinded
	}

    // Disable unity logs when running the game outside of Unity
	private void Awake()
	{
        #if UNITY_EDITOR
            Debug.unityLogger.logEnabled = true;
        #else
            Debug.unityLogger.logEnabled = false;
        #endif
    }

    // Setup all basics when the enemy is spawned
    void Start()
    {
        _currentState = AIStates.patrol;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _startingSpeed = _navMeshAgent.speed;
        _viewPosition = transform.GetChild(0).transform;

        if (_patrolPoints.Length == 0) Debug.LogWarning("Enemy " + gameObject.name + "has no patrol points. Add patrol points if this is not intended");
    }

    // Check for GameObjects around enemy and run the behavior with a fixed interval (FixedDeltaTime)
    void FixedUpdate()
    {
        if(_currentState != AIStates.blinded)
		{
			GameObject hitGameObject = VisibleObject();

			HandleBehavior(hitGameObject);
		}
	}

    // Looks what object is being seen by the enemy
	GameObject VisibleObject()
	{
        // Get all objects in the surrounding
        Collider[] targets = Physics.OverlapSphere(transform.position, 5);
        
        foreach (Collider c in targets)
        {
            Transform target = c.transform;
            Vector3 direction = (target.position - transform.position).normalized;

            // Check if bocket is over enemy's head
            if (c.CompareTag("VisionBlocker"))
            {
                if (Physics.Raycast(_viewPosition.position, _viewPosition.forward, 5, _layerMask)
                    && Physics.Raycast(_viewPosition.position, _viewPosition.up, 5, _layerMask))
                {
                    SetState(AIStates.blinded);
                    return c.gameObject;
                }
            }

            // Check if enemy can see player
            if (Vector3.Angle(transform.forward, direction) < 150 / 2)
            {
                float distance = Vector3.Distance(transform.position, target.position);

                if (Physics.Raycast(_viewPosition.position, direction, distance, _layerMask))
                {
                    if (c.CompareTag("Player") || c.CompareTag("MainCamera"))
                    {
                        SetState(AIStates.attack);
                        return c.gameObject;
                    }
                }
            }
        }
        SetState(AIStates.patrol);
        return null;
	}

    // Handle the behavior of enemu's AI
    private void HandleBehavior(GameObject hitObject)
	{
        switch (_currentState)
        {
            // Makes enemy patroll over preditermined waypoints
            case (AIStates.patrol):
                _navMeshAgent.isStopped = false;
                _navMeshAgent.speed = _startingSpeed;

                if (_patrolPoints.Length > 0 && (_navMeshAgent.remainingDistance < 1 || _navMeshAgent.destination == null))
                {
                    _navMeshAgent.SetDestination(_patrolPoints[_getNextDestination].position);
                }
                break;

            // Makes enemy go towards player and "kill" him
            case (AIStates.attack):
                _navMeshAgent.isStopped = false;
                _navMeshAgent.speed = _startingSpeed * 1.5f;
                 _navMeshAgent.destination = hitObject.transform.position;

                if(_navMeshAgent.remainingDistance < 0.3f)
				{
                    SceneManager.LoadScene(0);
				}
                break;

            // Makes enemy stop and run the RemoveVisionBlocker coroutine
            case (AIStates.blinded):
                _navMeshAgent.isStopped = true;
                StartCoroutine(RemoveVisionBlocker(hitObject));
                break;

            // Throws error when the AI state isn't recognized
            default:
                Debug.LogError("Current state of object " + gameObject.name + " is null or not recognized. Current state: " + _currentState);
                break;
        }
    }

    // Remove bucket after 10 seconds and makes enemy patrol again
    private IEnumerator RemoveVisionBlocker(GameObject visionBlocker)
	{
        yield return new WaitForSeconds(10f);
        Destroy(visionBlocker);
        SetState(AIStates.patrol);
    }

    // Set AI state
    private void SetState(AIStates state)
	{
        _currentState = state;
	}
}
