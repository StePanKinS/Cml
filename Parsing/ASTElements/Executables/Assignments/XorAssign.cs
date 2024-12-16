namespace Cml.Parsing;

internal class XorAssign(Executable address, Executable value, Location location) : Assign(address, value, location);