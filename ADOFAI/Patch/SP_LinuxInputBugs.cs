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
    private static bool PrefixImpl(Event evt, TMP_InputField __instance) => LinuxTextInputFix.Process(evt, __instance);
}

public sealed class SP_LinuxLegacyKeyInput() : SafeConditionalPatch(nameof(SP_LinuxLegacyKeyInput)) {
    protected override bool ShouldApply()
        => Application.platform == RuntimePlatform.LinuxPlayer && Core.Config.LinuxTextInputFix;
    protected override MethodBase GetTargetMethod()
        => typeof(LegacyInputField).GetMethod("KeyPressed", BindingFlags.Instance | BindingFlags.NonPublic)!;
    protected override HarmonyMethod Prefix()
        => new(typeof(SP_LinuxLegacyKeyInput).GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static bool PrefixImpl(Event evt, LegacyInputField __instance) => LinuxTextInputFix.Process(evt, __instance);
}

internal static class LinuxTextInputFix {
    private enum PendingKind {
        None,
        Text,
        Physical
    }

    private static PendingKind pendingKind;
    private static char pendingCharacter;
    private static int pendingFrame = -1;
    private static object? pendingSource;

    public static bool Process(Event evt, object source) {
        if(pendingFrame != Time.frameCount || !ReferenceEquals(pendingSource, source)) ClearPending();

        if(evt.keyCode == KeyCode.None && IsAsciiText(evt.character)) {
            bool duplicate = pendingKind == PendingKind.Physical && pendingCharacter == evt.character;
            if(duplicate) {
                ClearPending();
                return false;
            }

            pendingKind = PendingKind.Text;
            pendingCharacter = evt.character;
            pendingFrame = Time.frameCount;
            pendingSource = source;
            return true;
        }

        if(!TryGetCharacter(evt, out char value)) {
            if(evt.keyCode != KeyCode.None && IsAsciiText(evt.character) &&
               (evt.modifiers & (EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command)) == 0) {
                if(pendingKind == PendingKind.Text && pendingCharacter == evt.character) {
                    evt.character = '\0';
                    ClearPending();
                    return true;
                }

                pendingKind = PendingKind.Physical;
                pendingCharacter = evt.character;
                pendingFrame = Time.frameCount;
                pendingSource = source;
                return true;
            }

            ClearPending();
            return true;
        }

        if(pendingKind == PendingKind.Text && pendingCharacter == value) {
            evt.character = '\0';
            ClearPending();
            return true;
        }

        evt.character = value;
        evt.modifiers &= ~EventModifiers.FunctionKey;

        pendingKind = PendingKind.Physical;
        pendingCharacter = value;
        pendingFrame = Time.frameCount;
        pendingSource = source;
        return true;
    }

    private static void ClearPending() {
        pendingKind = PendingKind.None;
        pendingCharacter = '\0';
        pendingFrame = -1;
        pendingSource = null;
    }

    private static bool IsAsciiText(char value) => value is >= ' ' and <= '~';

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
