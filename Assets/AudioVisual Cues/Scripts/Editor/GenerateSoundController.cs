using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using AudioVisualCues;

namespace AudioVisualCues
{
    public class GenerateSoundController : ScriptableWizard
    {
        public string objectName;           //Optional name that can given to created plane gameobject
        public Camera mainCamera;
        public Canvas mainCanvas;

        static Camera cam;
        static Camera lastUsedCam;

        [MenuItem("GameObject/AudioVisual Cues/Generate Sound Controller...")]
        static void CreateWizard()
        {
            cam = Camera.current;
            // Hack because camera.current doesn't return editor camera if scene view doesn't have focus
            if (!cam)
            {
                cam = lastUsedCam;
            }
            else
            {
                lastUsedCam = cam;
            }

            //Open Wizard
            DisplayWizard("Generate Sound Controller", typeof(GenerateSoundController));
        }

        void OnWizardUpdate()
        {

        }

        private void OnWizardCreate()
        {
            if (mainCanvas == null)
            {
                // Display a dialog informing the user that they need to assign mainCanvas
                EditorUtility.DisplayDialog("Canvas Not Assigned", "Please assign a Canvas before creating the Sound Controller.", "OK");
                return; // Do not proceed further
            }

            //Create an empty gamobject
            GameObject soundController = new GameObject();

            //If user hasn't assigned a name, by default object name is 'SoundController'
            if (string.IsNullOrEmpty(objectName))
            {
                soundController.name = "SoundController";
            }
            else
            {
                soundController.name = objectName;
            }

            //Add SoundController as component
            SoundController sController = soundController.AddComponent<SoundController>();
            sController.mainCamera = mainCamera;
            sController.mainCanvas = mainCanvas;
            Selection.activeObject = soundController;
        }
    }
}