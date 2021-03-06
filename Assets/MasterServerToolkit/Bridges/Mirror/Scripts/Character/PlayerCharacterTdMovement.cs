﻿#if MIRROR
using UnityEngine;

namespace MasterServerToolkit.Bridges.Mirror.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacterInput), typeof(CharacterController))]
    public class PlayerCharacterTdMovement : PlayerCharacterMovement
    {
        [Header("Components"), SerializeField]
        protected PlayerCharacterTdLook lookController;

        [Header("Rotation Settings"), SerializeField, Range(5f, 20f)]
        private float rotationSmoothTime = 5f;

        /// <summary>
        /// The direction to which the character is required to look
        /// </summary>
        private Quaternion playerTargetDirectionAngle;

        protected override void UpdateMovement()
        {
            if (!characterController.enabled) return;

            if (characterController.isGrounded)
            {
                var aimDirection = lookController.AimDirection();

                // If we are moving but not armed mode
                if (inputController.IsMoving() && !inputController.IsArmed())
                {
                    // Calculate new angle of player
                    Vector3 currentDirection = inputController.MovementAxisDirection();

                    // 
                    if (!currentDirection.Equals(Vector3.zero))
                    {
                        playerTargetDirectionAngle = Quaternion.LookRotation(currentDirection) * lookController.GetRotation();
                    }
                }
                // If we are moving and armed mode
                else if (inputController.IsMoving() && inputController.IsArmed())
                {
                    playerTargetDirectionAngle = Quaternion.LookRotation(new Vector3(aimDirection.x, 0f, aimDirection.z));
                }
                // If we are not moving and not armed mode
                else if (!inputController.IsMoving() && inputController.IsArmed())
                {
                    playerTargetDirectionAngle = Quaternion.LookRotation(new Vector3(aimDirection.x, 0f, aimDirection.z));
                }

                // 
                if (movementIsAllowed)
                {
                    // Rotate character to target direction
                    transform.rotation = Quaternion.Lerp(transform.rotation, playerTargetDirectionAngle, Time.deltaTime * rotationSmoothTime);
                }

                // Let's calculate input direction
                var inputAxisAngle = inputController.MovementAxisDirection().Equals(Vector3.zero) ? Vector3.zero : Quaternion.LookRotation(inputController.MovementAxisDirection()).eulerAngles;
                
                //
                var compositeAngle = inputAxisAngle - transform.eulerAngles;

                // 
                calculatedInputDirection = Quaternion.Euler(compositeAngle) * lookController.GetRotation() * transform.forward * inputController.MovementAxisMagnitude();

                // 
                calculatedMovementDirection.y = -stickToGroundPower;
                calculatedMovementDirection.x = calculatedInputDirection.x * CurrentMovementSpeed;
                calculatedMovementDirection.z = calculatedInputDirection.z * CurrentMovementSpeed;

                // 
                if (inputController.IsJump() && IsJumpAvailable)
                {
                    calculatedMovementDirection.y = jumpPower;
                    nextJumpTime = Time.time + jumpRate;
                }
            }
            else
            {
                calculatedMovementDirection += Physics.gravity * gravityMultiplier * Time.deltaTime;
            }

            characterController.Move(calculatedMovementDirection * Time.deltaTime);
        }
    }
}
#endif