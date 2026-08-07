using DG.Tweening;
using Overlayer.Tag.Core;
using Overlayer.Tag.Diagnostics;
using Overlayer.TextEngine.Parse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Effects {
    private sealed class AnimationState {
        internal double Previous;
        internal double Target;
        internal double StartedAt;
        internal bool Initialized;
    }

    private static readonly Dictionary<string, AnimationState> easedValues = new();
    private static readonly Dictionary<string, AnimationState> movingValues = new();

    [Tag(TagType = TagType.ProcessFormat)]
    public static double EasedValue(string tagName, int digits = -1, double speed = 500, Ease ease = Ease.Linear) {
        if(!TryReadNumber(tagName, out double value)) return 0;
        AnimationState state = GetState(easedValues, tagName, value);
        double now = Time.realtimeSinceStartupAsDouble * 1000d;
        double current = Interpolate(state, now, speed, ease);
        if(value != state.Target) {
            state.Previous = current;
            state.Target = value;
            state.StartedAt = now;
            current = state.Previous;
        }
        return digits < 0 ? current : Math.Round(current, digits);
    }

    [Tag]
    public static string ColorRange(string tagName, double minimum, double maximum, string minimumHex,
        string maximumHex, Ease ease = Ease.Linear, int maxLength = -1) {
        if(!TryReadNumber(tagName, out double value) || !TryColor(minimumHex, out UnityEngine.Color from, out bool alpha)
            || !TryColor(maximumHex, out UnityEngine.Color to, out bool toAlpha)) return string.Empty;
        if(maximum <= minimum) return alpha || toAlpha ? UnityEngine.ColorUtility.ToHtmlStringRGBA(from) : UnityEngine.ColorUtility.ToHtmlStringRGB(from);
        float progress = Mathf.Clamp01((float)((value - minimum) / (maximum - minimum)));
        float eased = DOVirtual.EasedValue(0, 1, progress, ease);
        UnityEngine.Color color = UnityEngine.Color.LerpUnclamped(from, to, eased);
        string result = alpha || toAlpha ? UnityEngine.ColorUtility.ToHtmlStringRGBA(color) : UnityEngine.ColorUtility.ToHtmlStringRGB(color);
        return maxLength < 0 || result.Length <= maxLength ? result : result[..maxLength];
    }

    [Tag]
    public static double MovingMan(string tagName, double startSize, double endSize, double defaultSize,
        double speed, bool invert = false, Ease ease = Ease.OutExpo) {
        if(!TryReadNumber(tagName, out double value)) return defaultSize;
        AnimationState state = GetState(movingValues, tagName, value);
        double now = Time.realtimeSinceStartupAsDouble * 1000d;
        if(value != state.Target) {
            state.Target = value;
            state.StartedAt = now;
        }
        if(speed <= 0 || now - state.StartedAt >= speed) return defaultSize;
        float progress = Mathf.Clamp01((float)((now - state.StartedAt) / speed));
        float eased = DOVirtual.EasedValue(0, 1, invert ? 1 - progress : progress, ease);
        return startSize + (endSize - startSize) * eased;
    }

    private static AnimationState GetState(Dictionary<string, AnimationState> states, string key, double value) {
        if(!states.TryGetValue(key, out AnimationState? state)) {
            state = new AnimationState { Previous = value, Target = value, StartedAt = Time.realtimeSinceStartupAsDouble * 1000d, Initialized = true };
            states[key] = state;
        }
        return state;
    }

    private static double Interpolate(AnimationState state, double now, double speed, Ease ease) {
        if(speed <= 0) return state.Target;
        float progress = Mathf.Clamp01((float)((now - state.StartedAt) / speed));
        return state.Previous + (state.Target - state.Previous) * DOVirtual.EasedValue(0, 1, progress, ease);
    }

    private static bool TryReadNumber(string tagName, out double value) {
        value = 0;
        if(!TagManager.TryGet(tagName, out TagCore tag) || tag.RequiredParameterCount != 0) return false;
        try {
            object[] args = new object[tag.Parameters.Length];
            for(int i = 0; i < args.Length; i++) args[i] = tag.Parameters[i].DefaultValue;
            object result = tag.Invoke(args);
            return result != null && double.TryParse(result.ToString(), out value);
        } catch {
            return false;
        }
    }

    private static bool TryColor(string hex, out UnityEngine.Color color, out bool hasAlpha) {
        color = default;
        hex = hex.TrimStart('#');
        hasAlpha = hex.Length is 4 or 8;
        return hex.Length is 3 or 4 or 6 or 8 && UnityEngine.ColorUtility.TryParseHtmlString("#" + hex, out color);
    }
}

public static class ExpressionTag {
    [Tag(Name = "Expression", TagType = TagType.Advanced, Desc = "Evaluates a JavaScript expression.")]
    public static Func<string> Expression(ParsedTag parsed, DiagnosticContext context, List<CompileDiagnostic> diagnostics)
        => Overlayer.TagImpl.JavaScirpt.JSExpr(parsed, context, diagnostics);
}
