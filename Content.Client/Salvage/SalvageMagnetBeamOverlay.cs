using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Salvage
{
    public class SalvageBeamOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly ShaderInstance _baseShader;
        private readonly Dictionary<EntityUid, (ShaderInstance shd, SalvageBeamShaderInstance instance)> _ents = new();

        public SalvageBeamOverlay()
        {
            IoCManager.InjectDependencies(this);
            _baseShader = _prototypeManager.Index<ShaderPrototype>("SalvageBeam").Instance().Duplicate();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            Query(args.WorldBounds);

            if (_ents.Count == 0)
            {
                Logger.DebugS("smbo", "ded (no ents)");
                return;
            }

            var worldHandle = args.WorldHandle;
            foreach ((var shd, var instance) in _ents.Values)
            {
                worldHandle.UseShader(shd);
                worldHandle.DrawRect(instance.Line(), Color.White);
            }
        }

        private void Query(Box2Rotated bounds)
        {
            var matches = _entityManager.EntityQuery<SalvageMagnetComponent>();
            foreach (var match in matches)
            {
                // Are we already tracking it?
                if (_ents.Keys.Contains(match.Owner))
                {
                    Logger.DebugS("smbo", "ded (tracking)");
                    continue;
                }

                var ins = SalvageBeamShaderInstance.FromComp(_entityManager, match);

                if (ins is null)
                {
                    Logger.DebugS("smbo", "ded (no ins)");
                    continue;
                }

                if (!ShouldDraw(bounds, match, ins))
                {
                    Logger.DebugS("smbo", "ded (shouldn't draw)");
                    continue;
                }

                _ents.Add(match.Owner, (_baseShader.Duplicate(), ins));
            }

            foreach (var ent in _ents.Keys)
            {
                if (!_entityManager.EntityExists(ent)
                        || !_entityManager.TryGetComponent<SalvageMagnetComponent>(ent, out var comp)
                        || !ShouldDraw(bounds, comp, _ents[ent].instance))
                {
                    Logger.DebugS("smbo", "ded (purging)");
                    _ents[ent].shd.Dispose();
                    _ents.Remove(ent);
                }
            }
        }

        private static bool ShouldDraw(Box2Rotated bounds, SalvageMagnetComponent comp, SalvageBeamShaderInstance ins)
        {
            if (comp.AttachedEntity is null)
                return false;

            if (!bounds.CalcBoundingBox().Intersects(ins.Line().CalcBoundingBox()))
                return false;

            return true;
        }

        private sealed record SalvageBeamShaderInstance(Vector2 From, Vector2 To)
        {
            public static SalvageBeamShaderInstance? FromComp(IEntityManager em, SalvageMagnetComponent comp)
            {
                if (comp.AttachedEntity is null)
                    return null;

                if (!em.TryGetComponent<TransformComponent>(comp.Owner, out var magXform))
                    return null;

                if (!em.TryGetComponent<TransformComponent>(comp.AttachedEntity.Value, out var attXform))
                    return null;

                return new SalvageBeamShaderInstance(magXform.WorldPosition, attXform.WorldPosition);
            } 

            public Box2Rotated Line() => Box2Rotated.FromLine(From, To, 1f);
        };
    }
}

