using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;


public class Controller : MonoBehaviour
{
    // Boolean variable to keep track of model visibility state
    private bool modelOn = true;
    public SwitchButtons switchButtons;
    public PressableButtons pressableButtons;
    //GameObject model;


    public AudioSource buttonDown;
    public AudioSource buttonUp;

    bool buttonHeldDown = false;
    float holdStartTime = 0f;
    float holdDurationThreshold = 2f; // Adjust this threshold as needed. 
    bool actionExecuted = false;
    bool modelLocked = false;
    
    void Update()
    {
        ToggleParentModel();
        LockBody();
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

        modelOn= modelOn ? false : true;
    }

    void LockBody()
    {
       if (Input.GetButton("LockBody"))
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
                modelLocked = ToggleLockBody(modelLocked);
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

    bool ToggleLockBody(bool modelLocked)
    {
        // If model is not locked
        if (!modelLocked){
            // Lock the model
            pressableButtons.OnReleaseSpineClicked();
        }
        else
        {
            // Unlock model if model is locked
            pressableButtons.OnModifySpineClicked();
        }

        // Update model lock flag
        return !modelLocked;
    }

    void MoveModelWithDPad()
    {
        if (Input.GetButton("Up"))
        {

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
