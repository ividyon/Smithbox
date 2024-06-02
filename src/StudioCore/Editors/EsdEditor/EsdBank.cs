﻿using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Locators;
using StudioCore.UserProject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.TalkEditor;
public static class EsdBank
{
    public static bool IsLoaded { get; private set; }
    public static bool IsLoading { get; private set; }

    public static Dictionary<EsdScriptInfo, IBinder> TalkBank { get; private set; } = new();

    public static void SaveEsdScripts()
    {
        foreach (var (info, binder) in TalkBank)
        {
            SaveEsdScript(info, binder);
        }
    }

    public static void SaveEsdScript(EsdScriptInfo info, IBinder binder)
    {
        if (binder == null)
            return;

        //TaskLogs.AddLog($"SaveTalkScript: {info.Path}");

        var fileDir = @"\script\talk";
        var fileExt = @".talkesdbnd.dcx";

        foreach (BinderFile file in binder.Files)
        {
            foreach (ESD eFile in info.EsdFiles)
            {
                file.Bytes = eFile.Write();
            }
        }

        BND4 writeBinder = binder as BND4;
        byte[] fileBytes = null;

        var assetRoot = $@"{Project.GameRootDirectory}\{fileDir}\{info.Name}{fileExt}";
        var assetMod = $@"{Project.GameModDirectory}\{fileDir}\{info.Name}{fileExt}";

        switch (Project.Type)
        {
            case ProjectType.DS3:
                fileBytes = writeBinder.Write(DCX.Type.DCX_DFLT_10000_44_9);
                break;
            case ProjectType.SDT:
                fileBytes = writeBinder.Write(DCX.Type.DCX_KRAK);
                break;
            case ProjectType.ER:
                fileBytes = writeBinder.Write(DCX.Type.DCX_KRAK);
                break;
            case ProjectType.AC6:
                fileBytes = writeBinder.Write(DCX.Type.DCX_KRAK_MAX);
                break;
            default:
                TaskLogs.AddLog($"Invalid ProjectType during SaveESDScript");
                return;
        }

        // Add folder if it does not exist in GameModDirectory
        if (!Directory.Exists($"{Project.GameModDirectory}\\{fileDir}\\"))
        {
            Directory.CreateDirectory($"{Project.GameModDirectory}\\{fileDir}\\");
        }

        // Make a backup of the original file if a mod path doesn't exist
        if (Project.GameModDirectory == null && !File.Exists($@"{assetRoot}.bak") && File.Exists(assetRoot))
        {
            File.Copy(assetRoot, $@"{assetRoot}.bak", true);
        }

        if (fileBytes != null)
        {
            File.WriteAllBytes(assetMod, fileBytes);
            //TaskLogs.AddLog($"Saved at: {assetMod}");
        }
    }

    public static void LoadEsdScripts()
    {
        if (Project.Type == ProjectType.Undefined)
        {
            return;
        }

        IsLoaded = false;
        IsLoading = true;

        TalkBank = new();

        var fileDir = @"\script\talk";
        var fileExt = @".talkesdbnd.dcx";

        List<string> talkNames = ResourceMiscLocator.GetTalkBinders();

        foreach (var name in talkNames)
        {
            var filePath = $"{fileDir}\\{name}{fileExt}";

            if (File.Exists($"{Project.GameModDirectory}\\{filePath}"))
            {
                LoadEsdScript($"{Project.GameModDirectory}\\{filePath}");
                //TaskLogs.AddLog($"Loaded from GameModDirectory: {filePath}");
            }
            else
            {
                LoadEsdScript($"{Project.GameRootDirectory}\\{filePath}");
                //TaskLogs.AddLog($"Loaded from GameRootDirectory: {filePath}");
            }
        }

        IsLoaded = true;
        IsLoading = false;

        TaskLogs.AddLog($"ESD Script Bank - Load Complete");
    }

    private static void LoadEsdScript(string path)
    {
        if (path == null)
        {
            TaskLogs.AddLog($"Could not locate {path} when loading ESD file.",
                    LogLevel.Warning);
            return;
        }
        if (path == "")
        {
            TaskLogs.AddLog($"Could not locate {path} when loading ESD file.",
                    LogLevel.Warning);
            return;
        }

        var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        EsdScriptInfo talkInfo = new EsdScriptInfo(name, path);

        IBinder binder = BND4.Read(DCX.Decompress(path));

        foreach (var file in binder.Files)
        {
            if (file.Name.Contains(".esd"))
            {
                try
                {
                    ESD eFile = ESD.Read(file.Bytes);
                    talkInfo.EsdFiles.Add(eFile);
                }
                catch (Exception ex)
                {
                    TaskLogs.AddLog($"{file.ID} - Failed to read.\n{ex.ToString()}");
                }
            }
        }

        TalkBank.Add(talkInfo, binder);
    }

    public class EsdScriptInfo
    {
        public EsdScriptInfo(string name, string path)
        {
            Name = name;
            Path = path;
            EsdFiles = new List<ESD>();
        }

        public string Name { get; set; }
        public string Path { get; set; }

        public List<ESD> EsdFiles { get; set; }
    }
}