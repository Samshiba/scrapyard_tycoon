using Sandbox;
using Sandbox.UI;
using System;

namespace Sandbox
{
    // C'est un Panel standard. Le moteur va l'enregistrer sous le nom "zoom-graph-panel"
    public class ZoomGraphPanel : Panel
    {
        public Action<float> OnZoomScroll { get; set; }

        // La SEULE méthode fiable dans s&box pour capter la molette
        public override void OnMouseWheel( Vector2 value )
        {
            // value.y contient le delta de la molette
            OnZoomScroll?.Invoke( value.y );
        }
    }
}