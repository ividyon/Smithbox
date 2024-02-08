﻿using SoulsFormats;
using StudioCore.Editor;
using StudioCore.AssetLocator;
using StudioCore.UserProject;
using System;
using System.Collections.Generic;
using System.IO;

namespace StudioCore.Banks;

public class MaterialBank
{
    private static Dictionary<string, MTD> _mtds = new();
    private static Dictionary<string, MATBIN> _matbins = new();

    public static bool IsMatbin { get; private set; }

    public static IReadOnlyDictionary<string, MTD> Mtds => _mtds;

    public static IReadOnlyDictionary<string, MATBIN> Matbins => _matbins;

    public static void ReloadMaterials()
    {
        TaskManager.Run(new TaskManager.LiveTask("Resource - Load Materials", TaskManager.RequeueType.WaitThenRequeue,
            false, () =>
            {
                try
                {
                    IBinder mtdBinder = null;
                    if (Project.Type == ProjectType.DS3 || Project.Type == ProjectType.SDT)
                    {
                        mtdBinder = BND4.Read(LocatorUtils.GetAssetPath(@"mtd\allmaterialbnd.mtdbnd.dcx"));
                        IsMatbin = false;
                    }
                    else if (Project.Type is ProjectType.ER or ProjectType.AC6)
                    {
                        mtdBinder = BND4.Read(LocatorUtils.GetAssetPath(@"material\allmaterial.matbinbnd.dcx"));
                        IsMatbin = true;
                    }

                    if (mtdBinder == null)
                        return;

                    if (IsMatbin)
                    {
                        _matbins = new Dictionary<string, MATBIN>();
                        foreach (BinderFile f in mtdBinder.Files)
                        {
                            var matname = Path.GetFileNameWithoutExtension(f.Name);
                            // Because *certain* mods contain duplicate entries for the same material
                            if (!_matbins.ContainsKey(matname))
                                _matbins.Add(matname, MATBIN.Read(f.Bytes));
                        }
                    }
                    else
                    {
                        _mtds = new Dictionary<string, MTD>();
                        foreach (BinderFile f in mtdBinder.Files)
                        {
                            var mtdname = Path.GetFileNameWithoutExtension(f.Name);
                            // Because *certain* mods contain duplicate entries for the same material
                            if (!_mtds.ContainsKey(mtdname))
                                _mtds.Add(mtdname, MTD.Read(f.Bytes));
                        }
                    }

                    mtdBinder.Dispose();
                }
                catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
                {
                    TaskLogs.AddLog("Material files cannot not be found", Microsoft.Extensions.Logging.LogLevel.Warning, TaskLogs.LogPriority.Low);
                    _mtds = new Dictionary<string, MTD>();
                    _matbins = new Dictionary<string, MATBIN>();
                }
            }));
    }

    public static void LoadMaterials()
    {
        if (Project.Type == ProjectType.Undefined)
            return;

        ReloadMaterials();
    }
}