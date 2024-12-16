namespace Cml.Parsing;

internal class OrAssign(Executable address, Executable value, Location location) : Assign(address, value, location);