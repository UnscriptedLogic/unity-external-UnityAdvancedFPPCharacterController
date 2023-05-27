using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveManager
{
    public static readonly string path = Path.Combine(Application.persistentDataPath + "/settings.fps");
    public static readonly string json = Path.Combine(Application.persistentDataPath + "/settings.json");
    public static void SaveSettings (UISettings uISettings)
    {
        FileStream stream = new FileStream(path, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(stream);

        SettingsData settingsData = SettingsData.FromSettings(uISettings);
        writer.Write(settingsData.ToggleCrouch);
        writer.Write(settingsData.InvertYAxis);
        writer.Write(settingsData.ReduceMotion);
        writer.Write(settingsData.MoveCamera);
        writer.Write(settingsData.BobX);
        writer.Write(settingsData.BobY);
        writer.Write(settingsData.SensX);
        writer.Write(settingsData.SensY);
        writer.Write(settingsData.Smoothing);
        writer.Write(settingsData.Volume);
        
        stream.Close();
    }

    public static SettingsData LoadSettings ()
    {
        if (File.Exists(path))
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);

            SettingsData settingsData = new SettingsData();
            settingsData.ToggleCrouch = reader.ReadBoolean();
            settingsData.InvertYAxis = reader.ReadBoolean();
            settingsData.ReduceMotion = reader.ReadBoolean();
            settingsData.MoveCamera = reader.ReadBoolean();
            settingsData.BobX = reader.ReadBoolean();
            settingsData.BobY = reader.ReadBoolean();
            settingsData.SensX = reader.ReadSingle();
            settingsData.SensY = reader.ReadSingle();
            settingsData.Smoothing = reader.ReadSingle();
            settingsData.Volume = reader.ReadSingle();
            stream.Close();

            return settingsData;
        } else
        {
            return null;
        }
    }

    public static void SaveSettingsJson (UISettings uISettings)
    {
        SettingsData settingsData = SettingsData.FromSettings(uISettings);
        string jsonIn = JsonUtility.ToJson(settingsData);

        File.WriteAllText(json, jsonIn);
    }

    public static SettingsData LoadSettingsJson ()
    {
        if (File.Exists(json))
        {
            SettingsData settingsData = new SettingsData();

            string jsonOut = File.ReadAllText(json);
            settingsData = JsonUtility.FromJson<SettingsData>(jsonOut);

            return settingsData;
        } else
        {
            return SettingsData.FromDefault();
        }
    }
}