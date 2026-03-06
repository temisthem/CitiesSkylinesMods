using ColossalFramework.Math;
using PrecisionEngineering.Data;
using UnityEngine;

namespace PrecisionEngineering.Rendering
{
    internal static class DistanceRenderer
    {
        public const float Size = 1f;
        private const float DashSize = 3f;
        private const float HeightPadding = 20f;

        public static Vector3 GetLabelWorldPosition(DistanceMeasurement distance)
        {
            return distance.Position;
        }

        public static void Render(RenderManager.CameraInfo cameraInfo, DistanceMeasurement distance)
        {
            var renderManager = RenderManager.instance;

            if (!distance.IsStraight || distance.HideOverlay)
            {
                return;
            }

            var minHeight = Mathf.Min(distance.StartPosition.y, distance.EndPosition.y);
            var maxHeight = Mathf.Max(distance.StartPosition.y, distance.EndPosition.y);

            renderManager.OverlayEffect.DrawSegment(cameraInfo,
                distance.Flags == MeasurementFlags.Primary ? Settings.PrimaryColor : Settings.SecondaryColor,
                new Segment3(distance.StartPosition, distance.EndPosition), Size, DashSize,
                minHeight - HeightPadding,
                maxHeight + HeightPadding, true, true);
        }
    }
}
