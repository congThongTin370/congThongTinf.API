# Backend contract cho frontend agent

Tài liệu này mô tả hệ thống backend hiện tại để agent viết source frontend gọi đúng API. Chỉ mô tả những gì code đang làm. Module chưa có endpoint thì ghi là chưa có API.

Base URL local: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/`  
OpenAPI: `http://localhost:5000/swagger/v1/swagger.json`  
Docker Compose publish API ra cổng `5000` (container lắng nghe `8080`).

Mọi request/response JSON dùng `Content-Type: application/json`. ASP.NET Core serialize property theo **camelCase**.

Ngoại lệ: tạo bài báo và upload file dùng `multipart/form-data` (`POST /api/news`, `POST /api/news/images`, `POST /api/admin/banners`, `POST /api/documents`, `POST /api/blobs/upload`). Tên field form bind không phân biệt hoa thường (`file`, `title`, `coverImage`, …).

## Hệ thống

Cổng thông tin nội bộ Sư đoàn Không quân 370. Backend là ASP.NET Core 10, PostgreSQL, kiến trúc phân lớp:

| Project | Việc của lớp | Frontend có cần biết |
| --- | --- | --- |
| `congThongTinf.API` | Controller, JWT, Swagger | Có. Đây là bề mặt HTTP. |
| `congThongTinf.Service` | Use case, DTO, `ApiResult` | Có. Shape response và rule nghiệp vụ nằm ở đây. |
| `congThongTinf.Domain` | Entity, enum | Có. Giá trị enum trả về client lấy từ đây. |
| `congThongTinf.Infrastructure` | EF Core, repository, đọc claim user | Không gọi trực tiếp. |

Luồng một request: `Controller` bắt exception → `Service` ném lỗi qua `ErrorHelper` → `UnitOfWork` / PostgreSQL.

Redis có trong Docker Compose và connection string, hiện chưa gắn vào luồng API.

Entity đã có trong database:

- `User`, `Role`
- `News`, `Category`
- `Document`
- `Banner`

API đã mở:

- Auth: đăng nhập, đăng xuất, admin tạo tài khoản phóng viên
- Me: đọc profile/role của token, kiểm tra admin
- News: lấy danh sách (lọc theo ngày tạo và danh mục), lấy chi tiết, tạo, sửa, xóa
- Category: lấy danh sách, lấy chi tiết, tạo, sửa, xóa mềm (chỉ Admin ghi)
- Document: lấy danh sách có phân trang, tải file (`downloadUrl`), admin upload tài liệu nội bộ
- Admin: upload web banner
- Blob: endpoint test upload/xóa file Cloudflare R2
- System: seed / xóa dữ liệu local

Chưa có API: dashboard, quản lý user đầy đủ, xuất bản bài viết (chuyển status sang `Published`), lấy/sửa/xóa banner, sửa/xóa tài liệu.

## Vai trò

`RoleType` lưu trong DB và trong `UserDto.roleName` dưới dạng **số**:

| Giá trị | Số | Quyền đang gắn trên API |
| --- | --- | --- |
| `Admin` | `0` | Tạo tài khoản phóng viên. Gọi được `GET /api/me/check-admin`. Tạo, sửa, xóa danh mục. Upload banner. Upload tài liệu nội bộ. |
| `Journalist` | `1` | Đăng nhập, đăng xuất, đọc role của chính mình. Tạo bài viết, sửa và xóa **bài của chính mình**. Xem danh mục. |

Trong JWT, role là **chuỗi** `"Admin"` hoặc `"Journalist"`, vì token ghi `ClaimTypes.Role` bằng `RoleType.ToString()`.

`[Authorize(Roles = "Admin")]` so khớp chuỗi claim `role`, không so khớp số `0`.

## Xác thực

Scheme: JWT Bearer, HMAC-SHA256. Issuer và audience đều là `congThongTinf`. Secret chỉ nằm ở server (`JWT:SecretKey`).

Access token sống **30 phút**. Không có refresh token. `POST /api/auth/logout` không thu hồi JWT trên server; client phải xóa token. Token cũ vẫn hợp lệ đến khi hết hạn.

Header cho endpoint có `[Authorize]`:

```http
Authorization: Bearer {accessToken}
```

Claim trong token:

| Claim | Ý nghĩa |
| --- | --- |
| `sub` và `nameid` | `User.Id` (Guid) |
| `email` | Email đăng nhập |
| `role` | `"Admin"` hoặc `"Journalist"` |
| `jti` | Id của token |
| `iat` | Thời điểm phát hành, unix seconds |
| `exp` | Hết hạn |

`MapInboundClaims = true`, nên `GET /api/me/*` đọc được `ClaimTypes.Role` và `ClaimTypes.Email`.

### Tài khoản seed

`POST /api/system/seed-users-roles` hoặc `POST /api/system/seed-all-data` tạo:

| Email | Mật khẩu | Role |
| --- | --- | --- |
| `admin@370.local` | `Admin@123` | Admin |
| `admin2@370.local` | `Admin@123` | Admin |
| `nhabao1@gmail.com` | `123321@` | Journalist |
| `nhabao2@gmail.com` | `123321@` | Journalist |

`seed-news` chỉ tạo thêm `nhabao1@gmail.com` khi database chưa có user nào. Seed user trước thì bài mẫu gắn vào user đang active, gồm cả admin và phóng viên.

Các endpoint `/api/system/*` không có `[Authorize]`. Chỉ dùng cho môi trường local.

### Đăng nhập

`POST /api/auth/login` — public.

```json
{
  "email": "admin@370.local",
  "password": "Admin@123"
}
```

Thành công, HTTP `200`:

```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "code": "200",
    "message": "Đăng nhập thành công.",
    "data": {
      "accessToken": "<jwt>",
      "user": {
        "userId": "00000000-0000-0000-0000-000000000000",
        "username": "admin",
        "email": "admin@370.local",
        "fullName": "Quản trị viên 1",
        "roleName": 0,
        "isActive": true,
        "createdAt": "2026-10-08T00:00:00Z"
      }
    }
  },
  "error": null
}
```

`roleName` là số enum, không phải chuỗi `"Admin"`. Muốn chuỗi role thì gọi `GET /api/me/role` với token vừa nhận.

Lỗi nghiệp vụ của login (body vẫn là `ApiResult`, `isSuccess: false`):

| HTTP | `error.code` | Khi nào | `error.message` |
| --- | --- | --- | --- |
| 404 | `"404"` | Email không tồn tại | Không tìm thấy tài khoản với email này. |
| 401 | `"401"` | Sai mật khẩu | Mật khẩu không chính xác. |
| 403 | `"403"` | `isActive == false` | Tài khoản đã bị tạm khóa hoặc cấm sử dụng. |
| 404 | `"404"` | User không có role | Vai trò không hợp lệ. |

Email không tồn tại trả **404**, không trả 401.

### Đăng xuất

`POST /api/auth/logout` — cần token hợp lệ. Body rỗng.

Thành công: HTTP `200`, `value.data` là `true`, message `Đăng xuất thành công.`

User không còn trong DB: HTTP `404`. User bị khóa: HTTP `403`.

### Tạo phóng viên

`POST /api/auth/create-journalist` — token role `Admin`.

```json
{
  "username": "pv01",
  "email": "pv01@370.local",
  "password": "Secret@123",
  "fullName": "Phóng viên 01"
}
```

Thiếu username, email, password hoặc fullName: HTTP `400`, message `Username, email, mật khẩu và họ tên là bắt buộc.`

Email đã tồn tại: HTTP `409`, `Email này đã được đăng ký.`  
Username đã tồn tại: HTTP `409`, `Tên đăng nhập đã được sử dụng.`  
Chưa seed role Journalist: HTTP `404`, `Chưa có vai trò Journalist. Hãy seed roles trước.`

Thành công: HTTP `200`, `value.data` là `UserDto`, `roleName` = `1`.

### Me

`GET /api/me/role` — mọi token hợp lệ.

`value.data`:

```json
{
  "role": "Admin",
  "email": "admin@370.local"
}
```

`role` ở endpoint này là chuỗi.

`GET /api/me/check-admin` — chỉ role `Admin`.

`value.data`:

```json
{
  "isAdmin": true,
  "userId": "00000000-0000-0000-0000-000000000000",
  "email": "admin@370.local",
  "role": "Admin"
}
```

Journalist gọi endpoint này bị middleware chặn HTTP `403` trước khi vào action.

`GET /api/me/details` — mọi token hợp lệ.

Thành công: HTTP `200`, `value.data` là `UserDto` (cùng shape với `user` khi login). User trong token không còn trong DB hoặc `isActive == false`: HTTP `404`.

## Envelope `ApiResult<T>`

Mọi action auth, me, news, category, documents (trừ download file), admin banner, blobs, và nhánh thành công của system dùng class `ApiResult<T>` trong `congThongTinf.Service/Utils/ApiResult.cs`.

Ngoại lệ: `GET /api/documents/{id}/download` trả **binary file**, không bọc `ApiResult`. Thành công thì body là bytes, header `Content-Disposition: attachment`.

Thành công:

| Field | Kiểu | Giá trị |
| --- | --- | --- |
| `isSuccess` | bool | `true` |
| `isFailure` | bool | `false` |
| `value.code` | string | `"200"` |
| `value.message` | string | Câu tiếng Việt của action |
| `value.data` | T | Payload |
| `error` | null | `null` |

Thất bại do `ErrorHelper` trong service:

| Field | Kiểu | Giá trị |
| --- | --- | --- |
| `isSuccess` | bool | `false` |
| `isFailure` | bool | `true` |
| `value` | null | `null` |
| `error.code` | string | Cùng số với HTTP status, dạng chuỗi: `"400"`, `"401"`, `"403"`, `"404"`, `"409"`, `"500"` |
| `error.message` | string | `Exception.Message` |

Controller bắt exception, lấy status từ `Exception.Data["StatusCode"]`, rồi `return StatusCode(statusCode, errorResponse)`. Exception không gắn status thì HTTP **500**, `error.code` = `"500"`.

Cách đọc response:

1. HTTP 2xx và `isSuccess === true` → dùng `value.data`. Hiện message cho user bằng `value.message`.
2. Body có `isSuccess === false` → hiện `error.message`. Phân nhánh bằng HTTP status hoặc `error.code`.
3. Body không có `isSuccess` → đây là response của framework hoặc nhánh lỗi system, xem mục dưới.

`value.code` và `error.code` là string. So sánh với `"401"`, không so với số `401`, trừ khi frontend tự ép kiểu.

## Status code

### Lỗi nghiệp vụ, có envelope

`ErrorHelper` là nguồn status duy nhất trong service:

| Helper | HTTP | Ý nghĩa trong hệ thống này |
| --- | --- | --- |
| `BadRequest` | 400 | Thiếu field, khoảng ngày không hợp lệ, sai loại file upload |
| `Unauthorized` | 401 | Sai mật khẩu khi login |
| `Forbidden` | 403 | Đúng danh tính nhưng không được làm: tài khoản khóa, không phải admin khi tạo phóng viên |
| `NotFound` | 404 | Không thấy user, role, email login, danh mục, bài báo |
| `Conflict` | 409 | Trùng email hoặc username |
| `Internal` | 500 | Lỗi hệ thống chủ động |
| Exception thường | 500 | Lỗi không gắn status |

### Lỗi framework, không có envelope

Các case này xảy ra trước action hoặc ngoài `try/catch`. Body **không** phải `ApiResult`.

| Tình huống | HTTP | Body |
| --- | --- | --- |
| Thiếu header `Authorization`, token sai chữ ký, sai issuer/audience, hoặc hết hạn, trên action có `[Authorize]` | 401 | Thường rỗng. Header `WWW-Authenticate: Bearer`. |
| Token hợp lệ, role không nằm trong `[Authorize(Roles = "...")]` | 403 | Thường rỗng. |
| JSON body hỏng | 400 | `ProblemDetails` của ASP.NET (`type`, `title`, `status`, `traceId`). |
| `POST /api/system/*` lỗi DB hoặc exception | 500 | Chuỗi thuần, ví dụ `Error seeding data: Database issue.` |

Frontend nên coi 401 không có `isSuccess` là hết phiên: xóa token và đưa về màn đăng nhập. 403 không có `isSuccess` là đúng phiên nhưng sai role.

Login sai mật khẩu vẫn là 401 **có** envelope. Đừng xóa token ở nhánh đó vì lúc đó client chưa có phiên.

## Category

Danh mục bài báo. Entity `Category` có `name`, `description`, `icon`, `displayOrder`, `isActive`.

Xóa là **xóa mềm**: `DELETE` chỉ đặt `isActive = false`, bản ghi vẫn còn trong DB. Mọi endpoint đọc và sửa đều chỉ làm việc với danh mục đang `isActive = true`, nên danh mục đã xóa coi như không tồn tại (`404`).

### Danh sách

`GET /api/categories` — public, không cần token.

Chỉ trả danh mục đang active, sắp xếp theo `displayOrder` tăng dần, sau đó theo `name`. Không phân trang.

Thành công: HTTP `200`, `value.data` là mảng `CategoryDto`.

### Chi tiết

`GET /api/categories/{id}` — public, không cần token.

- Thành công: HTTP `200`, `value.data` là `CategoryDto`.
- Không tồn tại hoặc đã xóa: HTTP `404`, `Không tìm thấy danh mục.`

### Tạo

`POST /api/categories` — cần token, role `Admin`.

```json
{
  "name": "Huấn luyện",
  "description": "Tin tức huấn luyện",
  "icon": null,
  "displayOrder": 1,
  "isActive": true
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `name` | Có | Không được chỉ có khoảng trắng. Trùng tên (không phân biệt hoa thường) với danh mục đang active: `409` |
| `description` | Không | Mặc định chuỗi rỗng |
| `icon` | Không | string hoặc null |
| `displayOrder` | Không | Mặc định `0` |
| `isActive` | Không | Mặc định `true` |

Thành công: HTTP `200`, `value.data` là `CategoryDto` vừa tạo.

- `name` thiếu hoặc rỗng: HTTP `400`, `Tên danh mục là bắt buộc.`
- Trùng tên: HTTP `409`, `Tên danh mục đã tồn tại.`
- Không phải Admin: HTTP `403` (framework, không có envelope).

### Sửa

`PUT /api/categories/{id}` — cần token, role `Admin`.

Mọi field đều optional. Field không gửi hoặc gửi `null` thì giữ nguyên.

```json
{
  "name": "Chính trị",
  "description": "Tin chính trị",
  "icon": "flag",
  "displayOrder": 2,
  "isActive": true
}
```

Thành công: HTTP `200`, `value.data` là `CategoryDto` sau khi sửa.

- Không tồn tại hoặc đã xóa: HTTP `404`.
- `name` gửi chuỗi rỗng/khoảng trắng: HTTP `400`.
- Đổi sang tên đang được danh mục active khác dùng: HTTP `409`.
- Không phải Admin: HTTP `403` (framework).

Gửi `isActive: false` qua `PUT` cũng ẩn danh mục, nhưng nên dùng `DELETE` để xóa.

### Xóa

`DELETE /api/categories/{id}` — cần token, role `Admin`. Xóa mềm.

Thành công: HTTP `200`, `value.data` là `true`, message `Xóa danh mục thành công.`

- Không tồn tại hoặc đã xóa: HTTP `404`.
- Danh mục còn bài báo liên quan (kể cả bài Draft): HTTP `409`, `Không thể xóa danh mục đang có bài báo liên quan.`
- Không phải Admin: HTTP `403` (framework).

Danh mục đã xóa mềm vẫn gắn vào bài báo cũ; `categoryName` của bài đó vẫn hiện bình thường.

### CategoryDto

| Field | Kiểu |
| --- | --- |
| `id` | Guid |
| `name` | string |
| `description` | string |
| `icon` | string hoặc null |
| `displayOrder` | number |
| `isActive` | bool |
| `createdAt`, `updatedAt` | datetime UTC |

## News

`GET /api/news` — public, không cần token.

Query:

| Param | Kiểu | Rule |
| --- | --- | --- |
| `fromDate` | date/datetime, optional | Lọc `CreatedAt >=` đầu ngày |
| `toDate` | date/datetime, optional | Lọc `CreatedAt <` ngày kế tiếp, tức gồm cả ngày `toDate` |
| `categoryIds` | Guid lặp lại, optional | `?categoryIds={guid}&categoryIds={guid}` |

`fromDate` lớn hơn `toDate` (so theo ngày): HTTP `400`, message `Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.`

Danh sách sort `CreatedAt` giảm dần. Không phân trang. Trả mọi status, kể cả Draft và Pending.

### Chi tiết

`GET /api/news/{id}` — public.

- Thành công: HTTP `200`, `value.data` là `NewsDto`.
- Không tồn tại: HTTP `404`, `Bài báo với ID {id} không tồn tại.`

### Tạo bài

`POST /api/news` — cần token, role `Journalist`. `Content-Type: multipart/form-data`. Bài tạo ra có `authorId` là user trong token, status `Draft`.

Ảnh bìa là file tải từ máy local. Server upload lên R2 (folder `news/{userId}`), lấy URL và lưu vào `coverImageUrl`. Client **không** gửi URL ảnh.

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `title` | string | Có | Không được chỉ có khoảng trắng |
| `content` | string | Có | Không được chỉ có khoảng trắng |
| `categoryId` | Guid | Có | Phải là Guid của category đang active |
| `summary` | string | Không | Mặc định chuỗi rỗng |
| `coverImage` | file | Không | `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, tối đa 10 MB. Không gửi thì `coverImageUrl` là `null` |

Thành công: HTTP `200`, `value.data` là `NewsDto` vừa tạo.

- Thiếu `title`, `content` hoặc `categoryId`: HTTP `400`, `Tiêu đề, nội dung và danh mục là bắt buộc.`
- `categoryId` không tồn tại: HTTP `404`, `Danh mục với ID {id} không tồn tại.`
- `coverImage` rỗng hoặc sai loại: HTTP `400`, `File là bắt buộc.` hoặc `Ảnh phải là jpg, jpeg, png, webp hoặc gif.`
- `coverImage` lớn hơn 10 MB: HTTP `400`, `File exceeds 10 MB limit.`
- Không phải Journalist (kể cả Admin): HTTP `403`, `Chỉ phóng viên mới được thực hiện thao tác này.`
- User trong token không còn trong DB hoặc bị khóa: HTTP `401`, `Không xác định được người dùng hiện tại.`

Ví dụ gửi bằng form (HTML):

```html
<form enctype="multipart/form-data">
  <input name="title" value="Sư đoàn 370 tổ chức hội nghị giao ban tháng 10">
  <input name="summary" value="Tóm tắt bài báo">
  <textarea name="content">Nội dung bài báo</textarea>
  <input name="categoryId" value="{categoryGuid}">
  <input type="file" name="coverImage" accept=".jpg,.jpeg,.png,.webp,.gif">
</form>
```

Ví dụ `coverImageUrl` trong `NewsDto` trả về: `https://<PublicUrl>/news/{userId}/{guid}.jpg`.

### Upload ảnh trong bài

`POST /api/news/images` — cần token, role `Journalist`. `Content-Type: multipart/form-data`.

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `file` | Có | `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, tối đa 10 MB |

Thành công: HTTP `200`, `value.data` là `NewsImageDto`, message `Tải ảnh thành công.`

```json
{
  "url": "https://<PublicUrl>/news/{userId}/{guid}.jpg"
}
```

- Không gửi file / file rỗng: HTTP `400`, `File là bắt buộc.`
- Sai loại ảnh: HTTP `400`, `Ảnh phải là jpg, jpeg, png, webp hoặc gif.`
- Token không phải Journalist: HTTP `403` (framework).

Endpoint này chỉ trả URL. Không tạo bản ghi bài báo. Dùng khi cần chèn ảnh vào nội dung bài.

### Sửa bài

`PUT /api/news/{id}` — cần token, role `Journalist`, và phải là tác giả của bài.

Mọi field đều optional. Field không gửi hoặc gửi `null` thì giữ nguyên.

```json
{
  "title": "Tiêu đề mới",
  "summary": "Tóm tắt mới",
  "content": "Nội dung mới",
  "coverImageUrl": "https://...",
  "categoryId": "00000000-0000-0000-0000-000000000000"
}
```

Thành công: HTTP `200`, `value.data` là `NewsDto` sau khi sửa.

- Không tồn tại: HTTP `404`.
- Không phải tác giả: HTTP `403`, `Bạn chỉ được thao tác trên bài viết của chính mình.`
- Không phải Journalist: HTTP `403`.
- `title` hoặc `content` gửi chuỗi rỗng/khoảng trắng: HTTP `400`.
- `categoryId` không tồn tại: HTTP `404`.

### Xóa bài

`DELETE /api/news/{id}` — cần token, role `Journalist`, phải là tác giả. Xóa cứng (hard delete): bản ghi bị xóa khỏi DB, không khôi phục được. Soft delete chưa được hỗ trợ vì `BaseEntity` chưa có cờ xóa.

Thành công: HTTP `200`, `value.data` là `true`, message `Xóa bài báo thành công.`

- Không tồn tại: HTTP `404`.
- Không phải tác giả hoặc không phải Journalist: HTTP `403`.

### Quyền trên endpoint ghi

`POST`, `PUT`, `DELETE` dùng `[Authorize]`. Thiếu token trả 401 framework (không có envelope). Kiểm tra role Journalist và quyền tác giả nằm trong service, trả envelope `403`.

Role được lấy từ DB theo `userId` trong token (qua `AdminService.GetUserById`), không đọc từ claim `role`. Nếu user bị đổi role hoặc khóa trong DB, quyền thay đổi theo DB ngay cả khi token còn hạn.

`NewsStatus` trong `status` là số:

| Tên | Số |
| --- | --- |
| `Draft` | 0 |
| `Pending` | 1 |
| `Published` | 2 |
| `Archived` | 3 |

`NewsDto` trong `value.data[]`:

| Field | Kiểu |
| --- | --- |
| `id` | Guid |
| `title`, `summary`, `content` | string |
| `coverImageUrl` | string hoặc null |
| `categoryId` | Guid |
| `categoryName` | string |
| `authorId` | Guid |
| `authorName` | string, lấy `User.FullName` |
| `status` | number `0..3` |
| `publishedAt` | datetime hoặc null |
| `createdAt`, `updatedAt` | datetime UTC |
| `viewCount` | number |

## Document

Tài liệu nội bộ sư đoàn. File lưu trên Cloudflare R2, metadata lưu bảng `Documents`. Giới hạn kích thước file **10 MB** (cùng rule với `BlobService`).

`DocumentCategory` là số:

| Tên | Số |
| --- | --- |
| `General` | 0 |
| `Policy` | 1 |
| `Procedure` | 2 |
| `Report` | 3 |
| `Guideline` | 4 |

### Danh sách

`GET /api/documents` — public, không cần token.

Query:

| Param | Kiểu | Mặc định | Rule |
| --- | --- | --- | --- |
| `pageIndex` | number, optional | `1` | Trang bắt đầu từ `1`. `0` = trả toàn bộ, bỏ `Skip`/`Take` |
| `pageSize` | number, optional | `10` | Nếu `<= 0` server gán lại `10` |
| `search` | string, optional | — | Không phân biệt hoa thường, khớp `title`, `fileName`, hoặc `description` |
| `category` | number, optional | — | Lọc đúng `DocumentCategory`. Giá trị không thuộc enum: HTTP `400`, `Danh mục tài liệu không hợp lệ.` |

Sort `CreatedAt` giảm dần.

`Pagination<T>` kế thừa `List<T>`. System.Text.Json serialize `IEnumerable` thành **JSON array**, nên `value.data` là mảng `DocumentDto` của trang hiện tại. Các field `currentPage`, `totalPages`, `pageSize`, `totalCount`, `hasPrevious`, `hasNext` **không** có trong JSON. Frontend tự giữ `pageIndex`/`pageSize` đã gửi.

Thành công: HTTP `200`, message `Lấy danh sách tài liệu nội bộ thành công.` Mỗi phần tử có `downloadUrl` tuyệt đối (scheme + host của request hiện tại).

Ví dụ một phần tử trong `value.data[]`:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Nội quy sư đoàn",
  "description": "",
  "fileUrl": "https://<r2-public>/documents/ab12cd34.pdf",
  "downloadUrl": "http://localhost:5000/api/documents/3fa85f64-5717-4562-b3fc-2c963f66afa6/download",
  "fileName": "noi-quy.pdf",
  "fileType": "application/pdf",
  "fileSize": 102400,
  "category": 1,
  "uploadedByUserId": "00000000-0000-0000-0000-000000000000",
  "uploadedByUserName": "Quản trị viên 1",
  "createdAt": "2026-10-09T00:00:00Z"
}
```

`POST /api/documents` thành công cũng trả `DocumentDto` cùng shape, gồm `downloadUrl`.

### Tải file

`GET /api/documents/{id}/download` — public. **Không** trả `ApiResult`.

Thành công HTTP `200`:

| Header | Giá trị |
| --- | --- |
| `Content-Type` | `fileType` của tài liệu, ví dụ `application/pdf` |
| `Content-Disposition` | `attachment; filename="noi-quy.pdf"` (tên gốc `fileName`) |

Body là bytes của file trên R2. Đây chính là URL trong `downloadUrl`.

UI gắn `downloadUrl` vào nút. Click = điều hướng tới API, trình duyệt lưu file. Không `fetch`, không gắn JWT.

HTML:

```html
<a href="http://localhost:5000/api/documents/{id}/download" download="noi-quy.pdf">Tải xuống</a>
```

Razor:

```cshtml
<a class="btn" href="@item.DownloadUrl" download="@item.FileName">Tải xuống</a>
```

| Field | Dùng khi |
| --- | --- |
| `downloadUrl` | `href` của nút Tải xuống |
| `fileName` | attribute `download` (gợi ý tên file) |
| `fileUrl` | Xem inline (PDF mở tab mới). **Không** dùng cho nút download |

- Id không tồn tại: HTTP `404`, envelope `Tài liệu với ID {id} không tồn tại.`
- Bản ghi còn, object R2 mất: HTTP `404`, envelope `File not found.`
- CORS không áp dụng cho click thẻ `<a>` (top-level navigation).

### Upload

`POST /api/documents` — token role `Admin`. `Content-Type: multipart/form-data`.

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `file` | Có | Phần mở rộng: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt` |
| `title` | Không | Trống thì lấy tên file bỏ phần mở rộng |
| `description` | Không | Mặc định chuỗi rỗng |
| `category` | Không | Mặc định `0` (`General`) |

Thành công: HTTP `200`, `value.data` là `DocumentDto`, message `Tải tài liệu nội bộ thành công.`

- Không gửi file / file rỗng: HTTP `400`, `File là bắt buộc.`
- Sai loại file: HTTP `400`, `Tài liệu nội bộ phải là pdf, doc, docx, xls, xlsx, ppt, pptx hoặc txt.`
- `category` không thuộc enum: HTTP `400`, `Danh mục tài liệu không hợp lệ.`
- File lớn hơn 10 MB: HTTP `400`, `File exceeds 10 MB limit.`
- Token không phải Admin: HTTP `403` (framework, không có envelope).
- User trong token không còn trong DB hoặc bị khóa: HTTP `404`.

Không có API sửa, xóa, hoặc lấy chi tiết JSON theo id. Tải file dùng `GET /api/documents/{id}/download`.

### DocumentDto

| Field | Kiểu |
| --- | --- |
| `id` | Guid |
| `title`, `description` | string |
| `fileUrl` | string, URL public R2 (xem file, PDF mở trên trình duyệt) |
| `downloadUrl` | string, ví dụ `http://localhost:5000/api/documents/{id}/download`. Gắn vào `href` của nút Tải xuống |
| `fileName` | string, tên file gốc, dùng cho attribute `download` |
| `fileType` | string, content type |
| `fileSize` | number, bytes |
| `category` | number `0..4` |
| `uploadedByUserId` | Guid |
| `uploadedByUserName` | string hoặc null, `User.FullName` của người upload (list có include; upload mới thường `null`) |
| `createdAt` | datetime UTC |

## Banner

Banner trang chủ. File ảnh lưu R2 folder `banners`, metadata bảng `Banners`. Chưa có API lấy danh sách / chi tiết / xóa.

`POST /api/admin/banners` — token role `Admin`. `Content-Type: multipart/form-data`. Controller gắn `[Authorize(Roles = "Admin")]` trên cả class.

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `file` | Có | `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif` |
| `title` | Không | Trống thì lấy tên file bỏ phần mở rộng |
| `displayOrder` | Không | Mặc định `0` |
| `isActive` | Không | Mặc định `true` |

Thành công: HTTP `200`, `value.data` là `BannerDto`, message `Tải banner thành công.`

- Không gửi file / file rỗng: HTTP `400`, `File là bắt buộc.`
- Sai loại ảnh: HTTP `400`, `Banner phải là ảnh (jpg, jpeg, png, webp, gif).`
- File lớn hơn 10 MB: HTTP `400`, `File exceeds 10 MB limit.`
- Token không phải Admin: HTTP `403` (framework).
- User trong token không còn trong DB hoặc bị khóa: HTTP `404`.

Upload tạo bản ghi mới, không thay banner cũ.

### BannerDto

| Field | Kiểu |
| --- | --- |
| `id` | Guid |
| `title` | string |
| `imageUrl` | string, URL public ảnh |
| `fileName` | string |
| `fileType` | string |
| `fileSize` | number, bytes |
| `displayOrder` | number |
| `isActive` | bool |
| `uploadedByUserId` | Guid |
| `createdAt` | datetime UTC |

## Blob (test R2)

Endpoint thử Cloudflare R2. **Không** có `[Authorize]`. Không nên đưa vào UI người dùng.

`POST /api/blobs/upload` — `multipart/form-data`.

| Field / query | Ghi chú |
| --- | --- |
| `file` | Bắt buộc, tối đa 10 MB |
| `folder` | Query, mặc định `"test"` |

Thành công: HTTP `200`, `value.data`:

| Field | Kiểu |
| --- | --- |
| `key` | string, object key trên R2 |
| `url` | string, URL public |
| `size` | number |
| `contentType` | string |

Thiếu file: HTTP `400`, `File is required.`  
Quá 10 MB: HTTP `400`, `File exceeds 10 MB limit.`

`DELETE /api/blobs?key={key}` — xóa object theo key. Thành công: `value.data` = `true`. Key rỗng: HTTP `400`, `Key is required.`

## System API local

| Method | Path | Thành công |
| --- | --- | --- |
| POST | `/api/system/seed-all-data` | Xóa toàn bộ rồi seed role, 2 admin, 2 phóng viên, category, bài mẫu |
| POST | `/api/system/seed-users-roles` | Thêm role, admin và phóng viên nếu chưa có |
| POST | `/api/system/seed-news` | Thêm category và 4 bài mẫu |
| POST | `/api/system/clear-database` | Xóa News, Banners, Documents, Users, Roles, Categories |

Thành công trả `ApiResult`, `value.data` có `message` và count tương ứng (`roleCount`, `adminCount`, `journalistCount`, `newsCount`).

`seed-all-data` xóa dữ liệu trước. Gọi lại sẽ mất tài khoản phóng viên đã tạo.

## CORS và gọi từ browser

`Program.cs` đăng ký policy `AllowFrontend` và gắn bằng `app.UseCors("AllowFrontend")`. Policy cho mọi origin, mọi header và mọi method, nên browser ở origin khác gọi được API kèm header `Authorization`. Policy không bật credential/cookie. Cùng origin hoặc server-side gọi `localhost:5000` không cần CORS.

## Việc frontend nên làm

- Gửi `Authorization: Bearer {accessToken}` cho `/api/auth/logout`, `/api/auth/create-journalist`, `/api/me/*`, `POST`/`PUT`/`DELETE` `/api/categories`, `POST`/`PUT`/`DELETE` `/api/news`, `POST /api/news/images`, `POST /api/admin/banners`, `POST /api/documents`.
- `GET /api/news`, `GET /api/news/{id}`, `GET /api/categories`, `GET /api/documents`, `GET /api/documents/{id}/download` là public.
- Tạo bài báo, upload ảnh bài, upload banner và tài liệu dùng `multipart/form-data`, không dùng JSON. Riêng `POST /api/news` gửi ảnh bìa qua field `coverImage`, không gửi `coverImageUrl`.
- Giữ token phía client. Hết 30 phút hoặc gặp 401 framework thì bắt đăng nhập lại.
- Đọc payload ở `response.value.data`.
- Hiện lỗi ở `response.error.message` khi `isSuccess === false`.
- Map `user.roleName`: `0` Admin, `1` Journalist.
- Map `news.status`: `0` Draft, `1` Pending, `2` Published, `3` Archived.
- Map `document.category`: `0` General, `1` Policy, `2` Procedure, `3` Report, `4` Guideline.
- `GET /api/documents`: `value.data` là **mảng** item của trang; tự giữ `pageIndex`/`pageSize`. Mỗi item có `downloadUrl`.
- Nút tải tài liệu: `<a href="@item.DownloadUrl" download="@item.FileName">Tải xuống</a>`. Không dùng `fileUrl` cho nút download. Không `fetch` endpoint download.
- `GET /api/me/role` trả `role` dạng chuỗi khi cần label đúng với claim.
- Ẩn tạo phóng viên, upload banner, upload tài liệu, và gọi `check-admin` khi role không phải `Admin`.
- Coi `/api/system/*` và `/api/blobs/*` là công cụ local/test, không đưa vào UI người dùng.
