using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundVisual : MonoBehaviour
{
    private const int SAMPLE_SIZE = 1024;

    [Header("Basic Setup")]
    public bool IsCircle = true;
    public bool TurnOnHighlightParticle = false;
    public bool DebugMode = false;

    [Header("Highlight Particle")]
    public GameObject High = null; // Game Object need it?
    public float HighStartTime = 0.0f;
    public float HighEndTime = 0.0f;

    [Header("Value")]
    public float rmsValue;
    public float dbValue;

    [Header("Bass Setup")]
    public GameObject CirclePlane;
    public float CircleBassPercentage = 0.1f;
    public float CircleBassControl = 0.2f;
    public float BassMin = 10.0f;
    public float BassMax = 18.0f;

    [Header("BackGround Setup")]
    public float backgroundIntensity;
    public Material backgroundMaterial;
    public Color minColor;
    public Color maxColor;

    [Header("Visual Setup")]
    public float maxVisualScale = 25.0f;
    public float visualModifier = 50.0f;
    public float smoothSpeed = 10.0f;
    public float keepPercentage = 0.1f;

    private AudioSource source;
    private float[] samples;
    private float[] spectrum;
    private float sampleRate;

    private Transform[] visualList;
    private float[] visualScale;
    public int amnVisual = 100;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        samples = new float[1024];
        spectrum = new float[1024];
        sampleRate = AudioSettings.outputSampleRate;

        if (IsCircle)
        {
            SpawnCircle();
            SpawnCirclePlane();
            if (DebugMode)
                _Logger(0, "SpawnCircle, SpawnCirclePlane Done");
        }
        else
        {
            SpawnLine();
            if (DebugMode)
                _Logger(0, "SpawnLine Done");
        }
    }
    private void SpawnLine()
    {
        visualScale = new float[amnVisual];
        visualList = new Transform[amnVisual];

        for (int i = 0; i < amnVisual; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualList[i] = go.transform;
            visualList[i].position = Vector3.right * i;
        }
    }
    private void SpawnCircle()
    {
        visualScale = new float[amnVisual];
        visualList = new Transform[amnVisual];

        Vector3 center = Vector3.zero;
        float radius = 10.0f;

        for (int i = 0; i < amnVisual; i++)
        {
            float ang = i * 1.0f / amnVisual;
            ang = ang * Mathf.PI * 2;

            float x = center.x + Mathf.Cos(ang) * radius;
            float y = center.y + Mathf.Sin(ang) * radius;

            Vector3 pos = center + new Vector3(x, y, 0);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, pos);
            visualList[i] = go.transform;
        }
    }
    private void Update()
    {
        AnalyzeSound();
        UpdateBackground();
        UpdateVisual();
        if(TurnOnHighlightParticle)
        {
            HighlightRain();
        }
        if(IsCircle)
        {
            CirclePlaneBassKick();
        }
        else
        {
            _Logger(2, "undefined Error"); // never see this error lol :) 
            // Just How to Use _Logger Example
        }
        //Debug.Log(Time.timeSinceLevelLoad);
    }
    private void _Logger(int Log,string LogMsg)
    {
        switch(Log)
        {
            case 0:
                Debug.Log(LogMsg);
                break;
            case 1:
                Debug.LogWarning(LogMsg);
                break;
            case 2:
                Debug.LogError(LogMsg);
                break;
        }
    }
    private void SpawnCirclePlane()
    {
        Vector3 center = Vector3.zero;
        
        CirclePlane.transform.position = new Vector3(0, 0, -0.4f);
        CirclePlane.transform.localScale = new Vector3(20, 20, 1);
    }
    private void CirclePlaneBassKick()
    {
        Vector3 oldpos = new Vector3(0, 0, -0.4f);
        Vector3 newpos = new Vector3(0, 0, dbValue * -CircleBassPercentage + CircleBassControl);
        Vector3 nowPOS = CirclePlane.transform.position;
        if (dbValue > BassMin)
        {
            //CirclePlane.transform.localScale = Vector3.Lerp(oldscale, newscale, smoothSpeed + dbValue);
            CirclePlane.transform.position = Vector3.Lerp(oldpos, newpos, 1);
            //Debug.Log("Beat!");
        }
        else if (dbValue < BassMin)
        {
            CirclePlane.transform.position = Vector3.Lerp(nowPOS, oldpos, 0.5f);
        }
        if (CirclePlane.transform.position.z > -0.4f || CirclePlane.transform.position.z < -25.0f)
        {
            //CirclePlane.transform.localScale = Vector3.Lerp(newscale, oldscale, smoothSpeed); ;
            CirclePlane.transform.position = Vector3.Lerp(nowPOS, oldpos, smoothSpeed);
            //Debug.Log("less then Min Value");
        } 
    }
    private void HighlightRain()
    {
        bool chk = false;
        if (HighStartTime < Time.timeSinceLevelLoad && chk != true)
        {
            High.SetActive(true);
            chk = true;
        }
        if (HighEndTime < Time.timeSinceLevelLoad)
        {
            High.SetActive(false);
        }
    }
    private void UpdateVisual()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        int averageSize = (int)(( SAMPLE_SIZE * keepPercentage ) / amnVisual);

        while(visualIndex < amnVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }
            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * smoothSpeed;
            if(visualScale[visualIndex] < scaleY)
               visualScale[visualIndex] = scaleY;

            if (visualScale[visualIndex] > maxVisualScale)
                visualScale[visualIndex] = maxVisualScale;

            visualList[visualIndex].localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            visualIndex++;
        }
    }
    private void UpdateBackground()
    {
        backgroundIntensity -= Time.deltaTime * smoothSpeed;
        if (backgroundIntensity < dbValue / 40)
            backgroundIntensity = dbValue / 40;

        backgroundMaterial.color = Color.Lerp(maxColor, minColor, backgroundIntensity);
    }
    private void AnalyzeSound()
    {
        source.GetOutputData(samples, 0);

        //Get the RMS(root mean square)
        int i = 0;
        float sum = 0;
        for (; i < SAMPLE_SIZE; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        //get DB value
        dbValue = 20 * Mathf.Log10(rmsValue / 0.1f);

        //get sound spectrum
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
    }
}
