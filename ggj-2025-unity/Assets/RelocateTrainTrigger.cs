using UnityEngine;

public class RelocateTrainTrigger : MonoBehaviour
{
    public Transform resetPosition;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Train"))
        {
            print("train");
            var newPosition = new Vector3(
                resetPosition.position.x,
                other.gameObject.transform.position.y,
                other.gameObject.transform.position.z
            );

            var platformComponent = other.gameObject.GetComponent<MovingPlatform>();
            platformComponent.Mover.SetPosition(newPosition);

        }
    }
}