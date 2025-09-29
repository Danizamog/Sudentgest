<template>
  <footer class="site-footer">
    <div class="container footer-grid">
      <div class="copyright">© {{ new Date().getFullYear() }} StudentGest</div>
    </div>
  </footer>
</template>

<script>
export default { name: "Footer" };
</script>

<style scoped>
.site-footer {
  width: 100%;
  background: #111318;
  color: #e6e7ea;
  padding: 18px 0;
  margin-top: 20px;
  box-shadow: 0 -1px 0 rgba(0,0,0,0.15);
}
.footer-grid {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}
.links { text-align: right; }
.f-link {
  color: #cbd5e1;
  margin-left: 14px;
  text-decoration: none;
  font-weight: 500;
}
.f-link:hover { color: #ffffff; }

/* responsive */
@media (max-width: 768px) {
  .footer-grid {
    flex-direction: column;
    text-align: center;
    gap: 8px;
  }
  .links { text-align: center; }
}
</style>
