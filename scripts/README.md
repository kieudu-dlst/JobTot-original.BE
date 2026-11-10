> Historical reference: standalone SQL seed/export files and test suites were removed at the project owner's request. Commands referencing those deleted files are no longer available. Application source and EF migrations are retained; existing SQL Server data is unchanged.

# Company directory development data

Apply the current EF migrations before running seeds (`dotnet ef database update --project src/JobTot.Infrastructure --startup-project src/JobTot.Api`).
For the candidate job-detail page, `seed-job-candidate-details.sql` fills missing requirements and sample hiring steps on the original demo jobs only; see [job-detail documentation](../docs/job-detail.md).

`seed-company-directory.sql` inserts 100 fictional companies and 500 Jobs records (five per company), with 10 industries, all 34 official provincial units, salary ranges, descriptions, employee sizes, and related Media records.

Company names contain `(Demo)`, their descriptions identify them as fictional, and job descriptions explicitly state they are not real vacancies. Emails use `example.invalid`. `VerificationStatus = Verified` on 80 demo companies is simulated to exercise the UI, not a claim about actual verification.

Logo and cover SVG assets are in `BE/JobTot/src/JobTot.Api/wwwroot/uploads/companies`. The backend serves their existing Media URLs at `/uploads/companies/...`; Vite proxies that path in development and the frontend uses `VITE_API_BASE_URL` for separate-origin deployments. Job expiration is 60–89 days after the initial insertion.

From `BE/JobTot`:

```powershell
sqlcmd -S '.\SQLEXPRESS' -d JobTot -E -C -b -f 65001 -i scripts/seed-company-directory.sql
```

The seed runs in one transaction, checks the target database name, and uses deterministic IDs plus application locks. Running it again inserts only missing companies/jobs; it does not overwrite their edits or reset expiration dates. The official Province catalog is normalized in place: the existing eight demo IDs are reused, their letter codes are replaced by official two-digit codes and names, and missing provincial units are inserted. No records or foreign-key references are deleted.

To update only provinces (without inserting companies/jobs):

```powershell
sqlcmd -S '.\SQLEXPRESS' -d JobTot -E -C -b -f 65001 -i scripts/seed-vietnam-provinces.sql
```

`vietnam-provinces.json` contains the 34 units (28 provinces and 6 centrally governed cities) with official names and codes under Decision 19/2025/QĐ-TTg, effective 1 July 2025. The sources are the [signed decision](https://chinhphu.vn/?classid=1&docid=214409&orggroupid=3&pageid=27160) and the [Government's code table](https://baochinhphu.vn/bang-danh-muc-va-ma-so-cua-34-tinh-thanh-moi-3321-don-vi-hanh-chinh-cap-xa-moi-102250704153652947.htm). Numeric codes remain strings so leading zeros are preserved. Duplicate matches stop the transaction rather than guessing which referenced ID to retain. This script is for this application's eight old demo rows; it is not a migration of arbitrary pre-merger 63-province data.

The company directory's location filter includes every active provincial unit, including locations with no companies yet.

To regenerate the SQL and SVG fixtures from their source (Python standard library only):

```powershell
python scripts/generate-company-directory-seed.py
```

View the data at `/companies` and `/jobs`; both pages fetch real records from `/api/jobs`. After applying `AddJobDirectoryDetails`, run `seed-job-directory-details.sql` to fill missing experience, position, employment type and benefit fields on these original demo jobs. It only targets their deterministic IDs and demo descriptions, preserves existing edits and does not change salaries or expiration dates. See [job directory API](../docs/job-directory.md).
