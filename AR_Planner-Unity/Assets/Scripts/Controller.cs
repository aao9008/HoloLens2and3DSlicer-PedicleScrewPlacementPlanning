using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;


public class Controller : MonoBehaviour
{
    // Boolean variable to keep track of model visibility state
    private bool modelOn = true;
    public SwitchButtons switchButtons;
    private bool buttonHeldDown = false;
    private float holdStartTime = 0f;
    private float holdDurationThreshold = 2f; // Adjust this threshold as needed. 
    private bool actionExecuted = false;

    public AudioSource buttonDown;
    public AudioSource buttonUp;
    
    void Update()
    {
        ToggleParentModel();
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
