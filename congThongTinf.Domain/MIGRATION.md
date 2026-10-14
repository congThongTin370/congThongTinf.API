Không chạy CLI EF Core (`dotnet ef`, `Add-Migration`, `Update-Database`, `Remove-Migration`).

API tự áp dụng migration đang chờ khi khởi động, qua `Database.Migrate()` trong `congThongTinf.API/Architecture/MigrationExtensions.cs`.
