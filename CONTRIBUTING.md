# Contributing

Thanks for considering a contribution to Innovayse Docs.

## Getting started

1. Fork the repo and clone your fork.
2. Follow the setup instructions in [README.md](README.md) to get the backend, collab server,
   and frontend running locally.
3. Create a branch off `main` for your change.

## Making changes

- Keep pull requests focused on a single change — smaller PRs are easier to review.
- Match the existing code style in each service (`backend/` follows standard .NET conventions,
  `frontend/` and `collab/` follow the repo's ESLint/TypeScript config).
- Add or update tests for any behavior change:
  - Backend: `dotnet test` from `backend/`
  - Collab: `npm test` from `collab/`
- Run the frontend type check before submitting: `npx nuxt prepare && npx vue-tsc --noEmit -p .nuxt/tsconfig.app.json`
  from `frontend/` (the plain `vue-tsc --noEmit` silently checks zero files in this project — always
  pass `-p .nuxt/tsconfig.app.json`).

## Commit messages

Use a short imperative summary line, optionally followed by a blank line and more detail on the
*why* behind the change.

## Submitting a pull request

- Describe what the change does and why.
- Link any related issue.
- Make sure CI (if configured) passes before requesting review.

## Reporting bugs

Open an issue with steps to reproduce, expected vs. actual behavior, and relevant environment
details (browser, OS, service version).
