using UnityEngine;
using UnityEngine.SceneManagement;

public class EndTrigger : MonoBehaviour
{
	// Reloads scene when player enters trigger
	private void OnTriggerEnter(Collider other)
	{
		SceneManager.LoadScene(0);
	}
}
