using UnityEngine;

public class Item : MonoBehaviour
{
  private float _chewedAmount;

  public bool Chew(float amount)
  {
    _chewedAmount = Mathf.Clamp01(_chewedAmount + amount);
    transform.localScale = Vector3.one.WithY(1 - _chewedAmount);
    return _chewedAmount >= 1;
  }
}