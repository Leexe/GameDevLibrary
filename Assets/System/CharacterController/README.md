# Character Controller v1.2

## v1.2
- Added Inspector toggles for Jump, Slide, and Crouch behaviors (`_toggleJump`, `_slideToggle`, `_crouchToggle`) with `[ShowIf]` attribute support.
- Updated state transitions in `MyCharacterController.StateMachine` to respect the new toggles.
- Fixed `IsFalling` logic to properly account for grounded state.
- Disabled direct look-direction rotation in `MyCharacterController.Movement` to allow camera-driven rotation.
- Fixed null check pattern in `InputManager` unsubscription logic.

## v1.1
- Separated the central script into smaller partial classes.
- Integrated the stamina system script into the character controller.
- Added Odin serializable fields to the parameters.
