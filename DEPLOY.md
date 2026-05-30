# Deploy Portfolio ออนไลน์ฟรี (Render + Neon)

โปรเจกต์นี้เป็น **ASP.NET Core MVC** — ใช้ **Render** รันแอป + **Neon** เก็บข้อมูล PostgreSQL (แพลน Free ทั้งคู่)

| ส่วน | บริการ |
|------|--------|
| แอป .NET | [Render](https://render.com) Web Service (Docker) |
| ฐานข้อมูล | [Neon](https://neon.tech) PostgreSQL |

Repo: `https://github.com/FiFa254/WebApplication1`

---

## 1. สร้างฐานข้อมูล (Neon)

1. สมัคร [neon.tech](https://neon.tech)
2. สร้าง Project → คัดลอก **connection string** แบบ `postgresql://...` หรือ `postgres://...`

---

## 2. Deploy บน Render

1. Push โค้ดขึ้น GitHub
2. [dashboard.render.com](https://dashboard.render.com) → **New** → **Web Service**
3. เชื่อม repo `WebApplication1`
4. ตั้งค่า:
   - **Runtime:** Docker
   - **Dockerfile path:** `./Dockerfile`
   - **Plan:** Free
5. **Environment Variables:**
   - `DATABASE_URL` = connection string จาก Neon (ทั้งสตริง)
   - `ASPNETCORE_ENVIRONMENT` = `Production`
6. **Create Web Service** — รอ build จน Deploy สำเร็จ

แอปจะรัน migration PostgreSQL อัตโนมัติเมื่อเริ่มทำงาน

---

## 3. Blueprint (ทางเลือก)

มีไฟล์ `render.yaml` ใน repo — ใน Render เลือก **New** → **Blueprint** แล้วชี้ repo (ใส่ `DATABASE_URL` เองหลังสร้าง)

---

## 4. รันบนเครื่อง

ใช้ SQL Server LocalDB ตาม `appsettings.json` ได้เหมือนเดิม:

```powershell
cd WebApplication1
dotnet ef database update
dotnet run
```

ถ้าจะทดสบบแบบ production ให้ตั้ง `DATABASE_URL` ชี้ไป Neon แล้ว `dotnet run`

---

## 5. ข้อจำกัดแพลน Free

- **Render:** แอป sleep เมื่อไม่มี traffic (~15 นาที); เปิดครั้งแรกอาจช้า 30–60 วินาที
- **Neon:** มี quota ฟรีพอสำหรับโปรเจกต์ส่วนตัว
- **รูปอัปโหลด** ใน `wwwroot/uploads` บน Render Free **อาจหายหลัง redeploy** (ต้องการถาวรให้ใช้ cloud storage เช่น S3, Cloudinary)

---

## 6. Push การเปลี่ยนแปลง

```powershell
git add -A
git commit -m "Your message"
git push
```

Render จะ rebuild อัตโนมัติถ้าเปิด Auto-Deploy
