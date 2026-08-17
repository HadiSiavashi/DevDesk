using System.Drawing.Drawing2D;

namespace DevDesk.WinForms.Themes;

/// <summary>Material-style outlined icons painted with GDI+ (variable-font TTFs are unreliable in WinForms).</summary>
public static class UiIcons
{
    public static void Draw(Graphics g, string name, Rectangle bounds, Color color, float stroke = 0f)
    {
        if (stroke <= 0f)
            stroke = UiScale.Stroke();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);

        var pad = Math.Max(1, Math.Min(bounds.Width, bounds.Height) / 8);
        var r = RectangleF.FromLTRB(bounds.Left + pad, bounds.Top + pad, bounds.Right - pad, bounds.Bottom - pad);

        switch (name)
        {
            case "dashboard": DrawGrid(g, pen, r); break;
            case "sunny": DrawSun(g, pen, r); break;
            case "check_circle": DrawCheckCircle(g, pen, r); break;
            case "timer": DrawTimer(g, pen, r); break;
            case "account_tree": DrawTree(g, pen, r); break;
            case "calendar_today": DrawCalendar(g, pen, r); break;
            case "description": DrawDoc(g, pen, r); break;
            case "emoji_events": DrawTrophy(g, pen, r); break;
            case "repeat": DrawRepeat(g, pen, r); break;
            case "code": DrawCode(g, pen, r); break;
            case "bookmark": DrawBookmark(g, pen, r); break;
            case "dns": DrawDns(g, pen, r); break;
            case "menu_book": DrawBook(g, pen, r); break;
            case "analytics": DrawChart(g, pen, r); break;
            case "event_note": DrawEventNote(g, pen, r); break;
            case "rate_review": DrawReview(g, pen, r); break;
            case "settings": DrawGear(g, pen, r); break;
            case "search": DrawSearch(g, pen, r); break;
            case "add": DrawPlus(g, pen, r); break;
            case "notifications": DrawBell(g, pen, r); break;
            case "play_arrow": DrawPlay(g, brush, r); break;
            case "pause": DrawPause(g, brush, r); break;
            case "stop":
            case "stop_circle": DrawStop(g, brush, r); break;
            case "chevron_left": DrawChevron(g, pen, r, true); break;
            case "chevron_right": DrawChevron(g, pen, r, false); break;
            case "more_horiz": DrawMore(g, brush, r); break;
            case "more_vert": DrawMoreVert(g, brush, r); break;
            case "star": DrawStar(g, pen, r); break;
            case "edit": DrawEdit(g, pen, r); break;
            case "flag": DrawFlag(g, pen, r); break;
            case "close": DrawClose(g, pen, r); break;
            case "error": DrawError(g, pen, r); break;
            case "check": DrawCheck(g, pen, r); break;
            case "folder": DrawFolder(g, pen, r); break;
            case "menu": DrawMenu(g, pen, r); break;
            case "terminal": DrawTerminal(g, pen, r); break;
            case "save": DrawSave(g, pen, r); break;
            case "arrow_forward": DrawArrow(g, pen, r); break;
            case "today": DrawCalendar(g, pen, r); break;
            case "format_list_bulleted": DrawList(g, pen, r); break;
            case "center_focus_strong": DrawFocus(g, pen, r); break;
            case "timelapse": DrawTimer(g, pen, r); break;
            case "play_circle": DrawPlayCircle(g, pen, brush, r); break;
            case "checklist": DrawList(g, pen, r); break;
            case "psychology": DrawFocus(g, pen, r); break;
            case "task_alt": DrawCheckCircle(g, pen, r); break;
            case "priority_high": DrawFlag(g, pen, r); break;
            case "keyboard_return": DrawEnter(g, pen, r); break;
            case "help": DrawHelp(g, pen, r); break;
            default: DrawDot(g, brush, r); break;
        }
    }

    private static void DrawGrid(Graphics g, Pen p, RectangleF r)
    {
        var w = r.Width / 2.2f;
        var h = r.Height / 2.2f;
        g.DrawRectangle(p, r.X, r.Y, w, h);
        g.DrawRectangle(p, r.Right - w, r.Y, w, h);
        g.DrawRectangle(p, r.X, r.Bottom - h, w, h);
        g.DrawRectangle(p, r.Right - w, r.Bottom - h, w, h);
    }

    private static void DrawSun(Graphics g, Pen p, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var rad = r.Width * 0.22f;
        g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
        for (var i = 0; i < 8; i++)
        {
            var a = i * MathF.PI / 4;
            var x1 = cx + MathF.Cos(a) * rad * 1.5f;
            var y1 = cy + MathF.Sin(a) * rad * 1.5f;
            var x2 = cx + MathF.Cos(a) * rad * 2.1f;
            var y2 = cy + MathF.Sin(a) * rad * 2.1f;
            g.DrawLine(p, x1, y1, x2, y2);
        }
    }

    private static void DrawCheckCircle(Graphics g, Pen p, RectangleF r)
    {
        g.DrawEllipse(p, r);
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.28f, r.Y + r.Height * 0.52f),
            new PointF(r.X + r.Width * 0.44f, r.Y + r.Height * 0.68f),
            new PointF(r.X + r.Width * 0.72f, r.Y + r.Height * 0.34f)
        });
    }

    private static void DrawTimer(Graphics g, Pen p, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height * 0.55f;
        var rad = r.Width * 0.38f;
        g.DrawEllipse(p, cx - rad, cy - rad, rad * 2, rad * 2);
        g.DrawLine(p, cx, r.Y, cx, r.Y + r.Height * 0.12f);
        g.DrawLine(p, cx, cy, cx + rad * 0.45f, cy - rad * 0.2f);
    }

    private static void DrawTree(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X + r.Width * 0.3f, r.Y + r.Height * 0.2f, r.X + r.Width * 0.3f, r.Y + r.Height * 0.8f);
        g.DrawLine(p, r.X + r.Width * 0.3f, r.Y + r.Height * 0.5f, r.X + r.Width * 0.7f, r.Y + r.Height * 0.5f);
        g.DrawLine(p, r.X + r.Width * 0.7f, r.Y + r.Height * 0.35f, r.X + r.Width * 0.7f, r.Y + r.Height * 0.65f);
        g.DrawEllipse(p, r.X + r.Width * 0.2f, r.Y + r.Height * 0.12f, r.Width * 0.2f, r.Height * 0.18f);
        g.DrawEllipse(p, r.X + r.Width * 0.6f, r.Y + r.Height * 0.22f, r.Width * 0.2f, r.Height * 0.18f);
        g.DrawEllipse(p, r.X + r.Width * 0.6f, r.Y + r.Height * 0.62f, r.Width * 0.2f, r.Height * 0.18f);
    }

    private static void DrawCalendar(Graphics g, Pen p, RectangleF r)
    {
        g.DrawRectangle(p, r.X, r.Y + r.Height * 0.18f, r.Width, r.Height * 0.82f);
        g.DrawLine(p, r.X, r.Y + r.Height * 0.4f, r.Right, r.Y + r.Height * 0.4f);
        g.DrawLine(p, r.X + r.Width * 0.25f, r.Y, r.X + r.Width * 0.25f, r.Y + r.Height * 0.3f);
        g.DrawLine(p, r.X + r.Width * 0.75f, r.Y, r.X + r.Width * 0.75f, r.Y + r.Height * 0.3f);
    }

    private static void DrawDoc(Graphics g, Pen p, RectangleF r)
    {
        var pts = new[]
        {
            new PointF(r.X + r.Width * 0.2f, r.Y),
            new PointF(r.X + r.Width * 0.62f, r.Y),
            new PointF(r.Right, r.Y + r.Height * 0.28f),
            new PointF(r.Right, r.Bottom),
            new PointF(r.X + r.Width * 0.2f, r.Bottom)
        };
        g.DrawPolygon(p, pts);
        g.DrawLine(p, r.X + r.Width * 0.62f, r.Y, r.X + r.Width * 0.62f, r.Y + r.Height * 0.28f);
        g.DrawLine(p, r.X + r.Width * 0.62f, r.Y + r.Height * 0.28f, r.Right, r.Y + r.Height * 0.28f);
    }

    private static void DrawTrophy(Graphics g, Pen p, RectangleF r)
    {
        g.DrawArc(p, r.X + r.Width * 0.2f, r.Y, r.Width * 0.6f, r.Height * 0.7f, 180, 180);
        g.DrawLine(p, r.X + r.Width * 0.35f, r.Y + r.Height * 0.7f, r.X + r.Width * 0.65f, r.Y + r.Height * 0.7f);
        g.DrawLine(p, r.X + r.Width * 0.5f, r.Y + r.Height * 0.7f, r.X + r.Width * 0.5f, r.Bottom - r.Height * 0.1f);
        g.DrawLine(p, r.X + r.Width * 0.3f, r.Bottom, r.X + r.Width * 0.7f, r.Bottom);
    }

    private static void DrawRepeat(Graphics g, Pen p, RectangleF r)
    {
        g.DrawArc(p, r, 40, 220);
        var tip = new[]
        {
            new PointF(r.Right - r.Width * 0.05f, r.Y + r.Height * 0.15f),
            new PointF(r.Right - r.Width * 0.28f, r.Y + r.Height * 0.05f),
            new PointF(r.Right - r.Width * 0.18f, r.Y + r.Height * 0.35f)
        };
        g.DrawLines(p, tip);
    }

    private static void DrawCode(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.38f, r.Y),
            new PointF(r.X + r.Width * 0.18f, r.Y + r.Height / 2),
            new PointF(r.X + r.Width * 0.38f, r.Bottom)
        });
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.62f, r.Y),
            new PointF(r.X + r.Width * 0.82f, r.Y + r.Height / 2),
            new PointF(r.X + r.Width * 0.62f, r.Bottom)
        });
    }

    private static void DrawBookmark(Graphics g, Pen p, RectangleF r) =>
        g.DrawPolygon(p, new[]
        {
            new PointF(r.X + r.Width * 0.25f, r.Y),
            new PointF(r.X + r.Width * 0.75f, r.Y),
            new PointF(r.X + r.Width * 0.75f, r.Bottom),
            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.7f),
            new PointF(r.X + r.Width * 0.25f, r.Bottom)
        });

    private static void DrawDns(Graphics g, Pen p, RectangleF r)
    {
        g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height * 0.28f);
        g.DrawRectangle(p, r.X, r.Y + r.Height * 0.36f, r.Width, r.Height * 0.28f);
        g.DrawRectangle(p, r.X, r.Y + r.Height * 0.72f, r.Width, r.Height * 0.28f);
        using var b = new SolidBrush(p.Color);
        g.FillEllipse(b, r.X + r.Width * 0.12f, r.Y + r.Height * 0.08f, r.Width * 0.12f, r.Height * 0.12f);
        g.FillEllipse(b, r.X + r.Width * 0.12f, r.Y + r.Height * 0.44f, r.Width * 0.12f, r.Height * 0.12f);
        g.FillEllipse(b, r.X + r.Width * 0.12f, r.Y + r.Height * 0.8f, r.Width * 0.12f, r.Height * 0.12f);
    }

    private static void DrawBook(Graphics g, Pen p, RectangleF r)
    {
        g.DrawRectangle(p, r);
        g.DrawLine(p, r.X + r.Width / 2, r.Y, r.X + r.Width / 2, r.Bottom);
    }

    private static void DrawChart(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X, r.Bottom, r.Right, r.Bottom);
        g.DrawLine(p, r.X, r.Y, r.X, r.Bottom);
        using var b = new SolidBrush(p.Color);
        g.FillRectangle(b, r.X + r.Width * 0.15f, r.Y + r.Height * 0.45f, r.Width * 0.18f, r.Height * 0.55f);
        g.FillRectangle(b, r.X + r.Width * 0.42f, r.Y + r.Height * 0.2f, r.Width * 0.18f, r.Height * 0.8f);
        g.FillRectangle(b, r.X + r.Width * 0.69f, r.Y + r.Height * 0.35f, r.Width * 0.18f, r.Height * 0.65f);
    }

    private static void DrawEventNote(Graphics g, Pen p, RectangleF r)
    {
        DrawCalendar(g, p, r);
        g.DrawLine(p, r.X + r.Width * 0.3f, r.Y + r.Height * 0.62f, r.X + r.Width * 0.7f, r.Y + r.Height * 0.62f);
    }

    private static void DrawReview(Graphics g, Pen p, RectangleF r)
    {
        DrawDoc(g, p, r);
        g.DrawLine(p, r.X + r.Width * 0.35f, r.Y + r.Height * 0.55f, r.X + r.Width * 0.75f, r.Y + r.Height * 0.55f);
    }

    private static void DrawGear(Graphics g, Pen p, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var inner = r.Width * 0.22f;
        g.DrawEllipse(p, cx - inner, cy - inner, inner * 2, inner * 2);
        for (var i = 0; i < 8; i++)
        {
            var a = i * MathF.PI / 4;
            g.DrawLine(p,
                cx + MathF.Cos(a) * inner * 1.3f, cy + MathF.Sin(a) * inner * 1.3f,
                cx + MathF.Cos(a) * r.Width * 0.48f, cy + MathF.Sin(a) * r.Height * 0.48f);
        }
    }

    private static void DrawSearch(Graphics g, Pen p, RectangleF r)
    {
        var rad = r.Width * 0.32f;
        g.DrawEllipse(p, r.X + r.Width * 0.08f, r.Y + r.Height * 0.08f, rad * 2, rad * 2);
        g.DrawLine(p, r.X + r.Width * 0.58f, r.Y + r.Height * 0.58f, r.Right, r.Bottom);
    }

    private static void DrawPlus(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X + r.Width / 2, r.Y, r.X + r.Width / 2, r.Bottom);
        g.DrawLine(p, r.X, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
    }

    private static void DrawBell(Graphics g, Pen p, RectangleF r)
    {
        g.DrawArc(p, r.X + r.Width * 0.2f, r.Y, r.Width * 0.6f, r.Height * 0.7f, 180, 180);
        g.DrawLine(p, r.X + r.Width * 0.2f, r.Y + r.Height * 0.35f, r.X + r.Width * 0.2f, r.Y + r.Height * 0.72f);
        g.DrawLine(p, r.X + r.Width * 0.8f, r.Y + r.Height * 0.35f, r.X + r.Width * 0.8f, r.Y + r.Height * 0.72f);
        g.DrawLine(p, r.X + r.Width * 0.2f, r.Y + r.Height * 0.72f, r.X + r.Width * 0.8f, r.Y + r.Height * 0.72f);
        g.DrawArc(p, r.X + r.Width * 0.38f, r.Y + r.Height * 0.72f, r.Width * 0.24f, r.Height * 0.22f, 0, 180);
    }

    private static void DrawPlay(Graphics g, Brush b, RectangleF r) =>
        g.FillPolygon(b, new[]
        {
            new PointF(r.X + r.Width * 0.28f, r.Y),
            new PointF(r.Right, r.Y + r.Height / 2),
            new PointF(r.X + r.Width * 0.28f, r.Bottom)
        });

    private static void DrawPause(Graphics g, Brush b, RectangleF r)
    {
        g.FillRectangle(b, r.X + r.Width * 0.2f, r.Y, r.Width * 0.22f, r.Height);
        g.FillRectangle(b, r.X + r.Width * 0.58f, r.Y, r.Width * 0.22f, r.Height);
    }

    private static void DrawStop(Graphics g, Brush b, RectangleF r) =>
        g.FillRectangle(b, r.X + r.Width * 0.18f, r.Y + r.Height * 0.18f, r.Width * 0.64f, r.Height * 0.64f);

    private static void DrawChevron(Graphics g, Pen p, RectangleF r, bool left)
    {
        var x1 = left ? r.X + r.Width * 0.62f : r.X + r.Width * 0.38f;
        var x2 = left ? r.X + r.Width * 0.38f : r.X + r.Width * 0.62f;
        g.DrawLines(p, new[]
        {
            new PointF(x1, r.Y + r.Height * 0.2f),
            new PointF(x2, r.Y + r.Height * 0.5f),
            new PointF(x1, r.Y + r.Height * 0.8f)
        });
    }

    private static void DrawMore(Graphics g, Brush b, RectangleF r)
    {
        var y = r.Y + r.Height / 2 - r.Height * 0.08f;
        var s = r.Width * 0.16f;
        g.FillEllipse(b, r.X + r.Width * 0.12f, y, s, s);
        g.FillEllipse(b, r.X + r.Width * 0.42f, y, s, s);
        g.FillEllipse(b, r.X + r.Width * 0.72f, y, s, s);
    }

    private static void DrawMoreVert(Graphics g, Brush b, RectangleF r)
    {
        var x = r.X + r.Width / 2 - r.Width * 0.08f;
        var s = r.Height * 0.16f;
        g.FillEllipse(b, x, r.Y + r.Height * 0.12f, s, s);
        g.FillEllipse(b, x, r.Y + r.Height * 0.42f, s, s);
        g.FillEllipse(b, x, r.Y + r.Height * 0.72f, s, s);
    }

    private static void DrawStar(Graphics g, Pen p, RectangleF r)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var pts = new PointF[10];
        for (var i = 0; i < 10; i++)
        {
            var a = -MathF.PI / 2 + i * MathF.PI / 5;
            var rad = i % 2 == 0 ? r.Width * 0.48f : r.Width * 0.2f;
            pts[i] = new PointF(cx + MathF.Cos(a) * rad, cy + MathF.Sin(a) * rad);
        }
        g.DrawPolygon(p, pts);
    }

    private static void DrawEdit(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X + r.Width * 0.12f, r.Bottom - r.Height * 0.12f, r.X + r.Width * 0.55f, r.Y + r.Height * 0.2f);
        g.DrawLine(p, r.X + r.Width * 0.55f, r.Y + r.Height * 0.2f, r.X + r.Width * 0.72f, r.Y + r.Height * 0.38f);
        g.DrawLine(p, r.X + r.Width * 0.72f, r.Y + r.Height * 0.38f, r.X + r.Width * 0.28f, r.Bottom);
        g.DrawLine(p, r.X + r.Width * 0.12f, r.Bottom - r.Height * 0.12f, r.X + r.Width * 0.28f, r.Bottom);
    }

    private static void DrawFlag(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X + r.Width * 0.25f, r.Y, r.X + r.Width * 0.25f, r.Bottom);
        g.DrawPolygon(p, new[]
        {
            new PointF(r.X + r.Width * 0.25f, r.Y),
            new PointF(r.Right, r.Y + r.Height * 0.22f),
            new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.44f)
        });
    }

    private static void DrawClose(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X, r.Y, r.Right, r.Bottom);
        g.DrawLine(p, r.Right, r.Y, r.X, r.Bottom);
    }

    private static void DrawError(Graphics g, Pen p, RectangleF r)
    {
        g.DrawEllipse(p, r);
        g.DrawLine(p, r.X + r.Width / 2, r.Y + r.Height * 0.25f, r.X + r.Width / 2, r.Y + r.Height * 0.58f);
        using var b = new SolidBrush(p.Color);
        g.FillEllipse(b, r.X + r.Width / 2 - 1.5f, r.Y + r.Height * 0.7f, 3, 3);
    }

    private static void DrawCheck(Graphics g, Pen p, RectangleF r) =>
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.15f, r.Y + r.Height * 0.52f),
            new PointF(r.X + r.Width * 0.4f, r.Y + r.Height * 0.78f),
            new PointF(r.X + r.Width * 0.85f, r.Y + r.Height * 0.22f)
        });

    private static void DrawFolder(Graphics g, Pen p, RectangleF r)
    {
        g.DrawRectangle(p, r.X, r.Y + r.Height * 0.28f, r.Width, r.Height * 0.72f);
        g.DrawLine(p, r.X, r.Y + r.Height * 0.28f, r.X + r.Width * 0.12f, r.Y + r.Height * 0.1f);
        g.DrawLine(p, r.X + r.Width * 0.12f, r.Y + r.Height * 0.1f, r.X + r.Width * 0.45f, r.Y + r.Height * 0.1f);
        g.DrawLine(p, r.X + r.Width * 0.45f, r.Y + r.Height * 0.1f, r.X + r.Width * 0.55f, r.Y + r.Height * 0.28f);
    }

    private static void DrawMenu(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X, r.Y + r.Height * 0.25f, r.Right, r.Y + r.Height * 0.25f);
        g.DrawLine(p, r.X, r.Y + r.Height * 0.5f, r.Right, r.Y + r.Height * 0.5f);
        g.DrawLine(p, r.X, r.Y + r.Height * 0.75f, r.Right, r.Y + r.Height * 0.75f);
    }

    private static void DrawTerminal(Graphics g, Pen p, RectangleF r)
    {
        g.DrawRectangle(p, r);
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.18f, r.Y + r.Height * 0.35f),
            new PointF(r.X + r.Width * 0.32f, r.Y + r.Height * 0.5f),
            new PointF(r.X + r.Width * 0.18f, r.Y + r.Height * 0.65f)
        });
        g.DrawLine(p, r.X + r.Width * 0.4f, r.Y + r.Height * 0.65f, r.X + r.Width * 0.72f, r.Y + r.Height * 0.65f);
    }

    private static void DrawSave(Graphics g, Pen p, RectangleF r)
    {
        g.DrawPolygon(p, new[]
        {
            new PointF(r.X, r.Y),
            new PointF(r.Right - r.Width * 0.2f, r.Y),
            new PointF(r.Right, r.Y + r.Height * 0.2f),
            new PointF(r.Right, r.Bottom),
            new PointF(r.X, r.Bottom)
        });
        g.DrawRectangle(p, r.X + r.Width * 0.25f, r.Y, r.Width * 0.4f, r.Height * 0.28f);
    }

    private static void DrawArrow(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLine(p, r.X, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.62f, r.Y + r.Height * 0.22f),
            new PointF(r.Right, r.Y + r.Height / 2),
            new PointF(r.X + r.Width * 0.62f, r.Y + r.Height * 0.78f)
        });
    }

    private static void DrawList(Graphics g, Pen p, RectangleF r)
    {
        using var b = new SolidBrush(p.Color);
        for (var i = 0; i < 3; i++)
        {
            var y = r.Y + r.Height * (0.18f + i * 0.32f);
            g.FillEllipse(b, r.X, y, r.Width * 0.14f, r.Height * 0.14f);
            g.DrawLine(p, r.X + r.Width * 0.28f, y + r.Height * 0.07f, r.Right, y + r.Height * 0.07f);
        }
    }

    private static void DrawFocus(Graphics g, Pen p, RectangleF r)
    {
        g.DrawEllipse(p, r.X + r.Width * 0.2f, r.Y + r.Height * 0.2f, r.Width * 0.6f, r.Height * 0.6f);
        g.DrawLine(p, r.X + r.Width / 2, r.Y, r.X + r.Width / 2, r.Y + r.Height * 0.18f);
        g.DrawLine(p, r.X + r.Width / 2, r.Bottom - r.Height * 0.18f, r.X + r.Width / 2, r.Bottom);
        g.DrawLine(p, r.X, r.Y + r.Height / 2, r.X + r.Width * 0.18f, r.Y + r.Height / 2);
        g.DrawLine(p, r.Right - r.Width * 0.18f, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
    }

    private static void DrawPlayCircle(Graphics g, Pen p, Brush b, RectangleF r)
    {
        g.DrawEllipse(p, r);
        var inner = RectangleF.Inflate(r, -r.Width * 0.28f, -r.Height * 0.28f);
        DrawPlay(g, b, inner);
    }

    private static void DrawEnter(Graphics g, Pen p, RectangleF r)
    {
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.2f, r.Y + r.Height * 0.55f),
            new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.55f),
            new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.2f)
        });
        g.DrawLines(p, new[]
        {
            new PointF(r.X + r.Width * 0.38f, r.Y + r.Height * 0.38f),
            new PointF(r.X + r.Width * 0.2f, r.Y + r.Height * 0.55f),
            new PointF(r.X + r.Width * 0.38f, r.Y + r.Height * 0.72f)
        });
    }

    private static void DrawHelp(Graphics g, Pen p, RectangleF r)
    {
        g.DrawEllipse(p, r);
        g.DrawArc(p, r.X + r.Width * 0.3f, r.Y + r.Height * 0.22f, r.Width * 0.4f, r.Height * 0.35f, 180, 180);
        using var b = new SolidBrush(p.Color);
        g.FillEllipse(b, r.X + r.Width / 2 - 1.5f, r.Y + r.Height * 0.72f, 3, 3);
    }

    private static void DrawDot(Graphics g, Brush b, RectangleF r) =>
        g.FillEllipse(b, r.X + r.Width * 0.35f, r.Y + r.Height * 0.35f, r.Width * 0.3f, r.Height * 0.3f);
}
