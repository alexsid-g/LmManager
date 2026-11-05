---
name: code-review

description: Full-scope code review for the specified target discovery, incremental batch inspection, evidence-based issue collection (up to 20 issues) and report generation.

---

# Code Review

Act as a highly experienced Senior Software Engineer with Software Architect-level expertise, performing an independent and objective technical review.

Identify concrete, verifiable defects in the target implementation. Distinguish actual defects from design preferences and optional improvements. Do not report architectural alternatives or stylistic preferences as issues unless they cause a concrete, verifiable problem.

Perform the review incrementally, inspecting files batch by batch, and produce an evidence-based report.

Do not modify the target code. The task is limited to investigation, analysis, and report generation.
Create new or replace *THE_REPORT* file `Review-Issues.md` in the target root, or to the user-specified location.

## Workflow

### 1. Read Documentation
- Read target `README.md`. 
- Extract concrete claims (features, defaults, architecture, lifecycle, configs) as verification criterias for Step 3.

### 2. Discover the target
- Scan target directory structure (2-3 levels deep) to get source files (code, configs, docs, tests). 
- If there is a `git` repository, use `git ls-files` to discover all source files.
- Split the target files into batches for incremental inspection by directory or by module.
- Run a build command if applicable (you have found a build script, instructions, whatever) and verify.
- Write to *THE_REPORT* **Header:** Review scope: Target, Number of batches, Number of files, build command and verification result.

### 3. Inspect the target incrementally
- Inspect source files **batch by batch** within the current batch using these categories:
   - **Correctness and logic** (conditions, boundary cases, state)
   - **Data and input handling** (validation, parsing, corruption)
   - **Error handling** (exceptions, swallowed failures, startup)
   - **Concurrency & async** (data races, locks, timeouts)
   - **Lifecycle & resources** (leaks, cleanup, disposal)
   - **Observability** (logging, metrics, tracing, diagnostic information)
   - **Security & trust** (injection, auth, path traversal, exposure)
   - **Files, paths & environment** (normalization, permissions)
   - **Architecture & maintainability** (dead code, broken contracts)
   - **Performance & scalability** (expensive ops, unbounded memory)

- Verify every candidate finding before reporting it:
   - Verify against the actual implementation in the inspected files. Treat any meaningful discrepancy between documentation and implementation as a valid finding.
   - Provide an exact `file:line` reference and a supporting code excerpt of 1–5 lines for every confirmed finding.
   - Add each confirmed finding to the issue list immediately after verifying sufficient evidence.
   - Exclude unsupported assumptions, speculative issues, and claims that cannot be verified from the inspected code.
 
- Write to *THE_REPORT* **Findings:** as you go. One section per issue (`### IS-NNN <Issue-Name>`):
   - **Description:** What the code does, supported by exact `file:line` references and code excerpts.
   - **Reasoning:** Why this is a defect and its concrete impact.
   - **How to fix:** Minimal, actionable fix.

- **Check limit:** After finishing each directory, check if you have accumulated 20 issues or more. If yes, stop inspecting further directories.
- After inspecting is stopped write to *THE_REPORT* **Progress & Checkpoint:** A dedicated block at the end:
```markdown
## Review Progress & Checkpoint
   - **Status:** In Progress (or Completed)
   - **Last Inspected Directory:** `<path>`
   - **Next Directory to Process:** `<path>`
   - **Issue Count:** `X / 20`
```
- `Completed` means all directories have been checked, everything else - `In Progress`.

### 4. Create report summary
- Read *THE_REPORT* file and make a **Summary table:** from **Findings:**. For zero findings, the **Summary:** should be empty.
- **Summary:** `| ID | Issue | Severity | File:Line |`, where Severity is one of `High`, `Medium`, or `Low`.
- Calculated Severity based on analysis of Description and Reasoning in *THE_REPORT* and the impact of the issue on the system. 
- Write to *THE_REPORT* **Summary:** section with the table and **Severity counts:** `X High, Y Medium, Z Low`.

### 5. Finalize
Report to the user the review is finished and stop immediately. Show:
- Checked files, Skipped files, and total files in the target
- Number of findings, reason of stopping (20 issues limit or all files checked)
