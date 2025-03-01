using System;
using UnityEngine;
using System.Collections.Generic;

public class Item : MonoBehaviour
{
  public float GumMassValue = 0.25f;
  public float GumDamage = 0.1f;
  public Rigidbody Rigidbody => _rb;

  public SoundBank SfxPickup;
  public SoundBank SfxImpact;
  public SoundBank SfxChew;

  [SerializeField] private Rigidbody _rb = null;
  [SerializeField] private InteractableObject _interactable = null;

  private float _chewedAmount;
  private float _enableColliderTimer;
  private bool _wasThrown;
  private bool _didSplat;
  private List<Collider> _colliders = new();

  public void Pickup()
  {
    AudioManager.Instance.PlaySound(gameObject, SfxPickup);

    _rb.isKinematic = true;
    _interactable.enabled = false;

    foreach (var c in _colliders)
      c.enabled = false;
  }

  public void Drop()
  {
    _rb.isKinematic = false;
    _interactable.enabled = true;

    foreach (var c in _colliders)
      c.enabled = true;
  }

  public void Throw(Vector3 throwVec)
  {
    _wasThrown = true;
    _rb.isKinematic = false;
    _enableColliderTimer = 0.2f;
    _rb.AddForce(throwVec, ForceMode.VelocityChange);
  }

  public bool Chew(float amount)
  {
    AudioManager.Instance.PlaySound(gameObject, SfxChew);

    _chewedAmount = Mathf.Clamp01(_chewedAmount + amount);
    transform.localScale = Vector3.one.WithY(1 - _chewedAmount);
    return _chewedAmount >= 1;
  }

  public void Splat()
  {
    if (!_didSplat)
    {
      _didSplat = true;
      DespawnManager.Instance.AddObject(gameObject, 0, 0.25f);
      AudioManager.Instance.PlaySound(gameObject, SfxImpact);
    }
  }

  private void Awake()
  {
    if (!_rb)
      _rb = GetComponent<Rigidbody>();

    GetComponentsInChildren(_colliders);
  }

  private void Update()
  {
    if (_enableColliderTimer > 0)
    {
      _enableColliderTimer -= Time.deltaTime;
      if (_enableColliderTimer <= 0)
      {
        foreach (var c in _colliders)
          c.enabled = true;
      }
    }
  }

  private void OnCollisionEnter(Collision collision)
  {
    if (_wasThrown)
    {
      Splat();

      ISlappable slappable = collision.gameObject.GetComponent<ISlappable>();
      if (slappable != null)
      {
        slappable.ReceiveSlap(transform.position, GumDamage);
      }
    }
  }

  private void OnValidate()
  {
    if (!_rb)
      _rb = GetComponent<Rigidbody>();
  }
}