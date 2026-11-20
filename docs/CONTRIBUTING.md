# Contributing

How we work: **Issue -> branch -> commits -> pull request -> review -> merge.**

A **pull request (PR)** is a request to merge your branch into `main`. Teammates review it first.

## Steps

**1. Take an issue.** Every task is a GitHub issue. Assign yourself and note its number (e.g. `#12`).

**2. Make a branch.** Never work directly on `main`.

```
git checkout main
git pull
git checkout -b feature/12-create-post
```

Names: `feature/<issue#>-short-name` or `fix/<issue#>-short-name`.

**3. Commit and push.**

```
git add .
git commit -m "Add create post endpoint"
git push -u origin feature/12-create-post
```

**4. Check it works** before opening the PR:

```
docker compose up -d --build
```

Open http://localhost:8080 and click around.

**5. Open a pull request.** Click "Compare & pull request" on GitHub, write `Closes #12` in the description, and request a reviewer.

**6. Review and merge.** The reviewer comments or approves. Fix comments by pushing more commits to the same branch. After approval, click **Merge**, then delete the branch.

## Branch fell behind `main`?

```
git checkout main
git pull
git checkout feature/12-create-post
git merge main
```

On conflicts, fix the marked files, then `git add .` and `git commit`.

## Rules

- No direct pushes to `main`.
- One issue = one branch = one PR. Keep PRs small.
- `git pull` on `main` before starting a new branch.
- Never commit `.env` or passwords. New setting? Add it to `.env.example` too.
- Changed a model class? Add a migration and commit the `backend/Migrations/` files (see README).
- If two PRs both add migrations, tell the teamlead before merging the second one.