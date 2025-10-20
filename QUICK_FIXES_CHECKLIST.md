# ⚡ Quick Fixes Checklist - StudentGest

## 🔴 CRITICAL FIXES (Do First - Est. 8-10 hours)

### Fix 1: Teacher Assignment UI ⭐⭐⭐⭐⭐ ✅ COMPLETED
**Time:** 2-3 hours  
**Priority:** HIGHEST  
**Impact:** Directors can assign teachers to courses easily

**Tasks:**
- [x] Update `Courses.vue` - Add "Asignar Profesor" button to each course card
- [x] Create teacher selection modal in Courses.vue
- [x] Wire up `POST /api/courses/{curso_id}/assign-teacher` endpoint
- [ ] Test: Director assigns teacher, teacher sees course in "My Courses"

**Files Edited:**
- `front/src/views/Courses.vue` (added button, modal, and functions)

**Status:** ✅ Implemented and deployed. Ready for testing.

---

### Fix 2: Allow Teachers to Enroll Students ⭐⭐⭐⭐⭐
**Time:** 2 hours  
**Priority:** HIGHEST  
**Impact:** Teachers can manage their own course rosters

**Tasks:**
- [ ] Modify `back/Courses/controllers/course_controller.py`
- [ ] Update `enroll_course` method (line 99-135)
- [ ] Add check: Allow if user is Director OR teacher of the course
- [ ] Test: Teacher enrolls student in their course, student sees course

**Code Change:**
```python
# In course_controller.py, enroll_course method
user_rol = user_data.get("rol", "").lower()
if user_rol not in ["director", "admin"]:
    # Check if teacher of this course
    async with httpx.AsyncClient(timeout=10.0) as client:
        curso_check = await client.get(
            f"{SUPABASE_URL}/rest/v1/{schema}_cursos?id=eq.{enrollment.curso_id}&profesor_id=eq.{user_data['id']}",
            headers=headers
        )
        if not curso_check.json():
            raise HTTPException(403, "Solo puedes inscribir en tus cursos")
```

---

### Fix 3: Course Enrollment Management UI ⭐⭐⭐⭐
**Time:** 4-6 hours  
**Priority:** HIGH  
**Impact:** Directors can bulk-enroll students

**Tasks:**
- [ ] Create `front/src/views/CourseEnrollmentManagement.vue`
- [ ] Add route: `/course-enrollment` in router.js
- [ ] Add sidebar link (Director section) in AppLayout.vue
- [ ] Features:
  - [ ] Select course from dropdown
  - [ ] Multi-select students to enroll
  - [ ] "Enroll Selected" button
  - [ ] View current enrollments
  - [ ] Quick unenroll action
- [ ] Backend: Use existing `POST /api/courses/enroll` in loop or create bulk endpoint

**Component Structure:**
```vue
<template>
  <div class="enrollment-management">
    <h1>Gestión de Inscripciones</h1>
    
    <!-- Course Selector -->
    <select v-model="selectedCourse">
      <option v-for="course in courses">...</option>
    </select>
    
    <!-- Student Multi-Select -->
    <div class="student-list">
      <label v-for="student in availableStudents">
        <input type="checkbox" v-model="selectedStudents">
        {{ student.nombre }} {{ student.apellido }}
      </label>
    </div>
    
    <button @click="bulkEnroll">Inscribir Seleccionados</button>
    
    <!-- Current Enrollments -->
    <div class="current-enrollments">...</div>
  </div>
</template>
```

---

## 🟡 MEDIUM PRIORITY (Do Second - Est. 8-12 hours)

### Enhancement 1: Course Statistics in Cards ⭐⭐⭐
**Time:** 3-4 hours  
**Impact:** Better overview of courses

**Tasks:**
- [ ] Create backend endpoint: `GET /api/courses/{id}/stats`
- [ ] Returns: `{ profesor_name, estudiantes_count, ultimo_acceso }`
- [ ] Update Courses.vue to fetch and display stats on each card
- [ ] Show "Sin profesor" if no teacher assigned
- [ ] Show enrollment count badge

---

### Enhancement 2: Assignment-Grade Integration ⭐⭐⭐
**Time:** 4-5 hours  
**Impact:** Streamlined grading workflow

**Tasks:**
- [ ] Add `grade_weight_id` field to assignments (link to parcial)
- [ ] When teacher grades assignment, auto-register in Grades service
- [ ] Update AssignmentsView.vue - Add grading interface for teachers
- [ ] Link assignment points to grade weights

---

### Enhancement 3: Assignment File Uploads ⭐⭐⭐
**Time:** 6-8 hours  
**Impact:** Full assignment submission capability

**Tasks:**
- [ ] Set up Supabase Storage bucket: "assignments"
- [ ] Update AssignmentsView.vue - Add file upload component
- [ ] Backend: Update Tareas Program.cs - Add file URL field
- [ ] Store file path in database
- [ ] Teachers can download submitted files
- [ ] Students can replace submissions before deadline

---

## 🟢 LOW PRIORITY (Nice-to-Have)

### Improvement 1: Teacher Course Roster View ⭐⭐
**Time:** 2 hours  
**Impact:** Teachers see student list easily

**Tasks:**
- [ ] Add route: `/my-courses/:id/students`
- [ ] Reuse CourseDetail.vue logic but teacher-accessible
- [ ] Show enrolled students with contact info

---

### Improvement 2: Notification System ⭐
**Time:** 8-10 hours  
**Impact:** Users notified of important events

**Tasks:**
- [ ] Due date reminders for assignments
- [ ] Grade published notifications
- [ ] New forum posts in subscribed threads
- [ ] Excuse approval/rejection alerts

---

## ✅ Testing Checklist

After implementing fixes, test these scenarios:

### As Director:
- [ ] Create a course
- [ ] Assign a teacher to course (NEW FIX)
- [ ] Enroll multiple students (NEW UI)
- [ ] View course statistics (NEW)
- [ ] Remove student from course

### As Teacher:
- [ ] See assigned courses
- [ ] Enroll student in my course (NEW FIX)
- [ ] Take attendance
- [ ] Grade students
- [ ] Create assignment
- [ ] View assignment submissions

### As Student:
- [ ] See enrolled courses
- [ ] View grades
- [ ] View attendance history
- [ ] Submit excuse
- [ ] Complete assignment
- [ ] View forum posts

---

## 📋 Implementation Order

**Week 1: Critical Fixes**
1. Day 1-2: Teacher assignment UI (Fix 1)
2. Day 3: Teacher enrollment permission (Fix 2)
3. Day 4-5: Enrollment management UI (Fix 3)

**Week 2: Enhancements + Testing**
1. Day 1-2: Course statistics (Enhancement 1)
2. Day 3-4: Assignment features (Enhancement 2-3)
3. Day 5: Full system testing

---

## 🚀 Quick Start Commands

```bash
# Start all services
docker compose up -d

# Rebuild frontend after changes
docker compose build frontend
docker compose up -d frontend

# View logs
docker compose logs -f frontend
docker compose logs -f courses

# Check service status
docker compose ps
```

---

## 📝 Notes

- All backend endpoints already exist for most features
- Main work is frontend UI creation
- No database schema changes needed
- Existing auth and role system supports all fixes
- Multi-tenant logic already in place

**Estimated Total Time for Critical Fixes:** 8-10 hours  
**Estimated Total Time for All Fixes:** 25-30 hours
