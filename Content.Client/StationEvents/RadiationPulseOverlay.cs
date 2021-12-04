using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.DistortionMap;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.StationEvents
{
    public class RadiationPulseOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        private readonly DistortionMapOverlay? _dmo = default!;

        private const float MaxDist = 15.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private TimeSpan _lastTick = default;

        private readonly ShaderInstance _baseShader;
        private readonly ShaderInstance _baseDistShader;
        private readonly Dictionary<EntityUid, (ShaderInstance shd, ShaderInstance dshd, RadiationShaderInstance instance)> _pulses = new();

        public RadiationPulseOverlay()
        {
            IoCManager.InjectDependencies(this);
            OverlayManager.TryGetOverlay<DistortionMapOverlay>(out _dmo);
            _baseShader = _prototypeManager.Index<ShaderPrototype>("Radiation").Instance().Duplicate();
            _baseDistShader = _prototypeManager.Index<ShaderPrototype>("RadiationDistortion").Instance().Duplicate();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            RadiationQuery(args.Viewport.Eye);

            if (_pulses.Count == 0)
                return;

            DistortionMapOverlay.DistMapInstance? dm = default;
            if (_dmo is not null && !_dmo.TryGetMap(args.Viewport, out dm))
                dm = _dmo.CreateMap(args.Viewport);

            var worldHandle = args.WorldHandle;
            var viewport = args.Viewport;

            foreach ((var shd, var dshd, var instance) in _pulses.Values)
            {
                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = viewport.WorldToLocal(instance.CurrentMapCoords);
                tempCoords.Y = viewport.Size.Y - tempCoords.Y;
                shd?.SetParameter("renderScale", viewport.RenderScale);
                shd?.SetParameter("positionInput", tempCoords);
                shd?.SetParameter("range", instance.Range);
                var life = (_lastTick - instance.Start) / (instance.End - instance.Start);
                shd?.SetParameter("life", (float) life);

                worldHandle.UseShader(shd);
                worldHandle.DrawRect(
                        Box2.CenteredAround(
                            instance.CurrentMapCoords,
                            new Vector2(instance.Range, instance.Range) * 2f
                        ),
                        Color.White
                    );

                if (dm is not null)
                {
                    worldHandle.RenderInRenderTarget(dm.map, () =>
                        {
                            dshd?.SetParameter("renderScale", viewport.RenderScale);
                            dshd?.SetParameter("positionInput", tempCoords);
                            dshd?.SetParameter("range", instance.Range);
                            dshd?.SetParameter("life", (float) life);
                            worldHandle.UseShader(dshd);
                            worldHandle.DrawRect(
                                    Box2.CenteredAround(
                                        instance.CurrentMapCoords / dm.szDiv,
                                        new Vector2(instance.Range, instance.Range) * 2f / dm.szDiv
                                    ),
                                    Color.White
                                );
                        },
                        null,
                        true
                    );
                }
            }
        }

        //Queries all pulses on the map and either adds or removes them from the list of rendered pulses based on whether they should be drawn (in range? on the same z-level/map? pulse entity still exists?)
        private void RadiationQuery(IEye? currentEye)
        {
            if (currentEye == null)
            {
                _pulses.Clear();
                return;
            }

            _lastTick = _gameTiming.CurTime;

            var currentEyeLoc = currentEye.Position;

            var pulses = _entityManager.EntityQuery<RadiationPulseComponent>();
            foreach (var pulse in pulses) //Add all pulses that are not added yet but qualify
            {
                var pulseEntity = pulse.Owner;

                if (!_pulses.Keys.Contains(pulseEntity.Uid) && PulseQualifies(pulseEntity, currentEyeLoc))
                {
                    _pulses.Add(
                            pulseEntity.Uid,
                            (
                                _baseShader.Duplicate(),
                                _baseDistShader.Duplicate(),
                                new RadiationShaderInstance(
                                    pulseEntity.Transform.MapPosition.Position,
                                    pulse.Range,
                                    pulse.StartTime,
                                    pulse.EndTime
                                )
                            )
                    );
                }
            }

            var activeShaderIds = _pulses.Keys;
            foreach (var activePulseUid in activeShaderIds) //Remove all pulses that are added and no longer qualify
            {
                if (_entityManager.TryGetEntity(activePulseUid, out var pulseEntity) &&
                    PulseQualifies(pulseEntity, currentEyeLoc) &&
                    pulseEntity.TryGetComponent<RadiationPulseComponent>(out var pulse))
                {
                    var shaderInstance = _pulses[activePulseUid];
                    shaderInstance.instance.CurrentMapCoords = pulseEntity.Transform.MapPosition.Position;
                    shaderInstance.instance.Range = pulse.Range;
                } else {
                    _pulses[activePulseUid].shd.Dispose();
                    _pulses.Remove(activePulseUid);
                }
            }

        }

        private bool PulseQualifies(IEntity pulseEntity, MapCoordinates currentEyeLoc)
        {
            return pulseEntity.Transform.MapID == currentEyeLoc.MapId && pulseEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, pulseEntity.Transform.ParentUid, currentEyeLoc), MaxDist);
        }

        private sealed record RadiationShaderInstance(Vector2 CurrentMapCoords, float Range, TimeSpan Start, TimeSpan End)
        {
            public Vector2 CurrentMapCoords = CurrentMapCoords;
            public float Range = Range;
            public TimeSpan Start = Start;
            public TimeSpan End = End;
        };
    }
}

