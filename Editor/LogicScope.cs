using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClusterLogicWriter
{
    using GimmickTarget = ClusterVR.CreatorKit.Gimmick.GimmickTarget;
    using TargetStateTarget = ClusterVR.CreatorKit.Operation.TargetStateTarget;

    public enum LogicScopeValue
    {
        Item,
        Player,
        Global,
    }

    [Serializable]
    public class LogicScope : IEquatable<LogicScope>
    {
        public static LogicScopeValue Item => LogicScopeValue.Item;
        public static LogicScopeValue Player => LogicScopeValue.Player;
        public static LogicScopeValue Global => LogicScopeValue.Player;

        public LogicScopeValue Value { get => value; set { this.value = value; } }
        private LogicScopeValue value = 0;


        public LogicScope(LogicScopeValue scopeValue)
        {
            value = scopeValue;
        }

        public LogicScope(GimmickTarget target)
        {
            value = target.ToLogicScopeValue();
        }

        public LogicScope(TargetStateTarget target)
        {
            value = target.ToLogicScopeValue();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override bool Equals(object scope)
        {
            return Equals((LogicScope)scope);
        }

        public bool Equals(LogicScope scope)
        {
            return value == scope.value;
        }

        public static bool operator ==(LogicScope a, LogicScope b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(LogicScope a, LogicScope b)
        {
            return a.value != b.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static implicit operator LogicScope(LogicScopeValue scopeValue)
        {
            return new LogicScope(scopeValue);
        }

        public static implicit operator LogicScope(GimmickTarget target)
        {
            return new LogicScope(target);
        }

        public static implicit operator LogicScope(TargetStateTarget target)
        {
            return new LogicScope(target);
        }

        public static implicit operator LogicScopeValue(LogicScope scope)
        {
            switch (scope.Value)
            {
                case LogicScopeValue.Item:
                    return LogicScopeValue.Item;
                case LogicScopeValue.Player:
                    return LogicScopeValue.Player;
                case LogicScopeValue.Global:
                    return LogicScopeValue.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }

        public static implicit operator GimmickTarget(LogicScope scope)
        {
            switch (scope.Value)
            {
                case LogicScopeValue.Item:
                    return GimmickTarget.Item;
                case LogicScopeValue.Player:
                    return GimmickTarget.Player;
                case LogicScopeValue.Global:
                    return GimmickTarget.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }

        public static implicit operator TargetStateTarget(LogicScope scope)
        {
            switch (scope.Value)
            {
                case LogicScopeValue.Item:
                    return TargetStateTarget.Item;
                case LogicScopeValue.Player:
                    return TargetStateTarget.Player;
                case LogicScopeValue.Global:
                    return TargetStateTarget.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }
    }

    public static class LogicScopeValueExtension
    {
        public static LogicScopeValue ToLogicScopeValue(this GimmickTarget target)
        {
            switch (target)
            {
                case GimmickTarget.Item:
                    return LogicScopeValue.Item;
                case GimmickTarget.Player:
                    return LogicScopeValue.Player;
                case GimmickTarget.Global:
                    return LogicScopeValue.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid GimmickTarget");
            }
        }

        public static LogicScopeValue ToLogicScopeValue(this TargetStateTarget target)
        {
            switch (target)
            {
                case TargetStateTarget.Item:
                    return LogicScopeValue.Item;
                case TargetStateTarget.Player:
                    return LogicScopeValue.Player;
                case TargetStateTarget.Global:
                    return LogicScopeValue.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid TargetStateTarget");
            }
        }
    }

    public static class TargetEquivalence
    {
        public static bool Equals(this GimmickTarget gTarget, TargetStateTarget tsTarget)
        {
            return gTarget.ToString() == tsTarget.ToString();
        }

        public static bool Equals(this TargetStateTarget tsTarget, GimmickTarget gTarget)
        {
            return gTarget.ToString() == tsTarget.ToString();
        }
    }
}