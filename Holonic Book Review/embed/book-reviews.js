/**
 * Holonic Book Review — Embeddable widget
 * Drop on any page; use data-book-id and data-api-base on a container.
 * Fetches aggregated reviews via load-holons-for-parent and renders into the container.
 */
(function (global, doc) {
  'use strict';

  var DEFAULT_API_BASE = 'https://api.oasisweb4.com';

  function fetchReviews(apiBase, bookId) {
    var url = apiBase.replace(/\/$/, '') + '/api/data/load-holons-for-parent/' + encodeURIComponent(bookId) + '/All?loadChildren=false&recursive=false';
    return fetch(url, { headers: { 'Accept': 'application/json' } })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var list = (data && data.result != null) ? data.result : (data && data.Result != null ? data.Result : data);
        return Array.isArray(list) ? list : [];
      })
      .catch(function () { return []; });
  }

  function escapeHtml(s) {
    if (s == null) return '';
    var div = doc.createElement('div');
    div.textContent = s;
    return div.innerHTML;
  }

  function stars(n) {
    if (n == null || isNaN(n)) return '';
    var k = Math.min(5, Math.max(0, Math.round(Number(n))));
    return '★'.repeat(k) + '☆'.repeat(5 - k);
  }

  function render(container, reviews, options) {
    options = options || {};
    var showAggregate = options.showAggregate !== false;
    var avg = 0;
    var ratingCount = 0;
    var bySource = {};
    reviews.forEach(function (r) {
      var m = (r.metaData || {});
      var rt = m.rating;
      if (rt != null && !isNaN(rt)) {
        avg += Number(rt);
        ratingCount++;
      }
      var src = m.source || 'Other';
      bySource[src] = (bySource[src] || 0) + 1;
    });
    avg = ratingCount ? (avg / ratingCount).toFixed(1) : null;
    var html = '<div class="holonic-book-reviews" style="font-family:system-ui,-apple-system,sans-serif;font-size:14px;line-height:1.5;color:#333;">';
    if (showAggregate && (avg != null || reviews.length)) {
      html += '<div class="holonic-reviews-summary" style="margin-bottom:12px;color:#666;font-size:13px;">';
      if (avg != null) html += '<strong style="color:#c9a227;">' + avg + '</strong> average rating · ';
      html += reviews.length + ' review' + (reviews.length === 1 ? '' : 's');
      var srcParts = Object.keys(bySource).map(function (s) { return s + ': ' + bySource[s]; });
      if (srcParts.length) html += ' <span style="font-size:12px;">(' + srcParts.join(', ') + ')</span>';
      html += '</div>';
    }
    html += '<div class="holonic-reviews-list">';
    if (reviews.length === 0) {
      html += '<p style="color:#888;">No reviews yet.</p>';
    } else {
      reviews.forEach(function (r) {
        var m = (r.metaData || {});
        html += '<div class="holonic-review-item" style="border-left:3px solid #eee;padding-left:12px;margin-bottom:12px;">';
        html += '<div style="font-size:12px;color:#888;margin-bottom:4px;">' + escapeHtml(m.source || '') + (m.date ? ' · ' + new Date(m.date).toLocaleDateString() : '') + '</div>';
        html += '<div style="font-weight:600;margin-bottom:4px;">' + escapeHtml(r.name || '') + '</div>';
        if (m.rating != null) html += '<div style="color:#c9a227;margin-bottom:4px;">' + stars(m.rating) + '</div>';
        html += '<div style="color:#555;">' + escapeHtml(r.description || '') + '</div>';
        html += '</div>';
      });
    }
    html += '</div></div>';
    container.innerHTML = html;
  }

  function run() {
    var nodes = doc.querySelectorAll('[data-book-id][data-api-base]');
    nodes.forEach(function (node) {
      var bookId = node.getAttribute('data-book-id');
      var apiBase = node.getAttribute('data-api-base') || DEFAULT_API_BASE;
      if (!bookId) return;
      node.innerHTML = '<p style="color:#888;">Loading reviews…</p>';
      fetchReviews(apiBase, bookId).then(function (reviews) {
        render(node, reviews, { showAggregate: node.getAttribute('data-show-aggregate') !== 'false' });
      });
    });
  }

  if (doc.readyState === 'loading') {
    doc.addEventListener('DOMContentLoaded', run);
  } else {
    run();
  }

  global.HolonicBookReviews = {
    fetch: fetchReviews,
    render: render,
    run: run
  };
})(typeof window !== 'undefined' ? window : globalThis, typeof document !== 'undefined' ? document : null);
