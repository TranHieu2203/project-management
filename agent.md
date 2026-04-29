# Agent Login Notes

Nguon seed: `src/Modules/Auth/ProjectManagement.Auth.Infrastructure/Seeding/AuthSeeder.cs`

## Seed Authen Accounts

- Email: `pm1@local.test`
  - Password: `P@ssw0rd!123`
  - DisplayName: `PM One`

- Email: `pm2@local.test`
  - Password: `P@ssw0rd!123`
  - DisplayName: `PM Two`

## Quick Use For Browser/MCP Tests

- Login URL: theo moi truong dang chay (vd `http://localhost:4200` hoac URL frontend hien tai)
- Nguon du lieu dang nhap la account seed, dung cho local/dev test.

## Notes

- Khong dung cac tai khoan nay cho production.
- Neu thay doi seed trong `AuthSeeder.cs`, cap nhat lai file nay.

## Prompt Mau Cho Playwright MCP

### 1) Smoke test trang chu

```text
Dung Playwright MCP mo `http://localhost:4200`.
Cho page load xong, chup screenshot toan trang.
Kiem tra khong co loi nghiem trong trong console (error).
Bao cao ngan gon: URL hien tai, tieu de trang, ket qua check console.
```

### 2) Login voi account seed

```text
Dung Playwright MCP test login voi account seed:
- Email: `pm1@local.test`
- Password: `P@ssw0rd!123`

Buoc lam:
1. Mo `http://localhost:4200` (hoac trang login hien tai).
2. Dien email/password va submit.
3. Xac nhan login thanh cong bang 2 dau hieu: URL doi sang trang sau login va co text/chuc nang Dashboard.
4. Chup screenshot sau login.
5. Neu that bai, tra ve ly do cu the (selector khong tim thay, API loi, validation message...).
```

### 3) Tao task moi (happy path)

```text
Dung Playwright MCP login bang `pm1@local.test` / `P@ssw0rd!123`,
sau do tao 1 task moi voi ten `E2E Task MCP`.

Yeu cau:
- Sau khi tao, xac nhan task xuat hien trong danh sach.
- Chup screenshot truoc va sau khi tao.
- Ket qua tra ve theo checklist:
  - Login: pass/fail
  - Create task: pass/fail
  - Verify in list: pass/fail
```

### 4) Debug nhanh khi test fail

```text
Dung Playwright MCP chay lai flow login voi `pm1@local.test`.
Neu fail, thu thap bang chung debug:
- Screenshot tai buoc fail
- Console errors
- Network request bi fail (status code, endpoint)
- URL hien tai
Sau do de xuat nguyen nhan kha nghi nhat va buoc tiep theo de sua.
```
