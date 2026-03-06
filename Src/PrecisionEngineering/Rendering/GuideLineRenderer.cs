using ColossalFramework.Math;
using PrecisionEngineering.Data;

namespace PrecisionEngineering.Rendering
{
    internal static class GuideLineRenderer
    {
        private const float MinHeight = -1f;
        private const float MaxHeight = 1280f;
        private const float CenterLineWidth = 0.01f;
        private const float CenterLineDashSize = 8f;
        private const float LineLength = 100000f;

        public static void Render(RenderManager.CameraInfo cameraInfo, GuideLine guideLine)
        {
            var renderManager = RenderManager.instance;

            var direction = guideLine.Origin.Flatten().DirectionTo(guideLine.Intersect.Flatten());

            var line = new Segment3(guideLine.Origin, guideLine.Origin + direction * LineLength);

            renderManager.OverlayEffect.DrawSegment(cameraInfo, Settings.SecondaryColor,
                line, guideLine.Width, 0,
                MinHeight,
                MaxHeight, true, true);

            renderManager.OverlayEffect.DrawSegment(cameraInfo, Settings.SecondaryColor,
                line, CenterLineWidth, CenterLineDashSize,
                MinHeight,
                MaxHeight, true, true);
        }
    }
}
