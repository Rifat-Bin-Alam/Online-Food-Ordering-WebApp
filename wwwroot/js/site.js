/**
 * ONLINE FOOD ORDERING WEB APP — INTERACTIVE JAVASCRIPT ENGINE
 * Handles animated cart drawer, live count badges, asynchronous add-to-cart,
 * quantity steppers, live toast notifications, and notification dropdown.
 */

function initApp() {
  try { initCartDrawer(); } catch (e) { console.error('Cart drawer init error:', e); }
  try { initAddToCartButtons(); } catch (e) { console.error('Add to cart init error:', e); }
  try { initNotificationSystem(); } catch (e) { console.error('Notification init error:', e); }
  try { initCategoryFilter(); } catch (e) { console.error('Category filter init error:', e); }
  try { initScrollToTop(); } catch (e) { console.error('Scroll to top init error:', e); }
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}

/* ==========================================================================
   TOAST NOTIFICATIONS
   ========================================================================== */
function showToast(message, type = 'success') {
  let container = document.getElementById('toastContainer');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container-custom';
    document.body.appendChild(container);
  }

  const toast = document.createElement('div');
  toast.className = `toast-custom toast-${type}`;

  const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';

  toast.innerHTML = `
    <span style="font-weight: 800; font-size: 1.1rem; color: ${type === 'success' ? 'var(--accent-basil)' : 'var(--primary-color)'};">${icon}</span>
    <span style="flex: 1; font-size: 0.9rem; font-weight: 500; color: var(--text-espresso);">${escapeHtml(message)}</span>
    <button type="button" style="background:transparent; border:none; color:var(--text-muted); cursor:pointer; font-size:1rem; padding:0 4px;" aria-label="Close">&times;</button>
  `;

  const closeBtn = toast.querySelector('button');
  closeBtn.addEventListener('click', () => removeToast(toast));

  container.appendChild(toast);

  // Auto-dismiss after 4 seconds
  setTimeout(() => {
    removeToast(toast);
  }, 4000);
}

function removeToast(toast) {
  toast.style.opacity = '0';
  toast.style.transform = 'translateX(100%)';
  setTimeout(() => {
    if (toast.parentNode) {
      toast.parentNode.removeChild(toast);
    }
  }, 300);
}

function escapeHtml(text) {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

/* ==========================================================================
   CART BADGE & ANIMATION
   ========================================================================== */
function animateCartBadge() {
  const badges = document.querySelectorAll('.cart-badge');
  badges.forEach(b => {
    b.classList.remove('badge-pop');
    void b.offsetWidth; // Trigger reflow
    b.classList.add('badge-pop');
  });
}

function updateCartBadges(count) {
  const badges = document.querySelectorAll('.cart-badge');
  badges.forEach(b => {
    b.textContent = count;
    b.style.display = count > 0 ? 'inline-block' : 'none';
  });
  if (count > 0) {
    animateCartBadge();
  }
}

/* ==========================================================================
   CART DRAWER (Slide-in)
   ========================================================================== */
function initCartDrawer() {
  const trigger = document.getElementById('cartDrawerTrigger');
  const drawer = document.getElementById('cartDrawer');
  const backdrop = document.getElementById('cartDrawerBackdrop');
  const closeBtn = document.getElementById('cartDrawerClose');

  if (!drawer || !backdrop) return;

  function openDrawer() {
    // Auto-collapse mobile navbar menu if open
    const navCollapse = document.getElementById('navbarContent');
    if (navCollapse && navCollapse.classList.contains('show')) {
      if (typeof bootstrap !== 'undefined' && bootstrap.Collapse) {
        const bsCollapse = bootstrap.Collapse.getInstance(navCollapse) || new bootstrap.Collapse(navCollapse, { toggle: false });
        bsCollapse.hide();
      } else {
        navCollapse.classList.remove('show');
      }
    }

    drawer.classList.add('active');
    backdrop.classList.add('active');
    drawer.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
    refreshCartDrawer();
  }

  function closeDrawer() {
    drawer.classList.remove('active');
    backdrop.classList.remove('active');
    drawer.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
  }

  // Ensure drawer is explicitly closed and body scroll unlocked on page load
  closeDrawer();

  if (trigger) {
    trigger.addEventListener('click', function (e) {
      e.preventDefault();
      openDrawer();
    });
  }

  if (closeBtn) {
    closeBtn.addEventListener('click', function (e) {
      e.preventDefault();
      closeDrawer();
    });
  }

  backdrop.addEventListener('click', function (e) {
    e.preventDefault();
    closeDrawer();
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && drawer.classList.contains('active')) {
      closeDrawer();
    }
  });

  // Attach global window references
  window.openCartDrawer = openDrawer;
  window.closeCartDrawer = closeDrawer;
}

function refreshCartDrawer() {
  fetch('/Main/GetCartDrawerJson')
    .then(res => res.json())
    .then(data => {
      renderDrawerContents(data.items, data.cartTotal, data.cartCount);
      updateCartBadges(data.cartCount);
    })
    .catch(err => {
      console.error('Error fetching cart:', err);
    });
}

function renderDrawerContents(items, cartTotal, cartCount) {
  const container = document.getElementById('cartDrawerItems');
  const totalEl = document.getElementById('cartDrawerTotal');
  const footerEl = document.getElementById('cartDrawerFooter');

  if (!container) return;

  if (!items || items.length === 0) {
    container.innerHTML = `
      <div class="empty-cart-state">
        <div class="empty-cart-icon">🛒</div>
        <h4 style="font-size: 1.15rem; color: var(--text-espresso); font-weight: 700;">Your cart is hungry!</h4>
        <p class="empty-cart-text">Add your favorite artisanal dishes and treats to get started.</p>
        <button type="button" class="btn-primary-custom" onclick="window.closeCartDrawer();">Explore Menu</button>
      </div>
    `;
    if (totalEl) totalEl.textContent = '$0.00';
    if (footerEl) footerEl.style.display = 'none';
    return;
  }

  if (footerEl) footerEl.style.display = 'block';
  if (totalEl) totalEl.textContent = `$${Number(cartTotal).toFixed(2)}`;

  let html = '';
  items.forEach(item => {
    const itemTotal = (item.price * item.quantity).toFixed(2);
    const imgUrl = item.imagePath || '/images/burger.jpg';

    html += `
      <div class="drawer-item" id="drawer-item-${item.foodItemId}">
        <img src="${escapeHtml(imgUrl)}" alt="${escapeHtml(item.name)}" class="drawer-item-img" />
        <div class="drawer-item-info">
          <h5 class="drawer-item-title">${escapeHtml(item.name)}</h5>
          <div class="drawer-item-price">$${Number(item.price).toFixed(2)} each</div>
          <div style="display: flex; align-items: center; justify-content: space-between; margin-top: 0.5rem;">
            <div class="qty-stepper">
              <button type="button" class="qty-btn" onclick="updateItemQuantity(${item.foodItemId}, -1)" aria-label="Decrease quantity">−</button>
              <span class="qty-val" id="drawer-qty-${item.foodItemId}">${item.quantity}</span>
              <button type="button" class="qty-btn" onclick="updateItemQuantity(${item.foodItemId}, 1)" aria-label="Increase quantity">+</button>
            </div>
            <div style="font-weight: 800; color: var(--text-espresso); font-size: 0.95rem;">
              $<span id="drawer-item-total-${item.foodItemId}">${itemTotal}</span>
            </div>
            <button type="button" style="background:transparent; border:none; color:var(--text-muted); cursor:pointer; font-size:1.1rem; padding: 2px 6px;" 
                    onclick="removeCartItem(${item.foodItemId})" title="Remove item" aria-label="Remove item">🗑</button>
          </div>
        </div>
      </div>
    `;
  });

  container.innerHTML = html;
}

/* ==========================================================================
   ADD TO CART (AJAX)
   ========================================================================== */
function initAddToCartButtons() {
  document.querySelectorAll('.btn-add-cart').forEach(btn => {
    btn.addEventListener('click', function (e) {
      e.preventDefault();
      const foodId = this.getAttribute('data-food-id');
      if (!foodId) return;

      const originalHtml = this.innerHTML;
      this.disabled = true;
      this.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...`;

      fetch(`/Main/AddToCartAjax?id=${foodId}`, {
        method: 'POST',
        headers: {
          'X-Requested-With': 'XMLHttpRequest'
        }
      })
        .then(res => res.json())
        .then(data => {
          if (data.success) {
            this.classList.add('added');
            this.innerHTML = `✓ Added!`;
            updateCartBadges(data.cartCount);
            showToast(data.message || 'Added to cart!', 'success');

            // Render drawer and open it
            renderDrawerContents(data.items, data.cartTotal, data.cartCount);
            if (window.openCartDrawer) {
              window.openCartDrawer();
            }

            setTimeout(() => {
              this.classList.remove('added');
              this.innerHTML = originalHtml;
              this.disabled = false;
            }, 1200);
          } else {
            this.innerHTML = originalHtml;
            this.disabled = false;
            showToast(data.message || 'Could not add to cart.', 'error');
          }
        })
        .catch(err => {
          console.error(err);
          this.innerHTML = originalHtml;
          this.disabled = false;
          showToast('An error occurred while adding to cart.', 'error');
        });
    });
  });
}

/* ==========================================================================
   QUANTITY & REMOVAL ACTIONS
   ========================================================================== */
window.updateItemQuantity = function (foodItemId, delta) {
  fetch(`/Main/UpdateQuantityAjax?id=${foodItemId}&delta=${delta}`, {
    method: 'POST',
    headers: {
      'X-Requested-With': 'XMLHttpRequest'
    }
  })
    .then(res => res.json())
    .then(data => {
      if (data.success) {
        updateCartBadges(data.cartCount);
        renderDrawerContents(data.items, data.cartTotal, data.cartCount);

        // Also update full page cart if user is on /Main/Cart
        syncFullCartPage(data);
      } else {
        showToast(data.message || 'Could not update quantity.', 'error');
      }
    })
    .catch(err => {
      console.error(err);
      showToast('Error updating item quantity.', 'error');
    });
};

window.removeCartItem = function (foodItemId) {
  fetch(`/Main/RemoveFromCartAjax?id=${foodItemId}`, {
    method: 'POST',
    headers: {
      'X-Requested-With': 'XMLHttpRequest'
    }
  })
    .then(res => res.json())
    .then(data => {
      if (data.success) {
        showToast('Item removed from cart.', 'info');
        updateCartBadges(data.cartCount);
        renderDrawerContents(data.items, data.cartTotal, data.cartCount);
        syncFullCartPage(data);
      }
    })
    .catch(err => {
      console.error(err);
      showToast('Error removing item from cart.', 'error');
    });
};

function syncFullCartPage(data) {
  // If we are on /Main/Cart, dynamically update table rows or reload if needed
  const pageTable = document.getElementById('fullCartTable');
  if (!pageTable) return;

  if (data.cartCount === 0) {
    location.reload();
    return;
  }

  // Update line row
  if (data.foodItemId) {
    const row = document.getElementById(`cart-row-${data.foodItemId}`);
    if (row) {
      if (data.itemRemoved) {
        row.remove();
      } else {
        const qtyEl = row.querySelector('.page-cart-qty');
        const totalEl = row.querySelector('.page-cart-item-total');
        if (qtyEl) qtyEl.textContent = data.newQuantity;
        if (totalEl) totalEl.textContent = `$${Number(data.itemTotal).toFixed(2)}`;
      }
    }
  }

  const grandTotalEl = document.getElementById('fullCartGrandTotal');
  if (grandTotalEl) {
    grandTotalEl.textContent = `$${Number(data.cartTotal).toFixed(2)}`;
  }
}

/* ==========================================================================
   NOTIFICATION SYSTEM (Dropdown, Read/Unread, Polling)
   ========================================================================== */
function initNotificationSystem() {
  const markAllBtn = document.getElementById('btnMarkAllNotifications');
  if (markAllBtn) {
    markAllBtn.addEventListener('click', function (e) {
      e.preventDefault();
      fetch('/Main/MarkAllNotificationsReadAjax', {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      })
        .then(res => res.json())
        .then(data => {
          if (data.success) {
            updateNotificationBadge(0);
            document.querySelectorAll('.notification-item').forEach(item => {
              item.classList.remove('unread');
              item.classList.add('read');
            });
            showToast('All notifications marked as read.', 'info');
          }
        })
        .catch(err => console.error(err));
    });
  }

  // Lightweight periodic polling every 45 seconds for active users
  const bell = document.getElementById('notificationBell');
  if (bell) {
    setInterval(pollNotifications, 45000);
  }
}

window.markSingleNotificationRead = function (id, event) {
  fetch(`/Main/MarkNotificationReadAjax?id=${id}`, {
    method: 'POST',
    headers: { 'X-Requested-With': 'XMLHttpRequest' }
  })
    .then(res => res.json())
    .then(data => {
      if (data.success) {
        updateNotificationBadge(data.unreadCount);
        const item = document.getElementById(`notif-item-${id}`);
        if (item) {
          item.classList.remove('unread');
          item.classList.add('read');
        }
      }
    })
    .catch(err => console.error(err));
};

function updateNotificationBadge(count) {
  const badge = document.getElementById('notificationBadge');
  if (!badge) return;

  if (count > 0) {
    badge.textContent = count;
    badge.style.display = 'inline-block';
    badge.classList.remove('badge-pop');
    void badge.offsetWidth;
    badge.classList.add('badge-pop');

    // Subtle bell wiggle
    const bell = document.getElementById('notificationBell');
    if (bell) {
      bell.classList.add('bell-wiggle');
      setTimeout(() => bell.classList.remove('bell-wiggle'), 700);
    }
  } else {
    badge.style.display = 'none';
  }
}

function pollNotifications() {
  fetch('/Main/GetNotificationsAjax')
    .then(res => res.json())
    .then(data => {
      if (data && typeof data.unreadCount === 'number') {
        const badge = document.getElementById('notificationBadge');
        const currentCount = badge ? parseInt(badge.textContent || '0', 10) : 0;

        if (data.unreadCount > currentCount) {
          updateNotificationBadge(data.unreadCount);
        }
      }
    })
    .catch(err => console.error('Notification poll error:', err));
}

/* ==========================================================================
   CATEGORY FILTER (Client-side fast tab filtering)
   ========================================================================== */
function initCategoryFilter() {
  const pills = document.querySelectorAll('.cat-pill');
  const cards = document.querySelectorAll('.food-card-col');

  if (pills.length === 0 || cards.length === 0) return;

  pills.forEach(pill => {
    pill.addEventListener('click', function (e) {
      // If it has href to filter on server, allow it or filter client-side
      const category = this.getAttribute('data-category');
      if (!category) return;

      pills.forEach(p => p.classList.remove('active'));
      this.classList.add('active');

      if (category.toLowerCase() === 'all') {
        cards.forEach(card => card.style.display = '');
      } else {
        const target = category.toLowerCase();
        cards.forEach(card => {
          const cardCat = (card.getAttribute('data-category') || card.textContent).toLowerCase();
          if (cardCat.includes(target)) {
            card.style.display = '';
          } else {
            card.style.display = 'none';
          }
        });
      }
    });
  });
}

/* ==========================================================================
   SCROLL TO TOP BUTTON (Sticky Floating Control)
   ========================================================================== */
function initScrollToTop() {
  const btn = document.getElementById('btnScrollTop');
  if (!btn) return;

  const scrollThreshold = 300;
  let isTicking = false;

  function getScrollPosition() {
    return window.pageYOffset ||
      (document.documentElement && document.documentElement.scrollTop) ||
      (document.body && document.body.scrollTop) ||
      0;
  }

  function toggleScrollBtn() {
    const currentScroll = getScrollPosition();
    if (currentScroll > scrollThreshold) {
      btn.classList.add('visible');
    } else {
      btn.classList.remove('visible');
    }
  }

  function handleScroll() {
    if (!isTicking) {
      window.requestAnimationFrame(function () {
        toggleScrollBtn();
        isTicking = false;
      });
      isTicking = true;
    }
  }

  // Listen on both window and document to ensure all browsers capture scroll events
  window.addEventListener('scroll', handleScroll, { passive: true });
  document.addEventListener('scroll', handleScroll, { passive: true });

  // Initial check in case page is refreshed while scrolled
  toggleScrollBtn();

  // Click handler: smoothly scrolls back to the top of the viewport
  btn.addEventListener('click', function (e) {
    e.preventDefault();
    const prefersReducedMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const scrollMode = prefersReducedMotion ? 'auto' : 'smooth';

    try {
      window.scrollTo({
        top: 0,
        left: 0,
        behavior: scrollMode
      });
    } catch (err) {
      window.scrollTo(0, 0);
    }

    if (document.documentElement && document.documentElement.scrollTop > 0) {
      try {
        document.documentElement.scrollTo({
          top: 0,
          left: 0,
          behavior: scrollMode
        });
      } catch (err) {
        document.documentElement.scrollTop = 0;
      }
    }

    if (document.body && document.body.scrollTop > 0) {
      try {
        document.body.scrollTo({
          top: 0,
          left: 0,
          behavior: scrollMode
        });
      } catch (err) {
        document.body.scrollTop = 0;
      }
    }

    // Release focus after activation so focus ring doesn't persist
    btn.blur();
  });
}


