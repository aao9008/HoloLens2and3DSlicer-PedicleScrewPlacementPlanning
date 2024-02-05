using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class ModelToggle : MonoBehaviour 
{
    // Reference to the GameObjects holding the models
    public List<GameObject> models; // A list of all child models references
    List<string> modelNames = new List<string>(); // This list reperesents the speech command keywords. 
    public AudioSource buttonDown;
    public AudioSource buttonUp;

    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> actions = new Dictionary<string, System.Action>();

    private PressableButtons pressableButtons;
    private bool recognizerRunning = false;

    // We use the awake method to ensure that this script subscirbes to the ModelListReady event before the PressableButtons script invokes a call baack. 
    void Awake()
    {
        // Find the PressableButtons script in the scene
        pressableButtons = FindObjectOfType<PressableButtons>();

        if (pressableButtons != null)
        {
            // Subscribe to the event from PressableButtons
            PressableButtons.ModelListReady += HandleModelListReady;
            Debug.Log("Subscribed to OnModelListReady event");

        }
        else
        {
            Debug.Log("PressableButtons script not found!");
        }
    }

    // Method that will be called when the model list is ready
    void HandleModelListReady()
    {
        if (recognizerRunning)
        {
            StopRecognizer();
        }

        PopuluateDictionaryAndKeywords();

        if (!recognizerRunning)
        {
            StartRecognizer(modelNames.ToArray());
        }
    }

    void PopuluateDictionaryAndKeywords()
    {
        models = pressableButtons.modelList; // This list is a reference to all child models.
      

        foreach (GameObject model in models)
        {
            modelNames.Add("Toggle " + model.name);

            actions.Add("Toggle " + model.name, () =>
            {
                buttonDown.Play();

                model.SetActive(!model.activeSelf);

                Invoke("PlaySecondClip", 0.60f);
            });
        }
    }

    void StartRecognizer(string[] keywords)
    {
        if (keywords.Length < 1)
        {
            return;
        }
        
        // Initalize KeywordRecognizer
        keywordRecognizer = new KeywordRecognizer(keywords);

        // Register a method to handle the recognized keywords
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;

        // Start keyword recognition
        keywordRecognizer.Start();

        // Set statu flag to true
        recognizerRunning = true;
    }

    void StopRecognizer()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            // Stop keyword recognizer
            keywordRecognizer.Stop();
            
            // Unregister method from the recognizer
            keywordRecognizer.OnPhraseRecognized -= RecognizedSpeech;

            // Reset status flag to false
            recognizerRunning = false;
        }
    }

    // This method is called when a keyword is recognized by the KeywordRecognizer
    void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        // Declare an Action delegate to store the action associated with the recognized keyword
        System.Action keywordAction;

        // Check if the recognized keyword exists in the 'actions' dictionary
        // 'actions' is a dictionary mapping keywords to corresponding actions (delegates)
        if (actions.TryGetValue(speech.text, out keywordAction))
        {
            // If the keyword is found, invoke the associated action (delegate)
            // '?.Invoke()' checks if 'keywordAction' is not null before invoking it
            keywordAction?.Invoke();
        }
    }

    void PlaySecondClip()
    {
        // Play the second audio clip using the second AudioSource
        buttonUp.Play();
    }

    // This method is called when the GameObject this script is attached to is disabled or deactivated
    private void OnDisable()
    {
        // Unsubscribe the 'HandleModelListReady' method from the 'ModelListReady' event
        // This prevents 'HandleModelListReady' from being called when the event is triggered
        PressableButtons.ModelListReady -= HandleModelListReady;

        // Stop the recognizer to halt voice recognition
        StopRecognizer();
    }

    // This method is called when the GameObject this script is attached to is being destroyed
    void OnDestroy()
    {
        // Stop the recognizer to ensure proper cleanup
        StopRecognizer();
    }
}

