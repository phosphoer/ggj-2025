using UnityEngine;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
  [SerializeField] private Image _throwBarImage = null;
  [SerializeField] private Gradient _chargeGradient = null;

  public void SetThrowVector(Vector3 throwVector, float throwT)
  {
    _throwBarImage.color = _chargeGradient.Evaluate(throwT);
    _throwBarImage.transform.localScale = Vector3.one.WithY(throwT);
    _throwBarImage.transform.localRotation = Quaternion.LookRotation(Vector3.forward, throwVector.normalized);
  }
}