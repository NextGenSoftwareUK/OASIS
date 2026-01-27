/**
 * Seed data for Holonic Book Review demo.
 * Sample books (parent holons) and reviews (child holons) using
 * real independent publishers from the brief (§3).
 * Uses fixed UUIDs so the simulator can seed deterministically.
 */

(function (global) {
  'use strict';

  // Fixed UUIDs for reproducibility in simulator
  const BOOK_1_ID = '11111111-1111-4111-a111-111111111111';
  const BOOK_2_ID = '22222222-2222-4222-a222-222222222222';
  const BOOK_3_ID = '33333333-3333-4333-a333-333333333333';

  const books = [
    {
      id: BOOK_1_ID,
      name: 'The Green Glass Sea',
      description: 'A novel of family and wonder set in 1943 Los Alamos. Dewey and Suze navigate secrecy, friendship, and a world changing under their feet.',
      holonType: 'Holon',
      metaData: {
        author: 'Ellen Klages',
        publisher: 'Small Beer Press',
        isbn: '978-1-931520-22-5',
        genre: 'Speculative fiction',
        coverUrl: ''
      }
    },
    {
      id: BOOK_2_ID,
      name: 'The Wrong Kind of Woman',
      description: 'A sharp, witty debut about a widow who refuses to accept the narrow roles available to women at a 1970s liberal arts college.',
      holonType: 'Holon',
      metaData: {
        author: 'Sarah McCraw',
        publisher: 'Red Hen Press',
        isbn: '978-1-59709-025-4',
        genre: 'Literary fiction',
        coverUrl: ''
      }
    },
    {
      id: BOOK_3_ID,
      name: 'Music for the Dead and Resurrected',
      description: 'Poems that trace the legacies of displacement and loss across borders and generations, from Belarus to the American Midwest.',
      holonType: 'Holon',
      metaData: {
        author: 'Valzhyna Mort',
        publisher: 'Sarabande Books',
        isbn: '978-1-946448-78-4',
        genre: 'Poetry',
        coverUrl: ''
      }
    }
  ];

  const reviews = [
    {
      id: 'r1-b1-0001-0001-000000000001',
      parentHolonId: BOOK_1_ID,
      name: 'Review by Jane Reader',
      description: 'A moving and clever novel. The period detail feels just right, and the two girls\' voices are distinct and believable. I couldn\'t put it down.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'Jane Reader', source: 'Publisher website', date: '2026-01-10T14:30:00Z' }
    },
    {
      id: 'r1-b1-0001-0001-000000000002',
      parentHolonId: BOOK_1_ID,
      name: 'Review from City Lights Books',
      description: 'Our staff pick for January. Klages brings 1943 to life without a whiff of nostalgia—sharp, tender, and utterly absorbing.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'City Lights Books', source: 'Independent bookshop', date: '2026-01-12T11:00:00Z' }
    },
    {
      id: 'r1-b1-0001-0001-000000000003',
      parentHolonId: BOOK_1_ID,
      name: 'Review by conference attendee',
      description: 'Picked this up at AWP. Best book I\'ve read in months. The science and the kids\' perspective are perfectly balanced.',
      holonType: 'Holon',
      metaData: { rating: 4, reviewerName: 'Anonymous', source: 'Festival', date: '2026-01-14T09:15:00Z' }
    },
    {
      id: 'r2-b2-0002-0002-000000000001',
      parentHolonId: BOOK_2_ID,
      name: 'Review by Margaret S.',
      description: 'McCraw nails the tone of a seventies campus—the hypocrisy, the hope, and the way women had to fight for a seat at the table. Highly recommend.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'Margaret S.', source: 'Reader', date: '2026-01-11T16:20:00Z' }
    },
    {
      id: 'r2-b2-0002-0002-000000000002',
      parentHolonId: BOOK_2_ID,
      name: 'Review from Porter Square Books',
      description: 'We can\'t get enough of this one. It\'s on our front table and selling steadily. A perfect book-club pick.',
      holonType: 'Holon',
      metaData: { rating: 4, reviewerName: 'Porter Square Books', source: 'Independent bookshop', date: '2026-01-13T10:00:00Z' }
    },
    {
      id: 'r2-b2-0002-0002-000000000003',
      parentHolonId: BOOK_2_ID,
      name: 'Review by book blogger',
      description: 'Red Hen does it again. This is smart, funny, and deeply humane. Virginia’s journey from outsider to agitator feels earned.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'Lit in the City', source: 'Partner site', date: '2026-01-15T08:00:00Z' }
    },
    {
      id: 'r3-b3-0003-0003-000000000001',
      parentHolonId: BOOK_3_ID,
      name: 'Review by poet and reader',
      description: 'Mort’s lines are incisive and musical. The way she moves between Belarus and America, past and present, is masterful. Sarabande has brought us something essential.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'Elena V.', source: 'Reader', date: '2026-01-09T12:00:00Z' }
    },
    {
      id: 'r3-b3-0003-0003-000000000002',
      parentHolonId: BOOK_3_ID,
      name: 'Review from Seminary Co-op',
      description: 'Our poetry section wouldn’t be complete without this. Customers keep asking for it by name.',
      holonType: 'Holon',
      metaData: { rating: 5, reviewerName: 'Seminary Co-op Bookstores', source: 'Independent bookshop', date: '2026-01-16T14:00:00Z' }
    }
  ];

  const STORAGE_KEY_BOOKS = 'holonic_book_review_books';
  const STORAGE_KEY_REVIEWS = 'holonic_book_review_reviews';

  function getBooks() {
    return books.map(function (b) {
      return { id: b.id, name: b.name, description: b.description, holonType: b.holonType, metaData: b.metaData || {} };
    });
  }

  function getReviews() {
    return reviews.map(function (r) {
      return {
        id: r.id,
        parentHolonId: r.parentHolonId,
        name: r.name,
        description: r.description,
        holonType: r.holonType,
        metaData: r.metaData || {}
      };
    });
  }

  global.HolonicBookReviewSeed = {
    books: getBooks(),
    reviews: getReviews(),
    STORAGE_KEY_BOOKS: STORAGE_KEY_BOOKS,
    STORAGE_KEY_REVIEWS: STORAGE_KEY_REVIEWS,
    getBooks: getBooks,
    getReviews: getReviews
  };
})(typeof window !== 'undefined' ? window : globalThis);
