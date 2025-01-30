using Rewired.Glyphs.UnityUI;
using UnityEngine;
using UnityEngine.UI;

public class ThrowTutorialUI : MonoBehaviour
{
  [SerializeField] private UnityUIPlayerControllerElementGlyph _throwGlyph = null;

  public void SetPlayerInput(Rewired.Player playerInput)
  {
    _throwGlyph.playerId = playerInput.id;
    if (playerInput.controllers.hasMouse)
    {
      _throwGlyph.actionId = RewiredConsts.Action.ThrowStart;
    }
  }
}