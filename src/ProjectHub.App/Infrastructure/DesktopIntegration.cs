using System.Runtime.InteropServices;
using ProjectHub.Core.Models;
namespace ProjectHub.App.Infrastructure;

public sealed class DesktopIntegration : IDisposable
{
    private readonly MainWindow window;
    private readonly nint hwnd;
    private readonly SubclassProc callback;
    private bool tray;
    private const uint TrayMessage = 0x8001;
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate nint SubclassProc(nint h, uint msg, nint w, nint l, nuint id, nuint data);
    public DesktopIntegration(MainWindow window)
    {
        this.window = window;
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        callback = Handle;
        SetWindowSubclass(hwnd, callback, 1, 0);
    }
    public void Configure(HubSettings settings)
    {
        UnregisterHotKey(hwnd, 1);
        if (settings.HotkeyEnabled && !RegisterHotKey(hwnd, 1, settings.HotkeyModifiers | 0x4000, settings.HotkeyKey))
            window.ViewModel.Status = "Le raccourci global est déjà utilisé par une autre application.";
        if (tray)
        {
            var data = Data();
            Shell_NotifyIcon(2, ref data);
            tray = false;
        }
        if (settings.TrayEnabled)
        {
            var data = Data();
            tray = Shell_NotifyIcon(0, ref data);
        }
    }
    private nint Handle(nint h, uint msg, nint w, nint l, nuint id, nuint data)
    {
        if (msg == 0x0312)
        {
            window.ShowSearch();
            return 0;
        }
        if (msg == TrayMessage)
        {
            if (l == 0x0203 || l == 0x0202)
            {
                window.ShowSearch();
                return 0;
            }
            if (l == 0x0205)
            {
                var menu = CreatePopupMenu();
                AppendMenu(menu, 0, 1, "Ouvrir ProjectHub");
                AppendMenu(menu, 0, 2, "Rechercher");
                AppendMenu(menu, 0, 3, "Scanner");
                var recent = App.Engine.Projects.OrderByDescending(x => x.LastActivity).Take(5).ToArray();
                for (int i = 0; i < recent.Length; i++)
                    AppendMenu(menu, 0, (nuint)(10 + i), recent[i].Name);
                AppendMenu(menu, 0x800, 0, "");
                AppendMenu(menu, 0, 4, "Quitter");
                GetCursorPos(out var point);
                SetForegroundWindow(hwnd);
                var choice = TrackPopupMenu(menu, 0x100 | 0x2, point.X, point.Y, 0, hwnd, 0);
                DestroyMenu(menu);
                if (choice == 1 || choice == 2)
                    window.ShowSearch();
                else if (choice == 3)
                    _ = window.ViewModel.ScanAsync();
                else if (choice == 4)
                    window.ExitApp();
                else if (choice >= 10 && choice < 10 + recent.Length)
                    window.OpenEditor(recent[choice - 10]);
                return 0;
            }
        }
        return DefSubclassProc(h, msg, w, l);
    }
    private NotifyIconData Data() => new() { Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = hwnd, Id = 1, Flags = 1 | 2 | 4, CallbackMessage = TrayMessage, Icon = LoadIcon(0, (nint)32512), Tip = "ProjectHub · Bibliothèque de projets", Info = "", InfoTitle = "" };
    public void Dispose()
    {
        UnregisterHotKey(hwnd, 1);
        if (tray)
        {
            var d = Data();
            Shell_NotifyIcon(2, ref d);
            tray = false;
        }
        RemoveWindowSubclass(hwnd, callback, 1);
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size; public nint Window; public uint Id, Flags, CallbackMessage; public nint Icon; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip; public uint State, StateMask; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info; public uint Version; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle; public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X, Y;
    }
    [DllImport("comctl32.dll")] private static extern bool SetWindowSubclass(nint h, SubclassProc p, nuint id, nuint data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(nint h, SubclassProc p, nuint id);
    [DllImport("comctl32.dll")] private static extern nint DefSubclassProc(nint h, uint msg, nint w, nint l);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(nint h, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(nint h, int id);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll")] private static extern nint LoadIcon(nint h, nint name);
    [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] private static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint h);
}
