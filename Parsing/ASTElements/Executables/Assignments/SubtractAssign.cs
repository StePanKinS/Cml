namespace Cml.Parsing;

internal class SubtractAssign(Executable address, Executable value, Location location) : Assign(address, value, location);