using UnityEngine;
using System.Collections.Generic;

public class Item : MonoBehaviour
{
  public float GumMassValue = 0.25f;

  [SerializeField] private Rigidbody _rb = null;

  private float _chewedAmount;
  private List<Collider> _colliders = new();

  public void Pickup()
  {
    _rb.isKinematic = true;

    foreach (var c in _colliders)
      c.enabled = false;
  }

  public void Drop()
  {
    _rb.isKinematic = false;

    foreach (var c in _colliders)
      c.enabled = true;
  }

  public bool Chew(float amount)
  {
    _chewedAmount = Mathf.Clamp01(_chewedAmount + amount);
    transform.localScale = Vector3.one.WithY(1 - _chewedAmount);
    return _chewedAmount >= 1;
  }

  private void Awake()
  {
    GetComponentsInChildren(_colliders);
  }
}