#if DEDICATED
using System;
using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Replication.StateGroups;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage.Network;
using VRageMath;

namespace Shared.Patches;

[HarmonyPatch(typeof(MyPropertySyncStateGroup), "SyncPropertyChanged_Implementation")]
public static class TerminalPropertyBoundsPatch
{
    private const float TwoPi = (float)Math.PI * 2f;

    private static void Postfix(MyPropertySyncStateGroup __instance)
    {
        if (Common.Config?.Enabled != true || !Common.Config.SanitizeTerminalPropertyBounds)
            return;

        var block = GetReplicatedBlock(__instance);
        if (block == null)
            return;

        var changed = Sanitize(block);
        if (!changed || Common.Config?.AdminAuditExtendedEnabled != true || !AdminAudit.Enabled)
            return;

        var steamId = MyEventContext.Current.Sender.Value;
        AdminAudit.Log(
            "terminal-bounds-sanitized: player={0} block={1}/{2} grid={3}",
            AdminAudit.Player(steamId),
            block.BlockDefinition?.Id.ToString() ?? block.GetType().Name,
            block.EntityId,
            block.CubeGrid?.EntityId ?? 0);
    }

    private static MyCubeBlock GetReplicatedBlock(MyPropertySyncStateGroup group)
    {
        var owner = group.Owner;
        if (owner == null)
            return null;

        var ownerType = owner.GetType();
        object instance = AccessTools.Property(ownerType, "Instance")?.GetValue(owner, null);
        if (instance == null)
            instance = AccessTools.Method(ownerType, "GetInstance")?.Invoke(owner, null);

        return instance as MyCubeBlock;
    }

    private static bool Sanitize(MyCubeBlock block)
    {
        var changed = false;

        if (block is MyThrust)
        {
            var max = Math.Max(0f, ReadFloatProperty(block, "ThrustForceLength") ?? 1000000000f);
            changed |= ClampProperty(block, "ThrustOverride", 0f, max);
        }

        if (block is MyGyro)
        {
            changed |= ClampProperty(block, "GyroPower", 0f, 1f);
        }

        if (block is MyMotorStator)
        {
            changed |= ClampSyncField(block, "TargetVelocity", -1000f, 1000f);
            changed |= ClampSyncField(block, "Torque", 0f, 1000000000f);
            changed |= ClampSyncField(block, "BrakingTorque", 0f, 1000000000f);
            changed |= ClampProperty(block, "MinAngle", -TwoPi, TwoPi);
            changed |= ClampProperty(block, "MaxAngle", -TwoPi, TwoPi);
            changed |= EnsureMinMaxProperties(block, "MinAngle", "MaxAngle");
        }

        if (block is MyPistonBase)
        {
            changed |= ClampSyncField(block, "Velocity", -1000f, 1000f);
            changed |= ClampSyncField(block, "MinLimit", -1000f, 1000f);
            changed |= ClampSyncField(block, "MaxLimit", -1000f, 1000f);
            changed |= ClampSyncField(block, "MaxImpulseAxis", 0f, 1000000000f);
            changed |= ClampSyncField(block, "MaxImpulseNonAxis", 0f, 1000000000f);
            changed |= EnsureMinMaxSyncFields(block, "MinLimit", "MaxLimit");
        }

        if (block is MyMotorSuspension)
        {
            changed |= ClampProperty(block, "Friction", 0f, 100f);
            changed |= ClampProperty(block, "Power", 0f, 100f);
            changed |= ClampProperty(block, "PropulsionOverride", -100f, 100f);
            changed |= ClampProperty(block, "Height", -10f, 10f);
            changed |= ClampProperty(block, "MaxSteerAngle", -TwoPi, TwoPi);
            changed |= ClampProperty(block, "Strength", 0f, 1f);
        }

        return changed;
    }

    private static bool ClampProperty(object owner, string propertyName, float min, float max)
    {
        var property = AccessTools.Property(owner.GetType(), propertyName);
        if (property == null || !property.CanRead || !property.CanWrite)
            return false;

        var value = ReadFloat(property.GetValue(owner, null));
        if (!value.HasValue)
            return false;

        var clamped = ClampFinite(value.Value, min, max);
        if (clamped.Equals(value.Value))
            return false;

        property.SetValue(owner, clamped, null);
        return true;
    }

    private static bool ClampSyncField(object owner, string fieldName, float min, float max)
    {
        var valueProperty = GetSyncValueProperty(owner, fieldName, out var sync);
        if (valueProperty == null)
            return false;

        var value = ReadFloat(valueProperty.GetValue(sync, null));
        if (!value.HasValue)
            return false;

        var clamped = ClampFinite(value.Value, min, max);
        if (clamped.Equals(value.Value))
            return false;

        valueProperty.SetValue(sync, clamped, null);
        return true;
    }

    private static bool EnsureMinMaxProperties(object owner, string minPropertyName, string maxPropertyName)
    {
        var min = ReadFloatProperty(owner, minPropertyName);
        var max = ReadFloatProperty(owner, maxPropertyName);
        if (!min.HasValue || !max.HasValue || min.Value <= max.Value)
            return false;

        var maxProperty = AccessTools.Property(owner.GetType(), maxPropertyName);
        if (maxProperty == null || !maxProperty.CanWrite)
            return false;

        maxProperty.SetValue(owner, min.Value, null);
        return true;
    }

    private static bool EnsureMinMaxSyncFields(object owner, string minFieldName, string maxFieldName)
    {
        var minValueProperty = GetSyncValueProperty(owner, minFieldName, out var minSync);
        var maxValueProperty = GetSyncValueProperty(owner, maxFieldName, out var maxSync);
        if (minValueProperty == null || maxValueProperty == null)
            return false;

        var min = ReadFloat(minValueProperty.GetValue(minSync, null));
        var max = ReadFloat(maxValueProperty.GetValue(maxSync, null));
        if (!min.HasValue || !max.HasValue || min.Value <= max.Value)
            return false;

        maxValueProperty.SetValue(maxSync, min.Value, null);
        return true;
    }

    private static float? ReadFloatProperty(object owner, string propertyName)
    {
        var property = AccessTools.Property(owner.GetType(), propertyName);
        return property == null || !property.CanRead ? null : ReadFloat(property.GetValue(owner, null));
    }

    private static PropertyInfo GetSyncValueProperty(object owner, string fieldName, out object sync)
    {
        sync = null;
        var field = AccessTools.Field(owner.GetType(), fieldName);
        if (field == null)
            return null;

        sync = field.GetValue(owner);
        return sync == null ? null : AccessTools.Property(sync.GetType(), "Value");
    }

    private static float? ReadFloat(object value)
    {
        switch (value)
        {
            case float floatValue:
                return floatValue;
            case double doubleValue:
                return (float)doubleValue;
            default:
                return null;
        }
    }

    private static float ClampFinite(float value, float min, float max)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return Math.Max(0f, min);

        return MathHelper.Clamp(value, min, max);
    }
}
#endif
