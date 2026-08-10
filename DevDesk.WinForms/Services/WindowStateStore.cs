using System.Text.Json;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Services;

public sealed class WindowStateStore
{
    private static readonly string StoreDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevDesk");
    private static readonly string StorePath = Path.Combine(StoreDir, "window.json");

    public sealed class SavedWindowState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1180;
        public int Height { get; set; } = 740;
        public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
        public bool SidebarCollapsed { get; set; }
    }

    public SavedWindowState Load()
    {
        try
        {
            if (!File.Exists(StorePath)) return new SavedWindowState();
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<SavedWindowState>(json) ?? new SavedWindowState();
        }
        catch
        {
            return new SavedWindowState();
        }
    }

    public void Save(Form form, bool sidebarCollapsed = false)
    {
        try
        {
            Directory.CreateDirectory(StoreDir);
            var state = new SavedWindowState
            {
                WindowState = form.WindowState,
                SidebarCollapsed = sidebarCollapsed
            };

            if (form.WindowState == FormWindowState.Normal)
            {
                state.X = form.Location.X;
                state.Y = form.Location.Y;
                state.Width = form.Width;
                state.Height = form.Height;
            }
            else
            {
                var existing = Load();
                state.X = existing.X;
                state.Y = existing.Y;
                state.Width = existing.Width;
                state.Height = existing.Height;
            }

            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(StorePath, json);
        }
        catch { /* best effort */ }
    }

    public void Apply(Form form, SavedWindowState state)
    {
        if (state.WindowState == FormWindowState.Maximized)
        {
            form.WindowState = FormWindowState.Maximized;
            return;
        }

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(state.X, state.Y);
        form.Size = new Size(Math.Max(UiMetrics.MinWindowWidth - 40, state.Width), Math.Max(UiMetrics.MinWindowHeight - 40, state.Height));
    }
}
