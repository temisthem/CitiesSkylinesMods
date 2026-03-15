using System.Collections.Generic;
using HarmonyLib;
using PrecisionEngineering.Data;
using PrecisionEngineering.Data.Calculations;
using PrecisionEngineering.Utilities;
using UnityEngine;

namespace PrecisionEngineering.Patches
{
    /// <summary>
    /// Handles overriding the default NetTool SnapDirection and Snap methods.
    /// </summary>
    internal class SnapController
    {
        public static bool EnableSnapping;
        public static bool EnableAdvancedSnapping;

        /// <summary>
        /// Toggle the default NetTool Snap behaviour.
        /// </summary>
        public static bool EnableLengthSnapping = true;

        /// <summary>
        /// Lock object to use when accessing GuideLine data (as rendering appears to sometimes happen
        /// on a different thread)
        /// </summary>
        public static readonly object GuideLineLock = new object();

        /// <summary>
        /// The GuideLine object last snapped to.
        /// </summary>
        public static GuideLine? SnappedGuideLine;

        /// <summary>
        /// List of the GuideLine objects generated during the last SnapDirection call
        /// </summary>
        public static readonly IList<GuideLine> GuideLines = new List<GuideLine>();

        /// <summary>
        /// Printed to debug panel when debugging is enabled.
        /// </summary>
        public static string DebugPrint = "";

        [HarmonyPatch(typeof(NetTool), "SnapDirection")]
        internal static class SnapDirectionPatch
        {
            static bool Prefix(NetTool.ControlPoint newPoint, NetTool.ControlPoint oldPoint, NetInfo info,
                ref bool success, ref float minDistanceSq, ref NetTool.ControlPoint __result)
            {
                GuideLines.Clear();
                SnappedGuideLine = null;

                if (!EnableSnapping)
                {
                    // Let the original method run; postfix will handle EnableAdvancedSnapping
                    return true;
                }

                if (Debug.Enabled)
                {
                    DebugPrint = string.Format("oldPoint: {0}\nnewPoint:{1}", StringUtil.ToString(oldPoint),
                        StringUtil.ToString(newPoint));
                }

                minDistanceSq = info.GetMinNodeDistance();
                minDistanceSq = minDistanceSq * minDistanceSq;
                var controlPoint = newPoint;
                success = false;

                // If dragging from a node
                if (oldPoint.m_node != 0 && !newPoint.m_outside)
                {
                    var sourceNodeId = oldPoint.m_node;
                    var sourceNode = NetManager.instance.m_nodes.m_buffer[sourceNodeId];

                    var userLineDirection = (newPoint.m_position - sourceNode.m_position).Flatten();
                    var userLineLength = userLineDirection.magnitude;
                    userLineDirection.Normalize();

                    var closestSegmentId = NetNodeUtility.GetClosestSegmentId(sourceNodeId, userLineDirection);

                    if (closestSegmentId > 0)
                    {
                        var closestSegmentDirection = NetNodeUtility.GetSegmentExitDirection(sourceNodeId,
                            closestSegmentId);

                        var currentAngle = Vector3Extensions.Angle(closestSegmentDirection, userLineDirection,
                            Vector3.up);

                        var snappedAngle = Mathf.Round(currentAngle / Settings.SnapAngle) * Settings.SnapAngle;
                        var snappedDirection = Quaternion.AngleAxis(snappedAngle, Vector3.up) * closestSegmentDirection;

                        controlPoint.m_direction = snappedDirection.normalized;
                        controlPoint.m_position = sourceNode.m_position + userLineLength * controlPoint.m_direction;
                        controlPoint.m_position.y = newPoint.m_position.y;

                        minDistanceSq = (newPoint.m_position - controlPoint.m_position).sqrMagnitude;
                        success = true;
                    }
                }
                else if (oldPoint.m_segment != 0 && !newPoint.m_outside)
                {
                    var sourceSegmentId = oldPoint.m_segment;
                    var sourceSegment = NetManager.instance.m_segments.m_buffer[sourceSegmentId];

                    Vector3 segmentDirection;
                    Vector3 segmentPosition;

                    var userLineDirection = (newPoint.m_position - oldPoint.m_position).Flatten();
                    var userLineLength = userLineDirection.magnitude;
                    userLineDirection.Normalize();

                    sourceSegment.GetClosestPositionAndDirection(oldPoint.m_position, out segmentPosition,
                        out segmentDirection);

                    var currentAngle = Vector3Extensions.Angle(segmentDirection, userLineDirection, Vector3.up);

                    segmentDirection = segmentDirection.Flatten().normalized;

                    var snappedAngle = Mathf.Round(currentAngle / Settings.SnapAngle) * Settings.SnapAngle;
                    var snappedDirection = Quaternion.AngleAxis(snappedAngle, Vector3.up) * segmentDirection;

                    controlPoint.m_direction = snappedDirection.normalized;
                    controlPoint.m_position = oldPoint.m_position + userLineLength * controlPoint.m_direction;
                    controlPoint.m_position.y = newPoint.m_position.y;

                    minDistanceSq = (newPoint.m_position - controlPoint.m_position).sqrMagnitude;

                    success = true;
                }
                else if (oldPoint.m_direction.sqrMagnitude > 0.5f)
                {
                    if (newPoint.m_node == 0 && !newPoint.m_outside)
                    {
                        var currentAngle = Vector3Extensions.Angle(oldPoint.m_direction, newPoint.m_direction,
                            Vector3.up);

                        var snappedAngle = Mathf.Round(currentAngle / Settings.SnapAngle) * Settings.SnapAngle;
                        var snappedDirection = Quaternion.AngleAxis(snappedAngle, Vector3.up) *
                                               oldPoint.m_direction.Flatten();

                        controlPoint.m_direction = snappedDirection.normalized;

                        controlPoint.m_position = oldPoint.m_position +
                                                  Vector3.Distance(oldPoint.m_position.Flatten(),
                                                      newPoint.m_position.Flatten()) *
                                                  controlPoint.m_direction;

                        controlPoint.m_position.y = newPoint.m_position.y;

                        success = true;
                    }
                }
                else if (oldPoint.m_segment == 0 && oldPoint.m_node == 0 && newPoint.m_segment == 0 &&
                         newPoint.m_node == 0)
                {
                    var userLineDirection = (newPoint.m_position - oldPoint.m_position).Flatten();
                    var userLineLength = userLineDirection.magnitude;
                    userLineDirection.Normalize();

                    var snapDirection = Vector3.forward;

                    var currentAngle = Vector3Extensions.Angle(snapDirection, userLineDirection, Vector3.up);

                    var snappedAngle = Mathf.Round(currentAngle / Settings.SnapAngle) * Settings.SnapAngle;
                    var snappedDirection = Quaternion.AngleAxis(snappedAngle, Vector3.up) * snapDirection;

                    controlPoint.m_direction = snappedDirection.normalized;
                    controlPoint.m_position = oldPoint.m_position + userLineLength * controlPoint.m_direction;
                    controlPoint.m_position.y = newPoint.m_position.y;

                    minDistanceSq = (newPoint.m_position - controlPoint.m_position).sqrMagnitude;
                    success = true;
                }

                __result = controlPoint;
                // Skip original — postfix will handle EnableAdvancedSnapping
                return false;
            }

            static void Postfix(NetTool.ControlPoint newPoint, NetTool.ControlPoint oldPoint, NetInfo info,
                ref bool success, ref float minDistanceSq, ref NetTool.ControlPoint __result)
            {
                if (!EnableAdvancedSnapping)
                {
                    return;
                }

                if (__result.m_segment != 0 || __result.m_node != 0)
                {
                    return;
                }

                __result = SnapDirectionGuideLines(__result, oldPoint, info, ref success, ref minDistanceSq);
            }
        }

        public static NetTool.ControlPoint SnapDirectionGuideLines(NetTool.ControlPoint newPoint,
            NetTool.ControlPoint oldPoint,
            NetInfo info, ref bool success, ref float minDistanceSq)
        {
            var controlPoint = newPoint;

            lock (GuideLineLock)
            {
                SnappedGuideLine = null;
                GuideLines.Clear();

                Guides.CalculateGuideLines(info, oldPoint, controlPoint, GuideLines);

                if (GuideLines.Count == 0)
                {
                    if (Debug.Enabled)
                    {
                        DebugPrint += " (No GuideLines Found)";
                    }

                    return newPoint;
                }

                var minDist = float.MaxValue;
                var closestLine = GuideLines[0];

                if (GuideLines.Count > 1)
                {
                    for (var i = 0; i < GuideLines.Count; i++)
                    {
                        var gl = GuideLines[i];
                        var dist = Vector3Extensions.DistanceSquared(gl.Origin, newPoint.m_position) +
                                   gl.Distance * gl.Distance;

                        if (dist < minDist)
                        {
                            closestLine = gl;
                            minDist = dist;
                        }
                    }
                }

                if (closestLine.Distance <= Settings.GuideLinesSnapDistance + closestLine.Width)
                {
                    minDistanceSq = closestLine.Distance * closestLine.Distance;

                    if (Debug.Enabled)
                    {
                        DebugPrint += " Guide: " + closestLine.Intersect;
                    }

                    controlPoint.m_position = closestLine.Intersect;
                    controlPoint.m_position.y = newPoint.m_position.y;
                    controlPoint.m_direction = oldPoint.m_position.DirectionTo(newPoint.m_position);
                    success = true;

                    SnappedGuideLine = closestLine;
                }

                return controlPoint;
            }
        }

        [HarmonyPatch(typeof(NetTool), "Snap")]
        internal static class SnapPatch
        {
            static bool Prefix()
            {
                // When length snapping is disabled, skip the original Snap method entirely
                return EnableLengthSnapping;
            }
        }
    }
}
