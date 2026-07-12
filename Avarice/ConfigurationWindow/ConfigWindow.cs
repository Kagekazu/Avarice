using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons.MathHelpers;
using System.IO;

namespace Avarice.ConfigurationWindow;

internal unsafe partial class ConfigWindow : Window
{
    internal const float SelectWidth = 200f;
    public ConfigWindow() : base($"{P.Name} Configuration - {P.currentProfile.Name.Default("Unnamed profile")}###AvariceConfig")
    {
        Size = new(600, 440);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnClose()
    {
        base.OnClose();
        Svc.PluginInterface.SavePluginConfig(P.config);
    }

    private int selectedSection = 0;
    private static readonly string[] Sections = { "Overlays", "Feedback", "Profiles", "Statistics", "Advanced", "About" };
    private static readonly string IconPath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "res", "avarice_icon.png");

    private static readonly Vector4 Accent = new(0.941f, 0.475f, 0.310f, 1f);
    private static readonly Vector4 AccentBright = new(0.972f, 0.580f, 0.427f, 1f);
    private static Vector4 AccentA(float a) => new(Accent.X, Accent.Y, Accent.Z, a);

    public override void Draw()
    {
        using var colors = ImRaii.PushColor(ImGuiCol.CheckMark, Accent)
            .Push(ImGuiCol.SliderGrab, Accent)
            .Push(ImGuiCol.SliderGrabActive, AccentBright)
            .Push(ImGuiCol.Header, AccentA(0.26f))
            .Push(ImGuiCol.HeaderHovered, AccentA(0.42f))
            .Push(ImGuiCol.HeaderActive, AccentA(0.58f))
            .Push(ImGuiCol.Button, AccentA(0.32f))
            .Push(ImGuiCol.ButtonHovered, AccentA(0.50f))
            .Push(ImGuiCol.ButtonActive, AccentA(0.72f))
            .Push(ImGuiCol.FrameBgHovered, AccentA(0.20f))
            .Push(ImGuiCol.SeparatorHovered, AccentA(0.50f));
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4f);
        DrawBody();
    }

    private void DrawBody()
    {
        using (var nav = ImRaii.Child("##avnav", new Vector2(140f * ImGuiHelpers.GlobalScale, 0)))
        {
            if (nav)
                DrawNavRail();
        }

        ImGui.SameLine();

        using var body = ImRaii.Child("##avbody", new Vector2(0, 0));
        if (!body) return;
        switch (selectedSection)
        {
            case 1: TabSettings.DrawFeedback(); break;
            case 2: TabProfiles.Draw(); break;
            case 3: TabStatistics.Draw(); break;
            case 4: TabSettings.DrawAdvanced(); break;
            case 5: PunishLib.ImGuiMethods.AboutTab.Draw(Svc.PluginInterface.InternalName); break;
            case 100: InternalLog.PrintImgui(); break;
            case 101: Debug(); break;
            default: TabSettings.DrawOverlays(); break;
        }
    }

    private void DrawNavRail()
    {
        DrawNavLogo();

        var comboW = 104f * ImGuiHelpers.GlobalScale;
        var pAvail = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, (pAvail - comboW) / 2f));
        ImGui.SetNextItemWidth(comboW);
        using (var combo = ImRaii.Combo("##avprofile", P.currentProfile.Name.Default("Unnamed profile")))
        {
            if (combo)
            {
                foreach (var prof in P.config.Profiles)
                {
                    if (ImGui.Selectable(prof.Name.Default("Unnamed profile"), prof.GUID == P.currentProfile.GUID))
                        P.currentProfile = prof;
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        using (ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f)))
        {
            for (var i = 0; i < Sections.Length; i++)
            {
                if (ImGui.Selectable(Sections[i], selectedSection == i))
                    selectedSection = i;
            }
            if (P.currentProfile.Debug)
            {
                ImGui.Separator();
                if (ImGui.Selectable("Log", selectedSection == 100)) selectedSection = 100;
                if (ImGui.Selectable("Debug", selectedSection == 101)) selectedSection = 101;
            }
        }
    }

    private static void CenteredText(Vector4 color, string text)
    {
        var w = ImGui.GetContentRegionAvail().X;
        var tw = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, (w - tw) / 2f));
        ImGuiEx.Text(color, text);
    }

    private void DrawNavLogo()
    {
        if (!ThreadLoadImageHandler.TryGetTextureWrap(IconPath, out var logo) || logo == null) return;
        var avail = ImGui.GetContentRegionAvail().X;
        var size = Math.Min(avail, 88f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - size) * 0.5f);
        ImGui.Image(logo.Handle, new Vector2(size, size));
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private int ActionOverride = 0;

    private void Debug()
    {
        if(ImGui.Button("Open Positional Debug window"))
        {
            P.positionalDebugWindow.IsOpen = true;
        }

        if(ImGui.CollapsingHeader("StaticAutoDetectRadiusData"))
        {
            ImGuiEx.Text(P.StaticAutoDetectRadiusData.Select(x => x.ToString()).Join("\n"));
        }
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Visual Feedback System:");
            ImGui.Text("Test Feedback:");
            if(ImGui.Button("Show Success"))
            {
                VisualFeedbackManager.DisplayFeedback(true);
            }
            ImGui.SameLine();
            if(ImGui.Button("Show Failure"))
            {
                VisualFeedbackManager.DisplayFeedback(false);
            }
            ImGui.SameLine();
            if(ImGui.Button("Hide"))
            {
                VisualFeedbackManager.RemoveFeedback();
            }
            ImGui.InputInt("Action override test", ref ActionOverride);
            if(ImGui.Button("set action override"))
            {
                Svc.PluginInterface.GetOrCreateData("Avarice.ActionOverride", () => new List<uint>() { 0 })[0] = (uint)ActionOverride;
            }
            ImGuiEx.Text($"Current action override: {(Svc.PluginInterface.TryGetData<List<uint>>("Avarice.ActionOverride", out var data) ? data[0] : 0)}");
            ImGuiEx.Text($"Combo: {P.memory.LastComboMove}");
            foreach(var x in Svc.Objects.LocalPlayer?.StatusList)
            {
                ImGuiEx.TextCopy($"{x.GameData.ValueNullable?.Name}: id={x.StatusId}, time={x.RemainingTime}");
            }

            ImGuiEx.Text("N. S. ");
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.SameLine(0, 0);
                ImGuiEx.Text(ImGuiColors.DalamudRed, FontAwesomeIcon.Heart.ToIconString());
            }
            ImGuiEx.Text($"Is target positional: {Svc.Targets.Target?.HasPositional()}");
            if(ImGui.Button("Test IPC"))
            {
                Safe(TestIPC);
            }
        }
    }

    private void TestIPC()
    {
        var result = Svc.PluginInterface.GetIpcSubscriber<IntPtr, CardinalDirection>("Avarice.CardinalDirection").InvokeFunc(Svc.Targets.Target?.Address ?? IntPtr.Zero);
        Svc.Chat.Print(result.ToString());
    }

    internal static void DrawUnfilledSettings(string id, ref Brush b, bool displayCondition = true)
    {
        if(displayCondition)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.EnumCombo($"##b{id}", ref b.DisplayCondition);
            ImGuiEx.InvisibleButton(3);
        }
        ImGui.SameLine();
        b.Fill = Vector4.Zero;
        ImGuiEx.Text($"Thickness:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat($"##c{id}", ref b.Thickness, 0.1f, 0f, 10f);
        ImGui.SameLine();
        ImGuiEx.Text($"  Color:");
        ImGui.SameLine();
        ImGui.ColorEdit4($"##a{id}", ref b.Color, ImGuiColorEditFlags.NoInputs);
    }

    internal static void DrawUnfilledMultiSettings(string id, ref Brush b, ref Vector4 south, ref Vector4 east, ref Vector4 west, ref bool lines, ref bool makeSameColor)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo($"##b{id}", ref b.DisplayCondition);
        ImGuiEx.InvisibleButton(3);
        ImGui.SameLine();
        b.Fill = Vector4.Zero;
        ImGuiEx.Text($"Thickness:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat($"##c{id}", ref b.Thickness, 0.1f, 0f, 10f);
        ImGui.SameLine();
        if(!makeSameColor) { ImGuiEx.Text($"  Colours:"); }
        ImGuiEx.InvisibleButton(11);
        ImGui.SameLine();
        ImGui.Checkbox($"Colour match borders?##{id}", ref makeSameColor);
        ImGuiComponents.HelpMarker("If enabled, the borders of each segment will automatically be set to a higher alpha variation of their own respective setting.");
        if(!makeSameColor)
        {
            ImGuiEx.Text($"            Front:");
            ImGui.SameLine();
            ImGui.ColorEdit4($"##a{id}", ref b.Color, ImGuiColorEditFlags.NoInputs);
            ImGuiEx.Text($"            Rear:");
            ImGui.SameLine();
            ImGui.ColorEdit4($"##a{id}s", ref south, ImGuiColorEditFlags.NoInputs);
            ImGuiEx.Text($"            Left Flank:");
            ImGui.SameLine();
            ImGui.ColorEdit4($"##a{id}e", ref east, ImGuiColorEditFlags.NoInputs);
            ImGuiEx.Text($"            Right Flank:");
            ImGui.SameLine();
            ImGui.ColorEdit4($"##a{id}w", ref west, ImGuiColorEditFlags.NoInputs);
        }
        ImGuiEx.InvisibleButton(11);
        ImGui.SameLine();
        ImGui.Checkbox($"Display zoning separator lines?##{id}", ref lines);
        ImGuiEx.InvisibleButton(11);
        ImGui.SameLine();
        if(ImGui.RadioButton($"Display only max melee weaponskill range ring?##{id}", P.currentProfile.Radius3 && !P.currentProfile.Radius2))
        {
            P.currentProfile.Radius3 = true;
            P.currentProfile.Radius2 = false;
        }
        ImGuiEx.InvisibleButton(11);
        ImGui.SameLine();
        if(ImGui.RadioButton($"Display only max auto-attack range ring?##{id}", P.currentProfile.Radius2 && !P.currentProfile.Radius3))
        {
            P.currentProfile.Radius2 = true;
            P.currentProfile.Radius3 = false;
        }
        ImGuiEx.InvisibleButton(11);
        ImGui.SameLine();
        if(ImGui.RadioButton($"Display auto-attack/weaponskill combination ring?##{id}", P.currentProfile.Radius2 && P.currentProfile.Radius3))
        {
            P.currentProfile.Radius3 = true;
            P.currentProfile.Radius2 = true;
        }
    }
}
