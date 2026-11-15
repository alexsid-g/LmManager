# Discovery and Planning

Input: OutputDir
Output: `<OutputDir>/Batches.md`

Tasks:
1. Read `<OutputDir>/Context.md` and identify Target and ProjectRoot.
2. Read ProjectRoot/README.md.
3. Obtain a directory tree of ProjectRoot using an available filesystem listing tool.
4. Identify relevant source files and group them into logical batches.
5. Create `<OutputDir>/Batches.md`.

## Directory Tree

Obtain a directory tree using a filesystem listing tool:
- Depth of tree should be max 5.
- Include all source files (.axaml, .cs, .ts, .js, .html, .css, .py, .sh, .json, .md, .info).
- Exclude `<OutputDir>`, generated directories, build outputs, dependencies, binaries, and version control metadata.
- Preserve paths relative to ProjectRoot.

## Grouping Strategy

Group files into logically related batches:
1. Directory and module boundaries.
2. Functional relationships between files.
3. File roles inferred from filenames and directory structure.
4. Architectural context from `ProjectRoot/README.md` and `OutputDir/Context.md` if exist.

- Prefer grouping files by directory/module.
- Infer file roles and relationships from:
    - Filenames and directory structure
    - Content of `<ProjectRoot>/README.md` and `<OutputDir>/Context.md`
- Do not read file contents.

## Example `Batches.md`
For each defined group

```text
## Batch-NNN

<list of path to files>

```