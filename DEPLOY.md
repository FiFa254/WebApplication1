# Deploy DevFolio ออนไลน์ฟรี (Render + Neon)

โปรเจกต์นี้เป็น **ASP.NET Core MVC** — ใช้ **Render** รันแอป + **Neon** เก็บข้อมูล PostgreSQL (แพลน Free ทั้งคู่)

| ส่วน | บริการ |
|------|--------|
| แอป .NET | [Render](https://render.com) Web Service (Docker) |
| ฐานข้อมูล | [Neon](https://neon.tech) PostgreSQL |

Repo: `https://github.com/FiFa254/devfolio`

---

## 1. สร้างฐานข้อมูล (Neon)

1. สมัคร [neon.tech](https://neon.tech)
2. สร้าง Project → คัดลอก **connection string** แบบ `postgresql://...` หรือ `postgres://...`

---

## 2. Deploy บน Render

1. Push โค้ดขึ้น GitHub
2. [dashboard.render.com](https://dashboard.render.com) → **New** → **Web Service**
3. เชื่อม repo `DevFolio`
4. ตั้งค่า:
   - **Runtime:** Docker
   - **Dockerfile path:** `./Dockerfile`
   - **Plan:** Free
5. **Environment Variables:**
   - `DATABASE_URL` = connection string จาก Neon (ทั้งสตริง)
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ForwardedHeaders__TrustAll` = `true` (Render อยู่หลัง proxy)
   - `Admin__Username` = ชื่อ admin
   - `Admin__PasswordHash` = hash จาก `dotnet run -- hash-password "รหัสผ่าน"` (รันบนเครื่องตัวเอง)
6. **Create Web Service** — รอ build จน Deploy สำเร็จ

แอปจะรัน migration PostgreSQL อัตโนมัติเมื่อเริ่มทำงาน — Render ตรวจ `/health` ว่าแอปพร้อม

---

## 3. Blueprint (ทางเลือก)

มีไฟล์ `render.yaml` ใน repo — ใน Render เลือก **New** → **Blueprint** แล้วชี้ repo (ใส่ `DATABASE_URL`, `Admin__Username`, `Admin__PasswordHash` เองหลังสร้าง)

---

## 4. รันบนเครื่อง

ใช้ SQL Server LocalDB ตาม `appsettings.json` ได้เหมือนเดิม:

ดูหัวข้อ "Run locally" ใน [README.md](./README.md) — migration รันอัตโนมัติ ไม่ต้อง `dotnet ef database update`

ถ้าจะทดสอบแบบ production ให้ตั้ง `DATABASE_URL` ชี้ไป Neon แล้ว `dotnet run`

---

## 5. ข้อจำกัดแพลน Free

- **Render:** แอป sleep เมื่อไม่มี traffic (~15 นาที); เปิดครั้งแรกอาจช้า 30–60 วินาที
- **Neon:** มี quota ฟรีพอสำหรับโปรเจกต์ส่วนตัว
- **รูปอัปโหลด** บน Render Free **หายหลัง redeploy** เพราะไม่มี disk ถาวร — ต้องการถาวรให้ใช้ Render Disk (แพลนเสียเงิน) แล้วตั้ง `Storage__UploadsPath` + `DataProtection__KeysPath` ไปที่ disk นั้น หรือใช้ Docker compose บนเซิร์ฟเวอร์ตัวเอง (README)
- ไม่มี `DataProtection__KeysPath` ถาวร = admin ต้อง sign in ใหม่หลัง redeploy

---

## 6. Push การเปลี่ยนแปลง

```powershell
git add -A
git commit -m "Your message"
git push
```

Render จะ rebuild อัตโนมัติถ้าเปิด Auto-Deploy
