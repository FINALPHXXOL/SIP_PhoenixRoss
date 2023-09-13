using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class SoundController : MonoBehaviour
{
    public AudioSource audioSource;
    public Image image;
    public AudioMixerGroup mixerGroup; // Reference to the custom AudioMixerGroup
    public Vector3 offset;
    public float minOpacity = 0.2f;
    public float maxOpacity = 1.0f;
    public float maxDistance;
    public float rmsMultiplier = 50f;
    public float maxSize = 200f; // Maximum size the image can reach when the AudioSource is very close
    public float minSize = 100f; // Minimum size the image will have when the AudioSource is far away
    public float sizeExponent = 2.0f;

    private Color originalColor;

    private void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is not assigned!");
            return;
        }

        if (image == null)
        {
            Debug.LogError("Image is not assigned!");
            return;
        }

        if (mixerGroup == null)
        {
            Debug.LogError("AudioMixerGroup is not assigned!");
            return;
        }

        // Assign the custom mixer group to the AudioSource
        audioSource.outputAudioMixerGroup = mixerGroup;
        maxDistance = audioSource.maxDistance;

        originalColor = image.color;
    }

    private void Update()
    {
        CalculateOpacity();
        CalculateImageSizeBasedOnDistance();
        WaypointIndicator();
    }
    private AudioListener FindNearestAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length == 0)
        {
            Debug.LogWarning("No AudioListener found in the scene.");
            return null;
        }

        // Unity allows only one active AudioListener, so just return the first one in the list
        return listeners[0];
    }
    private void CalculateImageSizeBasedOnDistance()
    {
        // Find the nearest AudioListener and calculate the distance between AudioSource and it
        AudioListener nearestListener = FindNearestAudioListener();
        if (nearestListener != null)
        {
            float distance = Vector3.Distance(audioSource.transform.position, nearestListener.transform.position);
            Debug.Log("Distance: " + distance);

            // Map distance to image size range
            float imageSize = Mathf.Lerp(maxSize, minSize, 1 - Mathf.InverseLerp(maxDistance, 0f, distance));
            Debug.Log("Calculated Image Size: " + imageSize);

            image.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);
        }
    }

    private void CalculateOpacity()
    {
        #region decibels
        float[] samples = new float[256];
        AudioListener.GetOutputData(samples, 1); // Use channel 1 (1-based index) for the custom mixer group

        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += sample * sample;
        }

        float rms = Mathf.Sqrt(sum / samples.Length);
        float decibels = 20f * Mathf.Log10(rms * rmsMultiplier);

        // Map decibels to opacity range
        float opacity = Mathf.Lerp(minOpacity, maxOpacity, Mathf.InverseLerp(-80f, 0f, decibels));
        Color newColor = new Color(originalColor.r, originalColor.g, originalColor.b, opacity);
        image.color = newColor;
        #endregion decibels
    }

    private void WaypointIndicator()
    {
        
        float minX = image.GetPixelAdjustedRect().width / 2;
        float maxX = Screen.width - minX;

        float minY = image.GetPixelAdjustedRect().height / 2;
        float maxY = Screen.height - minY;

        Vector2 pos = Camera.main.WorldToScreenPoint(audioSource.transform.position + offset);

        if(Vector3.Dot((audioSource.transform.position - transform.position), transform.forward) < 0)
        {
            // Target is behind the player
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

        image.transform.position = pos;
        
    }
}