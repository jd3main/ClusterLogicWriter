using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ClusterVR.CreatorKit;
using ClusterVR.CreatorKit.Gimmick;


namespace ClusterLogicWriter
{
    using Operator = ClusterVR.CreatorKit.Operation.Operator;
    using ExpressionType = ClusterVR.CreatorKit.Operation.ExpressionType;
    using ValueType = ClusterVR.CreatorKit.Operation.ValueType;
    using _TargetStateTarget = ClusterVR.CreatorKit.Operation.TargetStateTarget;
    using _SourceStateTarget = ClusterVR.CreatorKit.Gimmick.GimmickTarget;

    using _Logic = ClusterVR.CreatorKit.Operation.Logic;

    [Serializable]
    public class Logic
    {
        [SerializeField] Statement[] statements;
        public Statement[] Statements { get => statements; set { statements = value; } }

        public bool IsValid()
        {
            return statements != null && statements.All(s => s == null || s.IsValid());
        }

        public static explicit operator _Logic(Logic logic)
        {
            string json = JsonUtility.ToJson(logic);
            _Logic _logic = JsonUtility.FromJson<_Logic>(json);
            return _logic;
        }

        public static explicit operator Logic(_Logic _logic)
        {
            string json = JsonUtility.ToJson(_logic);
            Logic logic = JsonUtility.FromJson<Logic>(json);
            return logic;
        }
    }

    [Serializable]
    public class Statement
    {
        [SerializeField] SingleStatement singleStatement;
        public SingleStatement SingleStatement { get => singleStatement; set { singleStatement = value; } }

        public TargetState TargetState => SingleStatement.TargetState;
        public Expression Expression => SingleStatement.Expression;

        public Statement(TargetState targetState, Expression expression)
        {
            SingleStatement = new SingleStatement();
            SingleStatement.TargetState = targetState;
            SingleStatement.Expression = expression;
        }

        public bool IsValid()
        {
            return singleStatement != null && singleStatement.IsValid();
        }
    }

    [Serializable]
    public class SingleStatement
    {
        [SerializeField] TargetState targetState;
        [SerializeField] Expression expression;
        public TargetState TargetState { get => targetState; set { targetState = value; } }
        public Expression Expression { get => expression; set { expression = value; } }

        public bool IsValid()
        {
            if (targetState == null || !targetState.IsValid())
            {
                return false;
            }
            if (targetState.ParameterType == ParameterType.Signal)
            {
                return expression == null || (expression.IsValid(out var parameterType) && parameterType.CanCastToValue());
            }
            else
            {
                return expression != null && expression.IsValid(out var parameterType) &&
                    ParameterTypeExtensions.TryGetCommonType(targetState.ParameterType, parameterType, out _);
            }
        }
    }

    [Serializable]
    public class Expression
    {
        [SerializeField] ExpressionType type;
        [SerializeField] Value value;
        [SerializeField] OperatorExpression operatorExpression;
        public ExpressionType Type { get => type; set { type = value; } }
        public Value Value { get => value; set { this.value = value; } }
        public OperatorExpression OperatorExpression { get => operatorExpression; set { operatorExpression = value; } }

        public bool IsValid(out ParameterType parameterType)
        {
            switch (type)
            {
                case ExpressionType.Value:
                    if (value == null)
                    {
                        parameterType = default;
                        return false;
                    }
                    else
                    {
                        return value.IsValid(out parameterType);
                    }
                case ExpressionType.OperatorExpression:
                    if (operatorExpression == null)
                    {
                        parameterType = default;
                        return false;
                    }
                    else
                    {
                        return operatorExpression.IsValid(out parameterType);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }


    [Serializable]
    public class OperatorExpression
    {
        [SerializeField] Operator @operator;

        [SerializeField] Expression[] operands;
        public Operator Operator { get => @operator; set { @operator = value; } }
        public Expression[] Operands { get => operands; set { operands = value; } }

        public bool IsValid(out ParameterType parameterType)
        {
            var requiredLength = @operator.GetRequiredLength();
            if (operands == null || operands.Length < requiredLength)
            {
                parameterType = default;
                return false;
            }

            switch (@operator)
            {
                case Operator.Not:
                {
                    parameterType = ParameterType.Bool;
                    return operands[0].IsValid(out var type) && type.CanCastToValue();
                }
                case Operator.Minus:
                case Operator.Sqrt:
                {
                    return operands[0].IsValid(out parameterType);
                }
                case Operator.Length:
                {
                    parameterType = ParameterType.Double;
                    return operands[0].IsValid(out _);
                }
                case Operator.Add:
                case Operator.Subtract:
                case Operator.Min:
                case Operator.Max:
                {
                    if (!operands[0].IsValid(out var type1) || !operands[1].IsValid(out var type2))
                    {
                        parameterType = default;
                        return false;
                    }
                    else
                    {
                        return ParameterTypeExtensions.TryGetCommonType(type1, type2, out parameterType);
                    }
                }
                case Operator.Multiply:
                {
                    if (operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2))
                    {
                        if (type1.CanCastToVector())
                        {
                            parameterType = type1;
                            return type2.CanCastToValue();
                        }
                        else if(type2.CanCastToVector())
                        {
                            parameterType = type2;
                            return type1.CanCastToValue();
                        }
                        else
                        {
                            return ParameterTypeExtensions.TryGetCommonType(type1, type2, out parameterType);
                        }
                    }
                    else
                    {
                        parameterType = default;
                        return false;
                    }
                }
                case Operator.Modulo:
                case Operator.Divide:
                {
                    if (operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2) && type2.CanCastToValue())
                    {
                        if (type1.CanCastToVector())
                        {
                            parameterType = type1;
                            return true;
                        }
                        else
                        {
                            return ParameterTypeExtensions.TryGetCommonType(type1, type2, out parameterType);
                        }
                    }
                    else
                    {
                        parameterType = default;
                        return false;
                    }
                }
                case Operator.Equals:
                case Operator.NotEquals:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                {
                    parameterType = ParameterType.Bool;
                    return operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2) &&
                        ParameterTypeExtensions.TryGetCommonType(type1, type2, out _);
                }
                case Operator.And:
                case Operator.Or:
                {
                    parameterType = ParameterType.Bool;
                    return operands[0].IsValid(out var type1) && type1.CanCastToValue() &&
                        operands[1].IsValid(out var type2) && type2.CanCastToValue();
                }
                case Operator.Dot:
                {
                    parameterType = ParameterType.Double;
                    return operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2) &&
                        ParameterTypeExtensions.TryGetCommonType(type1, type2, out _);
                }
                case Operator.Cross:
                {
                    parameterType = ParameterType.Vector3;
                    return operands[0].IsValid(out var type1) && type1.CanCastToVector() &&
                        operands[1].IsValid(out var type2) && type2.CanCastToVector();
                }
                case Operator.Rotate:
                {
                    if (operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2))
                    {
                        if (type1 == ParameterType.Vector3)
                        {
                            parameterType = ParameterType.Vector3;
                            return type2.CanCastToVector();
                        }
                        else if (type1.CanCastToValue())
                        {
                            parameterType = ParameterType.Vector2;
                            return type2.CanCastToVector();
                        }
                        else
                        {
                            parameterType = default;
                            return false;
                        }
                    }
                    else
                    {
                        parameterType = default;
                        return false;
                    }
                }
                case Operator.Condition:
                {
                    if (operands[0].IsValid(out var type1) && type1.CanCastToValue() &&
                        operands[1].IsValid(out var type2) && operands[2].IsValid(out var type3) &&
                        ParameterTypeExtensions.TryGetCommonType(type2, type3, out parameterType))
                    {
                        return true;
                    }
                    else
                    {
                        parameterType = default;
                        return false;
                    }
                }
                case Operator.Clamp:
                {
                    if (operands[0].IsValid(out var type1) && operands[1].IsValid(out var type2) && operands[2].IsValid(out var type3))
                    {
                        return ParameterTypeExtensions.TryGetCommonType(type1, type2, out parameterType) &&
                            ParameterTypeExtensions.TryGetCommonType(parameterType, type3, out parameterType);
                    }
                    else
                    {
                        parameterType = default;
                        return false;
                    }
                }
                default: throw new NotImplementedException();
            }
        }
    }

    [Serializable]
    public class Value
    {
        [SerializeField] ValueType type;
        [SerializeField] public ConstantValue constant;
        [SerializeField] SourceState sourceState;
        public ValueType Type { get => type; set { type = value; } }
        public IStateValueSet Constant { get => constant.StateValueSet; }
        public SourceState SourceState { get => sourceState; set { sourceState = value; } }

        public bool IsValid(out ParameterType parameterType)
        {
            switch (type)
            {
                case ValueType.Constant:
                    if (constant == null)
                    {
                        parameterType = default;
                        return false;
                    }
                    else
                    {
                        parameterType = constant.Type;
                        return constant.IsValid();
                    }
                case ValueType.RoomState:
                    if (sourceState == null)
                    {
                        parameterType = default;
                        return false;
                    }
                    else
                    {
                        parameterType = sourceState.Type;
                        return sourceState.IsValid();
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public static class OperatorExtensions
    {
        public static int GetRequiredLength(this Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Not:
                case Operator.Minus:
                case Operator.Length:
                case Operator.Sqrt:
                    return 1;
                case Operator.Add:
                case Operator.Multiply:
                case Operator.Subtract:
                case Operator.Divide:
                case Operator.Modulo:
                case Operator.Equals:
                case Operator.NotEquals:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                case Operator.And:
                case Operator.Or:
                case Operator.Min:
                case Operator.Max:
                case Operator.Dot:
                case Operator.Cross:
                case Operator.Rotate:
                    return 2;
                case Operator.Condition:
                case Operator.Clamp:
                    return 3;
                default: throw new NotImplementedException();
            }
        }
    }

    [Serializable]
    public class TargetState : IRoomState
    {
        public static readonly List<ParameterType> SelectableTypes = new List<ParameterType>(6)
            { ParameterType.Signal, ParameterType.Bool, ParameterType.Float, ParameterType.Integer, ParameterType.Vector2, ParameterType.Vector3 };

        [SerializeField] LogicScope target;
        [SerializeField, StateKeyString] string key;
        [SerializeField] ParameterType parameterType;

        public LogicScope Target { get => target; set { target = value; } }
        public string Key { get => key; set { key = value; } }
        public ParameterType ParameterType { get => parameterType; set { parameterType = value; } }
        public ParameterType Type { get => parameterType; set { parameterType = value; } }


        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(key) && SelectableTypes.Contains(parameterType);
        }

        public bool SameTarget(IRoomState state)
        {
            if (state == null)
                return false;
            return target == state.Target
                && key == state.Key;
        }
    }


    [Serializable]
    public class ConstantValue
    {
        public static readonly List<ParameterType> SelectableTypes = new List<ParameterType>(5)
            { ParameterType.Bool, ParameterType.Float, ParameterType.Integer, ParameterType.Vector2, ParameterType.Vector3 };

        [SerializeField] public ParameterType type = ParameterType.Bool;
        [SerializeField] public bool boolValue;
        [SerializeField] public float floatValue;
        [SerializeField] public int integerValue;
        [SerializeField] public Vector2 vector2Value;
        [SerializeField] public Vector3 vector3Value;

        public ParameterType Type { get => type; set { type = value; } }

        public IStateValueSet StateValueSet
        {
            get
            {
                switch (type)
                {
                    case ParameterType.Bool:
                        return new BoolStateValueSet(boolValue);
                    case ParameterType.Float:
                        return new FloatStateValueSet(floatValue);
                    case ParameterType.Integer:
                        return new IntegerStateValueSet(integerValue);
                    case ParameterType.Vector2:
                        return new Vector2StateValueSet(vector2Value);
                    case ParameterType.Vector3:
                        return new Vector3StateValueSet(vector3Value);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsValid()
        {
            return SelectableTypes.Contains(type);
        }
    }

    [Serializable]
    public class SourceState : IRoomState
    {
        public static readonly List<ParameterType> SelectableTypes = new List<ParameterType>(6)
            { ParameterType.Double, ParameterType.Bool, ParameterType.Float, ParameterType.Integer, ParameterType.Vector2, ParameterType.Vector3 };

        [SerializeField] LogicScope target;
        [SerializeField, StateKeyString] string key;
        [SerializeField] ParameterType type = ParameterType.Double;

        public LogicScope Target { get => target; set { target = value; } }
        public string Key { get => key; set { key = value; } }
        public ParameterType Type { get => type; set { type = value; } }

        public bool IsValid()
        {
            var typeIsValid = SelectableTypes.Contains(type);
            var keyIsValid = !string.IsNullOrWhiteSpace(key);
            return typeIsValid && keyIsValid;
        }

        public bool SameTarget(IRoomState state)
        {
            if (state == null)
                return false;
            return target == state.Target
                && key == state.Key;
        }
    }

    public interface IRoomState
    {
        LogicScope Target { get; set; }
        string Key { get; set; }
        ParameterType Type { get; set; }
        bool SameTarget(IRoomState state);
    }
}

