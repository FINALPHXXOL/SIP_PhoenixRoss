using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class SoundController : MonoBehaviour
{
    public AudioSettings[] audioSettings;
    public Canvas canvas;

    private void Start()
    {
        foreach (var settings in audioSettings)
        {
            settings.mixerGroup = settings.audioSource.outputAudioMixerGroup;
            settings.maxDistance = settings.audioSource.maxDistance;
            settings.originalColor = settings.image.color;
            if (settings.audioSource != null && settings.audioSource.clip != null)
            {
                settings.name = settings.audioSource.clip.name;
            }

            if (settings.audioSource == null)
            {
                Debug.LogError("AudioSource is not assigned for: " + settings.name);
                continue;
            }

            if (settings.image == null)
            {
                Debug.LogError("Image is not assigned for: " + settings.name);
                continue;
            }

            if (settings.mixerGroup == null)
            {
                Debug.LogError("AudioMixerGroup is not assigned for: " + settings.name);
                continue;
            }

            settings.activeImage = Instantiate(settings.image, canvas.transform);
            settings.imageSize = settings.activeImage.rectTransform.sizeDelta;

        }
    }

    private void Update()
    {
        foreach (var settings in audioSettings)
        {
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
                // Instantiate the image prefab
                settings.activeImage = Instantiate(settings.image, canvas.transform);
                settings.imageSize = settings.activeImage.rectTransform.sizeDelta;

            }
            if (settings.audioSource != null && settings.activeImage != null)
            {
                if (settings.calculateDistanceOpacity == true)
                {
                    CalculateOpacityBasedOnDistance(settings);
                } else
                {
                    CalculateOpacityBasedOnVolume(settings);
                }
                if (settings.calculateImageSizeBasedOnVolume == true)
                {
                    CalculateImageSizeBasedOnVolume(settings);
                } else
                {
                    CalculateImageSizeBasedOnDistance(settings);
                }
                WaypointIndicator(settings);
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

        Vector2 pos = Camera.main.WorldToScreenPoint(settings.audioSource.transform.position + settings.offset);

        if (Vector3.Dot((settings.audioSource.transform.position - Camera.main.transform.position), Camera.main.transform.TransformDirection(Vector3.forward)) < 0) 
        {
            //print(settings.name + " " + pos.x);
            //print("Screen Width" + Screen.width);
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
    public AudioSource audioSource;
    public Image image;
    public AudioMixerGroup mixerGroup;
    public Vector3 offset;
    public float minOpacity = 0.2f;
    public float maxOpacity = 1.0f;
    public float maxDistance;
    public float rmsMultiplier = 50f;
    public float maxSize = 200f;
    public float minSize = 100f;
    public bool calculateDistanceOpacity;
    public bool calculateImageSizeBasedOnVolume;
    [HideInInspector]
    public Color originalColor;
    [HideInInspector]
    public Image activeImage;
    [HideInInspector]
    public Vector2 imageSize;
}