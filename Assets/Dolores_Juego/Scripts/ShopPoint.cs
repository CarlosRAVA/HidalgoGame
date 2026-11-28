using UnityEngine;

namespace Dolores1810
{
    /// <summary>Carreta del mercader: al presionar E se abre la tienda sin salir del nivel.</summary>
    public class ShopPoint : MonoBehaviour, IInteractable
    {
        public string Prompt { get { return "Presiona E para comprar"; } }
        public bool Available { get { return true; } }
        public bool HintOnly { get { return false; } }

        public void Interact(PlatformerPlayer player)
        {
            AudioHub.Play(SfxId.ShopOpen, 0f);
            if (GameSession.I != null) GameSession.I.OpenShop();
        }
    }
}
