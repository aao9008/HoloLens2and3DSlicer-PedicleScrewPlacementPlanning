using UnityEngine;
using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Diagnostics;


public class Controller : MonoBehaviour
{
    // References to values in PressableButtons script
    public SwitchButtons switchButtons;
    public PressableButtons pressableButtons;
    public GameObject models;

    // Reference to gamecontroller
    private Gamepad gamepad;

    // Boolean variable to keep track of model visibility state
    private bool modelOn = true;

    // Audio files for audio cues
    public AudioSource buttonDown;
    public AudioSource buttonUp;

    private Coroutine resetCoroutine;
    private Coroutine speedCoroutine = null;

    bool buttonHeldDown = false;
    float holdStartTime = 0f;
    float holdDurationThreshold = 2f; // Adjust this threshold as needed. 
    bool actionExecuted = false;
    bool modelLocked = false;

    // Variables for DPad Movements and Left Joystick movments
    float YInput;
    float XInput;
    float moveSpeed = 0.5f;
    float maxSpeed = 1f;
    float minSpeed = 0.011f;

    float RightStickXInput;
    float RightStickYInput;

    float RightTriggerInput;
    float LeftTriggerInput;

    void start()
    {
        // Find the first connected Gamepad
        gamepad = Gamepad.current;

        //pressableButtons.spineModel.transform.position = new Vector3(-0.01959822f, 0.0004480685f, 0.3553342f);
    }

    void Update()
    {
        // Check for connected gamepad if null
        if (gamepad == null)
        {
            gamepad = Gamepad.current;
        }

        AdjustMoveSpeed();
        ResetMoveSpeed();
        ToggleParentModel();
        HoldButton(ToggleLockBody, "LockBody");
        MoveModelWithInput();
        AdjustHeight();
        RotateModel();
    }

    // This function handels logic for parent model visiblity controller button
    void ToggleParentModel()
    {
        // Check if button is pressed

        if (Input.GetButtonDown("ToggleSkin"))
        {
            // Call function to toggle modle visiblity.
            PlayAudioCue();
            ToggleModel();
        }
    }

    // This function executes model visiblility function when button on controller is pressed
    void ToggleModel()
    {
        if (modelOn)
        {
            switchButtons.OnTurnModelOFF(switchButtons.spineVisibility_Switch);
        }
        else
        {
            switchButtons.OnTurnModelON(switchButtons.spineVisibility_Switch);
        }

        modelOn = modelOn ? false : true;
    }

    // Logic for assigning a function to a button press and hold on controller 
    void HoldButton(Action action, string buttonName)
    {
        if (Input.GetButton(buttonName))
        {
            // Button is being held down
            if (!buttonHeldDown)
            {
                // Button has just been pressed, start tracking hold duration
                buttonHeldDown = true;
                actionExecuted = false;
                holdStartTime = Time.time;
            }
            else if (!actionExecuted && Time.time - holdStartTime >= holdDurationThreshold) // Check if the hold duration exceeds the threshold
            {
                // Execute the action for holding the button down for the specified duration
                action?.Invoke(); // Invoke the action

                // Play audio cue
                PlayAudioCue();

                // Set flag to indicate that action has been executed. 
                actionExecuted = true;
            }
        }
        else
        {
            // Button is released, reset button hold state
            buttonHeldDown = false;
            actionExecuted = false;
        }
    }

    // Logic for locking and unlocking model in place
    void ToggleLockBody()
    {
        // If model is not locked
        if (!modelLocked) {
            // Lock the model
            pressableButtons.OnReleaseSpineClicked();
        }
        else
        {
            // Unlock model if model is locked
            pressableButtons.OnModifySpineClicked();
        }

        // Update model lock flag
        modelLocked = !modelLocked;
    }

    // Logic for moving model along x and z axis using dpad
    void MoveModelWithInput()
    {
        if (pressableButtons.spineModel.GetComponent<ObjectManipulator>().enabled == false)
        {
            return;
        }
        YInput = Input.GetAxis("DPadY") + -Input.GetAxis("LeftStickY");
        XInput = Input.GetAxis("DPadX") + Input.GetAxis("LeftStickX");

        MoveModel();
    }

    void MoveModel()
    {
        Vector3 movementDirection = new Vector3(XInput, 0, YInput).normalized;

        if (movementDirection != Vector3.zero)
        {
            models.transform.Translate(movementDirection * moveSpeed * Time.deltaTime);
        }
    }

    void AdjustMoveSpeed()
    {
        if (Input.GetButtonDown("RightBumper") && moveSpeed < maxSpeed)
        {
            if (speedCoroutine != null)
            {
                StopCoroutine(speedCoroutine);
            }
            speedCoroutine = StartCoroutine(IncreaseMoveSpeed());
        }
        else if (Input.GetButtonDown("LeftBumper") && moveSpeed > minSpeed)
        {
            if (speedCoroutine != null)
            {
                StopCoroutine(speedCoroutine);
            }
            speedCoroutine = StartCoroutine(DecreaseMoveSpeed());
        }
    }

    IEnumerator IncreaseMoveSpeed()
    {
        if (moveSpeed > 0.10f)
        {
            moveSpeed += 0.1f;
        }
        else if (moveSpeed < 0.11f)
        {
            moveSpeed += 0.01f;
        }

        // Trigger haptic feedback
        gamepad.SetMotorSpeeds(0.5f, 0.5f);

        // Wait for 0.2 seconds
        yield return new WaitForSeconds(0.2f);

        // Stop haptic feedback
        gamepad.SetMotorSpeeds(0, 0);

        UnityEngine.Debug.Log("Increased Speed: " + moveSpeed);
    }

    IEnumerator DecreaseMoveSpeed()
    {
        if (moveSpeed > 0.11f)
        {
            moveSpeed -= 0.1f;
        }
        else if (moveSpeed < 0.11f && moveSpeed > minSpeed)
        {
            moveSpeed -= 0.01f;
        }

        // Trigger haptic feedback
        gamepad.SetMotorSpeeds(0.5f, 0.5f);

        // Wait for 0.2 seconds
        yield return new WaitForSeconds(0.2f);

        // Stop haptic feedback
        gamepad.SetMotorSpeeds(0, 0);

        UnityEngine.Debug.Log("Decreased Speed: " + moveSpeed);
    }

    void ResetMoveSpeed()
    {
        if (Input.GetButton("LeftBumper") && Input.GetButton("RightBumper"))
        {
            if (resetCoroutine == null)
            {
                resetCoroutine = StartCoroutine(ResetMoveSpeedAfterHold());
            }
        }
        else
        {
            // Stop the reset coroutine if either bumper is released
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
                resetCoroutine = null;
            }
        }
    }

    private IEnumerator ResetMoveSpeedAfterHold()
    {
        yield return new WaitForSeconds(2);

        if (Input.GetButton("LeftBumper") && Input.GetButton("RightBumper"))
        {
            moveSpeed = 0.5f;
            UnityEngine.Debug.Log("Move speed reset: " + moveSpeed);

            // Play audio cue
            PlayAudioCue();

            // Trigger haptic feedback
            gamepad.SetMotorSpeeds(1.0f, 1.0f); // Stronger feedback for reset
            yield return new WaitForSeconds(0.5f); // Duration of the haptic feedback
            gamepad.SetMotorSpeeds(0, 0); // Stop haptic feedback
        }

        // Clear the reset coroutine reference after completion
        resetCoroutine = null;
    }

    void AdjustHeight()
    {
        if (pressableButtons.spineModel.GetComponent<ObjectManipulator>().enabled == false)
        {
            return;
        }
        RightTriggerInput = Input.GetAxis("RightTrigger");
        LeftTriggerInput = -Input.GetAxis("LeftTrigger");

        DecreaseHeight();
        IncreaseHeight();
    }

    void DecreaseHeight()
    {
        Vector3 movementDirection = new Vector3(0, LeftTriggerInput, 0).normalized;

        if (movementDirection != Vector3.zero)
        {
            models.transform.Translate(movementDirection * moveSpeed * Time.deltaTime);
        }
    }

    void IncreaseHeight()
    {
        Vector3 movementDirection = new Vector3(0, RightTriggerInput, 0).normalized;

        if (movementDirection != Vector3.zero)
        {
            models.transform.Translate(movementDirection * moveSpeed * Time.deltaTime);
        }
    }

    void RotateModel()
    {
        if (pressableButtons.spineModel.GetComponent<ObjectManipulator>().enabled == false)
        {
            return;
        }
        RightStickYInput = Input.GetAxis("RightStickY");
        RightStickXInput = Input.GetAxis("RightStickX");
        RightStickXInput *= -1; // Left and right inputs will appear reversed if X input is not multiplied by -1.
        RightJoyStickMovement();
    }

    void RightJoyStickMovement()
    {
        Vector3 movementDirection = new Vector3(RightStickXInput, RightStickYInput, 0).normalized;

        if (movementDirection != Vector3.zero)
        {
            models.transform.Rotate(movementDirection * (moveSpeed * 40) * Time.deltaTime);
        }
    }

    // Audio cues for button action exectuions
    void PlayAudioCue()
    {
        buttonDown.Play();
        Invoke("PlaySecondClip", 0.40f);
    }

    void PlaySecondClip()
    {
        // Play the second audio clip using the second AudioSource
        buttonUp.Play();
    }
}
