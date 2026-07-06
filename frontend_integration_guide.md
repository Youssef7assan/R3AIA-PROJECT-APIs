# R3AIA - دليل بناء الموقع الكامل
## Frontend Implementation Guide for React Developer / AI Agent

---

# معلومات عامة

## اسم المشروع
**رعاية (R3AIA)** - منصة طبية خيرية تربط المرضى بالأطباء والصيدليات والمتطوعين.

## Base URL
```
http://localhost:5129
```

## التقنيات المطلوبة
- React 18+
- React Router v6
- Axios للـ API calls
- Framer Motion للـ Animations
- React Hook Form للفورمات
- React Toastify للإشعارات
- Tailwind CSS أو CSS Modules

---

# متطلبات التصميم (Design Requirements)

## الألوان الأساسية
```css
:root {
  --primary: #0EA5E9;       /* سماوي أساسي - Sky Blue */
  --primary-dark: #0284C7;  /* سماوي غامق */
  --primary-light: #7DD3FC; /* سماوي فاتح */
  --secondary: #FFFFFF;     /* أبيض */
  --background: #F0F9FF;    /* خلفية سماوي فاتح جداً */
  --text-dark: #0C4A6E;     /* نص غامق */
  --text-light: #64748B;    /* نص رمادي */
  --success: #10B981;       /* أخضر للنجاح */
  --error: #EF4444;         /* أحمر للخطأ */
  --warning: #F59E0B;       /* برتقالي للتحذير */
}
```

## الخطوط
- العناوين: `Cairo` أو `Tajawal` (عربي)
- النصوص: `Inter` أو `Roboto`
- حجم العناوين: 32px - 48px
- حجم النصوص: 14px - 16px

## الأنيميشن المطلوبة
1. **Fade In Up:** للعناصر عند ظهورها (opacity 0 to 1, translateY 20px to 0)
2. **Slide In:** للـ Sidebar والـ Modals
3. **Scale:** للأزرار عند الـ Hover (scale 1.05)
4. **Skeleton Loading:** أثناء تحميل البيانات
5. **Pulse:** لأيقونة الإشعارات الجديدة
6. **Smooth Transitions:** لكل التغييرات (300ms ease)

## المبادئ العامة
- تصميم نظيف (Clean UI) بمساحات بيضاء واسعة
- زوايا مستديرة (border-radius: 12px - 16px)
- ظلال خفيفة (box-shadow: 0 4px 20px rgba(0,0,0,0.08))
- أيقونات من مكتبة Lucide React أو Heroicons
- RTL Support كامل (الموقع عربي)
- Responsive لجميع الشاشات

---

# هيكل الصفحات (Page Structure)

## الصفحات العامة (Public)

### 1. صفحة الهوم (Landing Page)
**المسار:** `/`

**الأقسام:**
1. **Hero Section:**
   - عنوان كبير: "رعاية صحية مجانية للجميع"
   - وصف قصير: "نربط المرضى بالأطباء والصيدليات والمتطوعين لتقديم خدمات طبية خيرية"
   - زرين: "سجل كمريض" + "انضم كمتطوع"
   - صورة أو illustration طبية بأنيميشن floating

2. **How It Works Section:**
   - 4 خطوات بأيقونات:
     1. سجل حسابك
     2. أكمل بياناتك
     3. اطلب الخدمة
     4. احصل على المساعدة
   - كل خطوة تظهر بـ Fade In عند السكرول

3. **Services Section:**
   - 3 بطاقات:
     - استشارات طبية مجانية (أيقونة طبيب)
     - توفير الأدوية (أيقونة دواء)
     - توصيل مجاني (أيقونة توصيل)
   - Hover effect على البطاقات

4. **Statistics Section:**
   - أرقام متحركة (Counter Animation):
     - عدد المرضى المستفيدين
     - عدد الأطباء المتطوعين
     - عدد الطلبات المكتملة

5. **Donation Cases Section:**
   - عرض 3 حالات تبرع من الـ API
   - Progress bar لكل حالة
   - زر "تبرع الآن"

6. **Footer:**
   - روابط سريعة
   - معلومات التواصل
   - Social Media Icons

**API المستخدم:**
```
GET /api/Donations/cases
```

---

### 2. صفحة تسجيل الدخول (Login)
**المسار:** `/login`

**التصميم:**
- Split Screen: نصف للفورم، نصف لصورة/illustration
- خلفية gradient سماوي خفيف
- الفورم في بطاقة بيضاء مع ظل

**العناصر:**
- Logo في الأعلى
- عنوان "مرحباً بعودتك"
- حقل البريد الإلكتروني
- حقل كلمة المرور (مع أيقونة إظهار/إخفاء)
- زر "تسجيل الدخول" (سماوي، عرض كامل)
- رابط "نسيت كلمة المرور؟"
- رابط "ليس لديك حساب؟ سجل الآن"

**API:**
```
POST /api/Auth/login
Body: { email, password }
```

**بعد النجاح:**
- خزّن token في localStorage
- إذا hasCompletedProfile == false -> توجيه لـ /complete-profile
- إذا true -> توجيه للـ Dashboard حسب الـ Role

---

### 3. صفحة التسجيل (Register)
**المسار:** `/register`

**التصميم:**
- نفس تصميم Login (Split Screen)
- Stepper في الأعلى يوضح الخطوات

**الخطوة 1: اختيار الدور**
- 4 بطاقات كبيرة:
  - مريض (أيقونة + وصف قصير)
  - طبيب
  - صيدلية
  - متطوع
- Hover effect + أنيميشن عند الاختيار

**الخطوة 2: البيانات الأساسية**
- الاسم الكامل
- البريد الإلكتروني
- اسم المستخدم
- الرقم القومي (14 رقم)
- كلمة المرور
- تأكيد كلمة المرور
- زر "إنشاء الحساب"

**API:**
```
POST /api/Auth/register
Body: { email, userName, fullName, nationalID, password, role }
```

---

### 4. صفحة إكمال البيانات (Complete Profile)
**المسار:** `/complete-profile`

**التصميم:**
- بطاقة مركزية كبيرة
- Progress indicator في الأعلى
- حقول مختلفة حسب الـ Role

**للمريض:**
```
POST /api/Profiles/patient
Body: {
  fullName, phoneNumber, governorateId, cityId,
  address, hasChronicDisease, nidImage, socialProofImage
}
```
- Dropdown المحافظة (من API)
- Dropdown المدينة (يتغير حسب المحافظة)
- رفع صورة البطاقة
- رفع صورة إثبات اجتماعي

**للطبيب:**
```
POST /api/Profiles/doctor
Body: {
  fullName, phoneNumber, specialtyId,
  governorateId, cityId, clinicAddress
}
```

**للصيدلية:**
```
POST /api/Profiles/pharmacy
Body: {
  pharmacyName, phoneNumber,
  governorateId, cityId, address
}
```

**للمتطوع:**
```
POST /api/Profiles/volunteer
Body: {
  fullName, nationalID, phoneNumber, governorateId
}
```

---

## لوحة تحكم المريض (Patient Dashboard)

### 5. الصفحة الرئيسية للمريض
**المسار:** `/patient`

**التصميم:**
- Sidebar ثابت على اليمين (RTL)
- Header مع اسم المستخدم + أيقونة الإشعارات
- Main content area

**Sidebar Links:**
- الرئيسية
- طلب استشارة
- طلب دواء
- طلباتي
- الإشعارات
- تسجيل الخروج

**محتوى الصفحة الرئيسية:**
- بطاقة ترحيب بالاسم
- 2 بطاقات:
  - "طلب استشارة طبية" (أيقونة + زر)
  - "طلب دواء" (أيقونة + زر)
- قسم "آخر طلباتك" (آخر 3 طلبات)

---

### 6. طلب استشارة طبية
**المسار:** `/patient/consultation/new`

**الفورم:**
- Dropdown التخصص (من API التخصصات)
- Textarea وصف الحالة (multiline، حد أدنى 50 حرف)
- منطقة رفع صور (Drag & Drop)
  - حد أقصى 5 صور
  - معاينة الصور المرفوعة
  - زر حذف لكل صورة
- زر "إرسال الطلب"

**API:**
```
POST /api/MedicalRequests/create
Content-Type: multipart/form-data
FormData: Description, SpecialtyId, MedicalImages[]
```

**بعد النجاح:**
- Toast أخضر "تم إرسال طلبك بنجاح"
- توجيه لصفحة "طلباتي"

---

### 7. طلب دواء
**المسار:** `/patient/medicine/new`

**الفورم:**
- منطقة رفع صورة الروشتة (Drag & Drop)
  - معاينة الصورة
  - زر تغيير الصورة
- Checkbox "أريد توصيل الدواء للمنزل"
- زر "إرسال الطلب"

**API:**
```
POST /api/MedicineRequests
Content-Type: multipart/form-data
FormData: PrescriptionImage, NeedDelivery
```

---

### 8. صفحة طلباتي (المريض)
**المسار:** `/patient/my-requests`

**التصميم:**
- Tabs: "الاستشارات الطبية" | "طلبات الأدوية"
- لكل Tab: قائمة بطاقات

**بطاقة الاستشارة:**
- التخصص + الوصف المختصر
- Badge الحالة (Pending/Accepted/Cancelled) بألوان
- إذا Accepted:
  - اسم الطبيب + رقمه
  - عنوان العيادة
  - موعد الكشف
  - ملاحظات الطبيب
- زر "إلغاء الطلب" (يظهر Modal تأكيد)

**API الاستشارات:**
```
GET /api/MedicalRequests/my-requests
POST /api/MedicalRequests/cancel/{id}
```

**بطاقة طلب الدواء:**
- صورة الروشتة (مصغرة، قابلة للتكبير)
- Badge الحالة
- إذا تم التوفير:
  - اسم الصيدلية + رقمها + عنوانها
  - ملاحظات الصيدلية

**API الأدوية:**
```
GET /api/MedicineRequests/my-requests
```

---

## لوحة تحكم الطبيب (Doctor Dashboard)

### 9. الاستشارات المتاحة
**المسار:** `/doctor/consultations`

**التصميم:**
- جدول أو بطاقات
- فلتر حسب التاريخ (اختياري)

**بطاقة الطلب:**
- اسم المريض
- وصف مختصر (أول 100 حرف)
- أيقونة تدل على وجود صور مرفقة
- تاريخ الطلب
- زر "عرض التفاصيل"

**API:**
```
GET /api/MedicalRequests/for-doctor
```

**Empty State:**
- أيقونة + نص "لا توجد استشارات متاحة في تخصصك ومحافظتك حالياً"

---

### 10. تفاصيل الاستشارة
**المسار:** `/doctor/consultation/{id}`

**التصميم:**
- بطاقة كبيرة مقسمة لأقسام

**الأقسام:**
1. **بيانات المريض:**
   - الاسم، الهاتف، العنوان
   - المحافظة، المدينة
   - هل لديه مرض مزمن؟

2. **تفاصيل الطلب:**
   - الوصف الكامل
   - التخصص المطلوب
   - تاريخ الإنشاء

3. **الصور الطبية:**
   - Gallery قابلة للتكبير
   - Lightbox عند الضغط

4. **قسم الرد:**
   - Date & Time Picker للموعد (يجب أن يكون في المستقبل)
   - Textarea لملاحظات الطبيب
   - زر "إرسال الرد"

**API:**
```
GET /api/MedicalRequests/detail/{id}
POST /api/MedicalRequests/respond/{id}
Body: { appointmentDate, doctorNotes }
```

---

## لوحة تحكم الصيدلية (Pharmacy Dashboard)

### 11. طلبات الأدوية المتاحة
**المسار:** `/pharmacy/orders`

**بطاقة الطلب:**
- اسم المريض + مدينته
- صورة الروشتة (مصغرة)
- هل يحتاج توصيل؟ (Badge)
- زر "عرض الروشتة" -> يفتح Modal
- زر "توفير الدواء"

**API:**
```
GET /api/MedicineRequests/open
```

---

### 12. Modal توفير الدواء
**التصميم:**
- صورة الروشتة بحجم كبير
- Textarea لملاحظات الصيدلية (السعر، موعد الاستلام، إلخ)
- زرين: "إلغاء" + "تأكيد التوفير"

**API:**
```
POST /api/MedicineRequests/accept/{id}
Body: { pharmacyNotes }
```

---

## لوحة تحكم المتطوع (Volunteer Dashboard)

### 13. مهام التوصيل المتاحة
**المسار:** `/volunteer/tasks`

**بطاقة المهمة:**
- قسم "من أين؟":
  - اسم الصيدلية
  - العنوان
  - رقم الهاتف (قابل للنقر للاتصال)
- قسم "إلى أين؟":
  - اسم المريض
  - العنوان
  - رقم الهاتف
- زر "أخذ المهمة"

**API:**
```
GET /api/MedicineRequests/delivery-tasks
POST /api/MedicineRequests/take-delivery/{id}
```

---

### 14. مهامي الحالية
**المسار:** `/volunteer/my-tasks`

**بطاقة المهمة المأخوذة:**
- نفس التفاصيل السابقة
- Badge "قيد التوصيل"
- زر "تأكيد التوصيل" (بارز، أخضر)

**API:**
```
POST /api/MedicineRequests/mark-delivered/{id}
```

---

## لوحة تحكم الأدمن (Admin Dashboard)

### 15. توثيق المستخدمين
**المسار:** `/admin/verify-users`

**جدول المستخدمين:**
- الاسم، الدور، تاريخ التسجيل
- زر "قبول" (أخضر) + زر "رفض" (أحمر)

**API:**
```
POST /api/Admin/verify-user
Body: { userId, isApproved }
```

---

### 16. إدارة حالات التبرع
**المسار:** `/admin/donation-cases`

**فورم إنشاء حالة:**
- عنوان الحالة
- الوصف
- المبلغ المطلوب
- رفع صورة الحالة

**API:**
```
POST /api/Admin/create-case
FormData: Title, Description, GoalAmount, CaseImage
```

---

### 17. البلاغات
**المسار:** `/admin/reports`

**جدول البلاغات:**
- المُبلّغ، المُبلّغ عنه، السبب، التاريخ
- زر "حل البلاغ" -> Modal لكتابة التعليق

**API:**
```
GET /api/Admin/all-reports
PUT /api/Admin/resolve-report
Body: { reportId, adminComment }
```

---

### 18. مراقبة الطلبات المتأخرة
**المسار:** `/admin/stalled`

**API:**
```
GET /api/Admin/stalled-requests?minutes=60
```

---

## صفحات إضافية

### 19. صفحة التبرعات العامة
**المسار:** `/donations`

**بطاقة الحالة:**
- صورة الحالة
- العنوان + الوصف
- Progress Bar (المبلغ المجمع / المطلوب)
- زر "تبرع الآن"

**API:**
```
GET /api/Donations/cases
POST /api/Donations/pay
FormData: CaseId, Amount, ReceiptImage
```

---

### 20. صفحة الإشعارات
**المسار:** `/notifications`

**API:**
```
GET /api/Support/notifs
```

**التصميم:**
- قائمة الإشعارات
- الجديدة بخلفية سماوي فاتح
- المقروءة بخلفية بيضاء

---

# ملاحظات التنفيذ النهائية

## Loading States
- استخدم Skeleton loaders بدلاً من Spinners
- الـ Skeleton يكون بلون رمادي فاتح مع أنيميشن pulse

## Error Handling
- Toast أحمر للأخطاء
- رسالة واضحة من الـ API

## Success Messages
- Toast أخضر للنجاح
- مدة العرض: 3 ثواني

## Empty States
- أيقونة كبيرة + نص توضيحي
- زر CTA إذا أمكن

## Responsive Design
- Mobile First
- Breakpoints: 640px, 768px, 1024px, 1280px

---

# نهاية الدليل

هذا الدليل يغطي جميع صفحات الموقع بالتفصيل الكامل.
المطلوب من المطور: تنفيذ كل صفحة كما هو موصوف مع الالتزام بمتطلبات التصميم (سماوي + أبيض + أنيميشن احترافي).
