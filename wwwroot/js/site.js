/* رَشفة — cart & UI */
(function () {
  'use strict';

  var CART_KEY = 'cafeCart';

  function getCart() {
    try {
      var c = JSON.parse(localStorage.getItem(CART_KEY) || '[]');
      return Array.isArray(c) ? c : [];
    } catch (e) { return []; }
  }

  function saveCart(c) {
    try { localStorage.setItem(CART_KEY, JSON.stringify(c)); } catch (e) { /* storage unavailable */ }
    refresh();
  }

  function money(n) { return (Math.round(n * 100) / 100).toFixed(2); }

  function escapeHtml(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  function cartTotal(c) { return c.reduce(function (s, x) { return s + x.price * x.qty; }, 0); }
  function cartCount(c) { return c.reduce(function (s, x) { return s + x.qty; }, 0); }

  function addToCart(id, name, price) {
    id = Number(id); price = Number(price);
    var c = getCart();
    var x = c.find(function (i) { return Number(i.id) === id; });
    if (x) x.qty++;
    else c.push({ id: id, name: name, price: price, qty: 1 });
    saveCart(c);
    bumpBadges();
    toast('أُضيف ' + name + ' إلى السلة');
  }

  function changeQty(id, d) {
    id = Number(id);
    var c = getCart();
    var x = c.find(function (i) { return Number(i.id) === id; });
    if (x) x.qty += d;
    c = c.filter(function (i) { return i.qty > 0; });
    saveCart(c);
  }

  function lineHtml(x) {
    return '<div class="cart-line">' +
      '<div><div class="name">' + escapeHtml(x.name) + '</div>' +
      '<div class="sub">' + money(x.price) + ' × ' + x.qty + ' = <span class="line-total">' + money(x.price * x.qty) + '</span></div></div>' +
      '<div class="qty-tools">' +
      '<button type="button" data-qty="-1" data-id="' + Number(x.id) + '" aria-label="إنقاص">−</button>' +
      '<b>' + x.qty + '</b>' +
      '<button type="button" data-qty="1" data-id="' + Number(x.id) + '" aria-label="زيادة">+</button>' +
      '</div></div>';
  }

  var emptyHtml =
    '<div class="cart-empty">' +
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M6 7h12l-1 13H7L6 7Z"/><path d="M9 7a3 3 0 0 1 6 0"/></svg>' +
    '<b>سلتك فارغة</b>اختر مشروباً من القائمة لتبدأ طلبك.</div>';

  function renderInto(listId, totalId, c) {
    var list = document.getElementById(listId);
    if (!list) return;
    list.innerHTML = c.length ? c.map(lineHtml).join('') : emptyHtml;
    var t = document.getElementById(totalId);
    if (t) t.textContent = money(cartTotal(c));
  }

  function refresh() {
    var c = getCart();
    var n = cartCount(c);
    document.querySelectorAll('.cart-count').forEach(function (b) {
      b.textContent = n;
      b.style.visibility = n ? 'visible' : 'hidden';
    });
    renderInto('cartItems', 'cartTotal', c);
    renderInto('reviewItems', 'reviewTotal', c);

    var foot = document.getElementById('cartFoot');
    if (foot) foot.hidden = !c.length;
    var cont = document.getElementById('continueCheckout');
    if (cont) cont.setAttribute('aria-disabled', c.length ? 'false' : 'true');
  }

  function bumpBadges() {
    document.querySelectorAll('.cart-count').forEach(function (b) {
      b.classList.remove('bump'); void b.offsetWidth; b.classList.add('bump');
    });
  }

  var lastFocus = null;
  function openCart() {
    var p = document.getElementById('cartPanel');
    if (!p) return;
    lastFocus = document.activeElement;
    refresh();
    p.classList.add('open');
    p.setAttribute('aria-hidden', 'false');
    var bd = document.getElementById('cartBackdrop');
    if (bd) bd.classList.add('show');
    document.body.style.overflow = 'hidden';
    var closeBtn = p.querySelector('[data-close-cart]');
    if (closeBtn) closeBtn.focus();
  }

  function closeCart() {
    var p = document.getElementById('cartPanel');
    if (!p || !p.classList.contains('open')) return;
    p.classList.remove('open');
    p.setAttribute('aria-hidden', 'true');
    var bd = document.getElementById('cartBackdrop');
    if (bd) bd.classList.remove('show');
    document.body.style.overflow = '';
    if (lastFocus && lastFocus.focus) lastFocus.focus();
  }

  var toastTimer;
  function toast(s) {
    var e = document.getElementById('toastHolder');
    if (!e) return;
    e.textContent = s;
    e.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { e.classList.remove('show'); }, 2000);
  }

  /* ---------- events ---------- */
  document.addEventListener('click', function (e) {
    var t = e.target;
    var add = t.closest('.btn-add');
    if (add && !add.disabled) {
      addToCart(add.dataset.id, add.dataset.name, add.dataset.price);
      add.classList.add('added');
      setTimeout(function () { add.classList.remove('added'); }, 700);
      return;
    }
    var q = t.closest('[data-qty]');
    if (q) { changeQty(q.dataset.id, Number(q.dataset.qty)); return; }
    if (t.closest('[data-open-cart]')) { openCart(); return; }
    if (t.closest('[data-close-cart]') || t.id === 'cartBackdrop') { closeCart(); }
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeCart();
  });

  // Menu search
  var search = document.getElementById('menuSearch');
  if (search) {
    search.addEventListener('input', function () {
      var term = search.value.trim().toLowerCase();
      var shown = 0;
      document.querySelectorAll('#menuGrid .product-card').forEach(function (card) {
        var hit = !term || (card.dataset.search || '').toLowerCase().indexOf(term) !== -1;
        card.hidden = !hit;
        if (hit) shown++;
      });
      var empty = document.getElementById('searchEmpty');
      if (empty) empty.hidden = shown !== 0 || !term;
    });
  }

  // Keep other tabs in sync
  window.addEventListener('storage', function (e) { if (e.key === CART_KEY) refresh(); });

  // Public API (used by views)
  window.getCart = getCart;
  window.saveCart = saveCart;
  window.addToCart = addToCart;
  window.changeQty = changeQty;
  window.openCart = openCart;
  window.closeCart = closeCart;
  window.escapeHtml = escapeHtml;
  window.toast = toast;
  window.clearCart = function () { saveCart([]); };

  refresh();
})();
