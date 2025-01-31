using Content.Client.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Atmos.Overlays
{
    public class GasTileOverlay : Overlay
    {
        private readonly GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

        public GasTileOverlay()
        {
            IoCManager.InjectDependencies(this);

            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

            var mapId = _eyeManager.CurrentMap;
            var worldBounds = _eyeManager.GetWorldViewbounds();

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                drawHandle.SetTransform(mapGrid.WorldMatrix);

                foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                {
                    foreach (var (texture, color) in _gasTileOverlaySystem.GetOverlays(mapGrid.Index, tile.GridIndices))
                    {
                        drawHandle.DrawTexture(texture, new Vector2(tile.X, tile.Y), color);
                    }
                }
            }

            drawHandle.SetTransform(Matrix3.Identity);
        }
    }
}
