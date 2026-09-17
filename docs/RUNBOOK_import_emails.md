# Runbook: one-time Gmail archive import (issue #52)

Converts the `datasource/Gmail*` `.eml` export into published, back-dated `Poem` rows via
`dotnet PoesieDuLundi.dll import-emails <path> [--dry-run] [--manifest <path>]` — see
`ImportEmailsCommandLine`/`ImportEmailArchive` for what it does and why (dedup by Message-ID,
slug disambiguation, Draft→Scheduled→Published per poem), and `EmailTitleResolver`/
`EmailBodyCleanup` for how a title and a clean body are derived from one email. `datasource/` is
gitignored — it only ever exists on the machine running the import, never in the repo or the
container image.

This is a one-time, manually-triggered operation. It is **not** wired into `migrate` or any
deploy step — run it by hand, once, when the archive is ready to import.

## This is a redo, not a first run

Issue #52 was reopened: the original importer used the raw `Subject:` header and raw body
verbatim, which produced wrong titles for most of the archive (see the issue for the full
analysis) and shipped to production. The corrected parser lives in `EmailTitleResolver` /
`EmailBodyCleanup` now, but it can only produce *new*, correctly-titled poems — it has no way to
retitle what's already in the database. **Every existing `Poem` row must be wiped before the real
run below**, or the archive will double-import against the old bad data.

## 1. Dry run first (always)

```bash
cd api/src/PoesieDuLundi
dotnet run -- import-emails /path/to/datasource --dry-run
```

Read the report: counts of what would be imported, every non-`.eml` file, every parse failure
(now including subjects the resolver can't safely turn into a title — a reply-thread reply, a
subject with ambiguous trailing text, or a bare "la poésie du lundi" subject whose body has no
clear *title* line), every duplicate (by Message-ID), and every date that would end up with more
than one poem. Fix or manually re-title anything flagged before proceeding — the real run uses
the exact same scan/dedup/title logic, so a clean dry run is a reliable preview. The flagged
emails are **not** imported by this tool; add them by hand afterward if they turn out to be
genuine poems.

## 2. Back up the database before touching anything

```bash
sudo kubectl exec -n poesie poesie-db-0 -- \
  pg_dump -U poesie -d poesie -Fc -f /tmp/poesie-pre-import.dump
sudo kubectl cp poesie/poesie-db-0:/tmp/poesie-pre-import.dump ./poesie-pre-import.dump
sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/poesie-pre-import.dump
```

Keep `poesie-pre-import.dump` until the redo is verified good — this is the way back if the wipe
in step 3 needs undoing (there's no per-row manifest for what existed *before* the wipe, unlike
the targeted rollback the import itself gets in step 5).

## 3. Wipe the existing (wrongly-titled) poems

The admin API has a `DELETE /api/admin/poems/{id}` endpoint (issue #18) now, but wiping ~250 rows
one HTTP call at a time isn't worth scripting auth for a one-off. Delete them directly, the same
way the targeted rollback below does:

```bash
sudo kubectl exec -n poesie poesie-db-0 -- \
  psql -U poesie -d poesie -c 'DELETE FROM "Poems";'
```

Confirm the table is empty (`SELECT count(*) FROM "Poems";`) before moving on. This is exactly
what the step-2 backup is for if anything looks wrong afterward.

## 4. Real run, against production

The archive lives only on your machine — run the importer locally against production through a
port-forward, rather than shipping personal data into the cluster:

```bash
sudo kubectl port-forward -n poesie svc/poesie-db 5432:5432
```

In another shell:

```bash
cd api/src/PoesieDuLundi
export ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database=poesie;Username=poesie;Password=<from poesie-db-credentials>"
dotnet run -- import-emails /path/to/datasource --manifest ./import-emails-manifest.json
```

Use a **fresh manifest file** for the redo (don't reuse one from the original bad run) — an old
manifest would make this run skip every Message-ID it already "imported" last time, even though
those poems no longer exist after step 3.

Keep `import-emails-manifest.json` — it is the rollback key (step 5) and, run again, is what makes
a second pass a no-op (idempotency: it records the source Message-ID and poem id of every poem it
created; already-recorded Message-IDs are skipped).

## 5. Rollback

Two options, in order of preference:

- **Targeted delete**, keyed off the manifest — removes exactly what this run created, nothing
  created any other way:

  ```bash
  jq -r '.[].PoemId' import-emails-manifest.json > /tmp/poem-ids.txt
  sudo kubectl exec -n poesie poesie-db-0 -- \
    psql -U poesie -d poesie -c \
    "DELETE FROM \"Poems\" WHERE \"Id\" = ANY(ARRAY[$(paste -sd, /tmp/poem-ids.txt | sed "s/[^,]*/'&'/g")]::uuid[]);"
  ```

- **Full restore** from the step-2 backup, if something else changed in the meantime and a
  targeted delete isn't precise enough (this also undoes the step-3 wipe, restoring the original
  — wrongly-titled — poems):

  ```bash
  sudo kubectl cp ./poesie-pre-import.dump poesie/poesie-db-0:/tmp/restore.dump
  sudo kubectl exec -n poesie poesie-db-0 -- \
    pg_restore -U poesie -d poesie --clean --if-exists /tmp/restore.dump
  sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/restore.dump
  ```

## Verified

Dry-run against the real 265-file archive on the machine that has it: 195 poems would import
cleanly, 58 emails correctly flagged rather than guessed at (4 reply-thread replies that aren't
poems, ~15 subjects with ambiguous trailing text, the rest bare "la poésie du lundi" subjects
whose body has no clear *asterisk-wrapped* title line), 8 true cross-batch duplicates and 4
non-email attachments correctly excluded. Full write path (wipe → import → idempotent re-run →
targeted-delete rollback) was exercised end to end against a throwaway local Postgres container
before the original (now-superseded) version of this runbook was written; the write path itself
is unchanged by this redo, only the title/body derivation is.
