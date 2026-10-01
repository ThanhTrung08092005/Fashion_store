/**
 * Fashion CRM - Shared Utilities
 */

// ===== API CONFIG =====
// Tự động dùng port 5000 khi chạy http-server/Live Server.
// Nếu backend chạy port khác, đổi biến API_PORT bên dưới.
const API_PORT = 5000;
const API_BASE = `http://localhost:${API_PORT}/api`;

const API = {
  login:        `${API_BASE}/auth/login`,
  logout:       `${API_BASE}/auth/logout`,
  // Tài khoản
  accounts:     `${API_BASE}/taikhoan`,
  roles:        `${API_BASE}/vaitro`,
  // Khách hàng
  customers:    `${API_BASE}/khachhang`,
  // Quản lý
  managers:     `${API_BASE}/quanly`,
  // Sản phẩm
  products:     `${API_BASE}/sanpham`,
  categories:   `${API_BASE}/danhmuc`,
  // Đơn hàng
  orders:       `${API_BASE}/donhang`,
  // Phản hồi
  feedbacks:    `${API_BASE}/phanhoi`,
  // Đánh giá
  reviews:      `${API_BASE}/danhgia`,
  // Khảo sát
  surveys:      `${API_BASE}/khaosat`,
  surveyForms:  `${API_BASE}/phieukhaosat`,
};

// ===== LOCAL STORAGE HELPERS =====
const Storage = {
  set: (key, value) => localStorage.setItem(key, JSON.stringify(value)),
  get: (key) => { try { return JSON.parse(localStorage.getItem(key)); } catch { return null; } },
  remove: (key) => localStorage.removeItem(key),
  clear: () => localStorage.clear(),
};

// ===== AUTH HELPERS =====
const Auth = {
  getUser:  () => Storage.get('crm_user'),
  getToken: () => Storage.get('crm_token'),
  isLoggedIn: () => !!Storage.get('crm_token'),

  login(user, token) {
    Storage.set('crm_user', user);
    Storage.set('crm_token', token);
  },

  logout() {
    Storage.remove('crm_user');
    Storage.remove('crm_token');
    // Tìm đường về login.html từ bất kỳ thư mục nào
    const depth = window.location.pathname.split('/').filter(Boolean).length;
    const prefix = depth > 1 ? '../'.repeat(depth - 1) : '';
    window.location.href = prefix + 'login.html';
  },

  requireRole(...roles) {
    const user = Auth.getUser();
    if (!user) { window.location.href = '/login.html'; return false; }
    if (!roles.includes(user.vaiTro)) {
      Toast.error('Bạn không có quyền truy cập trang này.');
      setTimeout(() => Auth.logout(), 1500);
      return false;
    }
    return true;
  },

  redirectByRole() {
    const user = Auth.getUser();
    if (!user) return;
    // Xác định đường dẫn gốc (hoạt động với cả http-server lẫn file://)
    const base = window.location.origin;
    const root = window.location.pathname.includes('/customer/') ||
                 window.location.pathname.includes('/admin/')    ||
                 window.location.pathname.includes('/manager/')
      ? window.location.pathname.split('/').slice(0,-1).join('/').replace(/\/(customer|admin|manager)$/,'')
      : '';
    const map = {
      'Admin':      `${root}/admin/dashboard.html`,
      'QuanLy':     `${root}/manager/dashboard.html`,
      'KhachHang':  `${root}/customer/home.html`,
    };
    const dest = map[user.vaiTro] || map[user.VaiTro];
    if (dest) window.location.href = dest;
  }
};

// ===== HTTP CLIENT =====
const Http = {
  _headers() {
    const h = { 'Content-Type': 'application/json' };
    const t = Auth.getToken();
    if (t) h['Authorization'] = `Bearer ${t}`;
    return h;
  },

  _pascalize(value) {
    if (Array.isArray(value)) return value.map(item => Http._pascalize(item));
    if (!value || typeof value !== 'object') return value;
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [
      key.charAt(0).toUpperCase() + key.slice(1),
      Http._pascalize(item),
    ]));
  },

  async _handle(res) {
    if (res.status === 401) { Auth.logout(); return; }
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.message || data.Message || `Lỗi ${res.status}`);
    return Http._pascalize(data);
  },

  get:    (url, params) => {
    const u = params ? `${url}?${new URLSearchParams(params)}` : url;
    return fetch(u, { headers: Http._headers() }).then(Http._handle);
  },
  post:   (url, body)   => fetch(url, { method: 'POST',   headers: Http._headers(), body: JSON.stringify(body) }).then(Http._handle),
  put:    (url, body)   => fetch(url, { method: 'PUT',    headers: Http._headers(), body: JSON.stringify(body) }).then(Http._handle),
  delete: (url)         => fetch(url, { method: 'DELETE', headers: Http._headers() }).then(Http._handle),
  patch:  (url, body)   => fetch(url, { method: 'PATCH',  headers: Http._headers(), body: JSON.stringify(body) }).then(Http._handle),
};

// ===== TOAST =====
const Toast = {
  _container: null,
  _init() {
    if (!this._container) {
      this._container = document.createElement('div');
      this._container.id = 'toast-container';
      document.body.appendChild(this._container);
    }
  },
  _show(type, msg, duration = 3500) {
    this._init();
    const icons = { success: '✅', error: '❌', warning: '⚠️', info: 'ℹ️' };
    const el = document.createElement('div');
    el.className = `toast toast-${type}`;
    el.innerHTML = `
      <span class="toast-icon">${icons[type]}</span>
      <span class="toast-msg">${msg}</span>
      <button class="toast-close" onclick="this.parentElement.remove()">×</button>`;
    this._container.appendChild(el);
    setTimeout(() => el.style.transition = 'opacity .4s', duration - 400);
    setTimeout(() => { el.style.opacity = '0'; setTimeout(() => el.remove(), 400); }, duration);
  },
  success: (msg) => Toast._show('success', msg),
  error:   (msg) => Toast._show('error', msg),
  warning: (msg) => Toast._show('warning', msg),
  info:    (msg) => Toast._show('info', msg),
};

// ===== MODAL =====
const Modal = {
  open(id)  { document.getElementById(id)?.classList.add('active'); },
  close(id) { document.getElementById(id)?.classList.remove('active'); },
  confirm(title, msg, onConfirm) {
    const id = '__confirm_modal';
    let el = document.getElementById(id);
    if (!el) {
      el = document.createElement('div');
      el.id = id;
      el.className = 'modal-overlay';
      el.innerHTML = `
        <div class="modal">
          <div class="modal-header"><h3 class="modal-title" id="${id}_title"></h3></div>
          <div class="modal-body"><p id="${id}_msg"></p></div>
          <div class="modal-footer">
            <button class="btn btn-secondary" onclick="Modal.close('${id}')">Hủy</button>
            <button class="btn btn-danger"    id="${id}_confirm">Xác nhận</button>
          </div>
        </div>`;
      document.body.appendChild(el);
    }
    document.getElementById(`${id}_title`).textContent = title;
    document.getElementById(`${id}_msg`).textContent   = msg;
    document.getElementById(`${id}_confirm`).onclick   = () => { Modal.close(id); onConfirm(); };
    Modal.open(id);
  }
};

// Close modal when clicking overlay
document.addEventListener('click', e => {
  if (e.target.classList.contains('modal-overlay')) e.target.classList.remove('active');
});

// ===== LOADING =====
const Loading = {
  show() { document.getElementById('loading-overlay')?.classList.add('active'); },
  hide() { document.getElementById('loading-overlay')?.classList.remove('active'); },
};

// ===== FORMAT HELPERS =====
const Format = {
  currency: (v) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(v),
  date:     (v) => v ? new Date(v).toLocaleDateString('vi-VN') : '—',
  datetime: (v) => v ? new Date(v).toLocaleString('vi-VN') : '—',
  phone:    (v) => v || '—',

  statusBadge(status, map) {
    const entry = map[status] || { label: status, class: 'badge-secondary' };
    return `<span class="badge ${entry.class}">${entry.label}</span>`;
  },

  orderStatus(s) {
    const map = {
      'Chờ xác nhận': { label: 'Chờ xác nhận', class: 'badge-warning' },
      'Đã xác nhận':  { label: 'Đã xác nhận',  class: 'badge-info'    },
      'Đang giao':    { label: 'Đang giao',     class: 'badge-primary' },
      'Đã giao':      { label: 'Đã giao',       class: 'badge-success' },
      'Đã hủy':       { label: 'Đã hủy',        class: 'badge-danger'  },
    };
    return Format.statusBadge(s, map);
  },

  feedbackStatus(s) {
    const map = {
      'Chưa xử lý': { label: 'Chưa xử lý', class: 'badge-danger'  },
      'Đang xử lý': { label: 'Đang xử lý', class: 'badge-warning' },
      'Đã xử lý':   { label: 'Đã xử lý',   class: 'badge-success' },
      'Đã đóng':    { label: 'Đã đóng',     class: 'badge-secondary'},
    };
    return Format.statusBadge(s, map);
  },

  stars(n) {
    return '★'.repeat(n) + '☆'.repeat(5 - n);
  }
};

// ===== PAGINATION =====
class Paginator {
  constructor(containerSel, onChange) {
    this.container = document.querySelector(containerSel);
    this.onChange  = onChange;
    this.page      = 1;
    this.total     = 0;
    this.pageSize  = 10;
  }

  render() {
    if (!this.container) return;
    const pages = Math.ceil(this.total / this.pageSize) || 1;
    let html = `
      <button class="page-btn" onclick="this.getRootNode().host?._pager?.go(${this.page-1})" 
        ${this.page===1?'disabled':''}>‹</button>`;
    for (let i = 1; i <= pages; i++) {
      if (pages > 7 && Math.abs(i - this.page) > 2 && i !== 1 && i !== pages) {
        if (i === 2 || i === pages - 1) html += `<span style="padding:0 4px">…</span>`;
        continue;
      }
      html += `<button class="page-btn ${i===this.page?'active':''}" data-p="${i}">${i}</button>`;
    }
    html += `<button class="page-btn" ${this.page===pages?'disabled':''}>›</button>`;
    this.container.innerHTML = html;
    this.container.querySelectorAll('[data-p]').forEach(b =>
      b.addEventListener('click', () => this.go(+b.dataset.p)));
    const btns = this.container.querySelectorAll('.page-btn:not([data-p])');
    btns[0].addEventListener('click', () => this.go(this.page - 1));
    btns[btns.length-1].addEventListener('click', () => this.go(this.page + 1));
  }

  go(p) {
    const pages = Math.ceil(this.total / this.pageSize) || 1;
    if (p < 1 || p > pages) return;
    this.page = p;
    this.render();
    this.onChange(p);
  }

  setTotal(total) { this.total = total; this.page = 1; this.render(); }
}

// ===== CHART HELPERS (sử dụng Chart.js CDN) =====
const ChartHelper = {
  pie(canvasId, labels, data, colors) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;
    return new Chart(ctx, {
      type: 'doughnut',
      data: { labels, datasets: [{ data, backgroundColor: colors, borderWidth: 2 }] },
      options: { responsive: true, plugins: { legend: { position: 'bottom' } } }
    });
  },
  bar(canvasId, labels, datasets, title) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;
    return new Chart(ctx, {
      type: 'bar',
      data: { labels, datasets },
      options: {
        responsive: true,
        plugins: { legend: { display: datasets.length > 1 }, title: { display: !!title, text: title } },
        scales: { y: { beginAtZero: true } }
      }
    });
  },
  line(canvasId, labels, datasets) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;
    return new Chart(ctx, {
      type: 'line',
      data: { labels, datasets: datasets.map(d => ({ ...d, tension: 0.3, fill: false })) },
      options: { responsive: true, scales: { y: { beginAtZero: true } } }
    });
  }
};

// ===== DOM HELPERS =====
const $ = (sel, ctx = document) => ctx.querySelector(sel);
const $$ = (sel, ctx = document) => [...ctx.querySelectorAll(sel)];

function renderSidebarUser() {
  const user = Auth.getUser();
  if (!user) return;
  const nameEl  = document.getElementById('sidebar-user-name');
  const roleEl  = document.getElementById('sidebar-user-role');
  const avatarEl = document.getElementById('sidebar-avatar');
  if (nameEl)  nameEl.textContent  = user.hoTen || user.tenDangNhap;
  if (roleEl)  roleEl.textContent  = user.vaiTro;
  if (avatarEl) avatarEl.textContent = (user.hoTen || user.tenDangNhap || 'U')[0].toUpperCase();
}

function highlightActiveNav() {
  const path = window.location.pathname.split('/').pop();
  document.querySelectorAll('.nav-item').forEach(el => {
    const href = el.getAttribute('href') || '';
    el.classList.toggle('active', href.includes(path));
  });
}

document.addEventListener('DOMContentLoaded', () => {
  renderSidebarUser();
  highlightActiveNav();
});
