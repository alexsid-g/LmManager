# Initialization

Output: `Context.md`

Tasks:
1. Identify the requested review target (path only) and project root.
2. Read all `README.md`, searching upward from the target path to the project root.
3. Create a `Context.md`. 

## Directory Tree
If README does not provide sufficient information about project technologies or structure:
- Obtain a directory tree using a filesystem listing tool.
- Exclude generated directories, build outputs, dependencies, binaries, and version control metadata.

## Strict Restrictions
- Do not read source code files.
- Do not inspect individual files or their contents.
- Do not analyze classes, methods, dependencies, or implementation details.

## Example `Context.md`

```text
# Code Review Context

## Project Root
Path: <project root path>

## Target
Path: <target path>

## Project Overview
<Brief description of the project and its architecture relevant to the Target, based primarily on README.md>

## Technologies
- <Language>
- <Framework>
- <Build system>

## Workflow Status

- Phase 1: Completed
- Phase 2: Incomplete
- Phase 3: Incomplete
- Phase 4: Incomplete

## Notes
<Relevant observations and limitations>
```
