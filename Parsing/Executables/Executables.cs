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
    NameContext Locals,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record GetMember
(
    Executable Operand,
    Token<string> Member,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record GetTokenValue
(
    Token Token,
    StructDefinition ReturnType
) : Executable(ReturnType, Token.Location);

public record ControlFlow
(
    Executable Condition,
    Executable Body,
    Executable? ElseBody,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record WhileLoop
(
    Executable Condition,
    Executable Body,
    StructDefinition ReturnType,
    Location Location
) : Executable(ReturnType, Location);