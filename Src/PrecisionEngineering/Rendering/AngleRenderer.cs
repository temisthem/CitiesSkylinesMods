using PrecisionEngineering.Data;
using PrecisionEngineering.Utilities;
using UnityEngine;

namespace PrecisionEngineering.Rendering
{
    internal static class AngleRenderer
    {
        private const float BlueprintAngleDistance = 10f;
        private const float DefaultAngleDistance = 15f;
        private const float ArcLineWidth = 0.7f;
        private const float HeightPadding = 20f;

        public static float GetAngleDistance(MeasurementFlags flags)
        {
            if ((flags & MeasurementFlags.Blueprint) != 0)
            {
                return BlueprintAngleDistance;
            }

            return DefaultAngleDistance;
        }

        public static Color GetAngleColor(MeasurementFlags flags)
        {
            if ((flags & MeasurementFlags.Blueprint) != 0)
            {
                return Settings.BlueprintColor;
            }

            if ((flags & MeasurementFlags.Secondary) != 0)
            {
                return Settings.SecondaryColor;
            }

            return Settings.PrimaryColor;
        }

        public static Vector3 GetLabelWorldPosition(AngleMeasurement angle)
        {
            return angle.Position + angle.AngleNormal*GetAngleDistance(angle.Flags);
        }

        public static void Render(RenderManager.CameraInfo cameraInfo, AngleMeasurement angle)
        {
            if (angle.HideOverlay)
            {
                return;
            }

            var renderManager = RenderManager.instance;

            var centreAngle = Vector3.Angle(Vector3.right, angle.AngleNormal);

            if (Vector3.Cross(Vector3.right, angle.AngleNormal).y > 0f)
            {
                centreAngle = -centreAngle;
            }

            var arcs = BezierUtil.CreateArc(angle.Position, GetAngleDistance(angle.Flags),
                centreAngle - angle.AngleSize*.5f,
                centreAngle + angle.AngleSize*.5f);

            for (var i = 0; i < arcs.Count; i++)
            {
                renderManager.OverlayEffect.DrawBezier(cameraInfo, GetAngleColor(angle.Flags), arcs[i], ArcLineWidth, 0f, 0f,
                    angle.Position.y - HeightPadding,
                    angle.Position.y + HeightPadding, false, true);
            }
        }
    }
}
