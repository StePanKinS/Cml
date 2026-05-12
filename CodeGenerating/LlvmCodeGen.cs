using System.Text;

namespace Cml.CodeGeneration;

public class LlvmCodeGen(IEnumerable<FileDefinition> files)
{
    private IEnumerable<FileDefinition> Files = files;
    private List<string> stringLiterals = [];
    private List<double> doubleLiterals = [];
    private int ifsCounter = 0;
    private int loopsCounter = 0;
    private int valueCounter = 0;
    private Dictionary<Loop, int> loopNumbers = [];

    private static readonly Dictionary<BinaryOperationTypes, string> CompareOps = new()
    {
        { BinaryOperationTypes.Less,          "slt"  },
        { BinaryOperationTypes.LessEquals,    "sle"  },
        { BinaryOperationTypes.Greater,       "sgt"  },
        { BinaryOperationTypes.GreaterEquals, "sge"  },
        { BinaryOperationTypes.IsEquals,      "eq"   },
        { BinaryOperationTypes.NotEquals,     "ne"   },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> MathOps = new()
    {
        { BinaryOperationTypes.Add,        "add"  },
        { BinaryOperationTypes.Subtract,   "sub"  },
        { BinaryOperationTypes.Multiply,   "mul"  },
        { BinaryOperationTypes.BitwiseAnd, "and"  },
        { BinaryOperationTypes.BitwiseOr,  "or"   },
        { BinaryOperationTypes.BitwiseXor, "xor"  },
        { BinaryOperationTypes.LeftShift,  "shl"  },
        { BinaryOperationTypes.RightShift, "lshr" },
    };

    private static readonly Dictionary<BinaryOperationTypes, string> FloatMathOps = new()
    {
        { BinaryOperationTypes.Add,      "fadd" },
        { BinaryOperationTypes.Subtract, "fsub" },
        { BinaryOperationTypes.Multiply, "fmul" },
        { BinaryOperationTypes.Divide,   "fdiv" },
    };

    private (string startLabel, string endLabel) getLoopLabels(Loop loop)
    {
        int num;
        if (!loopNumbers.TryGetValue(loop, out num))
        {
            num = loopsCounter++;
            loopNumbers[loop] = num;
        }
        return ($"loop_start_{num}", $"loop_end_{num}");
    }

    private string getNextValueName()
    {
        return $"%{valueCounter++}";
    }

    private string getLlvmType(Typ type)
    {
        if (type is DefaultType.Integer intType)
            return intType.Size switch
            {
                1 => "i8",
                2 => "i16",
                4 => "i32",
                8 => "i64",
                0 => "i64",
                _ => throw new Exception($"Unknown int size {intType.Size}"),
            };
        else if (type is DefaultType.FloatingPoint fpType)
            return fpType.Size switch
            {
                4 => "float",
                8 => "double",
                _ => throw new Exception($"Unknown float size {fpType.Size}"),
            };
        else if (type == DefaultType.Bool)
            return "i1";
        else if (type == DefaultType.Void)
            return "void";
        else if (type is Pointer ptr)
            return $"ptr";
        else if (type is SizedArray arr)
            return $"[{arr.ElementCount} x {getLlvmType(arr.ElementType)}]";
        else if (type is StructType st)
            return $"%struct.{st.Name}";
        else if (type is EnumType enumType)
            return getLlvmType(enumType.UnderlyingType);
        else if (type is InterfaceType it)
            return "{ ptr, ptr }"; // self, vtable
        else if (type is MethodPointer)
            return "{ ptr, ptr }"; // self, mehtod
        else
            throw new Exception($"Unknown type {type}");
    }

    private string getVtableName(StructType st, InterfaceType it)
        => $"@vtable.{st.Name}-{it.Name}";

    private string getVtableType(InterfaceType it)
        => $"[{it.Methods.Length} x ptr]";

    public string Generate()
        => topLevel();

    private string topLevel()
    {
        StringBuilder sb = new();
        sb.AppendLine("; LLVM IR generated from CML");
        sb.AppendLine("target triple = \"x86_64-unknown-linux-gnu\"");
        sb.AppendLine("target datalayout = \"e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128\"");
        sb.AppendLine();

        foreach (var file in Files)
            generateNamespace(file, sb);

        generateStringGlobals(sb);
        sb.AppendLine();

        generateDoubleGlobals(sb);
        sb.AppendLine();

        return sb.ToString();
    }

    private void generateStructDefinition(StringBuilder sb, StructDefinition def)
    {
        var structType = def.StructType;
        var members = string.Join(", ", structType.Members.Select(m => getLlvmType(m.Type)));
        sb.AppendLine($"%struct.{structType.Name} = type {{ {members} }}");

        foreach (var iface in structType.Interfaces)
        {
            sb.AppendLine($"{getVtableName(structType, iface)} = private unnamed_addr constant {getVtableType(iface)} [");
            var methodPtrs = iface.Methods.Select(m => $"    ptr @{structType.GetMethod(m.name)!.FullName}");
            sb.AppendLine(string.Join(",\n", methodPtrs));
            sb.AppendLine("]");
            sb.AppendLine();
        }
    }

    private void generateStringGlobals(StringBuilder sb)
    {
        for (int i = 0; i < stringLiterals.Count; i++)
        {
            string escapedString = stringLiterals[i]
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\0A")
                .Replace("\r", "\\0D")
                .Replace("\t", "\\09");

            sb.AppendLine($"@str{i} = private unnamed_addr constant [{stringLiterals[i].Length + 1} x i8] c\"{escapedString}\\00\"");
        }
    }

    private void generateDoubleGlobals(StringBuilder sb)
    {
        for (int i = 0; i < doubleLiterals.Count; i++)
        {
            sb.AppendLine($"@double{i} = private unnamed_addr constant double {doubleLiterals[i]:G17}");
        }
    }

    private void generateNamespace(NamespaceDefinition nmsp, StringBuilder sb)
    {
        foreach (var def in nmsp)
        {
            switch (def)
            {
                case NamespaceDefinition ns:
                    generateNamespace(ns, sb);
                    break;

                case FunctionDefinition fn:
                    generateFunction(fn, sb);
                    break;

                case DefaultTypeDefinition:
                case EnumDefinition:
                case InterfaceDefinition:
                    break;

                case StructDefinition sd:
                    generateStructDefinition(sb, sd);
                    foreach (var method in sd.Methods)
                        generateFunction(method, sb);
                    break;

                case VariableDefinition vd:
                    generateVariableDefinition(vd, sb);
                    break;

                default:
                    throw new Exception($"Unknown def: {def}");
            }
        }
    }

    private void generateFunction(FunctionDefinition fn, StringBuilder sb)
    {
        if (fn.Modifyers.Contains(Keywords.External))
        {
            string returnType = getLlvmType(fn.ReturnType);
            var paramTypes = string.Join(", ", fn.Arguments.Arguments.Select(arg => getLlvmType(arg.Type)));
            if (fn.IsVariadic)
                paramTypes = paramTypes.Length == 0 ? "..." : $"{paramTypes}, ...";
            sb.AppendLine($"declare {returnType} @{fn.Name}({paramTypes})");
            sb.AppendLine();
            return;
        }

        string retType = getLlvmType(fn.ReturnType);

        var paramStrings = fn.Arguments.Arguments.Select((arg, idx) => $"{getLlvmType(arg.Type)} %arg{idx}").ToList();
        if (fn.MethodOf != null)
            paramStrings.Insert(0, $"ptr %self");

        var paramList = string.Join(", ", paramStrings);

        string linkage = fn.Modifyers.Contains(Keywords.Internal) ? "internal " : "";
        sb.AppendLine($"define {linkage}{retType} @{fn.FullName}({paramList}) {{");
        sb.AppendLine("entry:");

        int argIdx = 0;
        foreach (var arg in fn.Arguments.Arguments)
        {
            string allocName = $"%{arg.FullName}";
            sb.AppendLine($"    {allocName} = alloca {getLlvmType(arg.Type)}");
            sb.AppendLine($"    store {getLlvmType(arg.Type)} %arg{argIdx}, ptr {allocName}");
            argIdx++;
        }

        if (fn.Code == null)
            throw new Exception($"null code {fn}");
        generateExecutable(fn.Code, fn.Arguments, sb);

        if (!fn.ContainsReturn)
        {
            if (fn.ReturnType != DefaultType.Void)
                throw new Exception($"no return in non void func {fn}");

            sb.AppendLine("    ret void");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void generateVariableDefinition(VariableDefinition vd, StringBuilder sb)
    {
        if (vd.Modifyers.Contains(Keywords.External))
        {
            sb.Append($"@{vd.FullName} = external global {getLlvmType(vd.Type)}");
        }
        else
        {
            sb.Append($"@{vd.FullName} = global {getLlvmType(vd.Type)} undef");
        }
        sb.AppendLine();
    }

    private string generateExecutable(Executable exe, INameContainer locals, StringBuilder sb)
    {
        switch (exe)
        {
            case CodeBlock cb:
                return generateCodeBlock(cb, locals, sb);

            case FunctionCall fc:
                return generateFunctionCall(fc, locals, sb);

            case MethodValue mv:
                return generateMethodValue(mv, locals, sb);

            case VtableLookup vtl:
                return generateVtableLookup(vtl, locals, sb);

            case Identifyer id:
                return generateIdentifier(id, locals, sb);

            case Literal<string> stringLit:
                {
                    int scount = stringLiterals.Count;
                    stringLiterals.Add(stringLit.Value);
                    string result = getNextValueName();
                    sb.AppendLine($"        ; String {stringLit.Location}");
                    sb.AppendLine($"    {result} = getelementptr [{stringLit.Value.Length + 1} x i8], [{stringLit.Value.Length + 1} x i8]* @str{scount}, i64 0, i64 0");
                    return result;
                }

            case Literal<ulong> intLit:
                {
                    sb.AppendLine($"        ; Integer literal {intLit.Location}");
                    return $"{intLit.Value}";
                }

            case Literal<bool> boolLit:
                {
                    sb.AppendLine($"        ; Bool literal {boolLit.Location}");
                    return boolLit.Value ? "1" : "0";
                }

            case Literal<double> doubleLit:
                {
                    int dcount = doubleLiterals.Count;
                    doubleLiterals.Add(doubleLit.Value);
                    string result = getNextValueName();
                    sb.AppendLine($"    {result} = load double, ptr @double{dcount} ; Literal {doubleLit.Location}");
                    return result;
                }

            case BinaryOperation bo:
                return generateBinaryOp(bo, locals, sb);

            case UnaryOperation uo:
                return generateUnaryOp(uo, locals, sb);

            case Return ret:
                {
                    string retVal = generateExecutable(ret.Value, locals, sb);
                    sb.AppendLine($"        ; return {ret.Location}");
                    if (ret.Value.ReturnType == DefaultType.Void)
                        sb.AppendLine("    ret void");
                    else
                        sb.AppendLine($"    ret {getLlvmType(ret.Value.ReturnType)} {retVal}");
                    return "";
                }

            case ControlFlow cf:
                {
                    int ifNum = ifsCounter++;
                    sb.AppendLine($"    ; if {cf.Location}");
                    string condValue = generateExecutable(cf.Condition, locals, sb);
                    string testVal = getNextValueName();
                    sb.AppendLine($"    {testVal} = icmp ne i1 {condValue}, 0");
                    sb.AppendLine($"    br i1 {testVal}, label %if_then_{ifNum}, label %if_else_{ifNum}");

                    sb.AppendLine($"if_then_{ifNum}:");
                    generateExecutable(cf.Body, locals, sb);
                    sb.AppendLine($"    br label %if_end_{ifNum}");

                    sb.AppendLine($"if_else_{ifNum}:");
                    if (cf.ElseBody != null)
                        generateExecutable(cf.ElseBody, locals, sb);
                    sb.AppendLine($"    br label %if_end_{ifNum}");

                    sb.AppendLine($"if_end_{ifNum}:");
                    return "";
                }

            case WhileLoop wl:
                {
                    var (sl, el) = getLoopLabels(wl);
                    sb.AppendLine($"    ; while loop {wl.Location}");
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{sl}:");
                    string condValue = generateExecutable(wl.Condition, locals, sb);
                    sb.AppendLine($"    br i1 {condValue}, label %{sl}_body, label %{el}");
                    sb.AppendLine($"{sl}_body:");
                    generateExecutable(wl.Body, locals, sb);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{el}:");
                    return "";
                }

            case GetMember gm:
                return generateGetMember(gm, locals, sb);

            case GetElement ge:
                return generateGetElement(ge, locals, sb);

            case PostIncrement pi:
                return generateIncrement(pi.Operand, pi.IsDecrement, true, pi.Location, locals, sb);

            case Break bk:
                {
                    var (_, el) = getLoopLabels(bk.TargetLoop);
                    sb.AppendLine($"    br label %{el} ; Break {bk.Location}");
                    sb.AppendLine($"{valueCounter++}:");
                    return "";
                }

            case Continue ct:
                {
                    var (st, _) = getLoopLabels(ct.TargetLoop);
                    sb.AppendLine($"    br label %{st} ; Continue {ct.Location}");
                    sb.AppendLine($"{valueCounter++}:");
                    return "";
                }

            case InifiniteLoop il:
                {
                    var (sl, el) = getLoopLabels(il);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{sl}:   ; loop {il.Location}");
                    generateExecutable(il.Body, locals, sb);
                    sb.AppendLine($"    br label %{sl}");
                    sb.AppendLine($"{el}:");
                    return "";
                }

            case EnumMemberAccess ema:
                {
                    sb.AppendLine($"    ; Enum member {ema.EnumType.Name}.{ema.MemberName} = {ema.Value}");
                    return ema.Value.ToString();
                }

            case StructLiteral sl:
                return generateStructLiteral(sl, locals, sb);

            case SelfAccess sa:
                return "%self";

            case Nop nop:
                throw new Exception($"Nop encountered in codegen {nop.Location}");

            default:
                throw new NotImplementedException($"Code generation for {exe.GetType().Name} not implemented");
        }
    }

    private string generateCodeBlock(CodeBlock cb, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; CodeBlock {cb.Location}");
        foreach (var local in cb.Locals.Variables)
        {
            sb.AppendLine($"    %{local.FullName} = alloca {getLlvmType(local.Type)}");
        }

        foreach (var executable in cb.Code)
        {
            generateExecutable(executable, locals, sb);
        }
        return "";
    }

    private string generateBinaryOp(BinaryOperation bo, INameContainer locals, StringBuilder sb)
    {
        switch (bo.OperationType)
        {
            case BinaryOperationTypes.Assign:
                {
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string leftPtr = generateLHS(bo.Left, locals, sb);

                    sb.AppendLine($"        ; Assign {bo.Location}");
                    sb.AppendLine($"    store {getLlvmType(bo.Right.ReturnType)} {rightVal}, ptr {leftPtr}");

                    return rightVal;
                }

            case BinaryOperationTypes.Add:
            case BinaryOperationTypes.Subtract:
            case BinaryOperationTypes.Multiply:
            case BinaryOperationTypes.BitwiseAnd:
            case BinaryOperationTypes.BitwiseOr:
            case BinaryOperationTypes.BitwiseXor:
            case BinaryOperationTypes.LeftShift:
            case BinaryOperationTypes.RightShift:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    string opName = bo.ReturnType is DefaultType.FloatingPoint
                        ? FloatMathOps.GetValueOrDefault(bo.OperationType, "fadd")
                        : MathOps.GetValueOrDefault(bo.OperationType, "add");

                    sb.AppendLine($"    ; {bo.OperationType} {bo.Location}");
                    sb.AppendLine($"    {result} = {opName} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.Divide:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"    ; Division {bo.Location}");
                    DefaultType.FloatingPoint? fpType = bo.ReturnType as DefaultType.FloatingPoint;
                    if (fpType != null)
                    {
                        sb.AppendLine($"    {result} = fdiv {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    }
                    else if (bo.ReturnType is DefaultType.Integer intDiv)
                    {
                        string op = intDiv.IsSigned ? "sdiv" : "udiv";
                        sb.AppendLine($"    {result} = {op} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    }
                    else
                        throw new Exception($"Unknown division type {bo.ReturnType}");

                    return result;
                }

            case BinaryOperationTypes.Remainder:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();
                    sb.AppendLine($"    ; Remainder {bo.Location}");
                    DefaultType.Integer intMod = (DefaultType.Integer)bo.ReturnType;
                    string op = intMod.IsSigned ? "srem" : "urem";
                    sb.AppendLine($"    {result} = {op} {getLlvmType(bo.ReturnType)} {leftVal}, {rightVal}");
                    return result;
                }

            case BinaryOperationTypes.Less:
            case BinaryOperationTypes.LessEquals:
            case BinaryOperationTypes.Greater:
            case BinaryOperationTypes.GreaterEquals:
            case BinaryOperationTypes.IsEquals:
            case BinaryOperationTypes.NotEquals:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    string cmpOp = bo.Left.ReturnType is DefaultType.FloatingPoint ? "fcmp ogt" : "icmp";
                    string op = CompareOps.GetValueOrDefault(bo.OperationType, "eq");

                    sb.AppendLine($"        ; {bo.OperationType} {bo.Location}");
                    sb.AppendLine($"    {result} = {cmpOp} {op} {getLlvmType(bo.Left.ReturnType)} {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.LogicalAnd:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; LogicalAnd {bo.Location}");
                    sb.AppendLine($"    {result} = and i1 {leftVal}, {rightVal}");

                    return result;
                }

            case BinaryOperationTypes.LogicalOr:
                {
                    string leftVal = generateExecutable(bo.Left, locals, sb);
                    string rightVal = generateExecutable(bo.Right, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; LogicalOr {bo.Location}");
                    sb.AppendLine($"    {result} = or i1 {leftVal}, {rightVal}");

                    return result;
                }

            default:
                throw new NotImplementedException($"Binary operation {bo.OperationType} not implemented");
        }
    }

    private int getMemberIndex(StructType st, string memberName)
    {
        for (int i = 0; i < st.Members.Length; i++)
        {
            if (st.Members[i].Name == memberName)
                return i;
        }
        throw new Exception($"Member {memberName} not found in struct {st.Name}");
    }

    private string generateUnaryOp(UnaryOperation uo, INameContainer locals, StringBuilder sb)
    {
        switch (uo.OperationType)
        {
            case UnaryOperationTypes.Cast:
                {
                    return generateCast(uo, locals, sb);
                }

            case UnaryOperationTypes.GetReference:
                {
                    sb.AppendLine($"        ; GetReference {uo.Location}");
                    return generateLHS(uo.Operand, locals, sb);
                }

            case UnaryOperationTypes.Minus:
                {
                    string operandVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Minus {uo.Location}");
                    if (uo.Operand.ReturnType is DefaultType.FloatingPoint)
                        sb.AppendLine($"    {result} = fsub {getLlvmType(uo.Operand.ReturnType)} -0.0, {operandVal}");
                    else
                        sb.AppendLine($"    {result} = sub {getLlvmType(uo.Operand.ReturnType)} 0, {operandVal}");

                    return result;
                }

            case UnaryOperationTypes.Dereference:
                {
                    string ptrVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Dereference {uo.Location}");
                    if (uo.Operand.ReturnType is Pointer ptr)
                        sb.AppendLine($"    {result} = load {getLlvmType(ptr.PointsTo)}, ptr {ptrVal}");

                    return result;
                }

            case UnaryOperationTypes.Increment:
            case UnaryOperationTypes.Decrement:
                return generateIncrement(uo.Operand, uo.OperationType == UnaryOperationTypes.Decrement, false, uo.Location, locals, sb);

            case UnaryOperationTypes.Inverse:
                {
                    string operandVal = generateExecutable(uo.Operand, locals, sb);
                    string result = getNextValueName();

                    sb.AppendLine($"        ; Inverse {uo.Location}");
                    sb.AppendLine($"    {result} = xor i1 {operandVal}, 1");
                    return result;
                }

            default:
                throw new NotImplementedException($"Unary operation {uo.OperationType} not implemented");
        }
    }

    private string generateIncrement(Executable operand, bool isDecrement, bool isPostfix, Location location, INameContainer locals, StringBuilder sb)
    {
        string currentVal = generateExecutable(operand, locals, sb);
        string result = getNextValueName();
        string newVal = getNextValueName();

        sb.AppendLine($"        ; Increment {location}");
        sb.AppendLine($"    {newVal} = {(isDecrement ? "sub" : "add")} {getLlvmType(operand.ReturnType)} {currentVal}, 1");

        if (operand is Identifyer ident && ident.Definition is VariableDefinition variable)
            sb.AppendLine($"    store {getLlvmType(operand.ReturnType)} {newVal}, {getLlvmType(operand.ReturnType)}* %{variable.FullName}");

        if (isPostfix)
            return currentVal;
        else
            return newVal;
    }

    private string generateCast(UnaryOperation uo, INameContainer locals, StringBuilder sb)
    {
        Executable operand = uo.Operand;
        Typ target = uo.ReturnType;
        Typ source = operand.ReturnType;

        if (source is SizedArray && target is Pointer)
        {
            string array = generateLHS(operand, locals, sb);
            sb.AppendLine($"        ; Cast {operand.ReturnType} to {target} {uo.Location}");
            return array;
        }

        string operandVal = generateExecutable(operand, locals, sb);
        sb.AppendLine($"        ; Cast {operand.ReturnType} to {target} {uo.Location}");

        if (operand.ReturnType is Pointer && target is Pointer)
            return operandVal;

        else if (source is EnumType fromEnum && target == fromEnum.UnderlyingType)
            return operandVal;

        else if (target is EnumType toEnum && source == toEnum.UnderlyingType)
            return operandVal;

        string result = getNextValueName();

        if (operand.ReturnType is DefaultType.Integer fromInt && target is DefaultType.Integer toInt)
        {
            int fInt = fromInt.Size == 0 ? 8 : fromInt.Size;
            if (fInt < toInt.Size)
            {
                if (fromInt.IsSigned)
                    sb.AppendLine($"    {result} = sext {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
                else
                    sb.AppendLine($"    {result} = zext {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
            }
            else if (fInt > toInt.Size)
                sb.AppendLine($"    {result} = trunc {getLlvmType(fromInt)} {operandVal} to {getLlvmType(toInt)}");
            else
                return operandVal;

            return result;
        }
        else if (operand.ReturnType is DefaultType.FloatingPoint fromFp && target is DefaultType.FloatingPoint toFp)
        {
            if (fromFp.Size < toFp.Size)
                sb.AppendLine($"    {result} = fpext {getLlvmType(fromFp)} {operandVal} to {getLlvmType(toFp)}");
            else if (fromFp.Size > toFp.Size)
                sb.AppendLine($"    {result} = fptrunc {getLlvmType(fromFp)} {operandVal} to {getLlvmType(toFp)}");
            else
                return operandVal;

            return result;
        }
        else if (operand.ReturnType is DefaultType.Integer intType && target is DefaultType.FloatingPoint fpType)
        {
            if (intType.IsSigned)
                sb.AppendLine($"    {result} = sitofp {getLlvmType(intType)} {operandVal} to {getLlvmType(fpType)}");
            else
                sb.AppendLine($"    {result} = uitofp {getLlvmType(intType)} {operandVal} to {getLlvmType(fpType)}");
            return result;
        }
        else if (operand.ReturnType == DefaultType.Bool && target is DefaultType.Integer)
        {
            sb.AppendLine($"    {result} = zext i1 {operandVal} to {getLlvmType(target)}");
            return result;
        }
        else if (operand.ReturnType is DefaultType.FloatingPoint fpType2 && target is DefaultType.Integer intType2)
        {
            if (intType2.IsSigned)
                sb.AppendLine($"    {result} = fptosi {getLlvmType(fpType2)} {operandVal} to {getLlvmType(intType2)}");
            else
                sb.AppendLine($"    {result} = fptoui {getLlvmType(fpType2)} {operandVal} to {getLlvmType(intType2)}");
            return result;
        }
        else if (operand.ReturnType is DefaultType.Integer sInt && target is Pointer)
        {
            if (sInt.Size != 0 && (sInt.Size != 8 || sInt.IsSigned))
                throw new Exception($"Uncastable int {sInt}");
            sb.AppendLine($"    {result} = inttoptr i64 {operandVal} to ptr");
            return result;
        }
        else if (source is Pointer && target is SizedArray)
        {
            sb.AppendLine($"    {result} = load {getLlvmType(target)}, ptr {operandVal}");
            return result;
        }
        else if (source is InterfaceType && target is Pointer)
        {
            sb.AppendLine($"    {result} = extractvalue {{ ptr, ptr }} {operandVal}, 0");
            return result;
        }
        else if (source is Pointer ifacePtr && target is InterfaceType iface)
        {
            string undef = getNextValueName();
            sb.AppendLine($"    {result} = insertvalue {{ ptr, ptr }} undef, ptr {operandVal}, 0");

            if (ifacePtr.PointsTo is not StructType structType)
                throw new Exception($"Can not cast `{source}` to interface {target}");

            sb.AppendLine($"    {undef} = insertvalue {{ ptr, ptr }} {result}, ptr {getVtableName(structType, iface)}, 1");
            return undef;
        }
        else
            throw new Exception($"Unknown cast {operand.ReturnType} to {target}");
    }

    private string generateFunctionCall(FunctionCall fc, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Function call args {fc.Location}");
        var args = new List<(string value, string type)>();

        foreach (var arg in fc.Args)
        {
            string argVal = generateExecutable(arg, locals, sb);
            args.Add((argVal, getLlvmType(arg.ReturnType)));
        }

        var argStrings = args.Select(a => $"{a.type} {a.value}").ToList();

        sb.AppendLine($"        ; Func call");
        string funcPtr = generateExecutable(fc.FunctionPointer, locals, sb);

        if (fc.FunctionPointer.ReturnType is not FunctionPointer)
            throw new Exception($"Uncallable type {fc.FunctionPointer.ReturnType}");

        if (fc.FunctionPointer.ReturnType is MethodPointer)
        {
            string self = getNextValueName();
            string method = getNextValueName();

            sb.AppendLine($"    {self} = extractvalue {getLlvmType(fc.FunctionPointer.ReturnType)} {funcPtr}, 0");
            sb.AppendLine($"    {method} = extractvalue {getLlvmType(fc.FunctionPointer.ReturnType)} {funcPtr}, 1");

            argStrings.Insert(0, $"ptr {self}");
            funcPtr = method;
        }

        string argList = string.Join(", ", argStrings);
        string retType = getLlvmType(fc.ReturnType);

        if (fc.ReturnType == DefaultType.Void)
        {
            sb.AppendLine($"    call {retType} {funcPtr}({argList})");
            return "";
        }
        else
        {
            string result = getNextValueName();
            sb.AppendLine($"    {result} = call {retType} {funcPtr}({argList})");
            return result;
        }
    }

    // returns struct of { self, method ptr }
    private string generateMethodValue(MethodValue mv, INameContainer locals, StringBuilder sb)
    {
        string op = generateExecutable(mv.Operand, locals, sb);

        sb.AppendLine($"        ; Method value {mv.Location}");

        string funcName;
        if (mv.Method.Modifyers.Contains(Keywords.External))
            funcName = $"@{mv.Method.Name}";
        else
            funcName = $"@{mv.Method.FullName}";

        string opInsert = getNextValueName();
        string funcInsert = getNextValueName();

        sb.AppendLine($"    {opInsert} = insertvalue {getLlvmType(mv.ReturnType)} undef, ptr {op}, 0");
        sb.AppendLine($"    {funcInsert} = insertvalue {getLlvmType(mv.ReturnType)} {opInsert}, ptr {funcName}, 1");

        return funcInsert;
    }

    private string generateVtableLookup(VtableLookup vl, INameContainer locals, StringBuilder sb)
    {
        string op = generateExecutable(vl.Operand, locals, sb);

        sb.AppendLine($"        ; Vtable lookup {vl.Location}");

        string vtablePtr = getNextValueName();
        string vtableVal = getNextValueName();
        string selfPtr = getNextValueName();
        string methodPtr = getNextValueName();
        string selfInsert = getNextValueName();
        string fnInsert = getNextValueName();

        string retType = getLlvmType(vl.ReturnType);

        sb.AppendLine($"    {vtablePtr} = extractvalue {getLlvmType(vl.Operand.ReturnType)} {op}, 1");
        sb.AppendLine($"    {vtableVal} = load {getVtableType(vl.InterfaceType)}, ptr {vtablePtr}");
        sb.AppendLine($"    {selfPtr} = extractvalue {getLlvmType(vl.Operand.ReturnType)} {op}, 0");
        sb.AppendLine($"    {methodPtr} = extractvalue {getVtableType(vl.InterfaceType)} {vtableVal}, {vl.Index}");
        sb.AppendLine($"    {selfInsert} = insertvalue {retType} undef, ptr {selfPtr}, 0");
        sb.AppendLine($"    {fnInsert} = insertvalue {retType} {selfInsert}, ptr {methodPtr}, 1");

        return fnInsert;
    }

    private string generateIdentifier(Identifyer id, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"        ; Identifier {id.Definition?.GetType().Name} {id.Location}");

        if (id.Definition is VariableDefinition varDef)
        {
            string result = getNextValueName();
            string loadType = getLlvmType(varDef.Type);
            string locality = varDef.Global ? "@" : "%";
            sb.AppendLine($"    {result} = load {loadType}, ptr {locality}{varDef.FullName}");
            return result;
        }
        else if (id.Definition is FunctionDefinition fnDef)
        {
            if (fnDef.Modifyers.Contains(Keywords.External))
                return $"@{fnDef.Name}";
            else
                return $"@{fnDef.FullName}";
        }
        else
            throw new Exception($"Unsupported def {id.Definition}");
    }

    private string generateGetMember(GetMember gm, INameContainer locals, StringBuilder sb)
    {
        string baseVal = generateExecutable(gm.Operand, locals, sb);
        sb.AppendLine($"        ; GetMember {gm.Location}");

        if (gm.Operand.ReturnType is StructType st)
        {
            int memberIdx = getMemberIndex(st, gm.Member.Value);

            string result = getNextValueName();
            sb.AppendLine($"    {result} = extractvalue {getLlvmType(gm.Operand.ReturnType)} {baseVal}, {memberIdx}");

            return result;
        }
        else
            throw new Exception("Get member for non struct type");
    }

    private string generateGetElement(GetElement ge, INameContainer locals, StringBuilder sb)
    {
        string array = generateExecutable(ge.Operand, locals, sb);
        string index = generateExecutable(ge.Index, locals, sb);
        string tmp = getNextValueName();
        string elPtr = getNextValueName();
        string result = getNextValueName();
        sb.AppendLine($"        ; GetElement {ge.Location}");
        sb.AppendLine($"    {tmp} = alloca {getLlvmType(ge.Operand.ReturnType)}");
        sb.AppendLine($"    store {getLlvmType(ge.Operand.ReturnType)} {array}, ptr {tmp}");
        sb.AppendLine($"    {elPtr} = getelementptr {getLlvmType(ge.Operand.ReturnType)}, ptr {tmp}, i32 0, {getLlvmType(ge.Index.ReturnType)} {index}");
        sb.AppendLine($"    {result} = load {getLlvmType(ge.ReturnType)}, ptr {elPtr}");
        return result;
    }

    private string generateStructLiteral(StructLiteral sl, INameContainer locals, StringBuilder sb)
    {
        sb.AppendLine($"    ; Struct literal {sl.StructType.Name}");

        string structType = getLlvmType(sl.StructType);
        if (sl.FieldValues.Length == 0)
            return "zeroinitializer";

        string current;
        string next = "undef";

        for (int i = 0; i < sl.FieldValues.Length; i++)
        {
            string fieldVal = generateExecutable(sl.FieldValues[i], locals, sb);
            current = next;
            next = getNextValueName();
            sb.AppendLine($"    {next} = insertvalue {structType} {current}, {getLlvmType(sl.StructType.Members[i].Type)} {fieldVal}, {i}");
        }

        return next;
    }

    private string generateLHS(Executable executable, INameContainer locals, StringBuilder sb)
    {
        string returnVal;
        switch (executable)
        {
            case Identifyer ident:
                sb.AppendLine($"        ; lhs Identifyer {ident.Location}");
                if (ident.Definition is VariableDefinition varDef)
                {
                    string locality = varDef.Global ? "@" : "%";
                    returnVal = $"{locality}{varDef.FullName}";
                }
                else if (ident.Definition is FunctionDefinition)
                {
                    if (ident.Definition.Modifyers.Contains(Keywords.External))
                        returnVal = $"@{ident.Definition.Name}";
                    else
                        returnVal = $"@{ident.Definition.FullName}";
                }
                else
                    throw new Exception($"Can not get ptr for {ident.Definition}");

                break;

            case GetMember gm:
                {
                    string basePtr = generateLHS(gm.Operand, locals, sb);
                    sb.AppendLine($"        ; lhs GetMember {gm.Location}");

                    if (gm.Operand.ReturnType is StructType st)
                    {
                        int memberIdx = getMemberIndex(st, gm.Member.Value);

                        string elemPtr = getNextValueName();
                        sb.AppendLine($"    {elemPtr} = getelementptr {getLlvmType(st)}, ptr {basePtr}, i32 0, i32 {memberIdx}");

                        returnVal = elemPtr;
                    }
                    else
                        throw new Exception("Get member for non struct type");

                    break;
                }

            case GetElement ge:
                {
                    string array = generateLHS(ge.Operand, locals, sb);
                    string index = generateExecutable(ge.Index, locals, sb);
                    string elPtr = getNextValueName();
                    sb.AppendLine($"        ; GetElement {ge.Location}");
                    sb.AppendLine($"    {elPtr} = getelementptr {getLlvmType(ge.Operand.ReturnType)}, ptr {array}, i32 0, {getLlvmType(ge.Index.ReturnType)} {index}");
                    returnVal = elPtr;
                    break;
                }

            default:
                throw new Exception($"Can not generate ptr for {executable}");
        }

        return returnVal;
    }
}
