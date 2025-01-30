using Rewired.Glyphs.UnityUI;
using UnityEngine;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
  public bool IsTutorialEnabled => _tutorialRoot.activeSelf;

  [SerializeField] private Transform _throwBarRoot = null;
  [SerializeField] private Image _throwBarImage = null;
  [SerializeField] private Gradient _chargeGradient = null;
  [SerializeField] private GameObject _tutorialRoot = null;
  [SerializeField] private UnityUIPlayerControllerElementGlyph _throwGlyph = null;

  public void SetTutorialEnabled(bool tutorialEnabled)
  {
    _tutorialRoot.SetActive(tutorialEnabled);
  }

  public void SetPlayerInput(Rewired.Player playerInput)
  {
    _throwGlyph.playerId = playerInput.id;
  }

  public void SetThrowVector(Vector3 throwVector, float throwT)
  {
    _throwBarImage.color = _chargeGradient.Evaluate(throwT);
    _throwBarImage.transform.localScale = Vector3.one.WithY(throwT);
    _throwBarRoot.localRotation = Quaternion.LookRotation(Vector3.forward, throwVector.normalized);
  }
}