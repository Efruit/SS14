using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Salvage
{
    /// <summary>
    ///     A salvage magnet.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    [ComponentProtoName("SalvageMagnet")]
    public class SalvageMagnetComponent : Component
    {
        /// <summary>
        ///     Offset relative to magnet that salvage should spawn.
        ///     Keep in sync with marker sprite (if any???)
        /// </summary>
        private Vector2 _offset = Vector2.Zero;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offset")]
        public Vector2 Offset // TODO: Maybe specify a direction, and find the nearest edge of the magnets grid the salvage can fit at
        {
            get => _offset;
            set {
                _offset = value;
            }
        }

        /// <summary>
        ///     The entity attached to the magnet
        /// </summary>
        private EntityUid? _attachedEntity = null;
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("attachedEntity")]
        public EntityUid? AttachedEntity
        {
            get => _attachedEntity;
            set {
                _attachedEntity = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current state of this magnet
        /// </summary>
        private MagnetState _magnetState = MagnetState.Inactive;
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("magnetState")]
        public MagnetState MagnetState
        {
            get => _magnetState;
            set {
                _magnetState = value;
                Dirty();
            }
        }
    }

    [Serializable, NetSerializable]
    public struct MagnetState
    {
        public MagnetStateType StateType;
        public TimeSpan Until;

        public MagnetState(MagnetStateType mst, TimeSpan until)
        {
            StateType = mst;
            Until = until;
        }

        public static readonly MagnetState Inactive = new (MagnetStateType.Inactive, TimeSpan.Zero);
    };

    [Serializable, NetSerializable]
    public enum MagnetStateType
    {
        Inactive,
        Attaching,
        Holding,
        Detaching,
        CoolingDown,
    }

    [Serializable, NetSerializable]
    public class SalvageMagnetComponentState : ComponentState
    {
        public Vector2 Offset { get; set; }
        public EntityUid? AttachedEntity { get; set; }
        public MagnetState MagnetState { get; set; }

        public SalvageMagnetComponentState (Vector2 offset, EntityUid? attached, MagnetState magnetstate)
        {
            Offset = offset;
            AttachedEntity = attached;
            MagnetState = magnetstate;
        }
    }
}
