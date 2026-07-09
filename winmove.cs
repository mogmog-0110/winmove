using System;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public class WinMove : Form
{
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    const uint WM_CLOSE = 0x0010;

    ListView list;
    CheckBox maxChk;
    Label status;
    List<IntPtr> handles = new List<IntPtr>();

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new WinMove());
    }

    public WinMove()
    {
        Text = "WinMove";
        ClientSize = new Size(772, 470);
        MinimumSize = new Size(600, 380);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.White;

        list = new ListView();
        list.View = View.Details; list.FullRowSelect = true; list.MultiSelect = false; list.HideSelection = false;
        list.Location = new Point(10, 10);
        list.Size = new Size(752, 352);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.Columns.Add("Window", 470);
        list.Columns.Add("Process", 140);
        list.Columns.Add("Monitor", 120);
        list.DoubleClick += delegate { MoveTo(Screen.PrimaryScreen); };
        Controls.Add(list);

        Screen[] screens = Screen.AllScreens;
        int x = 10;
        for (int i = 0; i < screens.Length; i++)
        {
            Screen s = screens[i];
            Button b = new Button();
            b.Text = "Display " + (i + 1) + "\n" + s.Bounds.Width + "x" + s.Bounds.Height + (s.Primary ? " (main)" : "");
            b.Location = new Point(x, 370);
            b.Size = new Size(126, 40);
            b.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            b.FlatStyle = FlatStyle.System;
            if (s.Primary) b.Font = new Font(Font, FontStyle.Bold);
            int idx = i;
            b.Click += delegate { MoveTo(screens[idx]); };
            Controls.Add(b);
            x += 134;
        }

        maxChk = new CheckBox();
        maxChk.Text = "Maximize after move"; maxChk.AutoSize = true;
        maxChk.Location = new Point(12, 424);
        maxChk.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Controls.Add(maxChk);

        AddBtn("Refresh", 682, delegate { Populate(); });
        AddBtn("Kill", 604, delegate { Kill(); });
        AddBtn("Close", 526, delegate { CloseWin(); });

        status = new Label();
        status.AutoSize = true; status.ForeColor = Color.Gray;
        status.Location = new Point(190, 426);
        status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Controls.Add(status);

        Populate();
    }

    void AddBtn(string text, int x, EventHandler onClick)
    {
        Button b = new Button();
        b.Text = text; b.Size = new Size(72, 28);
        b.Location = new Point(x, 420);
        b.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        b.FlatStyle = FlatStyle.System;
        b.Click += onClick;
        Controls.Add(b);
    }

    void Populate()
    {
        list.BeginUpdate();
        list.Items.Clear(); handles.Clear();
        IntPtr self = this.Handle;
        Screen[] screens = Screen.AllScreens;
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            if (!IsWindowVisible(h) || h == self) return true;
            int len = GetWindowTextLength(h); if (len == 0) return true;
            StringBuilder sb = new StringBuilder(len + 2);
            GetWindowText(h, sb, sb.Capacity);
            string t = sb.ToString();
            if (t.Trim().Length == 0 || t == "Program Manager") return true;

            uint pid; GetWindowThreadProcessId(h, out pid);
            string pname = "";
            try { pname = Process.GetProcessById((int)pid).ProcessName; }
            catch { }

            Screen scr = Screen.FromHandle(h);
            int mi = 0;
            for (int i = 0; i < screens.Length; i++)
                if (screens[i].DeviceName == scr.DeviceName) { mi = i + 1; break; }
            bool offscreen = !scr.Primary && scr.Bounds.Width < 1000;

            ListViewItem it = new ListViewItem(t);
            it.SubItems.Add(pname);
            it.SubItems.Add("Display " + mi + (scr.Primary ? " (main)" : (offscreen ? " (virtual)" : "")));
            list.Items.Add(it);
            handles.Add(h);
            return true;
        }, IntPtr.Zero);
        list.EndUpdate();
        status.Text = list.Items.Count + " windows";
    }

    int Sel() { return list.SelectedIndices.Count == 0 ? -1 : list.SelectedIndices[0]; }

    void MoveTo(Screen target)
    {
        int i = Sel(); if (i < 0) { status.Text = "select a window first"; return; }
        IntPtr h = handles[i];
        ShowWindow(h, 9); // SW_RESTORE
        System.Threading.Thread.Sleep(60);
        RECT r; GetWindowRect(h, out r);
        Rectangle wa = target.WorkingArea;
        int w = Math.Min(r.R - r.L, wa.Width);
        int hh = Math.Min(r.B - r.T, wa.Height);
        int px = wa.X + (wa.Width - w) / 2;
        int py = wa.Y + (wa.Height - hh) / 2;
        SetWindowPos(h, IntPtr.Zero, px, py, w, hh, 0x54); // NOZORDER|NOACTIVATE|SHOWWINDOW
        if (maxChk.Checked) ShowWindow(h, 3); // SW_MAXIMIZE
        Populate();
    }

    void CloseWin()
    {
        int i = Sel(); if (i < 0) { status.Text = "select a window first"; return; }
        PostMessage(handles[i], WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        System.Threading.Thread.Sleep(120);
        Populate();
    }

    void Kill()
    {
        int i = Sel(); if (i < 0) { status.Text = "select a window first"; return; }
        uint pid; GetWindowThreadProcessId(handles[i], out pid);
        string pname = list.Items[i].SubItems[1].Text;
        if (MessageBox.Show("Force-kill process \"" + pname + "\" (PID " + pid + ")?\nUnsaved work will be lost.",
            "WinMove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { Process.GetProcessById((int)pid).Kill(); }
        catch (Exception ex) { status.Text = "kill failed: " + ex.Message; return; }
        System.Threading.Thread.Sleep(120);
        Populate();
    }
}
