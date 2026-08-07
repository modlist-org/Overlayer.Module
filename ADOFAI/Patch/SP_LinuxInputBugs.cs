using HarmonyLib;
using Overlayer.Patch.Safe;
using System.Reflection;
using TMPro;
using UnityEngine;
using LegacyInputField = UnityEngine.UI.InputField;

namespace Overlayer.Module.ADOFAI.Patch;

public sealed class SP_LinuxTMPKeyInput() : SafeConditionalPatch(nameof(SP_LinuxTMPKeyInput)) {
    protected override bool ShouldApply()
        => Application.platform == RuntimePlatform.LinuxPlayer && Core.Config.LinuxTextInputFix;
    protected override MethodBase GetTargetMethod()
        => typeof(TMP_InputField).GetMethod("KeyPressed", BindingFlags.Instance | BindingFlags.NonPublic)!;
    protected override HarmonyMethod Prefix()
        => new(typeof(SP_LinuxTMPKeyInput).GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static bool PrefixImpl(Event evt) => LinuxTextInputFix.Process(evt);
}

public sealed class SP_LinuxLegacyKeyInput() : SafeConditionalPatch(nameof(SP_LinuxLegacyKeyInput)) {
    protected override bool ShouldApply()
        => Application.platform == RuntimePlatform.LinuxPlayer && Core.Config.LinuxTextInputFix;
    protected override MethodBase GetTargetMethod()
        => typeof(LegacyInputField).GetMethod("KeyPressed", BindingFlags.Instance | BindingFlags.NonPublic)!;
    protected override HarmonyMethod Prefix()
        => new(typeof(SP_LinuxLegacyKeyInput).GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static bool PrefixImpl(Event evt) => LinuxTextInputFix.Process(evt);
}

internal static class LinuxTextInputFix {
    private enum PendingKind {
        None,
        Text,
        Physical
    }

    private static PendingKind pendingKind;
    private static int pendingFrame = -1;
    private const int PendingLifetimeFrames = 1;

    public static bool Process(Event evt) {
        int pendingAge = Time.frameCount - pendingFrame;
        if(pendingKind != PendingKind.None && (pendingAge < 0 || pendingAge > PendingLifetimeFrames)) ClearPending();

        if(evt.keyCode == KeyCode.None && IsAsciiText(evt.character)) {
            bool duplicate = pendingKind == PendingKind.Physical;
            if(duplicate) {
                ClearPending();
                return false;
            }

            pendingKind = PendingKind.Text;
            pendingFrame = Time.frameCount;
            return true;
        }

        if(!TryGetCharacter(evt, out char value)) {
            if(evt.keyCode != KeyCode.None && IsAsciiText(evt.character) &&
               (evt.modifiers & (EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command)) == 0) {
                if(pendingKind == PendingKind.Text) {
                    ClearPending();
                    return false;
                }

                pendingKind = PendingKind.Physical;
                pendingFrame = Time.frameCount;
                return true;
            }

            if(evt.keyCode == KeyCode.None || IsModifierKey(evt.keyCode)) return true;

            ClearPending();
            return true;
        }

        // Keep whichever half of the paired text/physical event arrived first.
        if(pendingKind == PendingKind.Text) {
            ClearPending();
            return false;
        }

        evt.character = value;
        evt.modifiers &= ~EventModifiers.FunctionKey;

        pendingKind = PendingKind.Physical;
        pendingFrame = Time.frameCount;
        return true;
    }

    private static void ClearPending() {
        pendingKind = PendingKind.None;
        pendingFrame = -1;
    }

    private static bool IsAsciiText(char value) => value is >= ' ' and <= '~';

    private static bool IsModifierKey(KeyCode key) => key is
        KeyCode.LeftShift or KeyCode.RightShift or
        KeyCode.LeftControl or KeyCode.RightControl or
        KeyCode.LeftAlt or KeyCode.RightAlt or
        KeyCode.LeftCommand or KeyCode.RightCommand or
        KeyCode.CapsLock;

    private static bool TryGetCharacter(Event evt, out char value) {
        value = '\0';
        if((evt.modifiers & (EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command)) != 0) return false;
        bool shifted = (evt.modifiers & (EventModifiers.Shift | EventModifiers.FunctionKey)) != 0;
        bool caps = (evt.modifiers & EventModifiers.CapsLock) != 0;

        if(evt.keyCode >= KeyCode.A && evt.keyCode <= KeyCode.Z) {
            value = (char)('a' + evt.keyCode - KeyCode.A);
            if(shifted ^ caps) value = char.ToUpperInvariant(value);
            return true;
        }

        const string plainNumbers = "0123456789";
        const string shiftedNumbers = ")!@#$%^&*(";
        if(evt.keyCode >= KeyCode.Alpha0 && evt.keyCode <= KeyCode.Alpha9) {
            int index = evt.keyCode - KeyCode.Alpha0;
            value = shifted ? shiftedNumbers[index] : plainNumbers[index];
            return true;
        }

        (char plain, char upper) = evt.keyCode switch {
            KeyCode.Space => (' ', ' '),
            KeyCode.Minus => ('-', '_'),
            KeyCode.Equals => ('=', '+'),
            KeyCode.LeftBracket => ('[', '{'),
            KeyCode.RightBracket => (']', '}'),
            KeyCode.Backslash => ('\\', '|'),
            KeyCode.Semicolon => (';', ':'),
            KeyCode.Quote => ('\'', '"'),
            KeyCode.Comma => (',', '<'),
            KeyCode.Period => ('.', '>'),
            KeyCode.Slash => ('/', '?'),
            KeyCode.BackQuote => ('`', '~'),
            _ => ('\0', '\0')
        };
        value = shifted ? upper : plain;
        return value != '\0';
    }
}
