using Rewired.Glyphs.UnityUI;
using UnityEngine;
using UnityEngine.UI;

public class JumpTutorialUI : MonoBehaviour
{
  [SerializeField] private UnityUIPlayerControllerElementGlyph _throwGlyph = null;

  public void SetPlayerInput(Rewired.Player playerInput)
  {
    _throwGlyph.playerId = playerInput.id;
  }
}