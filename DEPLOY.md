# Deploy DevFolio ออนไลน์ฟรี (Render + Neon)

| ส่วน | บริการ (Free) |
|------|--------|
| แอป .NET (Docker) | [Render](https://render.com) Web Service |
| ฐานข้อมูล PostgreSQL | [Neon](https://neon.tech) |

Repo: `https://github.com/FiFa254/devfolio` — ใช้เวลาประมาณ 15 นาที

---

## 1. สร้างรหัสผ่าน admin (บนเครื่องตัวเอง)

```powershell
cd C:\Users\64502\source\repos\DevFolio
dotnet run -- hash-password "รหัสผ่านที่ต้องการ"
```

คัดลอกบรรทัดที่ขึ้นต้นด้วย `AQAAAA...` เก็บไว้ใช้ในขั้นที่ 3 — **อย่า commit ค่านี้หรือรหัสผ่านลง repo**

---

## 2. สร้างฐานข้อมูล (Neon)

1. สมัคร [neon.tech](https://neon.tech) (ใช้บัญชี GitHub ได้)
2. **Create project** → ตั้งชื่อ `devfolio` → Region ใกล้ที่สุด (เช่น Singapore)
3. หน้า **Dashboard → Connection string** → คัดลอกแบบ `postgresql://...` ทั้งสตริง

---

## 3. Deploy บน Render

1. สมัคร [render.com](https://render.com) ด้วยบัญชี GitHub
2. **New → Blueprint** → เลือก repo `FiFa254/devfolio` → Render อ่าน `render.yaml` ให้เอง
3. กรอกค่าที่ Render ถาม:

   | Key | ค่า |
   |---|---|
   | `DATABASE_URL` | connection string จาก Neon (ขั้นที่ 2) |
   | `Admin__Username` | ชื่อ admin เช่น `admin` |
   | `Admin__PasswordHash` | hash จากขั้นที่ 1 |

   ค่าอื่น (`ASPNETCORE_ENVIRONMENT`, `ForwardedHeaders__TrustAll`, `Seed__DemoData`) ตั้งไว้ใน `render.yaml` แล้ว
4. **Apply** → รอ build (ครั้งแรก ~5–10 นาที) จนสถานะเป็น **Live**
5. เปิด URL `https://devfolio-xxxx.onrender.com`
   - หน้าแรกมี profile ตัวอย่าง 3 คน (`Seed__DemoData`) — ลบ/แก้ได้หลัง sign in
   - Sign in ที่ `/Account/Login`
   - ตรวจสถานะที่ `/health` ต้องได้ `Healthy`

แอปรัน migration PostgreSQL อัตโนมัติเมื่อเริ่มทำงาน

> ไม่ใช้ Blueprint ก็ได้: **New → Web Service** → Runtime **Docker** → Plan **Free** → Health Check Path `/health`
> แล้วใส่ Environment ทั้ง 6 ตัว: `DATABASE_URL`, `Admin__Username`, `Admin__PasswordHash`,
> `ASPNETCORE_ENVIRONMENT=Production`, `ForwardedHeaders__TrustAll=true`, `Seed__DemoData=true`

---

## 4. ใช้เป็นผลงานสมัครงาน

- ใส่ URL เว็บที่ **About** ของ repo บน GitHub (ปุ่มเฟือง → Website)
- ใส่ URL ใน README / Resume คู่กับลิงก์ repo
- บอกผู้สัมภาษณ์ว่าเว็บ Free **หลับหลังไม่มีคนใช้ ~15 นาที** เปิดครั้งแรกรอ 30–60 วินาที

---

## 5. ข้อจำกัดแพลน Free

- **Render:** sleep เมื่อไม่มี traffic ~15 นาที; ไม่มี disk ถาวร
  - **รูปที่อัปโหลดหายหลัง redeploy / restart** — profile ตัวอย่างไม่มีรูปจึงไม่กระทบ
  - admin ต้อง sign in ใหม่หลัง redeploy (ไม่มี `DataProtection__KeysPath` ถาวร)
- **Neon:** quota ฟรีพอสำหรับโปรเจกต์ส่วนตัว; ข้อมูล profile อยู่ถาวร
- ต้องการรูปถาวร: Render Disk (เสียเงิน) แล้วตั้ง `Storage__UploadsPath` + `DataProtection__KeysPath` ไปที่ disk นั้น หรือใช้ `docker compose` บนเซิร์ฟเวอร์ตัวเอง (ดู README)

---

## 6. อัปเดตเว็บ

Push เข้า `master` แล้ว Render build ใหม่อัตโนมัติ (Auto-Deploy เปิดเป็นค่าเริ่มต้น)

## 7. รันบนเครื่อง

ดูหัวข้อ "Run locally" ใน [README.md](./README.md) — migration รันอัตโนมัติ
