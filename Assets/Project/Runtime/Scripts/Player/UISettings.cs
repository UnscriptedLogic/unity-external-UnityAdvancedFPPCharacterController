using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[System.Serializable]
public class UISettings : MonoBehaviour
{
    public static UISettings Instance;
    public PlayerManager playerManager;
    public AudioMixer gameAudio;
    public Button saveButton;
    public bool savesInit = false;
    public Toggle[] toggles;
    public Toggle toggleCrouch;
    public Toggle invertYAxis;
    public Toggle reduceMotion;
    public Toggle moveCamera;
    public Toggle bobX;
    public Toggle bobY;
    public Toggle tilt;
    public Slider[] sliders;
    public Slider sensX;
    public Slider sensY;
    public Slider smoothing;
    public Slider volume;
    private void Awake() {
        Instance = this;
        saveButton.interactable = false;
        if(toggles.Length == 0)
        {
            toggles = new Toggle[7];
            toggles[0] = toggleCrouch;
            toggles[1] = invertYAxis;
            toggles[2] = reduceMotion;
            toggles[3] = moveCamera;
            toggles[4] = bobX;
            toggles[5] = bobY;
            toggles[6] = tilt;
        }

        if(sliders.Length == 0)
        {
            sliders = new Slider[4];
            sliders[0] = sensX;
            sliders[1] = sensY;
            sliders[2] = smoothing;
            sliders[3] = volume;
        }
    }

    private IEnumerator Start()
    {
        while (playerManager == null)
        {
            yield return new WaitForEndOfFrame();
        }
        StartCoroutine(LoadSettingsData());
        while (!savesInit)
        {
            yield return null;
        }
        InitializePlayerData();
        saveButton.interactable = false;
    }

    private void OnEnable()
    {
        saveButton.onClick.AddListener(() => ButtonCallback(saveButton));
        foreach (Toggle toggle in toggles)
        {
            toggle.onValueChanged.AddListener(delegate
                {
                    ToggleCallback(toggle);
                }
            );
        }

        foreach (Slider slider in sliders)
        {
            slider.onValueChanged.AddListener(delegate
                {
                    SliderCallback(slider, slider.value);
                }
            );
        }

        saveButton.interactable = false;
    }

    private void ButtonCallback(Button selectedButton)
    {
        if (selectedButton == saveButton)
        {
            InitializePlayerData();
            saveButton.interactable = false;
        }
    }

    Toggle savedToggle;
    private void ToggleCallback(Toggle selectedToggle)
    {
        if (selectedToggle != null) savedToggle = selectedToggle;
        if (savedToggle != null) saveButton.interactable = true;
    }

    Slider savedSlider;
    private void SliderCallback(Slider selectedSlider, float value)
    {
        if (selectedSlider != null) savedSlider = selectedSlider;
        if (savedSlider != null) saveButton.interactable = true;
    }

    public void SaveSettingsData()
    {
        SaveManager.SaveSettingsJson(this);
    }

    public IEnumerator LoadSettingsData()
    {
        SettingsData settingsData = SaveManager.LoadSettingsJson();
        bool[] dataToggles = new bool[toggles.Length];
            dataToggles[0] = settingsData.ToggleCrouch;
            dataToggles[1] = settingsData.InvertYAxis;
            dataToggles[2] = settingsData.ReduceMotion;
            dataToggles[3] = settingsData.MoveCamera;
            dataToggles[4] = settingsData.BobX;
            dataToggles[5] = settingsData.BobY;
            dataToggles[6] = settingsData.Tilt;
        float[] dataSliders = new float[sliders.Length];
            dataSliders[0] = settingsData.SensX;
            dataSliders[1] = settingsData.SensY;
            dataSliders[2] = settingsData.Smoothing;
            dataSliders[3] = settingsData.Volume;

        for (int i = 0; i < toggles.Length; i++)
        {
            while (toggles[i].isOn != dataToggles[i])
            {
                toggles[i].isOn = dataToggles[i];
                yield return null;
            }
        }

        for (int i = 0; i < sliders.Length; i++)
        {
            while (sliders[i].value != dataSliders[i])
            {
                sliders[i].value = dataSliders[i];
                yield return null;
            }
        }
        savesInit = true;
    }

    private void InitializePlayerData()
    {
        playerManager.playerMovement.toggleCrouch = toggleCrouch.isOn;
        playerManager.cameraManager.invertYAxis = invertYAxis.isOn;
        playerManager.cameraManager.reduceMotion = reduceMotion.isOn;
        playerManager.cameraManager.moveCamera = moveCamera.isOn;
        playerManager.cameraManager.moveBobX = bobX.isOn;
        playerManager.cameraManager.moveBobY = bobY.isOn;
        playerManager.cameraManager.tilt = tilt.isOn;
        playerManager.cameraManager.sensX = sensX.value;
        playerManager.cameraManager.sensY = sensY.value;
        playerManager.cameraController.damp = smoothing.value;
        gameAudio.SetFloat("master", volume.value);
    }
}

[System.Serializable]
public class SettingsData
{
    public bool ToggleCrouch { get { return toggleCrouch; } set { toggleCrouch = value; } }
    public bool toggleCrouch;
    public bool InvertYAxis { get { return invertYAxis; } set { invertYAxis = value; } }
    public bool invertYAxis;
    public bool ReduceMotion { get { return reduceMotion; } set { reduceMotion = value; } }
    public bool reduceMotion;
    public bool MoveCamera { get { return moveCamera; } set { moveCamera = value; } }
    public bool moveCamera;
    public bool BobX { get { return bobX; } set { bobX = value; } }
    public bool bobX;
    public bool BobY { get { return bobY; } set { bobY = value; } }
    public bool bobY;
    public bool Tilt { get { return tilt; } set { tilt = value; } }
    public bool tilt;
    public float SensX { get { return sensX; } set { sensX = value; } }
    public float sensX;
    public float SensY { get { return sensY; } set { sensY = value; } }
    public float sensY;
    public float Smoothing { get { return smoothing; } set { smoothing = value; } }
    public float smoothing;
    public float Volume { get { return volume; } set { volume = value; } }
    public float volume;

    public static SettingsData FromDefault ()
    {
        return new SettingsData
        {
            ToggleCrouch = true,
            InvertYAxis = false,
            ReduceMotion = false,
            MoveCamera = true,
            BobX = true,
            BobY = true,
            Tilt = true,
            SensX = 8.0f,
            SensY = 8.0f,
            Smoothing = 20f,
            Volume = 1.0f
        };
    }

    public static SettingsData FromSettings (UISettings uISettings)
    {
        return new SettingsData
        {
            ToggleCrouch = uISettings.toggleCrouch.isOn,
            InvertYAxis = uISettings.invertYAxis.isOn,
            ReduceMotion = uISettings.reduceMotion.isOn,
            MoveCamera = uISettings.moveCamera.isOn,
            BobX = uISettings.bobX.isOn,
            BobY = uISettings.bobY.isOn,
            Tilt = uISettings.tilt.isOn,
            SensX = uISettings.sensX.value,
            SensY = uISettings.sensY.value,
            Smoothing = uISettings.smoothing.value,
            Volume = uISettings.volume.value
        };
    }
}