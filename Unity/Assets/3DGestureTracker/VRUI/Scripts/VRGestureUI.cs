﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace WinterMute
{
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(VRControllerUIInput))]
    public class VRGestureUI : MonoBehaviour
    {
        public enum VRUIType { SteamVR, EdwonVR };
        public VRUIType vrUiType;
        VRGestureRig myAvatar;

        [HideInInspector]
        public HandType menuHandedness;
        private PanelManager panelManager;
        Transform vrMenuHand; // the hand to attach the hand ui to
        Transform vrHandUIPanel; // the actual ui
        Transform vrCam;
        VRGestureGallery vrGestureGallery;
        public float offsetZ;

        public VRGestureManager vrGestureManager; // the VRGestureManager script we want to interact with
        public RectTransform recordMenu; // the top level transform of the recordMenu where we will generate gesture buttons'
        public RectTransform editMenu; // the top level transform of the eidtMenu
        public RectTransform selectNeuralNetMenu; // the top level transform of the select neural net menu where we will generate buttons
        public GameObject buttonPrefab;

        // PARENT
        Canvas rootCanvas; // the canvas on the main VRGestureUI object

        [HideInInspector]
        //public VRControllerUIInput vrInput;

        // RECORD MENU
        private List<Button> gestureButtons;
        [Tooltip("the title of the gesture list on the record menu")]
        public CanvasRenderer recordListTitle;
        public CanvasRenderer editListTitle;
        public CanvasRenderer newGestureButton;

        // RECORDING MENU
        [Tooltip("the now recording indicator in the recording menu")]
        public Text nowRecordingLabel;
        public Image nowRecordingBackground;
        [Tooltip("the label that tells you what gesture your recording currently")]
        public Text nowRecordingGestureLabel;
        [Tooltip("the button that deletes gestures in the Recording Menu")]
        public Button deleteGestureButton;

        // EDITING MENU
        [Tooltip("the label that tells you what gesture your editing currently")]
        public Text nowEditingGestureLabel;

        // DETECT MENU
        [Tooltip("the ui text that should be updated with a gesture detect log")]
        public Text detectLog;

        // SELECT NEURAL NET MENU
        [Tooltip("the panel of the Select Neural Net Menu")]
        public RectTransform neuralNetTitle;

        // TRAINING MENU
        [Tooltip("the text feedback for the currently training neural net")]
        public Text neuralNetTraining;

        // default settings
        private Vector3 buttonRectScale; // new Vector3(0.6666f, 1, 0.2f);

        void Awake()
        {
            if (vrGestureManager == null)
                vrGestureManager = GameObject.FindObjectOfType<VRGestureManager>();
        }

        void Start()
        {
            menuHandedness = (Config.gestureHand == HandType.Left)? HandType.Right : HandType.Left;

            rootCanvas = GetComponent<Canvas>();
            //vrInput = GetComponent<VRControllerUIInput>();
            vrHandUIPanel = transform.Find("Panels");
            vrGestureGallery = transform.GetComponentInChildren<VRGestureGallery>();

            buttonRectScale = new Vector3(0.6666f, 1, 0.2f);

            // get vr player hand and camera
            if (vrUiType == VRUIType.EdwonVR)
            {
                myAvatar = VRGestureManager.Instance.rig;
                vrMenuHand = myAvatar.GetHand(menuHandedness);
                vrCam = VRGestureManager.Instance.rig.cameraEyeTransform;
            }
            else if (vrUiType == VRUIType.SteamVR)
            {
                SteamVR_ControllerManager ControllerManager;
                ControllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();
                if (menuHandedness == HandType.Left)
                {
                    vrMenuHand = ControllerManager.left.GetComponent<SteamVR_TrackedObject>().transform;
                }
                else
                {
                    vrMenuHand = ControllerManager.right.GetComponent<SteamVR_TrackedObject>().transform;
                }
                vrCam = GameObject.FindObjectOfType<SteamVR_Camera>().transform;
            }

            panelManager = transform.GetComponentInChildren<PanelManager>();

            GenerateRecordMenuButtons();
            GenerateEditMenuButtons();
            GenerateNeuralNetMenuButtons();
        }

        void Update()
        {
            Vector3 handToCamVector = vrCam.position - vrMenuHand.position;
            //Debug.DrawRay(vrHand.position, handToCamVector);
            //transform.position = vrHand.position + (offsetZ * handToCamVector);
            //transform.rotation = Quaternion.LookRotation(transform.position - vrCam.position);
            vrHandUIPanel.position = vrMenuHand.position + (offsetZ * handToCamVector);
            if (-handToCamVector != Vector3.zero)
                vrHandUIPanel.rotation = Quaternion.LookRotation(-handToCamVector, Vector3.up);

            // update detect log
            if (detectLog != null)
                detectLog.text = vrGestureManager.debugString;
            else
                Debug.Log("please set detect log on GestureUIController");

            UpdateCurrentNeuralNetworkText();
            UpdateNowRecordingStatus();
        }

        // events called by buttons when pressed

        public void BeginDetectMode()
        {
            //Debug.Log("begin detect mode");
            VRGestureManager.Instance.BeginDetect("");
        }

        // called when entering recording menu
        public void BeginReadyToRecordGesture(string gestureName)
        {
            //Debug.Log("begin ready to record gesture of type " + gestureName);
            nowRecordingGestureLabel.text = gestureName;
            deleteGestureButton.onClick.RemoveAllListeners();
            deleteGestureButton.onClick.AddListener(() => DeleteGesture(gestureName));
            deleteGestureButton.onClick.AddListener(() => panelManager.FocusPanel("Record Menu"));
            VRGestureManager.Instance.BeginReadyToRecord(gestureName);
        }

        public void BeginEditGesture(string gestureName)
        {
            nowEditingGestureLabel.text = gestureName;
            vrGestureManager.BeginEditing(gestureName);
        }

        public void SelectNeuralNet(string neuralNetName)
        {
            vrGestureManager.SelectNeuralNet(neuralNetName);
        }

        public void BeginTraining()
        {
            panelManager.FocusPanel("Training Menu");
            neuralNetTraining.text = vrGestureManager.currentNeuralNet;
            vrGestureManager.BeginTraining(OnFinishedTraining);
        }

        public void QuitTraining()
        {
            vrGestureManager.EndTraining(OnQuitTraining);
        }

        void OnFinishedTraining(string neuralNetName)
        {
            StartCoroutine(TrainingMenuDelay(1f));
        }

        void OnQuitTraining(string neuralNetName)
        {
            StartCoroutine(TrainingMenuDelay(1f));
        }

        public void CreateGesture()
        {
            Debug.Log("called create new gesture");
            string newGestureName = "Gesture " + (vrGestureManager.gestureBank.Count + 1);
            vrGestureManager.CreateGesture(newGestureName);
            GenerateRecordMenuButtons();
        }

        public void DeleteGesture(string gestureName)
        {
            vrGestureManager.DeleteGesture(gestureName);
        }

        IEnumerator TrainingMenuDelay(float delay)
        {
            // after training complete and a short delay go back to main menu
            yield return new WaitForSeconds(delay);
            panelManager.FocusPanel("Main Menu");
        }

        // generate UI elements

        void GenerateRecordMenuButtons()
        {
            GenerateGestureButtons(vrGestureManager.gestureBank, recordMenu.transform, GestureButtonsType.Record);

        }

        void GenerateEditMenuButtons()
        {
            GenerateGestureButtons(vrGestureManager.gestureBank, editMenu.transform, GestureButtonsType.Edit);
        }

        enum GestureButtonsType { Record, Edit };

        void GenerateGestureButtons(List<string> gesturesToGenerate, Transform buttonsParent, GestureButtonsType gestureButtonsType)
        {
            // first destroy the old gesture buttons if they are there
            if (gestureButtons != null)
            {
                if (gestureButtons.Count > 0)
                {
                    foreach (Button button in gestureButtons)
                    {
                        Destroy(button.gameObject);
                    }
                    gestureButtons.Clear();
                }
            }

            float gestureButtonHeight = 30;

            gestureButtons = GenerateButtonsFromList(gesturesToGenerate, buttonsParent, buttonPrefab, gestureButtonHeight);

            // set the functions that the button will call when pressed
            for (int i = 0; i < gestureButtons.Count; i++)
            {
                string gestureName = vrGestureManager.gestureBank[i];
                if (gestureButtonsType == GestureButtonsType.Record)
                {
                    gestureButtons[i].onClick.AddListener(() => BeginReadyToRecordGesture(gestureName));
                    gestureButtons[i].onClick.AddListener(() => panelManager.FocusPanel("Recording Menu"));
                }
                else if (gestureButtonsType == GestureButtonsType.Edit)
                {
                    gestureButtons[i].onClick.AddListener(() => BeginEditGesture(gestureName));
                    gestureButtons[i].onClick.AddListener(() => panelManager.FocusPanel("Editing Menu"));
                }
            }

            if (gestureButtonsType == GestureButtonsType.Record)
                AdjustListTitlePosition(recordListTitle.transform, gestureButtons.Count, gestureButtonHeight);
            else if (gestureButtonsType == GestureButtonsType.Edit)
                AdjustListTitlePosition(editListTitle.transform, gestureButtons.Count, gestureButtonHeight);

            if (gestureButtonsType == GestureButtonsType.Record)
            {
                // adjust new gesture button position
                float totalHeight = gestureButtons.Count * gestureButtonHeight;
                float y = -(totalHeight / 2);
                newGestureButton.transform.localPosition = new Vector3(0, y, 0);
            }
        }

        void GenerateNeuralNetMenuButtons()
        {
            int neuralNetMenuButtonHeight = 30;

            List<Button> buttons = GenerateButtonsFromList(vrGestureManager.neuralNets, selectNeuralNetMenu.transform, buttonPrefab, neuralNetMenuButtonHeight);

            // set the functions that the button will call when pressed
            for (int i = 0; i < buttons.Count; i++)
            {
                string neuralNetName = vrGestureManager.neuralNets[i];
                buttons[i].onClick.AddListener(() => panelManager.FocusPanel("Main Menu"));
                buttons[i].onClick.AddListener(() => SelectNeuralNet(neuralNetName));
            }

            AdjustListTitlePosition(neuralNetTitle.transform, buttons.Count, neuralNetMenuButtonHeight);
        }

        List<Button> GenerateButtonsFromList(List<string> list, Transform parent, GameObject prefab, float buttonHeight)
        {
            List<Button> buttons = new List<Button>();
            for (int i = 0; i < list.Count; i++)
            {
                // instantiate the button
                GameObject button = GameObject.Instantiate(prefab);
                button.transform.parent = parent;
                button.transform.localPosition = Vector3.zero;
                button.transform.localRotation = Quaternion.identity;
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.localScale = buttonRectScale;
                button.transform.name = list[i] + " Button";
                // set the button y position
                float totalHeight = list.Count * buttonHeight;
                float y = 0f;
                if (i == 0)
                {
                    y = totalHeight / 2;
                }
                y = (totalHeight / 2) - (i * buttonHeight);
                buttonRect.localPosition = new Vector3(0, y, 0);
                // set the button text
                Text buttonText = button.transform.GetComponentInChildren<Text>();
                buttonText.text = list[i];
                buttons.Add(button.GetComponent<Button>());
            }
            return buttons;
        }

        void AdjustListTitlePosition(Transform title, int totalButtons, float buttonHeight)
        {
            if (title != null)
            {
                float totalHeight = totalButtons * buttonHeight;
                float y = (totalHeight / 2) + buttonHeight;
                title.localPosition = new Vector3(0, y, 0);
            }
            else
            {
                //Debug.Log("the title is null, can't adjust position");
            }
        }

        void OnEnable()
        {
            PanelManager.OnPanelFocusChanged += PanelFocusChanged;
            EventManager.StartListening("VRGuiHitChanged", VRGuiHitChanged);
        }

        void OnDisable()
        {
            PanelManager.OnPanelFocusChanged -= PanelFocusChanged;
            EventManager.StopListening("VRGuiHitChanged", VRGuiHitChanged);
        }

        void VRGuiHitChanged(string hitBool)
        {
            if (hitBool == "True")
            {
                if (vrGestureManager.state == VRGestureManagerState.ReadyToRecord)
                {
                    TogglePanelAlpha("Recording Menu", 1f);
                    TogglePanelInteractivity("Recording Menu", true);
                }
            }
            else if (hitBool == "False")
            {
                if (vrGestureManager.state == VRGestureManagerState.ReadyToRecord || vrGestureManager.state == VRGestureManagerState.Recording)
                {
                    TogglePanelAlpha("Recording Menu", .35f);
                    TogglePanelInteractivity("Recording Menu", false);
                }
            }
        }

        void TogglePanelAlpha(string panelName, float toAlpha)
        {
            CanvasRenderer[] canvasRenderers = vrHandUIPanel.GetComponentsInChildren<CanvasRenderer>();
            foreach (CanvasRenderer cr in canvasRenderers)
            {
                cr.SetAlpha(toAlpha);
                //float startAlpha = cr.GetAlpha();
                //StartCoroutine(TweenAlpha(cr, startAlpha, toAlpha, 1f));
            }
        }

        void TogglePanelInteractivity(string panelName, bool interactive)
        {
            Button[] buttons = vrHandUIPanel.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = interactive;
            }
        }

        void PanelFocusChanged(string panelName)
        {
            if (panelName == "Main Menu")
            {
                vrGestureManager.state = VRGestureManagerState.Idle;
            }
            if (panelName == "Select Neural Net Menu")
            {
                vrGestureManager.RefreshNeuralNetList();
                vrGestureManager.state = VRGestureManagerState.Idle;
            }
            if (panelName == "Record Menu")
            {
                vrGestureManager.state = VRGestureManagerState.Idle;
                GenerateRecordMenuButtons();
            }
            if (panelName == "Recording Menu")
            {
                //vrGestureManager.state = VRGestureManagerState.ReadyToRecord;
                //Not sure why this is in here. This re-introduced the sticky button bug.
            }
            if (panelName == "Edit Menu")
            {
                vrGestureManager.state = VRGestureManagerState.Edit;
                GenerateEditMenuButtons();
            }
            if (panelName == "Editing Menu")
            {
                vrGestureManager.state = VRGestureManagerState.Editing;
            }
        }

        void UpdateCurrentNeuralNetworkText()
        {
            if (GetCurrentNeuralNetworkText() == null)
                return;

            Text title = GetCurrentNeuralNetworkText();
            title.text = vrGestureManager.currentNeuralNet;
        }

        void UpdateNowRecordingStatus()
        {
            if (vrGestureManager.state == VRGestureManagerState.ReadyToRecord 
                || vrGestureManager.state == VRGestureManagerState.EnteringRecord)
            {
                nowRecordingBackground.color = Color.grey;
                nowRecordingLabel.text = "ready to record";
            }
            else if (vrGestureManager.state == VRGestureManagerState.Recording)
            {
                nowRecordingBackground.color = Color.red;
                nowRecordingLabel.text = "RECORDING";
            }
        }

        Text GetCurrentNeuralNetworkText()
        {
            // update current neural network name on each currentNeuralNetworkTitle UI thingy
            if (panelManager == null)
                return null;
            if (vrHandUIPanel == null)
                return null;
            if (vrHandUIPanel.Find(panelManager.currentPanel) == null)
                return null;
            Transform currentPanelParent = vrHandUIPanel.Find(panelManager.currentPanel);
            if (currentPanelParent == null)
                return null;
            Transform currentNeuralNetworkTitle = currentPanelParent.FindChild("Current Neural Network");
            if (currentNeuralNetworkTitle == null)
                return null;

            Text title = currentNeuralNetworkTitle.FindChild("neural network name").GetComponent<Text>();
            return title;
        }

        IEnumerator TweenAlpha(CanvasRenderer cr, float alphaFrom, float alphaTo, float time)
        {
            float timer = 0f;

            while (timer < time)
            {
                float percent = timer / time;
                float alpha = percent.Remap(0, 1, alphaFrom, alphaTo);
                cr.SetAlpha(alpha);
                timer += Time.fixedDeltaTime;
                yield return new WaitForSeconds(Time.fixedDeltaTime);
            }
        }
    }
}
