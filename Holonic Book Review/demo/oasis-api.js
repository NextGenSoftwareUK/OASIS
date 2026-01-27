/**
 * OASIS API Client for Holonic Book Review demo.
 * Same endpoints as holonic-demo: save-holon, load-holon, load-holons-for-parent.
 * Optional baseURL in constructor for override.
 */

(function (global) {
  'use strict';

  function getDefaultBaseURL() {
    var hostname = typeof window !== 'undefined' ? window.location.hostname : '';
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      return 'http://localhost:5003';
    }
    if (typeof window !== 'undefined' && window.location.protocol === 'https:') {
      return 'https://api.oasisweb4.com';
    }
    return 'http://api.oasisweb4.com';
  }

  function BookReviewOASISClient(options) {
    options = options || {};
    this.baseURL = (options.baseURL || getDefaultBaseURL()).replace(/\/$/, '');
  }

  BookReviewOASISClient.prototype.getAuthHeaders = function () {
    var headers = { 'Content-Type': 'application/json' };
    try {
      var raw = typeof localStorage !== 'undefined' ? localStorage.getItem('oasis_auth') : null;
      if (raw) {
        var auth = JSON.parse(raw);
        if (auth && auth.token) {
          headers['Authorization'] = 'Bearer ' + auth.token;
        }
      }
    } catch (e) {
      console.warn('BookReviewOASISClient: getAuthHeaders', e);
    }
    return headers;
  };

  BookReviewOASISClient.prototype.request = function (endpoint, options) {
    var self = this;
    options = options || {};
    var url = this.baseURL + endpoint;
    var config = {
      method: options.method || 'GET',
      headers: Object.assign({}, this.getAuthHeaders(), options.headers || {})
    };
    if (options.body) config.body = options.body;

    return fetch(url, config)
      .then(function (response) {
        if (!response.ok) {
          return response.text().then(function (text) {
            throw new Error('HTTP ' + response.status + ': ' + text);
          });
        }
        var contentType = response.headers.get('content-type');
        if (contentType && contentType.indexOf('application/json') !== -1) {
          return response.json();
        }
        return response.text().then(function (text) {
          return { isError: false, result: text };
        });
      })
      .then(function (data) {
        return data;
      })
      .catch(function (err) {
        console.error('BookReviewOASISClient request error:', err);
        return { isError: true, message: err.message || 'Network error', error: err };
      });
  };

  BookReviewOASISClient.prototype.saveHolon = function (holonData) {
    return this.request('/api/data/save-holon', {
      method: 'POST',
      body: JSON.stringify(holonData)
    });
  };

  BookReviewOASISClient.prototype.loadHolon = function (holonId, loadChildren, recursive) {
    loadChildren = loadChildren !== false;
    recursive = recursive !== false;
    return this.request('/api/data/load-holon/' + encodeURIComponent(holonId) + '?loadChildren=' + loadChildren + '&recursive=' + recursive);
  };

  BookReviewOASISClient.prototype.loadHolonsForParent = function (parentId, holonType, loadChildren, recursive) {
    holonType = holonType || 'All';
    loadChildren = loadChildren !== false;
    recursive = recursive !== false;
    return this.request('/api/data/load-holons-for-parent/' + encodeURIComponent(parentId) + '/' + encodeURIComponent(holonType) + '?loadChildren=' + loadChildren + '&recursive=' + recursive);
  };

  global.BookReviewOASISClient = BookReviewOASISClient;
})(typeof window !== 'undefined' ? window : globalThis);
