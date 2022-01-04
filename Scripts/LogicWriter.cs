using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

using ClusterVR.CreatorKit;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit.Operation.Implements;

using System.Reflection;
using System;
using System.Linq;
using System.Text;


namespace ClusterLogicWriter
{
    using ValueType = ClusterVR.CreatorKit.Operation.ValueType;


    public class LogicWriter : MonoBehaviour
    {
        [SerializeField]
        private Component _logicComponent;
        public ILogic logicComponent
        {
            get => (ILogic)_logicComponent;
            set { _logicComponent = (Component)value; }
        }

        [SerializeField]
        private string _logicCode = "";
        public string logicCode { get => _logicCode; set { _logicCode = value; } }

        public string debugOutput;

        public bool omitCurrentTarget = true;


        private void Reset()
        {
            logicComponent = GetComponent<ILogic>();
        }


        public void Extract()
        {
            Logic logic = logicComponent.Logic;
            logicCode = ToString(logic.Statements);
        }

        public string ToString(IEnumerable<Statement> statements)
        {
            StringBuilder txt = new StringBuilder();
            foreach (Statement stm in statements)
            {
                txt.AppendLine(ToString(stm));
            }
            return txt.ToString();
        }

        public string ToString(Statement stm)
        {
            SingleStatement single = stm.SingleStatement;
            TargetState targetState = single.TargetState;
            Expression expression = single.Expression;

            string expText = ToString(expression);

            var targetType = targetState.ParameterType;
            string assignSymbol = (targetType == ParameterType.Signal) ? "<-" : "=";

            if (omitCurrentTarget && targetState.Target == GetLogicScope().ToTargetStateTarget())
            {
                return $"{targetState.Key} {assignSymbol} {expText}";
            }

            return $"{targetState.Target}.{targetState.Key} {assignSymbol} {expText}";

        }

        public string ToString(Expression exp)
        {
            switch (exp.Type)
            {
                case ExpressionType.Value:
                    return ToString(exp.Value);
                case ExpressionType.OperatorExpression:
                    return ToString(exp.OperatorExpression);
                default:
                    throw new ArgumentOutOfRangeException($"Invalid expression type");
            }
        }

        public string ToString(Value valExp)
        {
            switch (valExp.Type)
            {
                case ValueType.Constant:
                    var valueObject = GetStateValue(valExp.Constant);
                    var valueStr = valueObject.ToString();
                    var type = valueObject.GetType();
                    if (type == typeof(float) && !valueStr.Contains("."))
                        valueStr += ".0";
                    if (type == typeof(Vector2))
                        valueStr = "Vector2" + valueStr;
                    if (type == typeof(Vector3))
                        valueStr = "Vector3" + valueStr;
                    return valueStr;
                case ValueType.RoomState:
                    var key = valExp.SourceState.Key;
                    var target = valExp.SourceState.Target;
                    if (omitCurrentTarget && target == GetLogicScope().ToGimmickTarget())
                        return key;
                    return $"{target}.{key}";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid value type");
            }
        }

        public string ToString(OperatorExpression opExp)
        {
            switch (opExp.Operator)
            {
                case Operator.Not:
                    return $"!{ToString(opExp.Operands[0])}";
                case Operator.Minus:
                    return $"-{ToString(opExp.Operands[0])}";

                case Operator.Add:
                    return $"{ToString(opExp.Operands[0])} + {ToString(opExp.Operands[1])}";
                case Operator.Multiply:
                    return $"{ToString(opExp.Operands[0])} * {ToString(opExp.Operands[1])}";
                case Operator.Subtract:
                    return $"{ToString(opExp.Operands[0])} - {ToString(opExp.Operands[1])}";
                case Operator.Divide:
                    return $"{ToString(opExp.Operands[0])} / {ToString(opExp.Operands[1])}";
                case Operator.Modulo:
                    return $"{ToString(opExp.Operands[0])} % {ToString(opExp.Operands[1])}";

                case Operator.Equals:
                    return $"{ToString(opExp.Operands[0])} == {ToString(opExp.Operands[1])}";
                case Operator.NotEquals:
                    return $"{ToString(opExp.Operands[0])} != {ToString(opExp.Operands[1])}";
                case Operator.GreaterThan:
                    return $"{ToString(opExp.Operands[0])} > {ToString(opExp.Operands[1])}";
                case Operator.GreaterThanOrEqual:
                    return $"{ToString(opExp.Operands[0])} >= {ToString(opExp.Operands[1])}";
                case Operator.LessThan:
                    return $"{ToString(opExp.Operands[0])} < {ToString(opExp.Operands[1])}";
                case Operator.LessThanOrEqual:
                    return $"{ToString(opExp.Operands[0])} <= {ToString(opExp.Operands[1])}";

                case Operator.And:
                    return $"{ToString(opExp.Operands[0])} && {ToString(opExp.Operands[1])}";
                case Operator.Or:
                    return $"{ToString(opExp.Operands[0])} || {ToString(opExp.Operands[1])}";

                case Operator.Condition:
                    return $"{ToString(opExp.Operands[0])} ? {ToString(opExp.Operands[1])} :  {ToString(opExp.Operands[2])}";

                default:
                    string s = opExp.Operator.ToString() + "(";
                    for (int i = 0; i < opExp.Operands.Length; i++)
                    {
                        if (i > 0)
                            s += ",";
                        s += ToString(opExp.Operands[i]);
                    }
                    s += ")";
                    return s;
            }
        }


        public void Compile()
        {
            Statement[] statements = ParseStatements(logicCode);

            debugOutput = "";
            debugOutput += ToString(statements);

            Set(logicComponent.Logic, "statements", statements);
        }

        public Statement[] ParseStatements(string s)
        {
            List<Statement> statements = new List<Statement>();

            string[] lines = s.Split(new char[] { '\n', ';' });
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Statement stm = ParseStatement(line);
                if (stm != null)
                    statements.Add(stm);
            }
            return statements.ToArray();
        }

        public Statement ParseStatement(string s)
        {
            if (s == "")
                return null;

            List<string> tokens = Tokenize(s);

            // Debug.Log($"ParseStatement({TokensString(tokens)})");

            int assignIndex = Math.Max(tokens.IndexOf("="), tokens.IndexOf("<-"));
            if (assignIndex < 0)
                throw new Exception("= or <- needed");

            AssignType assignType;

            if (tokens[assignIndex] == "=")
                assignType = AssignType.Assign;
            else
                assignType = AssignType.Signal;



            TargetState targetState = ParseTarget(tokens.Take(assignIndex).ToList());
            Expression expression = ParseExpression(tokens.Skip(assignIndex + 1).ToList());

            if (assignType == AssignType.Signal)
                Set(targetState, "parameterType", ParameterType.Signal);
            else
            {
                var expectedType = CheckParameterType(expression);
                Set(targetState, "parameterType", expectedType);
            }


            // Construct the statement
            Statement stm = new Statement();
            SingleStatement single = new SingleStatement();
            Set(stm, "singleStatement", single);
            Set(single, "targetState", targetState);
            Set(single, "expression", expression);

            return stm;
        }


        public List<string> Tokenize(string s)
        {
            var tokens = new List<string>();

            string token = "";

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char lastChar = (i > 0) ? s[i - 1] : ' ';

                if (char.IsWhiteSpace(c))
                {
                    tokens.Add(token);
                    token = "";
                }
                else if (char.IsDigit(c))
                {
                    if (char.IsLetterOrDigit(lastChar) || ".".Contains(lastChar))
                        token += c;
                    else
                    {
                        tokens.Add(token);
                        token = c.ToString();
                    }
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    if (char.IsLetterOrDigit(lastChar) || lastChar == '_')
                    {
                        token += c;
                    }
                    else
                    {
                        tokens.Add(token);
                        token = c.ToString();
                    }
                }
                else if (c == '.')
                {
                    if (char.IsDigit(lastChar))
                        token += c;
                    else
                    {
                        tokens.Add(token);
                        token = c.ToString();
                    }
                }
                else if (c == '+')
                {
                    tokens.Add(token);
                    token = "+";
                }
                else if (c == '-')
                {
                    if (token == "<")
                    {
                        tokens.Add("<-");
                        token = "";
                    }
                    else
                    {
                        tokens.Add(token);
                        token = "-";
                    }
                }
                else if ("*/%".Contains(c))
                {
                    tokens.Add(token);
                    tokens.Add(c.ToString());
                    token = "";
                }
                else if ("&|".Contains(c))
                {
                    if (token == c.ToString())
                    {
                        tokens.Add(token + token);
                        token = "";
                    }
                    else
                    {
                        tokens.Add(token);
                        token = c.ToString();
                    }
                }
                else if (c == '<' || c == '>')
                {
                    tokens.Add(token);
                    token = c.ToString();
                }
                else if (c == '=')
                {
                    if (token != "" && "<=>!".Contains(token))
                    {
                        tokens.Add(token + "=");
                        token = "";
                    }
                    else
                    {
                        tokens.Add(token);
                        token = "=";
                    }
                }
                else if (c == '(' || c == ')')
                {
                    tokens.Add(token);
                    token = c.ToString();
                }
                else if (c == '!')
                {
                    tokens.Add(token);
                    token = "!";
                }
                else if (c == '?')
                {
                    tokens.Add(token);
                    token = "?";
                }
                else if (c == ':')
                {
                    tokens.Add(token);
                    token = ":";
                }
                else if (c == ',')
                {
                    tokens.Add(token);
                    token = ",";
                }
            }

            tokens.Add(token);

            // Remove empty tokens
            tokens = tokens.Where((t) => { return t.Length > 0; }).ToList();

            tokens = ProcessSignTokens(tokens);

            return tokens;
        }

        public List<string> ProcessSignTokens(IList<string> tokens)
        {
            var mergedTokens = new List<string>();
            mergedTokens.Add(tokens[0]);
            mergedTokens.Add(tokens[1]);
            for (int i = 2; i < tokens.Count; i++)
            {
                if (GetTokenType(tokens[i - 2]) != TokenType.Numeric
                    && GetTokenType(tokens[i - 2]) != TokenType.Name
                    && GetTokenType(tokens[i - 2]) != TokenType.TargetState
                    && (tokens[i - 1] == "+" || tokens[i - 1] == "-")
                    && GetTokenType(tokens[i]) == TokenType.Numeric)
                {
                    mergedTokens[mergedTokens.Count - 1] += tokens[i];
                }
                else
                {
                    mergedTokens.Add(tokens[i]);
                }
            }

            return mergedTokens;
        }

        public TokenType GetTokenType(string t)
        {
            if (t == ".")
                return TokenType.Dot;
            if (t == ",")
                return TokenType.Comma;
            if (t == "(" || t==")")
                return TokenType.Bracket;

            if (double.TryParse(t, out var val))
                return TokenType.Numeric;

            if (char.IsLetter(t[0]) && Enum.TryParse(t, false, out TargetStateTarget targetState))
            {
                return TokenType.TargetState;
            }

            if (t.All((c) => { return char.IsLetter(c) || char.IsDigit(c) || c == '_'; }))
                return TokenType.Name;

            return TokenType.Operator;
        }

        public ParameterType CheckParameterType(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Value:
                    return CheckParameterType(expression.Value);

                case ExpressionType.OperatorExpression:
                    return CheckParameterType(expression.OperatorExpression);

                default:
                    throw new ArgumentOutOfRangeException("Invalid expression type");
            }
        }

        public ParameterType CheckParameterType(Value value)
        {
            switch (value.Type)
            {
                case ValueType.Constant:
                    return value.Constant.ParameterType;
                case ValueType.RoomState:
                    return value.SourceState.Type;
                default:
                    throw new ArgumentOutOfRangeException("Invalid value type");
            }
        }

        public ParameterType CheckParameterType(OperatorExpression opExp)
        {
            opExp.IsValid(out ParameterType type);
            return type;
        }

        public TargetState ParseTarget(IList<string> tokens)
        {
            //Debug.Log($"ParseTarget({TokensString(tokens)})");

            Value targetValue = ParseValueExpression(tokens);
            TargetStateTarget targetScope = targetValue.SourceState.Target.ToTargetStateTarget();
            string key = targetValue.SourceState.Key;

            ParameterType targetType = targetValue.SourceState.Type;
            if (targetType == ParameterType.Double)
                targetType = ParameterType.Float;

            TargetState targetState = new TargetState();
            Set(targetState, "parameterType", targetType);
            Set(targetState, "target", targetScope);
            Set(targetState, "key", key);
            return targetState;
        }

        public Expression ParseExpression(IList<string> tokens)
        {
            //Debug.Log($"ParseExpression({TokensString(tokens)})");

            Expression exp = new Expression();

            if (TryParseFunctionName(tokens[0], out Operator op))
            {
                Set(exp, "type", ExpressionType.OperatorExpression);
                Set(exp, "operatorExpression", ParseFunctionCall(tokens));
            }
            else if (tokens.Any((t) => (GetTokenType(t) == TokenType.Operator)))
            {
                Set(exp, "type", ExpressionType.OperatorExpression);
                Set(exp, "operatorExpression", ParseOperatorExpression(tokens));
            }
            else
            {
                Set(exp, "type", ExpressionType.Value);
                Set(exp, "value", ParseValueExpression(tokens));
            }

            return exp;
        }

        public OperatorExpression ParseFunctionCall(IList<string> tokens)
        {
            //Debug.Log($"ParseFunctionCall({TokensString(tokens)})");

            Operator op = new Operator();

            Assert.IsTrue(TryParseFunctionName(tokens[0], out op));
            Assert.AreEqual(tokens[1], "(");
            Assert.AreEqual(tokens.Last(), ")");

            int numArgs = op.GetRequiredLength();

            var operands = new List<Expression>();

            List<string> paramTokens = new List<string>();
            int depth = 0;
            for (int i = 2; i < tokens.Count; i++)
            {
                string t = tokens[i];

                if ("),".Contains(t) && depth == 0)
                {
                    operands.Add(ParseExpression(paramTokens));
                    paramTokens.Clear();
                }
                else
                {
                    paramTokens.Add(t);
                }


                if (t == "(")
                    depth += 1;
                if (t == ")")
                    depth -= 1;
            }

            if (numArgs != operands.Count)
            {
                throw new Exception($"Function {Enum.GetName(typeof(Operator), op)} requires {numArgs} arguments but get {operands.Count}");
            }

            switch (op)
            {
                case Operator.Length:
                case Operator.Cross:
                case Operator.Dot:
                    foreach (var operand in operands)
                    {
                        if (operand.Value.Type == ValueType.RoomState)
                        {
                            Set(operand.Value.SourceState, "type", ParameterType.Vector3);
                        }
                    }
                    break;
            }

            var opExp = new OperatorExpression();
            Set(opExp, "operator", op);
            Set(opExp, "operands", operands.ToArray());
            return opExp;
        }

        public OperatorExpression ParseOperatorExpression(IList<string> tokens)
        {
            //Debug.Log($"ParseOperatorExpression({TokensString(tokens)})");

            Operator op = 0;
            List<Expression> operands = new List<Expression>();

            List<string> operandTokens = new List<string>();
            foreach (var t in tokens)
            {
                if (TryParseOperator(t, out Operator _op))
                {
                    op = _op;
                    operands.Add(ParseExpression(operandTokens));
                    operandTokens.Clear();
                }
                else
                {
                    operandTokens.Add(t);
                }
            }
            operands.Add(ParseExpression(operandTokens));
            operandTokens.Clear();

            OperatorExpression opExp = new OperatorExpression();
            Set(opExp, "operator", op);
            Set(opExp, "operands", operands.ToArray());
            return opExp;
        }

        public Value ParseValueExpression(IList<string> tokens)
        {
            //Debug.Log($"ParseValueExpression({TokensString(tokens)})");

            /* A Value can be:
             *      1. RoomState
             *      2. Constant
             *          * boolean
             *          * vectors
             *          * numeric
             **/

            Value value = new Value();

            if (char.IsLetter(tokens[0][0])
                && tokens[0] != "Vector2" && tokens[0] != "Vector3"
                && tokens[0].ToLower() != "true" && tokens[0].ToLower() != "false")
            {   // Is RoomState
                GimmickTarget gimmickTarget;
                string key;
                if (Enum.TryParse<GimmickTarget>(tokens[0], false, out gimmickTarget))
                {
                    key = tokens[2];
                }
                else
                {
                    gimmickTarget = GetLogicScope().ToGimmickTarget();
                    key = tokens[0];
                }

                Set(value, "type", ValueType.RoomState);
                Set(value, "sourceState", new SourceState());
                Set(value.SourceState, "target", gimmickTarget);    // Item, Player, Global
                Set(value.SourceState, "key", key);
            }
            else
            {   // Is Constant
                Set(value, "type", ValueType.Constant);
                ConstantValue constantValue = new ConstantValue();

                switch (tokens[0])
                {
                    case "true":
                    case "True":
                        Set(constantValue, "type", ParameterType.Bool);
                        Set(constantValue, "boolValue", true);
                        break;
                    case "false":
                    case "False":
                        Set(constantValue, "type", ParameterType.Bool);
                        Set(constantValue, "boolValue", false);
                        break;
                    case "Vector2":
                        Set(constantValue, "type", ParameterType.Vector2);
                        Set(constantValue, "vector2Value", ParseVector2(tokens));
                        break;
                    case "Vector3":
                        Set(constantValue, "type", ParameterType.Vector3);
                        Set(constantValue, "vector3Value", ParseVector3(tokens));
                        break;
                    default:
                        constantValue = ParseNumericValue(tokens[0]);
                        break;
                }

                Set(value, "constant", constantValue);
            }
            return value;
        }

        public LogicScope GetLogicScope()
        {
            if (logicComponent != null)
            {
                Type type = logicComponent.GetType();
                if (type == typeof(ItemLogic))
                    return LogicScope.Item;
                if (type == typeof(PlayerLogic))
                    return LogicScope.Player;
                if (type == typeof(GlobalLogic))
                    return LogicScope.Global;
            }
            return new LogicScope();
        }


        public Vector2 ParseVector2(IList<string> tokens)
        {
            Assert.AreEqual(tokens[0], "Vector2");
            Assert.AreEqual(tokens[1], "(");
            float p1 = float.Parse(tokens[2]);
            Assert.AreEqual(tokens[3], ",");
            float p2 = float.Parse(tokens[4]);
            Assert.AreEqual(tokens[5], ")");
            return new Vector2(p1, p2);
        }

        public Vector3 ParseVector3(IList<string> tokens)
        {
            Assert.AreEqual(tokens[0], "Vector3");
            Assert.AreEqual(tokens[1], "(");
            float p1 = float.Parse(tokens[2]);
            Assert.AreEqual(tokens[3], ",");
            float p2 = float.Parse(tokens[4]);
            Assert.AreEqual(tokens[5], ",");
            float p3 = float.Parse(tokens[6]);
            Assert.AreEqual(tokens[7], ")");
            return new Vector3(p1, p2, p3);
        }


        public ConstantValue ParseNumericValue(string token)
        {
            ConstantValue constValue = new ConstantValue();

            if (token.Contains("."))
            {
                Set(constValue, "type", ParameterType.Float);
                Set(constValue, "floatValue", float.Parse(token));
            }
            else
            {
                Set(constValue, "type", ParameterType.Integer);
                Set(constValue, "integerValue", int.Parse(token));
            }
            return constValue;
        }


        public bool TryParseFunctionName(string str, out Operator op)
        {
            if (char.IsLetter(str[0]))
            {
                bool isOperatorFunc = Enum.TryParse(str, false, out op);
                if (isOperatorFunc)
                    return true;
            }
            op = new Operator();
            return false;
        }

        public static bool TryParseOperator(string str, out Operator op)
        {
            switch (str)
            {
                case "!":
                    op = Operator.Not;
                    return true;
                case "-":
                    op = Operator.Minus;
                    return true;
                case "+":
                    op = Operator.Add;
                    return true;
                case "*":
                    op = Operator.Multiply;
                    return true;
                case "/":
                    op = Operator.Divide;
                    return true;

                case "%":
                    op = Operator.Modulo;
                    return true;
                case "==":
                    op = Operator.Equals;
                    return true;
                case "!=":
                    op = Operator.NotEquals;
                    return true;
                case ">":
                    op = Operator.GreaterThan;
                    return true;
                case ">=":
                    op = Operator.GreaterThanOrEqual;
                    return true;
                case "<":
                    op = Operator.LessThan;
                    return true;
                case "<=":
                    op = Operator.LessThanOrEqual;
                    return true;
                case "&&":
                    op = Operator.And;
                    return true;
                case "||":
                    op = Operator.Or;
                    return true;

                case "?":
                case ":":
                    op = Operator.Condition;
                    return true;

                default:
                    op = new Operator();
                    return false;
            }
        }

        public static AssignType ParseAssignType(string str)
        {
            switch (str)
            {
                case "=":
                    return AssignType.Assign;
                case "<-":
                    return AssignType.Signal;
                default:
                    throw new ArgumentOutOfRangeException("Invalid assign type");
            }
        }


        private static object GetStateValue(IStateValueSet stateValueSet)
        {
            ParameterType parType = stateValueSet.ParameterType;
            switch (parType)
            {
                case ParameterType.Bool:
                    return Get<bool>((BoolStateValueSet)stateValueSet, "value");
                case ParameterType.Float:
                    return Get<float>((FloatStateValueSet)stateValueSet, "value");
                case ParameterType.Integer:
                    return Get<int>((IntegerStateValueSet)stateValueSet, "value");
                case ParameterType.Vector2:
                    return Call<Vector2>((Vector2StateValueSet)stateValueSet, "GetValue");
                case ParameterType.Vector3:
                    return Call<Vector3>((Vector3StateValueSet)stateValueSet, "GetValue");
            }
            return null;
        }

        private static TVal Get<TVal>(object obj, string name)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            TVal value = (TVal)fieldInfo.GetValue(obj);
            return value;
        }

        private static void Set<TVal>(object obj, string name, TVal value)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(obj, value);
        }

        private static TVal Call<TVal>(object obj, string name, object[] parameters = null)
        {
            Type type = obj.GetType();
            MethodInfo methodInfo = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return (TVal)methodInfo.Invoke(obj, parameters);
        }

        private static string TokensString(IEnumerable<string> tokens)
        {
            string s = "";
            foreach (var t in tokens)
            {
                s += $"[{t}] ";
            }
            return s;
        }

    }

    public enum AssignType
    {
        Assign,
        Signal,
    }

    public enum TokenType
    {
        Name,
        Numeric,
        Operator,
        TargetState,
        Dot,
        Comma,
        Bracket,
    }

    public enum LogicScope
    {
        Item,
        Player,
        Global,
    }

    public static class LogicScopeFunctions
    {
        public static GimmickTarget ToGimmickTarget(this LogicScope scope)
        {
            switch (scope)
            {
                case LogicScope.Item:
                    return GimmickTarget.Item;
                case LogicScope.Player:
                    return GimmickTarget.Player;
                case LogicScope.Global:
                    return GimmickTarget.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }

        public static TargetStateTarget ToTargetStateTarget(this LogicScope scope)
        {
            switch (scope)
            {
                case LogicScope.Item:
                    return TargetStateTarget.Item;
                case LogicScope.Player:
                    return TargetStateTarget.Player;
                case LogicScope.Global:
                    return TargetStateTarget.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }

        public static TargetStateTarget ToTargetStateTarget(this GimmickTarget scope)
        {
            switch (scope)
            {
                case GimmickTarget.Item:
                    return TargetStateTarget.Item;
                case GimmickTarget.Player:
                    return TargetStateTarget.Player;
                case GimmickTarget.Global:
                    return TargetStateTarget.Global;
                default:
                    throw new ArgumentOutOfRangeException("Invalid LogicScope");
            }
        }
    }
}