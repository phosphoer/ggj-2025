using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

public class ToothMeteor : MonoBehaviour
{
    public float ForwardSpeed = 5f;
    public float ToothDamage = 0.4f;
    public GameObject ExplostionFX;

    ToothMeteorSpawner spawner;


    // Update is called once per frame
    void Update()
    {
        CheckForImpact();
        transform.position += transform.forward*ForwardSpeed;
    }

    void CheckForImpact()
    {
        RaycastHit rayHit = new RaycastHit();
        if(Physics.Raycast(transform.position,transform.forward,out rayHit,ForwardSpeed,1))
        {
            ISlappable slap = rayHit.collider.gameObject.GetComponentInParent<ISlappable>();
            if(slap != null)
            {
                slap.ReceiveSlap(transform.position, ToothDamage);
            }

            transform.position +=transform.forward*rayHit.distance;
            Explode();
        }

    }

    public void Explode()
    {
        if(spawner)spawner.CleanList();
        Instantiate(ExplostionFX,transform.position,ExplostionFX.transform.rotation);
        Destroy(gameObject);
    }

    public void SetStormSpawner(ToothMeteorSpawner master)
    {
        spawner = master;
    }

}
