document.addEventListener("DOMContentLoaded", () => {
  // 1. Dữ liệu chuỗi 7 ngày học
  const weekData = [
    { label: 'T2', done: true },
    { label: 'T3', done: true },
    { label: 'T4', done: true },
    { label: 'T5', done: true },
    { label: 'T6', today: true }, // Hôm nay
    { label: 'T7', done: false },
    { label: 'CN', done: false }
  ];

  const weekTimeline = document.getElementById('weekTimeline');
  if (weekTimeline) {
    weekTimeline.innerHTML = weekData.map(day => `
      <div class="t-day ${day.done ? 'done' : ''} ${day.today ? 'today' : ''}">
        <span class="t-dot">${day.done ? '✓' : day.today ? '🔥' : '💧'}</span>
        <span class="t-label">${day.label}</span>
      </div>
    `).join('');
  }

  // 2. Toast thông báo tương tác
  const toast = document.getElementById('toast');
  let toastTimer;

  function showToast(message) {
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toast.classList.remove('show');
    }, 2800);
  }

  // 3. Gắn sự kiện cho các nút hành động (data-msg)
  document.querySelectorAll('[data-msg]').forEach(elem => {
    elem.addEventListener('click', (e) => {
      showToast(elem.dataset.msg);
    });
  });

  // Hộp kho báu tương tác
  const chestImg = document.querySelector('.chest-img');
  if (chestImg) {
    chestImg.addEventListener('click', () => {
      showToast('🎉 Bạn đã nhận được +50 Xu vàng và +20 EXP từ Rương Kho Báu!');
    });
  }

  // 4. Mobile sidebar toggle
  const mobileMenuBtn = document.getElementById('mobileMenuBtn');
  const sidebar = document.getElementById('sidebar');

  if (mobileMenuBtn && sidebar) {
    mobileMenuBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      sidebar.classList.toggle('open');
    });

    document.addEventListener('click', (e) => {
      if (!sidebar.contains(e.target) && e.target !== mobileMenuBtn) {
        sidebar.classList.remove('open');
      }
    });
  }

  // 5. Chuyển đổi tab điều hướng
  document.querySelectorAll('.nav-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
      document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      if (sidebar) sidebar.classList.remove('open');
    });
  });

  // 6. Tìm kiếm
  const searchInput = document.querySelector('.search-input');
  if (searchInput) {
    searchInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        const query = searchInput.value.trim();
        if (query) {
          showToast(`🔍 Đang tìm kiếm bài học: "${query}"...`);
        }
      }
    });
  }
});
