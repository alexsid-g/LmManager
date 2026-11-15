# Incremental Batch Review 

Input:
    - BatchID
    - OutputDir
Output: `<OutputDir>/Issues-<BatchID>.md`

Tasks:
1. Read `<OutputDir>/Context.md` and identify Target and ProjectRoot.
2. Read all `README.md`, searching upward from the target path to the project root.
3. Read `<OutputDir>/Batches.md` and locate the exact <BatchID> section using the available find tool.
4. Extract the files assigned to this batch.
5. Inspect ONLY the files belonging to the requested batch. Do not review files assigned to other batches.
6. Create `<OutputDir>/Issues-<BatchID>.md`.
7. Do not modify Context.md, Batches.md, or project source files.

### 2. Batch Inspection
Inspect every file in the batch, covering relevant areas: correctness, data validation, error handling, concurrency, lifecycle, security, filesystem, architecture, observability, and performance.

For every potential finding:
1. Verify against the actual implementation and documentation.
2. Exclude speculative or unsupported claims.
3. Report documentation/implementation discrepancies as Low severity unless they independently demonstrate a more significant defect.
3. Provide exact `file:line` references and a supporting 1–5-line code excerpt.
4. Assign severity: `High`, `Medium`, or `Low` based on actual impact.
- Save each confirmed finding to `<OutputDir>/Issues.md` immediately.

## Example `Issues-BatchID>.md`
For each finding

```text
## IS-<BatchID>-NNN <Issue-Name> [<Severity>]

### Description 
<Defect, code reference, and supporting excerpt>

### Reasoning
<Why it is a defect and its concrete impact>

### Hot to Fix
<Minimal actionable correction>

```


