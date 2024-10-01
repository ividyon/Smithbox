﻿using Silk.NET.SDL;
using StudioCore.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Interface;

public static class DPI
{
    private const float DefaultDpi = 96f;
    private static float _dpi = DefaultDpi;

    public static EventHandler UIScaleChanged;

    public static float Dpi
    {
        get => _dpi;
        set
        {
            if (Math.Abs(_dpi - value) < 0.0001f) return; // Skip doing anything if no difference

            _dpi = value;
            if (UI.Current.System_ScaleByDPI)
                UIScaleChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static unsafe void UpdateDpi(IGraphicsContext _context)
    {
        if (SdlProvider.SDL.IsValueCreated && _context?.Window != null)
        {
            var window = _context.Window.SdlWindowHandle;
            int index = SdlProvider.SDL.Value.GetWindowDisplayIndex(window);
            float ddpi = 96f;
            float _ = 0f;
            SdlProvider.SDL.Value.GetDisplayDPI(index, ref ddpi, ref _, ref _);

            Dpi = ddpi;
        }
    }

    public static float GetUIScale()
    {
        var scale = UI.Current.System_UI_Scale;
        if (UI.Current.System_ScaleByDPI)
            scale = scale / DefaultDpi * Dpi;
        return scale;
    }
}