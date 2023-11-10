using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace AudioVisualCues
{
    public class ReadOnlyAttribute : PropertyAttribute { }
    public class SoundController : MonoBehaviour
    {
        public Canvas mainCanvas;
        public Camera mainCamera;
        public bool enableController = true;
        public List<MixerGroupData> mixerGroupsData;
        public AudioSettings[] audioSettings;

        private void Start()
        {

        }

        private void Update()
        {
            if (enableController == true && mainCanvas != null)
            {
                foreach (var settings in audioSettings)
                {
                    if (settings.audioSource != null && settings.image)
                    {
                        if (settings.hasAudioSourceImage == false)
                        {
                            InitiateAudioSource(settings);
                            settings.hasAudioSourceImage = true;
                        }
                        AudioListener nearestListener = FindNearestAudioListener();
                        float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position);
                        bool isWithinMaxDistance = distance < settings.maxDistance;
                        if (settings.activeImage != null && (!isWithinMaxDistance || !settings.audioSource.isPlaying || settings.audioSource.mute == true))
                        {
                            // Deactivate the image
                            Destroy(settings.activeImage.gameObject);
                        }
                        else if (settings.activeImage == null && (isWithinMaxDistance && settings.audioSource.isPlaying && settings.audioSource.mute == false))
                        {
                            //Instantiate the image prefab
                            settings.activeImage = Instantiate(settings.image, mainCanvas.transform);

                        }
                        if (settings.audioSource != null && settings.activeImage != null)
                        {
                            if (settings.calculateOpacityBasedOnDistance == true)
                            {
                                CalculateOpacityBasedOnDistance(settings);
                            }
                            else
                            {
                                CalculateOpacityBasedOnVolume(settings);
                            }
                            if (settings.calculateImageSizeBasedOnVolume == true)
                            {
                                CalculateImageSizeBasedOnVolume(settings);
                            }
                            else
                            {
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

        private void InitiateAudioSource(AudioSettings settings)
        {
            if (settings.nameOverride == false)
            {
                if (settings.audioSource.clip != null)
                {
                    settings.name = settings.audioSource.clip.name;
                }

            }

            if (settings.audioSource.outputAudioMixerGroup != null)
            {
                settings.mixerGroup = settings.audioSource.outputAudioMixerGroup;
            }

            if (settings.audioSource != null)
            {
                settings.maxDistance = settings.audioSource.maxDistance;
            }

            if (settings.image != null)
            {
                settings.originalColor = settings.image.color;
            }

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
            float[] samples = new float[256];
            AudioListener.GetOutputData(samples, 1); // Use channel 1 (1-based index) for the custom mixer group

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            float rms = Mathf.Sqrt(sum / samples.Length);
            float decibels = 20f * Mathf.Log10(rms * settings.rmsMultiplier);

            float opacity = Mathf.Lerp(settings.minOpacity, settings.maxOpacity, Mathf.InverseLerp(-80f, 0f, decibels));
            Color newColor = new Color(settings.originalColor.r, settings.originalColor.g, settings.originalColor.b, opacity);
            settings.activeImage.color = newColor;
        }

        private void CalculateOpacityBasedOnDistance(AudioSettings settings)
        {
            AudioListener nearestListener = FindNearestAudioListener();

            float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position);

            // Calculate opacity based on distance
            float opacityMultiplier = Mathf.InverseLerp(settings.maxDistance, 0f, distance);
            float opacity = Mathf.Lerp(settings.minOpacity, settings.maxOpacity, opacityMultiplier);

            Color newColor = new Color(settings.originalColor.r, settings.originalColor.g, settings.originalColor.b, opacity);
            settings.activeImage.color = newColor;
        }

        private void CalculateImageSizeBasedOnVolume(AudioSettings settings)
        {
            float[] samples = new float[256];
            AudioListener.GetOutputData(samples, 1); // Use channel 1 (1-based index) for the custom mixer group

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            float rms = Mathf.Sqrt(sum / samples.Length);
            float decibels = 20f * Mathf.Log10(rms * settings.rmsMultiplier);

            float imageSize = Mathf.Lerp(settings.minSize, settings.maxSize, Mathf.InverseLerp(-80f, 0f, decibels));
            settings.activeImage.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);
        }

        private void CalculateImageSizeBasedOnDistance(AudioSettings settings)
        {
            AudioListener nearestListener = FindNearestAudioListener();
            if (nearestListener != null)
            {
                float distance = Vector3.Distance(settings.audioSource.transform.position, nearestListener.transform.position);

                float imageSize = Mathf.Lerp(settings.maxSize, settings.minSize, 1 - Mathf.InverseLerp(settings.maxDistance, 0f, distance));
                settings.activeImage.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);
            }
        }

        private void WaypointIndicator(AudioSettings settings)
        {
            float minX = settings.activeImage.GetPixelAdjustedRect().width / 2;
            float maxX = Screen.width - minX;
            float minY = settings.activeImage.GetPixelAdjustedRect().height / 2;
            float maxY = Screen.height - minY;
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

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
        public string name; // Name for your reference
        public bool nameOverride;
        public AudioSource audioSource;
        [ReadOnly]
        public AudioMixerGroup mixerGroup;
        public bool useMixerGroupData;

        public Image image;
        public bool mImageOverride;
        public Vector3 offset;
        public bool mOffsetOverride;
        public float minOpacity = 0.2f;
        public bool mMinOpacityOverride;
        public float maxOpacity = 1.0f;
        public bool mMaxOpacityOverride;
        public float rmsMultiplier = 50f;
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
        public Image image;
        public Vector3 offset;
        public float minOpacity = 0.2f;
        public float maxOpacity = 1.0f;
        public float rmsMultiplier = 50f;
        public float maxSize = 200f;
        public float minSize = 100f;
        public bool calculateOpacityBasedOnDistance;
        public bool calculateImageSizeBasedOnVolume;
    }
}