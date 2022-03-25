using System;
using System.Reflection;
using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit;
using UnityEngine;

namespace ClusterLogicWriter
{
    public static class ReflectionHelper
    {
        public static TVal _Get<TVal>(this object obj, string name)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            TVal value = (TVal)fieldInfo.GetValue(obj);
            return value;
        }

        public static void _Set<TVal>(this object obj, string name, TVal value)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(obj, value);
        }

        public static TVal _Call<TVal>(this object obj, string name, object[] parameters = null)
        {
            Type type = obj.GetType();
            MethodInfo methodInfo = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return (TVal)methodInfo.Invoke(obj, parameters);
        }


        public static object GetStateValue(this IStateValueSet stateValueSet)
        {
            ParameterType parType = stateValueSet.ParameterType;
            switch (parType)
            {
                case ParameterType.Bool:
                    return _Get<bool>((BoolStateValueSet)stateValueSet, "value");
                case ParameterType.Float:
                    return _Get<float>((FloatStateValueSet)stateValueSet, "value");
                case ParameterType.Integer:
                    return _Get<int>((IntegerStateValueSet)stateValueSet, "value");
                case ParameterType.Vector2:
                    return _Call<Vector2>((Vector2StateValueSet)stateValueSet, "GetValue");
                case ParameterType.Vector3:
                    return _Call<Vector3>((Vector3StateValueSet)stateValueSet, "GetValue");
            }
            return null;
        }
    }
}