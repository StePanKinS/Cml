namespace Cml.Parsing.Executables;

public abstract record Executable
(
    StructDefinition ReturnType,
    Location Location
);

public record UnaryOperation
(
    UnaryOperationTypes OperationType,
    Executable Operand,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record BinaryOperation
(
    BinaryOperationTypes OperationType,
    Executable Left,
    Executable Right,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record FunctionCall
(
    Executable FunctionPointer,
    Executable[] Args,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record CodeBlock
(
    Executable[] Code,
    LocalVariables Locals,
    Location Location
) : Executable(DefaultTypes.Void, Location);

public record GetMember
(
    Executable Operand,
    Token<string> Member,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

// public record GetTokenValue
// (
//     Token Token,
//     StructDefinition ReturnType
// ) : Executable(ReturnType, Token.Location);

public record Identifyer
(
    Definition Definition,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record Literal<T>
(
    T Value,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record ControlFlow
(
    Executable Condition,
    Executable Body,
    Executable? ElseBody,
    Location Location
) : Executable(DefaultTypes.Void, Location);

public record WhileLoop
(
    Executable Condition,
    Executable Body,
    Location Location
) : Executable(DefaultTypes.Void, Location);

public record Return
(
    Executable Value,
    Location Location
) : Executable(DefaultTypes.Void, Location);

public record Nop
(
    Location Location
) : Executable(DefaultTypes.Void, Location);