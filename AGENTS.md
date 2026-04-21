# Cml Agent Notes

## Repo Shape
- Single-project .NET CLI compiler; no monorepo or test project.
- Entry flow is `Program.cs` -> `Compiler.Compile` -> `Lexing/` + `Parsing/` -> `CodeGenerating/LlvmCodeGen.cs` -> external `llc` -> external `gcc` (as linker).
- LLVM output is hard-coded for Linux x86_64 in `CodeGenerating/LlvmCodeGen.cs`.

## Verified Commands
- Build: `dotnet build`
- Show CLI help: `dotnet run -- --help`
- Running: `dotnet run -- test.cml && ./a.out`
- Lexer-only inspection: `dotnet run -- test.cml -t`

## Toolchain / Verification
- `Cml.csproj` targets `net10.0`; use a .NET 10 SDK.
- Native codegen requires both `llc` and `gcc` on `PATH`.
- There is no automated test suite in this repo. The practical verification path is `dotnet build`, then compile a focused `.cml` sample.

## Gotchas
- Do not trust `0 Errors` by itself. `Compiler.cs` waits for `llc`/`gcc` but does not check their exit codes, so backend failures can still print `0 Errors`.
- `.gitignore` ignores `*.cml`, `a.out`
