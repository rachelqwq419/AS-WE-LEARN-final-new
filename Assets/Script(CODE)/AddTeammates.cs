using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddTeammates : MonoBehaviour
{
   public GameObject characterPrefab;
   private bool hasJoined = false;
   private bool playerNearBy = false;

    // Update is called once per frame
    void Update()
    {
        if (playerNearBy && Input.GetKeyDown(KeyCode.E))
        {
            tryAddTeammate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
       if (other.CompareTag("Player"))
        {
            playerNearBy = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
       if (other.CompareTag("Player"))
        {
            playerNearBy = false;
        }
    }

    private void tryAddTeammate()
    {
        if (!hasJoined)
        {
            bool added = TeamManager.Instance.AddToTeam(characterPrefab);
            if (added)
            {
                hasJoined = true;
                gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Team is full, cannot add more teammates.");
            }
        }
     }
}
