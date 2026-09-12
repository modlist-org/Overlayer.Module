using Overlayer.Tag.Core;
using System;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Vfx {
    [Tag] public static float CamX => CamPosition.x;
    [Tag] public static float CamY => CamPosition.y;

    [Tag(Desc = "Camera rotation in degrees (0-359)")]
    public static float CamRot => NormalizedRotation;

    [Tag(Desc = "Camera rotation in radians (0-Tau)")]
    public static float CamRotRad => NormalizedRotation * MathF.PI / 180f;

    [Tag(Desc = "Camera zoom (1 = normal)")]
    public static float CamZoom {
        get {
            try {
                var cam = scrCamera.instance;
                if(cam == null) {
                    return 0f;
                }
                var camobj = cam.camobj;
                if(camobj == null) {
                    return 0f;
                }
                float size = camobj.orthographicSize;
                float normal = cam.camsizenormal;
                if(size <= 0f || normal <= 0f) {
                    return 0f;
                }
                if(float.IsNaN(size) || float.IsInfinity(size)) {
                    return 0f;
                }
                if(float.IsNaN(normal) || float.IsInfinity(normal)) {
                    return 0f;
                }
                return normal / size;
            } catch {
                return 0f;
            }
        }
    }

    private static Vector3 CamPosition {
        get {
            try {
                var cam = scrCamera.instance;
                if(cam == null) {
                    return Vector3.zero;
                }
                var t = cam.transform;
                if(t == null) {
                    return Vector3.zero;
                }
                return t.position;
            } catch {
                return Vector3.zero;
            }
        }
    }

    private static float NormalizedRotation {
        get {
            try {
                var vfx = scrVfxPlus.instance;
                if(vfx == null) {
                    return 0f;
                }
                float z = vfx.camAngle;
                if(float.IsNaN(z) || float.IsInfinity(z)) {
                    return 0f;
                }
                z %= 360f;
                if(z < 0f) {
                    z += 360f;
                }
                return z;
            } catch {
                return 0f;
            }
        }
    }
}
