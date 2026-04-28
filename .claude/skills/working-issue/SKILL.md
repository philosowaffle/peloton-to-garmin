---
name: working-issue
description: Starts work on an existing GitHub issue by fetching its details, creating the correct branch, writing the spec file, committing, and pushing — leaving the project in a ready-to-implement state. Use this skill whenever the user says "work on issue", "start issue", "pick up issue", "implement issue", or provides an issue number and implies they want to begin working on it. Always use this skill when the intent is to begin development on a tracked GitHub issue.
---

# Working a GitHub Issue

When the user wants to start work on an issue, execute the full setup sequence before writing any implementation code.

## Step 1: Fetch the issue

```bash
gh issue view <number> --json number,title,body,labels
```

Read the issue fully — understand the problem statement, acceptance criteria, and any blocking relationships. If the issue is marked as blocked by another open issue, flag this to the user before continuing.

## Step 2: Create and push the branch

```bash
git checkout master
git pull origin master
git checkout -b issue<number>-<short-description>
git push -u origin issue<number>-<short-description>
```

Derive the short description from the issue title — lowercase, hyphen-separated, 3–5 words.

## Step 3: Write the implementation plan

Create `tmp/plan.md` with:
- What needs to be built or changed
- Key files or packages involved
- Order of operations
- Any decisions or trade-offs worth noting upfront

```bash
git add tmp/plan.md
git commit -m "[<number>] implementation plan"
git push
```

## Step 4: Validate before every commit

Before committing any code changes (not docs-only commits), always run:

```bash
dotnet workload restore
dotnet restore
dotnet build --no-restore --configuration Debug
dotnet test
```

All steps must pass cleanly. Fix any failures before committing — do not rely on CI to catch these.

## Step 5: Implement the change

The project is now ready for implementation. After implementing, open a PR.

---

## Opening the PR

When the user asks to open the PR:

1. (Optional agent-team mode) Run `reviewer-agent` as a pre-PR quality gate before opening:
   ```
   Use reviewer-agent to review the branch against the spec before I open the PR.
   ```
   Address any **must fix** items before proceeding. Suggestions are optional.

2. Delete `tmp/` and commit:
   ```bash
   git rm -r tmp/
   git commit -m "[<number>] remove tmp/ before merge"
   git push
   ```

3. Open the PR:
   ```bash
   gh pr create --title "[<number>] <short description>" --body-file tmp/pr-body.md
   ```
   - PR title format: `[<number>] <short description>` matching the project's commit style
   - Body should include a summary, list of changes, test plan, and `Closes #<number>`
   - Write the body to a temp file, then clean it up after the `gh pr create` call

---

## After the PR is Open

### Step 1: Monitor CI

```bash
gh pr checks <pr-number> --watch
```

If any check fails, investigate and fix:
```bash
gh run view <run-id> --log-failed
```

Push fixes and re-verify until all checks pass.

### Step 2: Wait for code review

Once CI passes, wait for any automated or human review to complete before proceeding.

```bash
gh pr view <pr-number> --comments
```

### Step 3: Address review feedback

Read the review carefully. For each piece of feedback:
- If the concern is valid: make the fix, commit, and push
- If the concern is a false positive or intentional: leave a reply explaining why no change was made

After addressing feedback, re-check that CI still passes.

### Step 4: Mark ready

Once CI is green and all review feedback is addressed, tell the user the PR is ready to merge.
