namespace Cml.Types;

public class MethodPointer(Typ operandType, Typ returnType, Typ[] args, bool isVariadic)
    : FunctionPointer(returnType, args, isVariadic, $"fn {operandType.Name}::{getSignatureName(returnType, args, isVariadic)}", 8)
{
    public MethodPointer(Executable operand, FunctionDefinition method)
        : this(operand.ReturnType, method.ReturnType, method.Arguments.Arguments.Select(a => a.Type).ToArray(), method.IsVariadic)
    { }

    public MethodPointer(Executable operand, FunctionPointer funcPtr)
        : this(operand.ReturnType, funcPtr.ReturnType, funcPtr.Args, funcPtr.IsVariadic)
    { }

    public Typ OperandType = operandType;
}
