using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    public float TimeToDestroy = 1f;

    float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        timer+=Time.deltaTime;
        if(timer>=TimeToDestroy) Destroy(gameObject);
    }
}
