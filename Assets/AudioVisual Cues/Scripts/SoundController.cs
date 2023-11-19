using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace AudioVisualCues
{
    public class ReadOnlyAttribute : PropertyAttribute { } //Attribute for ReadOnly variables inside Inspector
    public class SoundController : MonoBehaviour
    {
        public Canvas mainCanvas; //Canvas that will be drawn on
        public Camera mainCamera; //Camera used to track orientation with AudioSources
        public bool enableController = true; //true for running script processes | false to stop script processes
        public MixerGroupData[] mixerGroupsData; //array of Mixer Groups
        public AudioSettings[] audioSettings; //array of Audio Settings

        private void Start()
        {

        }

        private void Update()
        {
            if (enableController == true && mainCanvas != null && mainCamera != null) 
            {
                foreach (var settings in audioSettings) 
                {
                    if (settings.enableSetting == true) 
                    {
                        if (settings.audioSource != null && settings.image != null) 
                        {
                            if (settings.hasAudioSourceImage == false)
                            {
                                //Initiate Audio Setting with inputted values
                                InitiateAudioSource(settings);
                                settings.hasAudioSourceImage = true;
                            }

                            AudioListener nearestListener = FindNearestAudioListener(); // Finds nearest AudioListener (only one AudioListener allowed per scene)
                            float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position);
                            bool isWithinMaxDistance = distance < settings.maxDistance;

                            // Handles if sound is being played into AudioListener or not.
                            if (settings.activeImage == null && (isWithinMaxDistance && settings.audioSource.isPlaying && !settings.audioSource.mute))
                            {
                                //Instantiate the image prefab
                                settings.activeImage = Instantiate(settings.image, mainCanvas.transform);
                            }
                            else if ((settings.activeImage != null && (!isWithinMaxDistance || !settings.audioSource.isPlaying || settings.audioSource.mute)))
                            {
                                // Deactivate the image
                                Destroy(settings.activeImage.gameObject);
                            }

                            // Runs processes for how images are drawn on the canvas.
                            if (settings.audioSource != null && settings.activeImage != null)
                            {
                                if (settings.calculateOpacityBasedOnDistance == true)
                                {
                                    // Process for changing the opacity on the image based on distance
                                    CalculateOpacityBasedOnDistance(settings);
                                }
                                else
                                {
                                    // Process for changing the opacity based on the volume that's playing
                                    CalculateOpacityBasedOnVolume(settings);
                                }
                                if (settings.calculateImageSizeBasedOnVolume == true)
                                {
                                    // Process for calculating the image size based on volume
                                    CalculateImageSizeBasedOnVolume(settings);
                                }
                                else
                                {
                                    // Process for calculating the image size based on distance
                                    CalculateImageSizeBasedOnDistance(settings);
                                }
                                WaypointIndicator(settings);
                            }
                        }
                        else
                        {
                            settings.hasAudioSourceImage = false;
                        }
                    }
                    else
                    {
                        if (settings.activeImage != null)
                        {
                            // Deactivate the image
                            Destroy(settings.activeImage.gameObject);
                        }
                    }
                }
            }
            else
            {
                foreach (var settings in audioSettings)
                {
                    if (settings.activeImage != null)
                    {
                        // Deactivate the image
                        Destroy(settings.activeImage.gameObject);
                    }
                }
            }
        }

        // Reinitiates entire controller
        public void RedrawController()
        {
            foreach (var settings in audioSettings)
            {
                if (settings.audioSource != null && settings.image)
                {
                    InitiateAudioSource(settings);
                    settings.hasAudioSourceImage = true;
                }
            }
        }

        // Reinitiates a single setting
        public void RedrawSetting(AudioSettings settings)
        {
            if (settings.audioSource != null && settings.image != null)
            {
                InitiateAudioSource(settings);
                settings.hasAudioSourceImage = true;
            }
        }

        // Assigns values to an Audio Setting
        private void InitiateAudioSource(AudioSettings settings)
        {
            if (settings.activeImage != null)
            {
                // Deactivate the image
                Destroy(settings.activeImage.gameObject);
            }

            if (string.IsNullOrEmpty(settings.name))
            {
                // Sets name to clip name or audioSource name if the Audio Setting name is blank.
                if (settings.audioSource.clip != null)
                {
                    settings.name = settings.audioSource.clip.name;
                }
                else
                {
                    settings.name = settings.audioSource.name;
                }
                
            }

            if (settings.audioSource.outputAudioMixerGroup != null)
            {
                // assigns mixer group from audioSources mixer group
                if (settings.audioSource.outputAudioMixerGroup != null)
                {
                    settings.mixerGroup = settings.audioSource.outputAudioMixerGroup;
                }
            }

            if (settings.audioSource != null)
            {
                // sets maxDistance value from audioSource
                if (settings.audioSource != null)
                {
                    settings.maxDistance = settings.audioSource.maxDistance;
                }
            }

            if (settings.image != null)
            {
                // saves original image color
                settings.originalColor = settings.image.color;
            }

            // assigns false for every override if use of mixer group data isn't true or if mixerGroup isn't assigned.
            if (settings.useMixerGroupData == false || settings.mixerGroup == null)
            {
                settings.mImageOverride = false;
                settings.mOffsetOverride = false;
                settings.mMinOpacityOverride = false;
                settings.mMaxOpacityOverride = false;
                settings.mRmsMultiplierOverride = false;
                settings.mMaxSizeOverride = false;
                settings.mMinSizeOverride = false;
                settings.mCalculateOpacityBasedOnDistanceOverride = false;
                settings.mCalculateImageSizeBasedOnVolumeOverride = false;
            } 
            else if (settings.useMixerGroupData == true)
            {
                // runs process for each audio setting for each override if checked true or false.
                foreach (var mixer in mixerGroupsData)
                {
                    if (settings.mixerGroup == mixer.mixerGroup)
                    {
                        if (settings.mImageOverride == false)
                        {
                            settings.image = mixer.image;
                        }

                        if (settings.mOffsetOverride == false)
                        {
                            settings.offset = mixer.offset;
                        }

                        if (settings.mMinOpacityOverride == false)
                        {
                            settings.minOpacity = mixer.minOpacity;
                        }

                        if (settings.mMaxOpacityOverride == false)
                        {
                            settings.maxOpacity = mixer.maxOpacity;
                        }

                        if (settings.mRmsMultiplierOverride == false)
                        {
                            settings.rmsMultiplier = mixer.rmsMultiplier;
                        }

                        if (settings.mMaxSizeOverride == false)
                        {
                            settings.maxSize = mixer.maxSize;
                        }

                        if (settings.mMinSizeOverride == false)
                        {
                            settings.minSize = mixer.minSize;
                        }

                        if (settings.mCalculateOpacityBasedOnDistanceOverride == false)
                        {
                            settings.calculateOpacityBasedOnDistance = mixer.calculateOpacityBasedOnDistance;
                        }

                        if (settings.mCalculateImageSizeBasedOnVolumeOverride == false)
                        {
                            settings.calculateImageSizeBasedOnVolume = mixer.calculateImageSizeBasedOnVolume;
                        }

                    }
                }
            }
        }

        private void CalculateOpacityBasedOnVolume(AudioSettings settings)
        {
            float[] samples = new float[256]; // ranges from values 0 - 256
            settings.audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris); // BlackmanHarris accurately outputs volume from audioSource

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            float rms = Mathf.Sqrt(sum / samples.Length);
            float decibels = 20f * Mathf.Log10(rms * settings.rmsMultiplier); // assigns decibels based on rms sensitivity

            float opacity = Mathf.Lerp(settings.minOpacity, settings.maxOpacity, Mathf.InverseLerp(-80f, 0f, decibels)); // returns opacity from Lerp of minOpacity, maxOpacity, InLerp of decibels
            Color newColor = new Color(settings.originalColor.r, settings.originalColor.g, settings.originalColor.b, opacity);
            settings.activeImage.color = newColor; // activeImage's opacity changes to newColor opacity
        }

        private void CalculateOpacityBasedOnDistance(AudioSettings settings)
        {
            AudioListener nearestListener = FindNearestAudioListener();
            if (nearestListener != null)
            {
                float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position); // distance is based on nearest AudioListener

                // Calculate opacity based on distance
                float opacityMultiplier = Mathf.InverseLerp(settings.maxDistance, 0f, distance);
                float opacity = Mathf.Lerp(settings.minOpacity, settings.maxOpacity, opacityMultiplier); // returns opacity from Lerp of minOpacity, maxOpacity, and distance

                Color newColor = new Color(settings.originalColor.r, settings.originalColor.g, settings.originalColor.b, opacity);
                settings.activeImage.color = newColor; // activeImage's opacity changes to newColor opacity
            } 
        }

        private void CalculateImageSizeBasedOnVolume(AudioSettings settings)
        {
            float[] samples = new float[256]; // ranges from values 0 - 256
            settings.audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris); // Use BlackmanHarris for better results

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            float rms = Mathf.Sqrt(sum / samples.Length);
            float decibels = 20f * Mathf.Log10(rms * settings.rmsMultiplier); // assigns decibels based on rms sensitvity

            float imageSize = Mathf.Lerp(settings.minSize, settings.maxSize, Mathf.InverseLerp(-80f, 0f, decibels)); // returns imageSize from Lerp of minSize, maxSize, and decibels
            settings.activeImage.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);  // activeImage's size changes to new imageSize
        }

        private void CalculateImageSizeBasedOnDistance(AudioSettings settings)
        {
            AudioListener nearestListener = FindNearestAudioListener();
            if (nearestListener != null)
            {
                float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position);

                float imageSize = Mathf.Lerp(settings.maxSize, settings.minSize, 1 - Mathf.InverseLerp(settings.maxDistance, 0f, distance)); // returns imageSize from Lerp of maxSize, minSize, and InLerp of distance
                settings.activeImage.rectTransform.sizeDelta = new Vector2(imageSize, imageSize); // activeImage's size changes to new imageSize
            }
        }

        // Process for changing position of image on screen based on audioSource's position in the world in relation to the mainCamera
        private void WaypointIndicator(AudioSettings settings)
        {
            float minX = settings.activeImage.GetPixelAdjustedRect().width / 2;
            float maxX = Screen.width - minX;
            float minY = settings.activeImage.GetPixelAdjustedRect().height / 2;
            float maxY = Screen.height - minY;

            Vector2 pos = mainCamera.WorldToScreenPoint(settings.audioSource.transform.position + settings.offset);

            if (Vector3.Dot((settings.audioSource.transform.position - mainCamera.transform.position), mainCamera.transform.TransformDirection(Vector3.forward)) < 0)
            {
                //Source is behind player
                if (pos.x < Screen.width / 2)
                {
                    pos.x = maxX;
                }
                else
                {
                    pos.x = minX;
                }
            }

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            settings.activeImage.transform.position = pos;
        }

        // Process for finding the first available AudioListener in the world.
        private AudioListener FindNearestAudioListener()
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length == 0)
            {
                Debug.LogWarning("No AudioListener found in the scene.");
                return null;
            }

            return listeners[0];
        }
    }

    [System.Serializable]
    public class AudioSettings
    {
        public bool enableSetting = true; // sets if Audio Setting should be activated.
        public string name; // Name for your reference
        public AudioSource audioSource;
        [ReadOnly]
        public AudioMixerGroup mixerGroup; //mixerGroup assigned from audioSource
        public bool useMixerGroupData; // sets if MixerGroupData is used onto AudioSetting (is ignored if mixerGroup isn't present)

        public Image image; // image is visual cue you'd like to be used.
        public bool mImageOverride; // every mOverride will ignore settings from MixerGroupData if set to true
        public Vector3 offset; // changes offset where image is draw from the audioSource location
        public bool mOffsetOverride;
        public float minOpacity = 0.2f;
        public bool mMinOpacityOverride;
        public float maxOpacity = 1.0f;
        public bool mMaxOpacityOverride;
        public float rmsMultiplier = 50f; /* sets sensitivity of volume. The higher it's set, the higher the decibel value is.
        This may need to be tweaked independently for each sound used. */
        public bool mRmsMultiplierOverride;
        public float maxSize = 200f;
        public bool mMaxSizeOverride;
        public float minSize = 100f;
        public bool mMinSizeOverride;
        public bool calculateOpacityBasedOnDistance;
        public bool mCalculateOpacityBasedOnDistanceOverride;
        public bool calculateImageSizeBasedOnVolume;
        public bool mCalculateImageSizeBasedOnVolumeOverride;
        [HideInInspector]
        public Color originalColor;
        [HideInInspector]
        public Image activeImage;
        [HideInInspector]
        public bool hasAudioSourceImage = false;
        [HideInInspector]
        public float maxDistance;
    }

    [System.Serializable]
    public class MixerGroupData
    {
        public AudioMixerGroup mixerGroup;
        public Image image; // image is visual cue you'd like to be used.
        public Vector3 offset; // changes offset where image is draw from the audioSource location
        public float minOpacity = 0.2f;
        public float maxOpacity = 1.0f;
        public float rmsMultiplier = 50f;
        public float maxSize = 200f;
        public float minSize = 100f;
        public bool calculateOpacityBasedOnDistance;
        public bool calculateImageSizeBasedOnVolume;
    }
}