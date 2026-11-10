> Historical reference: standalone SQL seed/export files and test suites were removed at the project owner's request. Commands referencing those deleted files are no longer available. Application source and EF migrations are retained; existing SQL Server data is unchanged.

## Tối ưu truy vấn BE (15/10/2026)

Migration `20261015011931_AddReadQueryPerformanceIndexes` bổ sung 7 index cho lịch sử
ứng tuyển, việc đã lưu, chiến dịch và truy vấn công ty; thay thế 6 index đơn đã được
index ghép bao phủ. Migration không sửa bản ghi nghiệp vụ. Đã áp dụng vào database
`JobTot` trên `.\SQLEXPRESS` cục bộ sau khi kiểm chứng trong transaction rollback.

Với database ở môi trường khác, chạy từ thư mục `BE/JobTot`:

```powershell
dotnet ef database update AddReadQueryPerformanceIndexes `
  --project src/JobTot.Infrastructure `
  --startup-project src/JobTot.Api `
  --configuration Release
```

Chi tiết thay đổi, số đo trước/sau, cách kiểm tra và hoàn tác:
[Tối ưu hiệu năng backend](../docs/backend-performance.md).

# SQL Server preview data

Application preview content is stored in `JobTot.dbo.DemoDatasets` and read through
`GET /api/demo-data/{key}`. No JSON data files are loaded at runtime.

The local JobTot database has been populated with all four public preview datasets.
For another database, apply EF migrations first, then run:

```powershell
sqlcmd -S .\SQLEXPRESS -d JobTot -E -C -b -f 65001 -i database/preview-data.sql
```

Run from the backend root. The script is transactional and inserts missing keys only;
existing database edits are preserved. Application startup does not seed data.
`preview-data.sql` is a provisioning script; the running application reads SQL Server.

Only explicitly public preview keys are exposed. Account profiles and conversations
continue to use their existing authorized database tables and endpoints. Never put
private conversations or credentials in public preview content.
