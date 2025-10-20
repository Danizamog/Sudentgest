# Browser Cache Troubleshooting Guide

## 🔴 Problem
You're seeing old JavaScript code even after rebuilding the Docker container. This is because:
1. **Browser is caching** the old JavaScript files
2. **Service Workers** might be caching the app
3. **Nginx cache headers** might be set incorrectly

## ✅ Solution Steps

### Step 1: Hard Refresh in Browser
**Windows/Linux:**
- `Ctrl + Shift + R` (Chrome, Firefox, Edge)
- Or `Ctrl + F5`

**Mac:**
- `Cmd + Shift + R` (Chrome, Firefox)
- Or `Cmd + Shift + Delete` → Clear cache

### Step 2: Clear Browser Cache Completely
1. Open DevTools (`F12`)
2. Right-click on the refresh button
3. Select "Empty Cache and Hard Reload"

### Step 3: Open in Incognito/Private Mode
- `Ctrl + Shift + N` (Chrome/Edge)
- `Ctrl + Shift + P` (Firefox)

This bypasses all caching.

### Step 4: Clear Application Storage
1. Open DevTools (`F12`)
2. Go to "Application" tab
3. Under "Storage" → Click "Clear site data"
4. Refresh the page

### Step 5: Disable Cache in DevTools (During Development)
1. Open DevTools (`F12`)
2. Go to "Network" tab
3. Check "Disable cache" checkbox
4. Keep DevTools open while testing

## 🔧 What I Fixed in the Code

### 1. Fixed Session Cookie Error
**File**: `front/src/App.vue`

**Before:**
```javascript
onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()
  await handleSession(session)  // ❌ Always called, even with no session
})
```

**After:**
```javascript
onMounted(async () => {
  const { data: { session } } = await supabase.auth.getSession()
  if (session) {  // ✅ Only call if session exists
    await handleSession(session)
  }
})
```

### 2. Fixed Layout Overflow
**File**: `front/src/App.vue`

**Before:**
```css
#app {
  height: 100vh;
  overflow: hidden;  /* ❌ Prevented scrolling */
}
```

**After:**
```css
#app {
  min-height: 100vh;  /* ✅ Allows scrolling */
}
```

### 3. Added Cache Prevention Headers
**File**: `front/index.html`

Added these meta tags:
```html
<meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate">
<meta http-equiv="Pragma" content="no-cache">
<meta http-equiv="Expires" content="0">
```

## 🧪 How to Test

### 1. Clear Browser Cache First!
Use one of the methods above.

### 2. Open DevTools Console
Look for these changes:

**BEFORE (Old Code):**
```
POST http://localhost/auth/session-cookie
400 Bad Request
"Token inválido"
```

**AFTER (New Code):**
```
(No errors on landing page)
```

### 3. Test Sign In Button
1. Go to `http://localhost`
2. Click "Iniciar Sesión"
3. Should navigate to `/signin`
4. Should see login form

### 4. Test Full Flow
1. Landing page → No console errors ✅
2. Click "Iniciar Sesión" → See login form ✅
3. Login with Google/Email → Redirect to app ✅
4. App shows sidebar ✅
5. Sidebar can collapse/expand ✅

## 🚨 If Still Not Working

### Option 1: Use a Different Browser
Try Firefox, Edge, or Chrome Incognito to rule out caching.

### Option 2: Check Container Logs
```powershell
docker logs sudentgest-frontend-1
```

### Option 3: Verify Build Output
```powershell
# Check if files exist in container
docker exec sudentgest-frontend-1 ls -la /usr/share/nginx/html/assets

# Check built JS files
docker exec sudentgest-frontend-1 cat /usr/share/nginx/html/index.html
```

### Option 4: Force Rebuild and Restart
```powershell
# Stop everything
docker compose down

# Remove images
docker rmi sudentgest-frontend:latest

# Rebuild from scratch
docker compose build --no-cache frontend

# Start
docker compose up -d frontend
```

### Option 5: Add Version Query String
Manually navigate to:
```
http://localhost?v=2
```

This forces a new cache key.

## 📝 Expected Behavior After Fix

### Landing Page (http://localhost)
- ✅ No console errors
- ✅ Modern hero section visible
- ✅ Navigation links work
- ✅ "Iniciar Sesión" button works
- ✅ Footer visible
- ✅ Can scroll smoothly

### Sign In Page (http://localhost/signin)
- ✅ Login form visible
- ✅ Email and password inputs
- ✅ Google sign-in button
- ✅ Clean layout (no duplicate elements)

### After Login (http://localhost/teacher-dashboard or /home)
- ✅ Sidebar visible on left
- ✅ Dashboard content visible
- ✅ Can collapse/expand sidebar
- ✅ Navigation works
- ✅ No 400 errors in console

## 🎯 Quick Checklist

Before reporting "still not working":

- [ ] Hard refreshed browser (`Ctrl + Shift + R`)
- [ ] Cleared browser cache completely
- [ ] Tried incognito/private mode
- [ ] Disabled cache in DevTools
- [ ] Waited for full Docker rebuild to complete
- [ ] Restarted the frontend container
- [ ] Checked console for actual errors (not cached errors)
- [ ] Tried a different browser

## 💡 Pro Tips

1. **Always keep DevTools open during development** with cache disabled
2. **Use incognito mode** for testing to avoid cache issues
3. **Check the Network tab** to see if files are being loaded or cached (look for "from disk cache")
4. **Look at the timestamp** of files in Network tab to verify they're fresh
5. **Use hard refresh** after every rebuild

---

**Last Updated**: After adding cache prevention headers and fixing session handling
**Status**: Build in progress - wait for completion before testing
