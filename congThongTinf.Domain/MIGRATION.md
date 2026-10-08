Tạo migration:

Add-Migration InitialCreate -Project congThongTinf.Domain -StartupProject congThongTinf.API

Cập nhật database:

Update-Database -Project congThongTinf.Domain -StartupProject congThongTinf.API

Xóa migration gần nhất nếu chưa áp dụng:

Remove-Migration -Project congThongTinf.Domain -StartupProject congThongTinf.API