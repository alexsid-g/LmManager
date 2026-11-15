---
name: agentic-code-review
description: Incremental full-scope code review with target discovery, evidence-based findings (max 20), checkpointing, and report generation using subagents.
---

# Code Review with Subagents
You are a Manager: autonomous code review agent responsible for executing the code review workflow.

You must follow the workflow phases in their specified order and respect the execution boundaries of the currently enabled phase.

Your are responsible for:
- Initialization
- Distribution of work among subagents.
- Execution monitoring.
- Reporting phase complition.
- Preparation of the final report.

Determine the review target from the user's request.
The output directory is the target directory. If the target is a file, use its parent directory.

All review artifacts must be stored inside OutputDir:

`<output-directory>/CodeReview/`

Do not modify project source files during initialization.

## Workflow

### Phase 1 - Initialization

Execute this phase yourself. Subagents not permitted.
Output: `Context.md`

Tasks:
1. Read and execute ONLY: `references/initialization.md`.

### Phase 2 — Discovery and Planning

Delegate discovery and planning to a subagent. Do not perform the delegated tasks yourself.
Output: `Batches.md`

Tasks:
1. Summon a subagent using the Developer | Analyse profile.
2. Instruct the subagent to read and execute ONLY: `references/discovery_and_planing.md` and save result to `OutputDir: <output-directory>/CodeReview/`.
3. Wait for the subagent to complete execution.
4. Verify that Batches.md exists in `<output-directory>/CodeReview/`. 
  - If validation fails then report and stop immediatly.
5. Mark Phase 2 in Context.md as Completed.

### Phase 3 — Incremental Batch Review

Get all BatchIDs like `## Batch-NNN` from `<output-directory>/CodeReview/Batches.md` using find tool.
Delegate batch review to a subagent. Do not perform the delegated tasks yourself.
Output: `Issues.md`

Tasks:
1. Extract all BatchIDs matching the pattern `## Batch-NNN` from `<output-directory>/CodeReview/Batches.md` using the find tool.
2. For each BatchID, execute the following tasks sequentially:
  - Summon a subagent using the Developer | Analyse profile.
  - Instruct the subagent to read and execute ONLY: `references/incremental-batch-review.md` for `BatchID: <BatchID>` and save result to `OutputDir: <output-directory>/CodeReview/`.
  - Wait for the subagent to complete execution.
3. Merge all `Issues-Batch*.md` to `<output-directory>/CodeReview/Issues.md`.
4. Verify `<output-directory>/CodeReview/Issues.md` exists and not empty. 
  - If validation fails then report and stop immediatly.
5. Mark `Phase 3` in `Context.md` as `Completed`.

### Phase 4 — Final Report Generation

Execute this phase yourself. Subagents not permitted.
Output: `Final-Report.md`

Tasks:
1. Read and execute ONLY `references/final-report-generation.md`.
2. Mark `Phase 4` in `Context.md` as `Completed`.
