using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isLocked = true;

    public void Unlock()
    {
        if (isLocked)
        {
            isLocked = false;
            GetComponent<SpriteRenderer>().color = Color.green;
            GetComponent<Collider2D>().enabled = false;
            Debug.Log("Door unlocked!");
        }
    }
}
