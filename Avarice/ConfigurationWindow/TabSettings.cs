using Avarice.ConfigurationWindow.Player;
using Dalamud.Interface.Components;
using static Avarice.ConfigurationWindow.ConfigWindow;

namespace Avarice.ConfigurationWindow;

internal static class TabSettings
{
    // Sound effect names for the dropdown (SE1-SE16)
    private static readonly string[] SoundNames = new[]
    {
        "<se.1>", "<se.2>", "<se.3>", "<se.4>", "<se.5>", "<se.6>", "<se.7>", "<se.8>",
        "<se.9>", "<se.10>", "<se.11>", "<se.12>", "<se.13>", "<se.14>", "<se.15>", "<se.16>"
    };

    internal static void DrawOverlays()
    {
        BoxDrawing.ContentsAction();

        SectionHeader("Target");
        BoxAnticipation.ContentsAction();
        ImGui.Separator();
        BoxCurrentSegment.ContentsAction();
        ImGui.Separator();
        BoxFront.ContentsAction();
        ImGui.Separator();
        BoxMeleeRing.ContentsAction();
        ImGui.Separator();
        BoxHitboxSettings.ContentsAction();

        SectionHeader("Player");
        BoxPlayerDot.ContentsAction();
        ImGui.Separator();
        BoxPlayerHitbox.ContentsAction();
        ImGui.Separator();
        BoxPlayerDotOthers.ContentsAction();

        SectionHeader("World");
        BoxCompass.Draw();
        TabTank.Draw();
    }

    internal static void DrawFeedback()
    {
        ImGuiHelpers.ScaledDummy(2f);
        BoxFeedback.ContentsAction();
    }

    internal static void DrawAdvanced()
    {
        ImGuiHelpers.ScaledDummy(2f);
        BoxRendering.ContentsAction();
        if (Svc.PluginInterface.TryGetData<bool[]>("Splatoon.IsInUnsafeZone", out _))
        {
            SectionHeader("Splatoon");
            BoxSplatoon.ContentsAction();
        }
    }

    private static void SectionHeader(string label)
    {
        ImGui.Spacing();
        ImGui.SetWindowFontScale(0.82f);
        ImGuiEx.Text(new Vector4(0.96f, 0.62f, 0.44f, 1f), label.ToUpperInvariant());
        ImGui.SetWindowFontScale(1f);
        ImGui.Separator();
        ImGui.Spacing();
    }

    static InfoBox BoxDrawing = new()
    {
        Label = "Drawing",
        ContentsAction = delegate
        {
            ImGui.Checkbox("Enable drawing", ref P.currentProfile.DrawingEnabled);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5);
            ImGuiEx.Text(new Vector4(0.7f, 0.7f, 1.0f, 1.0f), "(/avarice draw)");
            ImGuiComponents.HelpMarker("Toggle all overlay drawing features. Can also be toggled with /avarice draw command");

            bool prevOnlyPositional = P.config.OnlyDrawIfPositional;
            if (ImGui.Checkbox("Only show for positional targets", ref P.config.OnlyDrawIfPositional) && prevOnlyPositional != P.config.OnlyDrawIfPositional)
            {
                Svc.PluginInterface.SavePluginConfig(P.config);
            }
            ImGuiComponents.HelpMarker("When enabled, overlays will only be shown when targeting an enemy that requires positional attacks");

            if (P.config.OnlyDrawIfPositional)
            {
                ImGui.Indent();
                ImGui.Checkbox("Still show distance indicator for non-positional targets", ref P.currentProfile.MaxMeleeIgnorePositionalCheck);
                ImGuiComponents.HelpMarker("When enabled, the Enemy Distance Indicator will still show even when targeting enemies without positionals (like omnidirectional bosses)");
                ImGui.Checkbox("Show positional Ring without Positional checks when non-positional buffs are present", ref P.currentProfile.ShowPositionalWithoutCheckWhenNonPositionalBuffs);
                ImGuiComponents.HelpMarker("When enabled, the Enemy Distance Indicator and positional ring without positional checks when the target has non-positional buffs");
                ImGui.Unindent();
            }
        }
    };

    static InfoBox BoxFeedback = new()
    {
        Label = "Positional feedback",
        ContentsAction = delegate
        {
            P.config.VisualFeedbackSettings ??= new VisualFeedbackSettings();
            P.config.AudioFeedbackSettings ??= new AudioFeedbackSettings();

            var visualSettings = P.config.VisualFeedbackSettings;
            var audioSettings = P.config.AudioFeedbackSettings;

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Visual Mode:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            var currentMode = (int)visualSettings.Mode;
            var modeNames = new[] { "Vector (Checkmark/X)", "Game VFX (VFXEditor)" };
            if (ImGui.Combo("##visualMode", ref currentMode, modeNames, modeNames.Length))
            {
                visualSettings.Mode = (VisualFeedbackMode)currentMode;
                Svc.PluginInterface.SavePluginConfig(P.config);
            }

            if (visualSettings.Mode == VisualFeedbackMode.GameVfx)
            {
                if (!VfxEditorManager.IsVfxEditorAvailable())
                {
                    ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "VFXEditor plugin not detected!");
                    ImGuiComponents.HelpMarker("Game VFX mode requires the VFXEditor plugin to be installed. Install it from the Dalamud plugin installer.");
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), "VFXEditor detected");
                }
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Game VFX includes built-in sounds");
            }

            SectionHeader("On hit");
            ImGui.Indent();

            ImGui.Checkbox("Visual##hit", ref P.currentProfile.EnableVFXSuccess);
            if (P.currentProfile.EnableVFXSuccess && visualSettings.Mode == VisualFeedbackMode.Vector)
            {
                ImGui.SameLine();
                var successColor = visualSettings.SuccessColor;
                if (ImGui.ColorEdit4("##hitColor", ref successColor, ImGuiColorEditFlags.NoInputs))
                {
                    visualSettings.SuccessColor = successColor;
                    Svc.PluginInterface.SavePluginConfig(P.config);
                }
            }

            if (visualSettings.Mode == VisualFeedbackMode.Vector)
            {
                ImGui.Checkbox("Audio##hit", ref P.currentProfile.EnableAudioSuccess);
                if (P.currentProfile.EnableAudioSuccess)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120f);
                    var successIndex = (int)audioSettings.SuccessSoundId - 1;
                    if (successIndex < 0 || successIndex > 15) successIndex = 1;
                    if (ImGui.Combo("##hitSound", ref successIndex, SoundNames, 16))
                    {
                        audioSettings.SuccessSoundId = (uint)(successIndex + 1);
                        Svc.PluginInterface.SavePluginConfig(P.config);
                    }
                }
            }

            if (P.currentProfile.EnableVFXSuccess || (visualSettings.Mode == VisualFeedbackMode.Vector && P.currentProfile.EnableAudioSuccess))
            {
                if (ImGui.Button("Test Hit"))
                    PositionalFeedbackManager.TestFeedback(true);
            }

            ImGui.Unindent();

            SectionHeader("On miss");
            ImGui.Indent();

            ImGui.Checkbox("Visual##miss", ref P.currentProfile.EnableVFXFailure);
            if (P.currentProfile.EnableVFXFailure && visualSettings.Mode == VisualFeedbackMode.Vector)
            {
                ImGui.SameLine();
                var failureColor = visualSettings.FailureColor;
                if (ImGui.ColorEdit4("##missColor", ref failureColor, ImGuiColorEditFlags.NoInputs))
                {
                    visualSettings.FailureColor = failureColor;
                    Svc.PluginInterface.SavePluginConfig(P.config);
                }
            }

            if (visualSettings.Mode == VisualFeedbackMode.Vector)
            {
                ImGui.Checkbox("Audio##miss", ref P.currentProfile.EnableAudioFailure);
                if (P.currentProfile.EnableAudioFailure)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120f);
                    var failureIndex = (int)audioSettings.FailureSoundId - 1;
                    if (failureIndex < 0 || failureIndex > 15) failureIndex = 5;
                    if (ImGui.Combo("##missSound", ref failureIndex, SoundNames, 16))
                    {
                        audioSettings.FailureSoundId = (uint)(failureIndex + 1);
                        Svc.PluginInterface.SavePluginConfig(P.config);
                    }
                }
            }

            if (P.currentProfile.EnableVFXFailure || (visualSettings.Mode == VisualFeedbackMode.Vector && P.currentProfile.EnableAudioFailure))
            {
                if (ImGui.Button("Test Miss"))
                    PositionalFeedbackManager.TestFeedback(false);
            }

            ImGui.Unindent();

            if (visualSettings.Mode == VisualFeedbackMode.Vector && (P.currentProfile.EnableVFXSuccess || P.currentProfile.EnableVFXFailure))
            {
                ImGui.SetNextItemWidth(150f);
                var iconSize = visualSettings.IconSize;
                if (ImGui.SliderFloat("Icon Size", ref iconSize, 5f, 100f))
                {
                    visualSettings.IconSize = iconSize;
                    Svc.PluginInterface.SavePluginConfig(P.config);
                }
            }

            SectionHeader("Chat");
            ImGui.Checkbox("Print on miss", ref P.currentProfile.EnableChatMessagesFailure);
            ImGui.SameLine();
            ImGui.Checkbox("Print on hit", ref P.currentProfile.EnableChatMessagesSuccess);
            ImGui.Checkbox("Encounter summary on combat end", ref P.currentProfile.Announce);
        }
    };

    static InfoBox BoxRendering = new()
    {
        Label = "Rendering",
        ContentsAction = delegate
        {
            ImGuiEx.Text(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), "Warning: Pictomancy may have issues on Mac/Linux.");

            if (ImGui.Checkbox("Render under UI (Pictomancy)", ref P.config.UsePictomancyRenderer))
            {
                Svc.PluginInterface.SavePluginConfig(P.config);
            }
            ImGuiComponents.HelpMarker("When enabled, overlays will render underneath the game's native UI elements (action bars, job gauges, etc.) instead of on top.");

            if (P.config.UsePictomancyRenderer)
            {
                ImGui.Indent();

                if (ImGui.Checkbox("Clip around native UI", ref P.config.PictomancyClipNativeUI))
                {
                    Svc.PluginInterface.SavePluginConfig(P.config);
                }
                ImGuiComponents.HelpMarker("Automatically clips rendering around native UI elements.");

                ImGui.SetNextItemWidth(150f);
                int maxAlpha = P.config.PictomancyMaxAlpha;
                if (ImGui.SliderInt("Max Opacity", ref maxAlpha, 0, 255))
                {
                    P.config.PictomancyMaxAlpha = (byte)maxAlpha;
                    Svc.PluginInterface.SavePluginConfig(P.config);
                }
                ImGuiComponents.HelpMarker("Maximum opacity for all rendered overlays (0-255).");

                ImGui.Unindent();
            }
        }
    };

    static InfoBox BoxSplatoon = new()
    {
        Label = "Splatoon",
        ContentsAction = TabSplatoon.Draw
    };

    static InfoBox BoxCurrentSegment = new()
    {
        Label = "Current Slice Highlight Settings",
        ContentsAction = delegate
        {
            ImGui.Checkbox("Current Slice Highlight Settings", ref P.currentProfile.EnableCurrentPie);
            if (P.currentProfile.EnableCurrentPie)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo($"##cb1", ref P.currentProfile.CurrentPieSettings.DisplayCondition);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Rear Colour:");
                ImGui.SameLine();
                ImGui.ColorEdit4($"##ca1", ref P.currentProfile.CurrentPieSettings.Fill, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Flank Colour:");
                ImGui.SameLine();
                ImGui.ColorEdit4($"##ca1f", ref P.currentProfile.CurrentPieSettingsFlank.Fill, ImGuiColorEditFlags.NoInputs);
            }
        }
    };

    static InfoBox BoxFront = new()
    {
        Label = "Front Slice Indicator",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(200f);
            ImGui.Checkbox("Front Slice Indicator", ref P.currentProfile.EnableFrontSegment);
            if (P.currentProfile.EnableFrontSegment)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo($"##cb2", ref P.currentProfile.FrontSegmentIndicator.DisplayCondition);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Colour:");
                ImGui.SameLine();
                ImGui.ColorEdit4($"##ca2", ref P.currentProfile.FrontSegmentIndicator.Fill, ImGuiColorEditFlags.NoInputs);
            }
        }
    };

    static InfoBox BoxMeleeRing = new()
    {
        Label = "Enemy Distance Indicator",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(SelectWidth);
            ImGui.Checkbox("Enemy Distance Indicator", ref P.currentProfile.EnableMaxMeleeRing);
            if (P.currentProfile.EnableMaxMeleeRing)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo($"##mrd", ref P.currentProfile.MaxMeleeSettingsN.DisplayCondition);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Radius 3y:");
                ImGui.SameLine();
                ImGui.Checkbox("##r3", ref P.currentProfile.Radius3);
                ImGui.SameLine();
                ImGuiEx.Text("Radius 2y:");
                ImGui.SameLine();
                ImGui.Checkbox("##r2", ref P.currentProfile.Radius2);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Lines:");
                ImGui.SameLine();
                ImGui.Checkbox("##lines", ref P.currentProfile.DrawLines);
                DrawUnfilledSettings("mr", ref P.currentProfile.MaxMeleeSettingsN, true);
            }
        }
    };

    static InfoBox BoxAnticipation = new()
    {
        Label = "Positional Anticipation",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(SelectWidth);
            ImGui.Checkbox("Enable anticipation pie", ref P.currentProfile.EnableAnticipatedPie);
            if (P.currentProfile.EnableAnticipatedPie)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo($"##adt", ref P.currentProfile.AnticipatedPieSettings.DisplayCondition);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGuiEx.Text("Color:");
                ImGui.SameLine();
                ImGui.ColorEdit4($"##ca3", ref P.currentProfile.AnticipatedPieSettings.Fill, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.InvisibleButton(3);
                ImGui.SameLine();
                ImGui.Checkbox("Disable on True North", ref P.currentProfile.AnticipatedDisableTrueNorth);

                var job = Svc.ClientState.LocalPlayer?.ClassJob.RowId ?? 0u;
                if (job == 30)
                {
                    ImGui.Checkbox("Show rear when Trick Attack is off cooldown", ref P.currentProfile.TrickAttack);
                    ImGui.Checkbox("Show both valid positionals based on Kazematoi charges", ref P.currentProfile.Kazematoi);
                }
                else if (job == 34)
                {
                    ImGui.Checkbox("Disable anticipation while under Meikyo Shisui", ref P.currentProfile.Meikyo);
                }
                else if (job == 39)
                {
                    ImGuiEx.Text("Anticipate first:");
                    ImGui.SameLine();
                    ImGui.RadioButton("Rear", ref P.currentProfile.Reaper, 0);
                    ImGui.SameLine();
                    ImGui.RadioButton("Flank", ref P.currentProfile.Reaper, 1);
                }

                if (P.currentProfile.UseRotationSolver || P.RotationSolverWatcher.Available)
                {
                    ImGui.Checkbox("Use Rotation Solver to anticipate positionals", ref P.currentProfile.UseRotationSolver);
                }
            }
        }
    };

    static InfoBox BoxHitboxSettings = new()
    {
        Label = "Melee Range Options",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("Ability/weaponskill Range Size", ref P.currentProfile.MeleeSkillAtk, 0.01f, 0.1f, 10f);
            ImGuiEx.InvisibleButton(3);
            ImGui.SameLine();
            ImGui.Checkbox("Include hitbox##1", ref P.currentProfile.MeleeSkillIncludeHitbox);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragFloat("Melee Auto-attack Range Size", ref P.currentProfile.MeleeAutoAtk, 0.01f, 0.1f, 10f);
            ImGuiEx.InvisibleButton(3);
            ImGui.SameLine();
            ImGui.Checkbox("Include hitbox##2", ref P.currentProfile.MeleeAutoIncludeHitbox);
        }
    };

    static InfoBox BoxPlayerDot = new()
    {
        Label = "Player Damage Pixel",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(SelectWidth);
            ImGui.Checkbox("Player Damage Pixel", ref P.currentProfile.EnablePlayerDot);
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Displays the player's damage hitbox — a small pixel between your feet. Customizable via Thickness, but the default is recommended.");
            if (P.currentProfile.EnablePlayerDot)
            {
                DrawUnfilledSettings("dot", ref P.currentProfile.PlayerDotSettings);
            }
        }
    };

    static InfoBox BoxPlayerDotOthers = new()
    {
        Label = "Entity Damage Pixels",
        ContentsAction = delegate
        {
            ImGui.Checkbox("Party Members", ref P.currentProfile.PartyDot);
            if (P.currentProfile.PartyDot)
            {
                DrawUnfilledSettings("dotp", ref P.currentProfile.PartyDotSettings);
            }
            ImGui.Checkbox("All Players", ref P.currentProfile.AllDot);
            if (P.currentProfile.AllDot)
            {
                DrawUnfilledSettings("dota", ref P.currentProfile.AllDotSettings);
            }
        }
    };

    static InfoBox BoxPlayerHitbox = new()
    {
        Label = "Player Reach Outline",
        ContentsAction = delegate
        {
            ImGui.SetNextItemWidth(SelectWidth);
            ImGui.Checkbox("Player Reach Outline", ref P.currentProfile.EnablePlayerRing);
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Displays a ring around you showing the reach of auto attacks.");
            if (P.currentProfile.EnablePlayerRing)
            {
                DrawUnfilledSettings("hitbox", ref P.currentProfile.PlayerRingSettings);
            }
        }
    };
}
