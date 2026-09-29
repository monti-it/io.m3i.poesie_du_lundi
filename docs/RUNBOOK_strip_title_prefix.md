# Runbook: strip the "La poésie du lundi :" title prefix (issue #62)

Removes the redundant "La poésie du lundi :" site-name prefix from existing poem titles via
`dotnet PoesieDuLundi.dll strip-title-prefix [--dry-run]` — see
`StripTitlePrefixCommandLine`/`StripTitlePrefix`/`PoemTitlePrefixCleanup` for what counts as a
match (tolerant of casing, spacing, straight vs full-width colon, and missing accents) and what's
left alone (titles with no subtitle after the prefix, and anything that doesn't start with the
prefix, e.g. a `Fwd:`-prefixed subject line).

This is a one-time, manually-triggered operation. It is **not** wired into `migrate` or any
deploy step — run it by hand, once, against the current data.

## 1. Dry run first (always)

```bash
cd api/src/PoesieDuLundi
dotnet run -- strip-title-prefix --dry-run
```

Read the report: every poem it would retitle, old title next to new title. Anything that looks
wrong (a title that shouldn't have matched, or one that should have but didn't) means the pattern
needs a look before proceeding — the real run uses the exact same matching logic, so a clean dry
run is a reliable preview.

## 2. Back up the database before a real run

```bash
sudo kubectl exec -n poesie poesie-db-0 -- \
  pg_dump -U poesie -d poesie -Fc -f /tmp/poesie-pre-title-cleanup.dump
sudo kubectl cp poesie/poesie-db-0:/tmp/poesie-pre-title-cleanup.dump ./poesie-pre-title-cleanup.dump
sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/poesie-pre-title-cleanup.dump
```

Keep `poesie-pre-title-cleanup.dump` until the cleanup is verified good — it's the only way back,
since the tool doesn't write a manifest of what it changed.

## 3. Real run, against production

```bash
sudo kubectl port-forward -n poesie svc/poesie-db 5432:5432
```

In another shell:

```bash
cd api/src/PoesieDuLundi
export ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database=poesie;Username=poesie;Password=<from poesie-db-credentials>"
dotnet run -- strip-title-prefix
```

Running it again is safe: a title that's already been stripped no longer matches the prefix, so a
second pass reports zero changes.

## 4. Rollback

Restore from the step-2 backup:

```bash
sudo kubectl cp ./poesie-pre-title-cleanup.dump poesie/poesie-db-0:/tmp/restore.dump
sudo kubectl exec -n poesie poesie-db-0 -- \
  pg_restore -U poesie -d poesie --clean --if-exists /tmp/restore.dump
sudo kubectl exec -n poesie poesie-db-0 -- rm /tmp/restore.dump
```

Only the `Title` column changes, and only for retitled poems — slugs, bodies, series and
publication state are untouched, so a targeted fix (re-running with a corrected pattern) is also
an option instead of a full restore, if only the matching logic was wrong.
