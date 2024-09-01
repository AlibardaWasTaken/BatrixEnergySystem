using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Net;
using System.Globalization;
using UnityEngine.Events;

namespace EnergySystemInitiator
{
    public static class Initiator
    {
        public static void Initiate()
        {
            if(GameObject.Find("BATRIX_ENERGY_SYS_INIT") == null)
            {
               var gm = new GameObject("BATRIX_ENERGY_SYS_INIT");
                gm.AddComponent<InitiatorCore>();
                Debug.Log("Created EnCore");
            }
        }

    }



    public class InitiatorCore : MonoBehaviour
    {
        public static InitiatorCore instance;

        public void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            instance = this;
            Debug.Log("EnCore Became Instance");
            // Get the directory of the current assembly
            var assemblyLocation = Assembly.GetAssembly(typeof(Map)).Location;
            var directory = Path.GetDirectoryName(assemblyLocation);

            // Search for the ENSYS DLL
            var existingDll = Directory.GetFiles(directory, "ENSYS_*.dll").FirstOrDefault();
            int existingVersion = 0;

            if (existingDll != null)
            {
                // Extract the version from the existing DLL's name
                var versionString = Path.GetFileNameWithoutExtension(existingDll).Split('_')[1];
                int.TryParse(versionString, NumberStyles.Any, CultureInfo.InvariantCulture, out existingVersion);
            }

            bool downloadSuccess = false;
            string finalDllPath = null;
            Debug.Log("Existing version " + existingVersion);         
                using (var webClient = new WebClient())
                {
                    try
                    {
                        // Fetch the latest version information from the GitHub JSON
                        var json = webClient.DownloadString("https://raw.githubusercontent.com/AlibardaWasTaken/BatrixEnergySystem/main/Version.json");

                        // Manually parse the version from the JSON string
                        var versionTag = "\"Version\":";
                        var startIndex = json.IndexOf(versionTag) + versionTag.Length;
                        var endIndex = json.IndexOf('}', startIndex);
                        var versionString = json.Substring(startIndex, endIndex - startIndex).Trim();

                        

                        // Convert the extracted version string to a double
                        int latestVersion;
                    int.TryParse(versionString, NumberStyles.Any, CultureInfo.InvariantCulture, out latestVersion);

                        Debug.Log("got version from git" + latestVersion);

                    if(latestVersion > existingVersion)
                    {
                        // Download the new DLL
                        var downloadUrl = "https://github.com/AlibardaWasTaken/BatrixEnergySystem/raw/main/ENSYS.dll";
                        var tempPath = Path.Combine(directory, "ENSYS.dll");
                        webClient.DownloadFile(downloadUrl, tempPath);

                        // Rename the downloaded DLL to include the version
                        var newDllName = $"ENSYS_{latestVersion}.dll";
                        var newDllPath = Path.Combine(directory, newDllName);

                        // Delete the old DLL if it exists
                        if (existingDll != null)
                        {
                            File.Delete(existingDll);
                        }

                        // Rename the new DLL
                        File.Move(tempPath, newDllPath);
                        finalDllPath = newDllPath;
                   
                        downloadSuccess = true;
                    }


                    }
                    catch (WebException e )
                    {
                        // Handle failure to download (e.g., no internet connection)
                        Debug.Log(e);
                        downloadSuccess = false;
                    }
                }
            

            if (downloadSuccess == false)
            {
                if(existingVersion == 0)
                {
                    // Fallback to the backup DLL
                    var backupDllPath = Path.Combine(ModAPI.Metadata.MetaLocation, "EnSystem", "BACKUP_ENSYS.dll");
                    var fallbackDllName = "ENSYS_1.0.dll";
                    var fallbackDllPath = Path.Combine(directory, fallbackDllName);
                    Debug.Log(backupDllPath);
                    Debug.Log(fallbackDllPath);
                    if (File.Exists(backupDllPath))
                    {
                        File.Copy(backupDllPath, fallbackDllPath, overwrite: true);
                        finalDllPath = fallbackDllPath;
                    }
                    else
                    {
                        Debug.Log("Backup ENSYS DLL not found in the specified path.");

                        DialogBox dialog = (DialogBox)null;
                        dialog = DialogBoxManager.Dialog("[Batrix Energy System]\n" + ModAPI.Metadata.Name + "\nUnable to initialize this mod.", new DialogButton("Close", true));
                        return;
                    }
                }
                else
                {
                    finalDllPath = existingDll;
                }

            }

            // Load the final DLL
            if (finalDllPath != null)
            {
                try
                {
 
                    
                    var bytes = File.ReadAllBytes(finalDllPath);
                    var InvokeObj = Assembly.Load(bytes);
                    Debug.Log($"Successfully loaded assembly from {finalDllPath}");


                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load assembly from {finalDllPath}: {ex.Message}");
                }
            }
            Debug.Log("EnCore Initiated");
        }

    }

    public class VersionInfo
    {
        public string Version { get; set; }
    }
}
