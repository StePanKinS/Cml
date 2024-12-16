namespace Cml.Parsing;

internal class MultiplyAssign(Executable address, Executable value, Location location) : Assign(address, value, location);