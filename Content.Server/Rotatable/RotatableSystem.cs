using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Rotatable;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Rotatable
{
    /// <summary>
    ///     Handles verbs for the <see cref="RotatableComponent"/> and <see cref="FlippableComponent"/> components.
    /// </summary>
    public sealed class RotatableSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<FlippableComponent, GetVerbsEvent<Verb>>(AddFlipVerb);
            SubscribeLocalEvent<RotatableComponent, GetVerbsEvent<Verb>>(AddRotateVerbs);
        }

        private void AddFlipVerb(EntityUid uid, FlippableComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new()
            {
                Act = () => TryFlip(uid, component, args.User),
                Text = Loc.GetString("flippable-verb-get-data-text"),
                DoContactInteraction = true
            };
            // TODO VERB ICONS Add Uno reverse card style icon?
            args.Verbs.Add(verb);
        }

        private void AddRotateVerbs(EntityUid uid, RotatableComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess
                || !args.CanInteract
                || Transform(uid).NoLocalRotation) // Good ol prototype inheritance, eh?
                return;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!component.RotateWhileAnchored &&
                EntityManager.TryGetComponent(uid, out PhysicsComponent? physics) &&
                physics.BodyType == BodyType.Static)
                return;

            Verb resetRotation = new ()
            {
                DoContactInteraction = true,
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation = Angle.Zero,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Text = "Reset",
                Priority = -2, // show CCW, then CW, then reset
                CloseMenu = false,
            };
            args.Verbs.Add(resetRotation);

            // rotate clockwise
            Verb rotateCW = new()
            {
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation -= component.Increment,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
                Priority = -1,
                CloseMenu = false, // allow for easy double rotations.
            };
            args.Verbs.Add(rotateCW);

            // rotate counter-clockwise
            Verb rotateCCW = new()
            {
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation += component.Increment,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png")),
                Priority = 0,
                CloseMenu = false, // allow for easy double rotations.
            };
            args.Verbs.Add(rotateCCW);
        }

        /// <summary>
        ///     Replace a flippable entity with it's flipped / mirror-symmetric entity.
        /// </summary>
        public void TryFlip(EntityUid uid, FlippableComponent component, EntityUid user)
        {
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physics) &&
                physics.BodyType == BodyType.Static)
            {
                _popup.PopupEntity(Loc.GetString("flippable-component-try-flip-is-stuck"), uid, user);
                return;
            }

            var oldTransform = EntityManager.GetComponent<TransformComponent>(uid);
            var entity = EntityManager.SpawnEntity(component.MirrorEntity, oldTransform.Coordinates);
            var newTransform = EntityManager.GetComponent<TransformComponent>(entity);
            newTransform.LocalRotation = oldTransform.LocalRotation;
            newTransform.Anchored = false;
            EntityManager.DeleteEntity(uid);
        }
    }
}
