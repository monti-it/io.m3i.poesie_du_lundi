# Runbook: one-time Gmail archive import (issue #52)

Converts the `datasource/Gmail*` `.eml` export into published, back-dated `Poem` rows via
`dotnet PoesieDuLundi.dll import-emails <path> [--dry-run] [--manifest <path>]` — see
`ImportEmailsCommandLine`/`ImportEmailArchive` for what it does and why (dedup by Message-ID,
slug disambiguation, Draft→Scheduled→Published per poem). `datasource/` is gitignored — it only
ever exists on the machine running the import, never in the repo or the container image.

This is a one-time, manually-triggered operation. It is **not** wired into `migrate` or any
deploy step — run it by hand, once, when the archive is ready to import.

## 1. Dry run first (always)

```bash
cd api/src/PoesieDuLundi
dotnet run -- import-emails /path/to/datasource --dry-run
```

Read the report: counts of what would be imported, every non-`.eml` file, every parse failure,
every duplicate (by Message-ID) and every date that would end up with more than one poem. Fix
anything that looks wrong in the archive before proceeding — the real run uses the exact same
scan/dedup logic, so a clean dry run is a reliable preview.

## 2. Back up the database before a real run

The API has no `Delete` endpoint for poems as of this writing, so the backup is the only way
back if the import needs undoing wholesale.

```bash
sudo kubectl exec -n poesie poesie-db-0 -- \
  pg_dump -U poesie -d poesie -Fc -f /tmp/poesie-pre-import.dump
sudo kubectl cp poesie/poesie-db-0:/tmp/poesie-pre-import.dump ./poesie-pre-import.dump
sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/poesie-pre-import.dump
```

Keep `poesie-pre-import.dump` until the import is verified good.

## 3. Real run, against production

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

Keep `import-emails-manifest.json` — it is the rollback key (step 4) and, run again, is what
makes a second pass a no-op (idempotency: it records the source Message-ID and poem id of every
poem it created; already-recorded Message-IDs are skipped).

## 4. Rollback

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
  targeted delete isn't precise enough:

  ```bash
  sudo kubectl cp ./poesie-pre-import.dump poesie/poesie-db-0:/tmp/restore.dump
  sudo kubectl exec -n poesie poesie-db-0 -- \
    pg_restore -U poesie -d poesie --clean --if-exists /tmp/restore.dump
  sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/restore.dump
  ```

## Verified

This procedure (dry run → backup → real run → idempotent re-run → targeted-delete rollback) was
exercised end to end against a throwaway local Postgres container with the real 265-file archive
before this runbook was written: 250 poems imported, 10 true cross-batch duplicates and 4
non-email attachments correctly excluded, 1 email with no readable body correctly flagged, a
second run correctly imported zero (idempotent), and the manifest correctly identified every
created poem for a targeted delete.
