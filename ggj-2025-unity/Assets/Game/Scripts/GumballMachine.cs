using UnityEngine;

public class GumballMachine : MonoBehaviour, ISlappable
{
  public float SlapWobbleScale = 10;

  [SerializeField] private WobbleAnimation _wobble = null;

  void ISlappable.ReceiveSlap(Vector3 fromPos)
  {
    Vector3 wobbleAxis = Vector3.Cross(Vector3.up, (transform.position - fromPos).normalized);
    _wobble.StartWobble(wobbleAxis, SlapWobbleScale);
  }
}