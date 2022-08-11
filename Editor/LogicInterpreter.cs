﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;

using ClusterVR.CreatorKit;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit.Operation.Implements;


namespace ClusterLogicWriter
{
    using ValueType = ClusterVR.CreatorKit.Operation.ValueType;
    using _Logic = ClusterVR.CreatorKit.Operation.Logic;

    [System.Serializable]
    public class LogicInterpreter
    {
        public ILogic logicComponent;
        public bool omitCurrentTarget = true;
        public bool compressStatements = true;
        public bool codeModified = true;

        public List<RoomState> roomStates = new List<RoomState>();

        private string logicCode = "";

        public string LogicCode
        {
            get => logicCode;
            set
            {
                if (value != logicCode)
                {
                    logicCode = value;
                    codeModified = true;
                }
            }
        }

        public LogicInterpreter()
        {
            Debug.Log("LogicInterpreter()");
        }

        public void Extract()
        {
            Logic logic = (Logic)logicComponent.Logic;

            ExtractTypes(logic.Statements);

            if (compressStatements)
            {
                logic.Statements = CompressStatements(logic.Statements).ToArray();
            }

            LogicCode = ToCode(logic.Statements);

            codeModified = false;
        }

        public void Compile()
        {
            Logic logic = ParseLogic(LogicCode);
            logic.Statements = Flatten(logic.Statements).ToArray();

            logicComponent.Set("logic", (_Logic)logic);
            codeModified = false;
        }

        public string ToCode(IEnumerable<Statement> statements)
        {
            StringBuilder txt = new StringBuilder();
            foreach (Statement stm in statements)
            {
                txt.AppendLine(ToCode(stm));
            }
            return txt.ToString();
        }

        public string ToCode(Statement stm)
        {
            return ToCode(stm.SingleStatement);
        }

        public string ToCode(SingleStatement singleStm)
        {
            TargetState targetState = singleStm.TargetState;
            Expression expression = singleStm.Expression;

            string expText = ToCode(expression);

            var targetType = targetState.ParameterType;
            string assignSymbol = (targetType == ParameterType.Signal) ? "<-" : "=";

            if (omitCurrentTarget && targetState.Target == GetLogicScope())
            {
                return $"{targetState.Key} {assignSymbol} {expText}";
            }

            return $"{targetState.Target}.{targetState.Key} {assignSymbol} {expText}";

        }

        public string ToCode(Expression exp)
        {
            switch (exp.Type)
            {
                case ExpressionType.Value:
                    return ToCode(exp.Value);
                case ExpressionType.OperatorExpression:
                    return ToCode(exp.OperatorExpression);
                default:
                    throw new ArgumentOutOfRangeException($"Invalid expression type");
            }
        }

        public string ToCode(Value valExp)
        {
            switch (valExp.Type)
            {
                case ValueType.Constant:
                    var valueObject = valExp.Constant.GetStateValue();
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
                    if (omitCurrentTarget && target == GetLogicScope())
                        return key;
                    return $"{target}.{key}";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid value type");
            }
        }

        public string ToCode(OperatorExpression opExp)
        {
            switch (opExp.Operator)
            {
                case Operator.Not:
                    return $"!{ToCode(opExp.Operands[0])}";
                case Operator.Minus:
                    return $"-{ToCode(opExp.Operands[0])}";

                case Operator.Add:
                    return $"{ToCode(opExp.Operands[0])} + {ToCode(opExp.Operands[1])}";
                case Operator.Multiply:
                    return $"{ToCode(opExp.Operands[0])} * {ToCode(opExp.Operands[1])}";
                case Operator.Subtract:
                    return $"{ToCode(opExp.Operands[0])} - {ToCode(opExp.Operands[1])}";
                case Operator.Divide:
                    return $"{ToCode(opExp.Operands[0])} / {ToCode(opExp.Operands[1])}";
                case Operator.Modulo:
                    return $"{ToCode(opExp.Operands[0])} % {ToCode(opExp.Operands[1])}";

                case Operator.Equals:
                    return $"{ToCode(opExp.Operands[0])} == {ToCode(opExp.Operands[1])}";
                case Operator.NotEquals:
                    return $"{ToCode(opExp.Operands[0])} != {ToCode(opExp.Operands[1])}";
                case Operator.GreaterThan:
                    return $"{ToCode(opExp.Operands[0])} > {ToCode(opExp.Operands[1])}";
                case Operator.GreaterThanOrEqual:
                    return $"{ToCode(opExp.Operands[0])} >= {ToCode(opExp.Operands[1])}";
                case Operator.LessThan:
                    return $"{ToCode(opExp.Operands[0])} < {ToCode(opExp.Operands[1])}";
                case Operator.LessThanOrEqual:
                    return $"{ToCode(opExp.Operands[0])} <= {ToCode(opExp.Operands[1])}";

                case Operator.And:
                    return $"{ToCode(opExp.Operands[0])} && {ToCode(opExp.Operands[1])}";
                case Operator.Or:
                    return $"{ToCode(opExp.Operands[0])} || {ToCode(opExp.Operands[1])}";

                case Operator.Condition:
                    return $"{ToCode(opExp.Operands[0])} ? {ToCode(opExp.Operands[1])} :  {ToCode(opExp.Operands[2])}";

                default:
                    string s = opExp.Operator.ToString() + "(";
                    for (int i = 0; i < opExp.Operands.Length; i++)
                    {
                        if (i > 0)
                            s += ",";
                        s += ToCode(opExp.Operands[i]);
                    }
                    s += ")";
                    return s;
            }
        }

        public void ExtractTypes(IEnumerable<Statement> statements)
        {
            Debug.Log("ExtractTypes()");

            IEnumerable<IRoomState> targetStates = statements.Select(stm => stm.TargetState);

            IEnumerable<IRoomState> sourceStates = statements.Where(stm => stm.Expression.Type == ExpressionType.Value)
                                                              .Select(stm => stm.Expression.Value.SourceState);

            Expression[] operands = statements.Where(stm => stm.Expression.Type == ExpressionType.OperatorExpression)
                                              .Select(stm => stm.Expression.OperatorExpression.Operands)
                                              .Aggregate((a,b) => a.Union(b).ToArray());
            IEnumerable<Expression> roomStateOperands = operands.Where(exp => exp.Value.Type == ValueType.RoomState);
            IEnumerable<IRoomState> operandSourceStates = roomStateOperands.Select(exp => exp.Value.SourceState);

            IEnumerable<IRoomState> states = operandSourceStates.Concat(sourceStates).Concat(targetStates);

            roomStates.Clear();
            foreach (var state in states)
            {
                var found = roomStates.FindIndex(s => s.SameTarget(state));
                if (found == -1)
                {
                    roomStates.Add(new RoomState(state));
                }
                else
                {
                    roomStates[found] = new RoomState(state);
                }
            }
        }


        public Logic ParseLogic(string s)
        {
            List<Statement> statements = new List<Statement>();

            string[] lines = s.Split(new char[] {'\r', '\n', ';' }).Where(line=>!string.IsNullOrWhiteSpace(line)).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Statement stm = ParseStatement(line);
                if (stm != null)
                    statements.Add(stm);
            }

            Logic logic = new Logic();
            logic.Statements = statements.ToArray();

            return logic;
        }

        public List<Statement> Flatten(IEnumerable<Statement> statements)
        {
            var stms = statements.ToList();
            for (int i=0; i<stms.Count; i++)
            {
                if (stms[i].Expression.Type == ExpressionType.OperatorExpression)
                {
                    var operands = stms[i].Expression.OperatorExpression.Operands;
                    for (int j = 0; j < operands.Length; j++)
                    {
                        if (operands[j].Type == ExpressionType.OperatorExpression)
                        {
                            var innerStm = new Statement(stms[i].TargetState, operands[j]);
                            stms.Insert(i, innerStm);

                            operands[j] = new Expression();
                            operands[j].Type = ExpressionType.Value;
                            operands[j].Value = new Value();
                            operands[j].Value.Type = ValueType.RoomState;
                            operands[j].Value.SourceState = new SourceState();
                            operands[j].Value.SourceState.Target = stms[i].TargetState.Target;
                            operands[j].Value.SourceState.Key = stms[i].TargetState.Key;
                            operands[j].Value.SourceState.Type = stms[i].TargetState.Type;
                            Debug.Log(stms[i].TargetState.Type);
                        }
                    }
                }
            }
            return stms;
        }

        public Statement ParseStatement(string s)
        {
            Debug.Log($"ParseStatement({s})");
            if (s == "")
                return null;

            List<string> tokens = Tokenize(s);

            Debug.Log($"tokens = {TokensToString(tokens)}");

            int assignIndex = Math.Max(tokens.IndexOf("="), tokens.IndexOf("<-"));
            if (assignIndex < 0)
                throw new Exception("= or <- needed");

            TargetState targetState = ParseTarget(tokens.Take(assignIndex).ToList());
            AssignType assignType = ParseAssignType(tokens[assignIndex]);
            Expression expression = ParseExpression(tokens.Skip(assignIndex + 1).ToList());

            if (assignType == AssignType.Signal)
                targetState.ParameterType = ParameterType.Signal;
            else
            {
                var expectedType = PredictParameterType(expression);
                targetState.ParameterType = expectedType;
            }

            Debug.Log($"expression = {ToCode(expression)}");

            return new Statement(targetState, expression);
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
                    else if (lastChar == '.' && !Enum.IsDefined(typeof(LogicScope), tokens.Last().TrimEnd('.')))
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
                    else if (!Enum.IsDefined(typeof(LogicScope), tokens.Last()))
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


        /// <summary>
        /// Function <c>ProcessSignTokens</c> merge each sign and the folloing numbers into one token.
        /// </summary>
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

        public ParameterType PredictParameterType(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Value:
                    return PredictParameterType(expression.Value);

                case ExpressionType.OperatorExpression:
                    return PredictParameterType(expression.OperatorExpression);

                default:
                    throw new ArgumentOutOfRangeException("Invalid expression type");
            }
        }

        public ParameterType PredictParameterType(Value value)
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

        public ParameterType PredictParameterType(OperatorExpression opExp)
        {
            opExp.IsValid(out ParameterType type);
            return type;
        }

        public TargetState ParseTarget(IList<string> tokens)
        {
            Debug.Log($"ParseTarget({TokensToString(tokens)})");

            Value targetValue = ParseValueExpression(tokens);
            LogicScope targetScope = targetValue.SourceState.Target;
            string key = targetValue.SourceState.Key;

            ParameterType targetType = targetValue.SourceState.Type;
            if (targetType == ParameterType.Double)
                targetType = ParameterType.Float;

            TargetState targetState = new TargetState();
            targetState.ParameterType = targetType;
            targetState.Target = targetScope;
            targetState.Key = key;
            return targetState;
        }

        public Expression ParseExpression(IList<string> tokens)
        {
            Debug.Log($"ParseExpression({TokensToString(tokens)})");

            Expression exp = new Expression();

            if (TryParseFunctionName(tokens[0], out Operator op))
            {
                exp.Type = ExpressionType.OperatorExpression;
                exp.OperatorExpression = ParseFunctionCall(tokens);
            }
            else if (tokens.Any((t) => (GetTokenType(t) == TokenType.Operator)))
            {
                exp.Type = ExpressionType.OperatorExpression;
                exp.OperatorExpression = ParseOperatorExpression(tokens);
            }
            else
            {
                exp.Type = ExpressionType.Value;
                exp.Value = ParseValueExpression(tokens);
            }

            return exp;
        }

        public OperatorExpression ParseFunctionCall(IList<string> tokens)
        {
            Debug.Log($"ParseFunctionCall({TokensToString(tokens)})");

            Operator op = new Operator();

            Assert.IsTrue(TryParseFunctionName(tokens[0], out op));
            Assert.IsTrue(tokens[1] == "(");


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
                        Debug.Log(ToCode(operand));
                        if (operand.Type == ExpressionType.Value
                            && operand.Value.Type == ValueType.RoomState)
                        {
                            operand.Value.SourceState.Type = ParameterType.Vector3;
                        }
                    }
                    break;
            }

            OperatorExpression opExp = new OperatorExpression();
            opExp.Operator = op;
            opExp.Operands = operands.ToArray();
            return opExp;
        }


        public OperatorExpression ParseOperatorExpression(IList<string> tokens)
        {
            Debug.Log($"ParseOperatorExpression({TokensToString(tokens)})");

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
            opExp.Operator = op;
            opExp.Operands = operands.ToArray();
            return opExp;
        }

        public Value ParseValueExpression(IList<string> tokens)
        {
            Debug.Log($"ParseValueExpression({TokensToString(tokens)})");

            Value value = new Value();
            Debug.Log($"value = {JsonUtility.ToJson(value)}");

            if (IsRoomState(tokens[0]))
            {
                GimmickTarget gimmickTarget;
                string key;
                if (Enum.TryParse(tokens[0], false, out gimmickTarget))
                {
                    key = tokens[2];
                }
                else
                {
                    gimmickTarget = (GimmickTarget)GetLogicScope();
                    key = tokens[0];
                }

                value.Type = ValueType.RoomState;
                value.SourceState = new SourceState();
                value.SourceState.Target = (LogicScope)gimmickTarget;    // Item, Player, Global
                value.SourceState.Key = key;

                value.constant = null;
            }
            else   // Is Constant
            {
                value.Type = ValueType.Constant;
                ConstantValue constantValue = new ConstantValue();

                switch (tokens[0])
                {
                    case "true":
                    case "True":
                        constantValue.Type = ParameterType.Bool;
                        constantValue.boolValue = true;
                        break;
                    case "false":
                    case "False":
                        constantValue.Type = ParameterType.Bool;
                        constantValue.boolValue = false;
                        break;
                    case "Vector2":
                        constantValue.type = ParameterType.Vector2;
                        constantValue.vector2Value = ParseVector2(tokens);
                        break;
                    case "Vector3":
                        constantValue.Type = ParameterType.Vector3;
                        constantValue.vector3Value = ParseVector3(tokens);
                        break;
                    default:
                        constantValue = ParseNumericValue(tokens[0]);
                        break;
                }

                value.constant = constantValue;
                value.SourceState = null;
            }
            Debug.Log($"value.constaint = {value.constant}");
            Debug.Log($"value.SourceState = {value.SourceState}");
            return value;
        }

        public bool IsRoomState(string token)
        {
            return char.IsLetter(token[0])
                && token != "Vector2"
                && token != "Vector3"
                && token.ToLower() != "true"
                && token.ToLower() != "false";
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
                throw new ArgumentOutOfRangeException($"{type} is not a type of Logic Script");
            }
            throw new ArgumentOutOfRangeException($"{logicComponent} not set");
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
            Debug.Log($"ParseNumericValue({token})");
            ConstantValue constValue = new ConstantValue();

            if (token.Contains("."))
            {
                constValue.Type = ParameterType.Float;
                constValue.floatValue = float.Parse(token);
            }
            else
            {
                constValue.Type = ParameterType.Integer;
                constValue.integerValue = int.Parse(token);
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


        public List<Statement> CompressStatements(IEnumerable<Statement> statements)
        {
            List<Statement> stms = statements.ToList();
            for (int i = 1; i < stms.Count; i++)
            {
                if (stms[i].Expression.Type == ExpressionType.OperatorExpression
                    && stms[i].TargetState.Equals(stms[i - 1].TargetState))
                {
                    var operands = stms[i].Expression.OperatorExpression.Operands;
                    for (int j = 0; j < operands.Length; j++)
                    {
                        if (operands[j].Type == ExpressionType.Value
                            && operands[j].Value.Type == ValueType.RoomState
                            && operands[j].Value.SourceState.Equals(stms[i].TargetState))
                        {
                            operands[j] = stms[i - 1].Expression;
                        }
                    }

                    stms.RemoveAt(i - 1);
                    i -= 1;
                }
            }
            return stms;
        }

        private static string TokensToString(IEnumerable<string> tokens)
        {
            string s = "";
            foreach (var t in tokens)
            {
                s += $"[{t}] ";
            }
            return s;
        }

    }


    [Serializable]
    public class RoomState: IRoomState
    {
        public LogicScope target;
        public string key;
        public ParameterType type;

        public LogicScope Target { get => target; set { target = value; } }
        public string Key { get => key; set { key = value; } }
        public ParameterType Type { get => type; set { type = value; } }

        public RoomState(IRoomState state)
        {
            target = state.Target;
            key = state.Key;
            type = state.Type;
        }

        public bool SameTarget(IRoomState state)
        {
            if (state == null)
                return false;
            return target == state.Target
                && key == state.Key;
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
}