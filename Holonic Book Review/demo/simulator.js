/**
 * Holonic Book Review — Simulator
 * In-memory + localStorage stand-in when OASIS API is unavailable.
 * Same method shapes as oasis-api.js: saveHolon, loadHolon, loadHolonsForParent.
 * Swap to real API by replacing the client; data model stays the same.
 */

(function (global) {
  'use strict';

  var STORAGE_KEY_BOOKS = 'holonic_book_review_books';
  var STORAGE_KEY_REVIEWS = 'holonic_book_review_reviews';

  function uuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      var r = (Math.random() * 16) | 0;
      var v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  function loadFromStorage(key, fallback) {
    try {
      var raw = typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
      if (raw) {
        var parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed;
      }
    } catch (e) {
      console.warn('Simulator: could not load from localStorage', e);
    }
    return fallback ? fallback() : [];
  }

  function saveToStorage(key, data) {
    try {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(key, JSON.stringify(data));
      }
    } catch (e) {
      console.warn('Simulator: could not save to localStorage', e);
    }
  }

  function getBooks() {
    var seed = typeof global.HolonicBookReviewSeed !== 'undefined' ? global.HolonicBookReviewSeed : null;
    var fallback = seed && seed.getBooks ? seed.getBooks : function () { return []; };
    return loadFromStorage(STORAGE_KEY_BOOKS, fallback);
  }

  function getReviews() {
    var seed = typeof global.HolonicBookReviewSeed !== 'undefined' ? global.HolonicBookReviewSeed : null;
    var fallback = seed && seed.getReviews ? seed.getReviews : function () { return []; };
    return loadFromStorage(STORAGE_KEY_REVIEWS, fallback);
  }

  function ensureSeeded() {
    var books = loadFromStorage(STORAGE_KEY_BOOKS, null);
    var reviews = loadFromStorage(STORAGE_KEY_REVIEWS, null);
    var seed = typeof global.HolonicBookReviewSeed !== 'undefined' ? global.HolonicBookReviewSeed : null;
    if (!Array.isArray(books) || books.length === 0) {
      books = seed && seed.getBooks ? seed.getBooks() : [];
      saveToStorage(STORAGE_KEY_BOOKS, books);
    }
    if (!Array.isArray(reviews) || reviews.length === 0) {
      reviews = seed && seed.getReviews ? seed.getReviews() : [];
      saveToStorage(STORAGE_KEY_REVIEWS, reviews);
    }
  }

  function BookReviewSimulator() {
    ensureSeeded();
  }

  BookReviewSimulator.prototype.saveHolon = function (holonData) {
    ensureSeeded();
    var hasParent = holonData && (holonData.parentHolonId || holonData.parentOmniverseId);
    var books = getBooks();
    var reviews = getReviews();

    if (hasParent) {
      var parentId = holonData.parentHolonId || holonData.parentOmniverseId || '';
      var reviewId = holonData.id || uuid();
      var review = {
        id: reviewId,
        parentHolonId: parentId,
        name: holonData.name || 'Review',
        description: holonData.description || '',
        holonType: holonData.holonType || 'Holon',
        metaData: holonData.metaData || {}
      };
      reviews.push(review);
      saveToStorage(STORAGE_KEY_REVIEWS, reviews);
      return Promise.resolve({ isError: false, result: review });
    } else {
      var bookId = holonData.id || uuid();
      var book = {
        id: bookId,
        name: holonData.name || 'Untitled',
        description: holonData.description || '',
        holonType: holonData.holonType || 'Holon',
        metaData: holonData.metaData || {}
      };
      books.push(book);
      saveToStorage(STORAGE_KEY_BOOKS, books);
      return Promise.resolve({ isError: false, result: book });
    }
  };

  BookReviewSimulator.prototype.loadHolon = function (holonId) {
    ensureSeeded();
    var books = getBooks();
    var reviews = getReviews();
    var id = (holonId || '').toString();
    var found = books.filter(function (b) { return (b.id || '').toString() === id; })[0] ||
                reviews.filter(function (r) { return (r.id || '').toString() === id; })[0];
    if (found) {
      return Promise.resolve({ isError: false, result: found });
    }
    return Promise.resolve({ isError: true, message: 'Holon not found' });
  };

  BookReviewSimulator.prototype.loadHolonsForParent = function (parentId) {
    ensureSeeded();
    var reviews = getReviews();
    var pid = (parentId || '').toString();
    var children = reviews.filter(function (r) {
      return (r.parentHolonId || r.parentOmniverseId || '').toString() === pid;
    });
    return Promise.resolve({ isError: false, result: children });
  };

  BookReviewSimulator.prototype.resetToSeed = function () {
    var seed = typeof global.HolonicBookReviewSeed !== 'undefined' ? global.HolonicBookReviewSeed : null;
    if (seed && seed.getBooks && seed.getReviews) {
      saveToStorage(STORAGE_KEY_BOOKS, seed.getBooks());
      saveToStorage(STORAGE_KEY_REVIEWS, seed.getReviews());
    }
  };

  global.BookReviewSimulator = BookReviewSimulator;
})(typeof window !== 'undefined' ? window : globalThis);
