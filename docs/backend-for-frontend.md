# Backend contract cho frontend agent

Tài liệu này mô tả hệ thống backend hiện tại để agent viết source frontend gọi đúng API. Chỉ mô tả những gì code đang làm. Module chưa có endpoint thì ghi là chưa có API.

Base URL local: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/`  
OpenAPI: `http://localhost:5000/swagger/v1/swagger.json`  
Docker Compose publish API ra cổng `5000` (container lắng nghe `8080`).

Mọi request/response JSON dùng `Content-Type: application/json`. ASP.NET Core serialize property theo **camelCase**.

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

API đã mở:

- Auth: đăng nhập, đăng xuất, admin tạo tài khoản phóng viên
- Me: đọc role của token, kiểm tra admin
- News: lấy danh sách, lọc theo ngày tạo và danh mục
- System: seed / xóa dữ liệu local

Chưa có API: dashboard, quản lý user đầy đủ, CRUD bài viết, xuất bản, văn bản nội bộ (`Document`).

## Vai trò

`RoleType` lưu trong DB và trong `UserDto.roleName` dưới dạng **số**:

| Giá trị | Số | Quyền đang gắn trên API |
| --- | --- | --- |
| `Admin` | `0` | Tạo tài khoản phóng viên. Gọi được `GET /api/me/check-admin`. |
| `Journalist` | `1` | Đăng nhập, đăng xuất, đọc role của chính mình. Chưa có API bài viết riêng. |

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

`seed-news` chỉ tạo `journalist@370.local` / `Journalist@123` khi database chưa có user nào. Seed user trước thì bài mẫu gắn vào user có sẵn, thường là admin.

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

## Envelope `ApiResult<T>`

Mọi action auth, me, news, và nhánh thành công của system dùng class `ApiResult<T>` trong `congThongTinf.Service/Utils/ApiResult.cs`.

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
| `BadRequest` | 400 | Thiếu field, khoảng ngày không hợp lệ |
| `Unauthorized` | 401 | Sai mật khẩu khi login |
| `Forbidden` | 403 | Đúng danh tính nhưng không được làm: tài khoản khóa, không phải admin khi tạo phóng viên |
| `NotFound` | 404 | Không thấy user, role, email login |
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

`DocumentCategory` (chưa có API): `General=0`, `Policy=1`, `Procedure=2`, `Report=3`, `Guideline=4`.

## System API local

| Method | Path | Thành công |
| --- | --- | --- |
| POST | `/api/system/seed-all-data` | Xóa toàn bộ rồi seed role, 2 admin, category, bài mẫu |
| POST | `/api/system/seed-users-roles` | Thêm role và admin nếu chưa có |
| POST | `/api/system/seed-news` | Thêm category và 4 bài mẫu |
| POST | `/api/system/clear-database` | Xóa News, Documents, Users, Roles, Categories |

Thành công trả `ApiResult`, `value.data` có `message` và count tương ứng (`roleCount`, `adminCount`, `newsCount`).

`seed-all-data` xóa dữ liệu trước. Gọi lại sẽ mất tài khoản phóng viên đã tạo.

## CORS và gọi từ browser

`Program.cs` đăng ký policy `AllowFrontend` và gắn bằng `app.UseCors("AllowFrontend")`. Policy cho mọi origin, mọi header và mọi method, nên browser ở origin khác gọi được API kèm header `Authorization`. Policy không bật credential/cookie. Cùng origin hoặc server-side gọi `localhost:5000` không cần CORS.

## Việc frontend nên làm

- Gửi `Authorization: Bearer {accessToken}` cho `/api/auth/logout`, `/api/auth/create-journalist`, `/api/me/*`.
- Giữ token phía client. Hết 30 phút hoặc gặp 401 framework thì bắt đăng nhập lại.
- Đọc payload ở `response.value.data`.
- Hiện lỗi ở `response.error.message` khi `isSuccess === false`.
- Map `user.roleName`: `0` Admin, `1` Journalist.
- Map `news.status`: `0` Draft, `1` Pending, `2` Published, `3` Archived.
- `GET /api/me/role` trả `role` dạng chuỗi khi cần label đúng với claim.
- Ẩn tạo phóng viên và ẩn gọi `check-admin` khi role không phải `Admin`.
- Coi `/api/system/*` là công cụ seed local, không đưa vào UI người dùng.
