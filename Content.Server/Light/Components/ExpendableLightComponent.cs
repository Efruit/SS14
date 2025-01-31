using Content.Server.Clothing.Components;
using Content.Server.Items;
using Content.Server.Sound.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Component;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a handheld expendable light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    public class ExpendableLightComponent : SharedExpendableLightComponent, IUse
    {
        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated => CurrentState == ExpendableLightState.Lit || CurrentState == ExpendableLightState.Fading;

        [ViewVariables]
        private float _stateExpiryTime = default;
        private AppearanceComponent? _appearance = default;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryActivate();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent<ItemComponent>(out var item))
            {
                item.EquippedPrefix = "unlit";
            }

            CurrentState = ExpendableLightState.BrandNew;
            Owner.EnsureComponent<PointLightComponent>();
            Owner.TryGetComponent(out _appearance);
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        private bool TryActivate()
        {
            if (!Activated && CurrentState == ExpendableLightState.BrandNew)
            {
                if (Owner.TryGetComponent<ItemComponent>(out var item))
                {
                    item.EquippedPrefix = "lit";
                }

                CurrentState = ExpendableLightState.Lit;
                _stateExpiryTime = GlowDuration;

                UpdateSpriteAndSounds(Activated);
                UpdateVisualizer();

                return true;
            }

            return false;
        }

        private void UpdateVisualizer()
        {
            _appearance?.SetData(ExpendableLightVisuals.State, CurrentState);

            switch (CurrentState)
            {
                case ExpendableLightState.Lit:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, TurnOnBehaviourID);
                    break;

                case ExpendableLightState.Fading:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, FadeOutBehaviourID);
                    break;

                case ExpendableLightState.Dead:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, string.Empty);
                    break;
            }
        }

        private void UpdateSpriteAndSounds(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                switch (CurrentState)
                {
                    case ExpendableLightState.Lit:
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), LitSound.GetSound(), Owner);

                        if (IconStateLit != string.Empty)
                        {
                            sprite.LayerSetState(2, IconStateLit);
                            sprite.LayerSetShader(2, "shaded");
                        }

                        sprite.LayerSetVisible(1, true);
                        break;
                    }
                    case ExpendableLightState.Fading:
                    {
                        break;
                    }
                    default:
                    case ExpendableLightState.Dead:
                    {
                        if (DieSound != null) SoundSystem.Play(Filter.Pvs(Owner), DieSound.GetSound(), Owner);

                        sprite.LayerSetState(0, IconStateSpent);
                        sprite.LayerSetShader(0, "shaded");
                        sprite.LayerSetVisible(1, false);
                        break;
                    }
                }
            }

            if (Owner.TryGetComponent(out ClothingComponent? clothing))
            {
                clothing.ClothingEquippedPrefix = on ? "Activated" : string.Empty;
            }
        }

        public void Update(float frameTime)
        {
            if (!Activated) return;

            _stateExpiryTime -= frameTime;

            if (_stateExpiryTime <= 0f)
            {
                switch (CurrentState)
                {
                    case ExpendableLightState.Lit:

                        CurrentState = ExpendableLightState.Fading;
                        _stateExpiryTime = FadeOutDuration;

                        UpdateVisualizer();

                        break;

                    default:
                    case ExpendableLightState.Fading:

                        CurrentState = ExpendableLightState.Dead;
                        Owner.Name = SpentName;
                        Owner.Description = SpentDesc;

                        UpdateSpriteAndSounds(Activated);
                        UpdateVisualizer();

                        if (Owner.TryGetComponent<ItemComponent>(out var item))
                        {
                            item.EquippedPrefix = "unlit";
                        }

                        break;
                }
            }
        }

        [Verb]
        public sealed class ActivateVerb : Verb<ExpendableLightComponent>
        {
            protected override void GetData(IEntity user, ExpendableLightComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.CurrentState == ExpendableLightState.BrandNew)
                {
                    data.Text = "Activate";
                    data.Visibility = VerbVisibility.Visible;
                }
                else
                {
                    data.Visibility = VerbVisibility.Invisible;
                }
            }

            protected override void Activate(IEntity user, ExpendableLightComponent component)
            {
                component.TryActivate();
            }
        }
    }
}
