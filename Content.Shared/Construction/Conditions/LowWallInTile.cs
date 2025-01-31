﻿using Content.Shared.Maps;
using Content.Shared.Window;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class LowWallInTile : IConstructionCondition
    {
        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var lowWall = false;

            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.IncludeAnchored))
            {
                if (entity.HasComponent<SharedCanBuildWindowOnTopComponent>())
                    lowWall = true;

                // Already has a window.
                if (entity.HasComponent<SharedWindowComponent>())
                    return false;
            }

            return lowWall;
        }
    }
}
