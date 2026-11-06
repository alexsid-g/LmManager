---
name: code-review
description: Incremental full-scope code review with target discovery, evidence-based findings (max 20), checkpointing, and report generation.
---

# Code Review

Act as a Senior Software Engineer / Software Architect performing an independent, objective code review.

Identify concrete, verifiable defects. Do not report stylistic preferences or architectural alternatives unless they cause verifiable problem. 

Do not modify target code. Only investigate, analyze, and generate `Review-Issues.md` in the target root (THE_REPORT), unless another location is specified.

## Workflow

### 1. Discover
- Read `README.md` and extract verifiable claims about features, architecture, defaults, lifecycle, and configuration.
- Scan the target structure (2–3 levels deep). Use `git ls-files` when available.
- Identify source, config, doc, and test files; group them into logical batches by directory or module.
- Run the available build command, if applicable.
- In the end of Discover, save to THE_REPORT report header: target, batch count, file count, build command, and result after inspection.

### 2. Inspect incrementally
Inspect every batch, covering relevant areas: correctness, data validation, error handling, concurrency, lifecycle, security, filesystem, architecture, observability, and performance.

For each candidate:
- Verify against the actual implementation and documentation. Report documentation/implementation discrepancies, but do not spend much time on it, just write them as `Low`.
- Exclude speculative or unsupported claims.
- Require exact `file:line` references and a supporting 1–5-line code excerpt.
- Save each confirmed finding to THE_REPORT immediately.

Use this structure for each finding:

`### IS-NNN <Issue-Name>`
- **Description:** (`Severity`) Defect, code reference, and supporting excerpt. Assign Severity to `High`, `Medium`, or `Low` based on actual impact. Include one row per finding; leave the table empty if there are none.
- **Reasoning:** Why it is a defect and its concrete impact.
- **How to fix:** Minimal actionable correction.

After each batch, check the confirmed finding count and write them to THE_REPORT.
Stop inspecting further directories at 20 findings. Do not omit critical confirmed findings already discovered.

Append this checkpoint after inspection stops:
```markdown
## Review Progress & Checkpoint
- **Status:** In Progress | Completed
- **Last Inspected Directory:** `<path>`
- **Next Directory to Process:** `<path>`
- **Issue Count:** `X / 20`
```

Use `Completed` only when all directories are inspected; otherwise use `In Progress`.

### 3. Summary
Read `Findings` from THE_REPORT and insert `Summary` table before `Findings`:

```markdown
## Summary
| ID | Issue name | Severity | File:Line |
```
Append: `**Severity counts:** X High, Y Medium, Z Low`

### 4. Finalize
Report and stop immediately:
- Checked, skipped, and total file counts.
- Total findings.
- Stop reason: 20-finding limit or all files inspected.
