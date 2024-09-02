using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;

public class InitiatorCore : MonoBehaviour
{
    public static InitiatorCore instance;

    private const string FOLDER_NAME = "EnSys";
    private const string DLL_NAME = "ENSYS.dll";
    private const string VERSION_FILE_NAME = "EnergySysVersion.json";

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }

        instance = this;
        Debug.Log("EnCore Became Instance");

        var assemblyLocation = Assembly.GetAssembly(typeof(Map)).Location;
        var baseDirectory = Directory.GetParent(Path.GetDirectoryName(assemblyLocation)).FullName;
        var enSysDirectory = Path.Combine(baseDirectory, FOLDER_NAME);

        // Create EnSys folder if it doesn't exist
        if (!Directory.Exists(enSysDirectory))
        {
            Directory.CreateDirectory(enSysDirectory);
        }

        var versionFilePath = Path.Combine(enSysDirectory, VERSION_FILE_NAME);
        var dllPath = Path.Combine(enSysDirectory, DLL_NAME);

        int existingVersion = ReadLocalVersion(versionFilePath);

        Debug.Log("Existing version " + existingVersion);

        bool downloadSuccess = false;

        using (var webClient = new WebClient())
        {
            try
            {
                var json = webClient.DownloadString("https://raw.githubusercontent.com/AlibardaWasTaken/BatrixEnergySystem/main/Version.json");
                int latestVersion = ParseVersionFromJson(json);

                Debug.Log("Got version from git " + latestVersion);

                if (latestVersion > existingVersion)
                {
                    Debug.Log("New version detected");
                    var downloadUrl = "https://github.com/AlibardaWasTaken/BatrixEnergySystem/raw/main/ENSYS.dll";
                    var tempPath = Path.Combine(enSysDirectory, "ENSYS_temp.dll");
                    webClient.DownloadFile(downloadUrl, tempPath);
                    Debug.Log("New version downloaded");

                    if (File.Exists(dllPath))
                    {
                        Debug.Log("Trying to delete old version");
                        File.Delete(dllPath);
                        Debug.Log("Old version deleted");
                    }

                    File.Move(tempPath, dllPath);
                    Debug.Log("New version moved");

                    // Update the version file
                    WriteVersionToFile(versionFilePath, latestVersion);

                    downloadSuccess = true;
                }
            }
            catch (WebException e)
            {
                Debug.Log(e);
                downloadSuccess = false;
            }
        }

        if (!downloadSuccess)
        {
            if (existingVersion == 0)
            {
                var backupDllPath = Path.Combine(ModAPI.Metadata.MetaLocation, "EnSystem", "BACKUP_ENSYS.dll");
                Debug.Log("Backup DLL path: " + backupDllPath);
                Debug.Log("Target DLL path: " + dllPath);
                if (File.Exists(backupDllPath))
                {
                    File.Copy(backupDllPath, dllPath, overwrite: true);
                    // Set initial version to 1.0
                    WriteVersionToFile(versionFilePath, 1);
                }
                else
                {
                    Debug.Log("Backup ENSYS DLL not found in the specified path.");
                    DialogBoxManager.Dialog("[Batrix Energy System]\n" + ModAPI.Metadata.Name + "\nUnable to initialize this mod.", new DialogButton("Close", true));
                    return;
                }
            }
        }

        if (File.Exists(dllPath))
        {
            try
            {
                var bytes = File.ReadAllBytes(dllPath);
                var InvokeObj = Assembly.Load(bytes);
                Debug.Log($"Successfully loaded assembly from {dllPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load assembly from {dllPath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"DLL not found at {dllPath}");
        }

        Debug.Log("EnCore Initiated");
    }

    private int ReadLocalVersion(string versionFilePath)
    {
        if (File.Exists(versionFilePath))
        {
            var json = File.ReadAllText(versionFilePath);
            return ParseVersionFromJson(json);
        }
        return 0;
    }

    private int ParseVersionFromJson(string json)
    {
        var versionTag = "\"Version\":";
        var startIndex = json.IndexOf(versionTag) + versionTag.Length;
        var endIndex = json.IndexOf('}', startIndex);
        var versionString = json.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');
        return int.Parse(versionString);
    }

    private void WriteVersionToFile(string versionFilePath, int version)
    {
        var json = $"{{\"Version\": \"{version}\"}}";
        File.WriteAllText(versionFilePath, json);
    }
}