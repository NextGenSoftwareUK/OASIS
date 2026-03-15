/**
 * OASIS Dev Portal Module
 * Ported from STAR Web UI DevPortalPage.tsx for Oportal.
 * Renders stats, resource grid, filters, and detail/download modal.
 * Requires: oasisAPI (api/oasisApi.js). Optional: call loadDevPortal() when tab "dev-portal" is shown.
 */

(function () {
  'use strict';

  const DEMO_STATS = {
    totalResources: 47,
    totalDownloads: 125000,
    activeDevelopers: 8923,
    averageRating: 4.8,
    popularCategories: [
      { category: 'Getting Started', count: 12 },
      { category: 'Integration', count: 18 },
      { category: 'Advanced', count: 8 },
      { category: 'Examples', count: 6 },
      { category: 'Tools', count: 3 },
    ],
    recentUpdates: [],
    featuredResources: [],
  };

  const DEMO_RESOURCES = [
    {
      id: '1',
      title: 'STAR CLI - Command Line Interface',
      description: 'Complete command-line tool for interacting with the OASIS ecosystem. Deploy, manage, and monitor your applications.',
      type: 'cli',
      category: 'getting-started',
      downloadUrl: '/downloads/star-cli-v2.1.0.zip',
      version: '2.1.0',
      size: '45.2 MB',
      downloads: 45678,
      rating: 4.9,
      tags: ['cli', 'deployment', 'management', 'monitoring'],
      author: 'OASIS Team',
      lastUpdated: '2024-01-15T10:30:00Z',
      featured: true,
      difficulty: 'beginner',
      estimatedTime: '15 minutes',
      prerequisites: ['Node.js 18+', 'Git'],
      languages: ['JavaScript', 'TypeScript'],
      frameworks: ['Node.js'],
      platforms: ['Windows', 'macOS', 'Linux'],
      content: 'The STAR CLI is your gateway to the OASIS ecosystem. With simple commands, you can deploy applications, manage avatars, and interact with the blockchain.',
      codeExamples: ['star login', 'star deploy my-app', 'star avatar create --name "My Avatar"', 'star blockchain connect --network ethereum'],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=star-cli-demo',
      githubUrl: 'https://github.com/oasis/star-cli',
      documentationUrl: 'https://docs.oasis.network/star-cli',
      supportUrl: 'https://support.oasis.network/star-cli',
    },
    {
      id: '2',
      title: 'OASIS Avatar SSO SDK Pack',
      description: 'Complete SDK package for integrating OASIS Avatar SSO into your applications. Includes widgets, API clients, and documentation.',
      type: 'sdk',
      category: 'integration',
      downloadUrl: '/downloads/oasis-avatar-sso-sdk-v1.5.2.zip',
      version: '1.5.2',
      size: '128.7 MB',
      downloads: 23456,
      rating: 4.8,
      tags: ['sso', 'authentication', 'avatar', 'sdk', 'widget'],
      author: 'OASIS Team',
      lastUpdated: '2024-01-14T14:20:00Z',
      featured: true,
      difficulty: 'intermediate',
      estimatedTime: '2 hours',
      prerequisites: ['JavaScript', 'React/Vue/Angular', 'Node.js'],
      languages: ['JavaScript', 'TypeScript', 'Python', 'Java', 'C#'],
      frameworks: ['React', 'Vue', 'Angular', 'Express', 'Django', 'Spring'],
      platforms: ['Web', 'Mobile', 'Desktop'],
      content: 'The OASIS Avatar SSO SDK Pack provides everything you need to integrate seamless authentication into your applications.',
      codeExamples: [
        'import { OasisAvatarSSO } from "@oasis/avatar-sso";',
        'const sso = new OasisAvatarSSO({ clientId: "your-client-id" });',
        'await sso.login();',
        'const user = await sso.getCurrentUser();',
      ],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=avatar-sso-demo',
      githubUrl: 'https://github.com/oasis/avatar-sso-sdk',
      documentationUrl: 'https://docs.oasis.network/avatar-sso',
      supportUrl: 'https://support.oasis.network/avatar-sso',
    },
    {
      id: '3',
      title: 'Postman Collection - WEB4 OASIS API',
      description: 'Complete Postman collection with all WEB4 OASIS API endpoints, examples, and authentication setup.',
      type: 'postman',
      category: 'tools',
      downloadUrl: '/downloads/oasis-api-postman-collection-v3.2.1.json',
      version: '3.2.1',
      size: '2.1 MB',
      downloads: 18923,
      rating: 4.7,
      tags: ['api', 'postman', 'testing', 'documentation'],
      author: 'OASIS Team',
      lastUpdated: '2024-01-13T16:45:00Z',
      featured: true,
      difficulty: 'beginner',
      estimatedTime: '30 minutes',
      prerequisites: ['Postman', 'API knowledge'],
      languages: ['JSON'],
      frameworks: ['Postman'],
      platforms: ['Cross-platform'],
      content: 'Ready-to-use Postman collection with all WEB4 OASIS API endpoints, including authentication, examples, and test scenarios.',
      codeExamples: ['GET /api/avatar/profile', 'POST /api/avatar/create', 'GET /api/nft/list', 'POST /api/oapp/deploy'],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=postman-oasis-api',
      githubUrl: 'https://github.com/oasis/api-postman-collection',
      documentationUrl: 'https://docs.oasis.network/api/postman',
      supportUrl: 'https://support.oasis.network/api',
    },
    {
      id: '4',
      title: 'Getting Started with OASIS Development',
      description: 'Comprehensive guide to start developing on the OASIS platform. From setup to deployment.',
      type: 'documentation',
      category: 'getting-started',
      downloadUrl: '/downloads/getting-started-guide-v1.0.pdf',
      version: '1.0',
      size: '15.8 MB',
      downloads: 34567,
      rating: 4.9,
      tags: ['guide', 'tutorial', 'getting-started', 'development'],
      author: 'OASIS Team',
      lastUpdated: '2024-01-12T09:15:00Z',
      featured: false,
      difficulty: 'beginner',
      estimatedTime: '1 hour',
      prerequisites: ['Basic programming knowledge'],
      languages: ['Multiple'],
      frameworks: ['Multiple'],
      platforms: ['Cross-platform'],
      content: 'Step-by-step guide to get started with OASIS development.',
      codeExamples: ['npm install -g @oasis/star-cli', 'star init my-project', 'star deploy', 'star monitor'],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=getting-started-oasis',
      githubUrl: 'https://github.com/oasis/getting-started-guide',
      documentationUrl: 'https://docs.oasis.network/getting-started',
      supportUrl: 'https://support.oasis.network/getting-started',
    },
    {
      id: '5',
      title: 'Building a Decentralized Social Media App',
      description: 'Complete tutorial on building a decentralized social media application using OASIS technologies.',
      type: 'tutorial',
      category: 'advanced',
      downloadUrl: '/downloads/social-media-tutorial-v2.3.0.zip',
      version: '2.3.0',
      size: '89.4 MB',
      downloads: 12345,
      rating: 4.8,
      tags: ['tutorial', 'social-media', 'decentralized', 'advanced'],
      author: 'OASIS Community',
      lastUpdated: '2024-01-11T11:30:00Z',
      featured: false,
      difficulty: 'advanced',
      estimatedTime: '8 hours',
      prerequisites: ['React', 'Node.js', 'Blockchain basics', 'IPFS'],
      languages: ['JavaScript', 'TypeScript'],
      frameworks: ['React', 'Express', 'IPFS'],
      platforms: ['Web'],
      content: 'Learn to build a fully decentralized social media application using OASIS, IPFS, and blockchain technologies.',
      codeExamples: ['const post = await ipfs.add(JSON.stringify(postData));', 'const hash = await blockchain.storePost(post.hash);', 'const feed = await getDecentralizedFeed();'],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=social-media-tutorial',
      githubUrl: 'https://github.com/oasis/social-media-tutorial',
      documentationUrl: 'https://docs.oasis.network/tutorials/social-media',
      supportUrl: 'https://support.oasis.network/tutorials',
    },
    {
      id: '6',
      title: 'Case Study: Enterprise OASIS Integration',
      description: 'Real-world case study of a Fortune 500 company integrating OASIS for their blockchain infrastructure.',
      type: 'case-study',
      category: 'examples',
      downloadUrl: '/downloads/enterprise-case-study-v1.2.pdf',
      version: '1.2',
      size: '8.7 MB',
      downloads: 8765,
      rating: 4.6,
      tags: ['case-study', 'enterprise', 'integration', 'blockchain'],
      author: 'OASIS Enterprise Team',
      lastUpdated: '2024-01-10T13:20:00Z',
      featured: false,
      difficulty: 'intermediate',
      estimatedTime: '45 minutes',
      prerequisites: ['Enterprise architecture knowledge'],
      languages: ['Multiple'],
      frameworks: ['Multiple'],
      platforms: ['Enterprise'],
      content: 'Detailed case study showing how a major enterprise successfully integrated OASIS for their blockchain infrastructure.',
      codeExamples: ['Enterprise integration patterns', 'Scalability solutions', 'Security implementations', 'Performance optimizations'],
      screenshots: [],
      videoUrl: 'https://youtube.com/watch?v=enterprise-case-study',
      githubUrl: 'https://github.com/oasis/enterprise-examples',
      documentationUrl: 'https://docs.oasis.network/case-studies/enterprise',
      supportUrl: 'https://support.oasis.network/enterprise',
    },
  ];

  let state = {
    stats: null,
    resources: [],
    searchTerm: '',
    selectedCategory: 'all',
    selectedDifficulty: 'all',
    selectedType: 'all',
    viewMode: 'grid',
    selectedResource: null,
    modalOpen: false,
    loading: true,
  };

  function getTypeIcon(type) {
    const icons = {
      cli: '⌘',
      sdk: '{ }',
      api: 'API',
      documentation: '📖',
      tutorial: '🎓',
      'case-study': '📄',
      example: '{ }',
      postman: 'API',
    };
    return icons[type] || '↓';
  }

  function getDifficultyClass(difficulty) {
    const map = { beginner: 'difficulty-beginner', intermediate: 'difficulty-intermediate', advanced: 'difficulty-advanced' };
    return map[difficulty] || '';
  }

  function filterResources() {
    const r = state.resources;
    const term = (state.searchTerm || '').toLowerCase();
    return r.filter(function (resource) {
      const matchesSearch =
        !term ||
        resource.title.toLowerCase().includes(term) ||
        resource.description.toLowerCase().includes(term) ||
        (resource.tags && resource.tags.some(function (tag) { return tag.toLowerCase().includes(term); }));
      const matchesCategory = state.selectedCategory === 'all' || resource.category === state.selectedCategory;
      const matchesDifficulty = state.selectedDifficulty === 'all' || resource.difficulty === state.selectedDifficulty;
      const matchesType = state.selectedType === 'all' || resource.type === state.selectedType;
      return matchesSearch && matchesCategory && matchesDifficulty && matchesType;
    });
  }

  function renderStats() {
    const s = state.stats;
    if (!s) return '';
    return (
      '<div class="dev-portal-stats stats-grid">' +
      '<div class="stat-card"><div class="stat-label">Resources Available</div><div class="stat-value">' + (s.totalResources || 0) + '</div></div>' +
      '<div class="stat-card"><div class="stat-label">Total Downloads</div><div class="stat-value">' + (s.totalDownloads || 0).toLocaleString() + '</div></div>' +
      '<div class="stat-card"><div class="stat-label">Active Developers</div><div class="stat-value">' + (s.activeDevelopers || 0).toLocaleString() + '</div></div>' +
      '<div class="stat-card"><div class="stat-label">Average Rating</div><div class="stat-value">' + (s.averageRating || 0) + '/5</div></div>' +
      '</div>'
    );
  }

  function renderFilters() {
    const categories = [
      { value: 'all', label: 'All Categories' },
      { value: 'getting-started', label: 'Getting Started' },
      { value: 'integration', label: 'Integration' },
      { value: 'advanced', label: 'Advanced' },
      { value: 'examples', label: 'Examples' },
      { value: 'tools', label: 'Tools' },
    ];
    const types = [
      { value: 'all', label: 'All Types' },
      { value: 'cli', label: 'CLI' },
      { value: 'sdk', label: 'SDKs' },
      { value: 'api', label: 'APIs' },
      { value: 'documentation', label: 'Documentation' },
      { value: 'tutorial', label: 'Tutorials' },
      { value: 'case-study', label: 'Case Studies' },
      { value: 'postman', label: 'Postman' },
    ];
    const difficulties = [
      { value: 'all', label: 'All Levels' },
      { value: 'beginner', label: 'Beginner' },
      { value: 'intermediate', label: 'Intermediate' },
      { value: 'advanced', label: 'Advanced' },
    ];
    var catOpts = categories.map(function (c) {
      return '<option value="' + c.value + '"' + (state.selectedCategory === c.value ? ' selected' : '') + '>' + c.label + '</option>';
    }).join('');
    var typeOpts = types.map(function (t) {
      return '<option value="' + t.value + '"' + (state.selectedType === t.value ? ' selected' : '') + '>' + t.label + '</option>';
    }).join('');
    var diffOpts = difficulties.map(function (d) {
      return '<option value="' + d.value + '"' + (state.selectedDifficulty === d.value ? ' selected' : '') + '>' + d.label + '</option>';
    }).join('');
    return (
      '<div class="dev-portal-filters">' +
      '<input type="text" id="dev-portal-search" class="dev-portal-search" placeholder="Search resources..." value="' + (state.searchTerm || '').replace(/"/g, '&quot;') + '">' +
      '<select id="dev-portal-category" class="dev-portal-select">' + catOpts + '</select>' +
      '<select id="dev-portal-type" class="dev-portal-select">' + typeOpts + '</select>' +
      '<select id="dev-portal-difficulty" class="dev-portal-select">' + diffOpts + '</select>' +
      '<div class="dev-portal-view-toggle">' +
      '<button type="button" class="dev-portal-view-btn' + (state.viewMode === 'grid' ? ' active' : '') + '" data-view="grid" title="Grid">⊞</button>' +
      '<button type="button" class="dev-portal-view-btn' + (state.viewMode === 'list' ? ' active' : '') + '" data-view="list" title="List">≡</button>' +
      '</div>' +
      '</div>'
    );
  }

  function renderResourceCard(resource) {
    var tagsHtml = (resource.tags || []).slice(0, 3).map(function (tag) {
      return '<span class="dev-portal-tag">' + escapeHtml(tag) + '</span>';
    }).join('');
    if ((resource.tags || []).length > 3) {
      tagsHtml += '<span class="dev-portal-tag">+' + ((resource.tags || []).length - 3) + ' more</span>';
    }
    var featuredBadge = resource.featured ? '<span class="dev-portal-featured">Featured</span>' : '';
    return (
      '<div class="portal-card dev-portal-card' + (state.viewMode === 'list' ? ' dev-portal-card-list' : '') + '" data-resource-id="' + escapeHtml(resource.id) + '">' +
      '<div class="dev-portal-card-header">' +
      '<span class="dev-portal-type-icon" title="' + escapeHtml(resource.type) + '">' + getTypeIcon(resource.type) + '</span>' +
      '<div class="dev-portal-card-title-wrap">' +
      '<h3 class="dev-portal-card-title">' + escapeHtml(resource.title) + ' ' + featuredBadge + '</h3>' +
      '<div class="dev-portal-chips">' +
      '<span class="dev-portal-chip ' + getDifficultyClass(resource.difficulty) + '">' + escapeHtml(resource.difficulty) + '</span>' +
      '<span class="dev-portal-chip outline">' + escapeHtml(resource.type) + '</span>' +
      '<span class="dev-portal-chip outline">v' + escapeHtml(resource.version) + '</span>' +
      '</div>' +
      '</div>' +
      '</div>' +
      '<p class="dev-portal-card-desc">' + escapeHtml(resource.description) + '</p>' +
      '<div class="dev-portal-tags">' + tagsHtml + '</div>' +
      '<div class="dev-portal-meta">' +
      '<span class="dev-portal-rating">★ ' + (resource.rating || 0) + '/5</span>' +
      '<span class="dev-portal-downloads">(' + (resource.downloads || 0).toLocaleString() + ' downloads)</span>' +
      '<span class="dev-portal-size">' + escapeHtml(resource.size || '') + '</span>' +
      '</div>' +
      '<div class="dev-portal-meta-secondary">' +
      '<span>⏱ ' + escapeHtml(resource.estimatedTime || '') + '</span>' +
      '<span>👤 ' + escapeHtml(resource.author || '') + '</span>' +
      '</div>' +
      '<div class="dev-portal-card-actions">' +
      '<button type="button" class="dev-portal-btn dev-portal-btn-primary view-details-btn" data-resource-id="' + escapeHtml(resource.id) + '">View Details</button>' +
      (resource.githubUrl ? '<a href="' + escapeHtml(resource.githubUrl) + '" target="_blank" rel="noopener" class="dev-portal-btn dev-portal-btn-outline">GitHub</a>' : '') +
      (resource.documentationUrl ? '<a href="' + escapeHtml(resource.documentationUrl) + '" target="_blank" rel="noopener" class="dev-portal-btn dev-portal-btn-outline">Docs</a>' : '') +
      '</div>' +
      '</div>'
    );
  }

  function escapeHtml(str) {
    if (str == null) return '';
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
  }

  function renderResourcesGrid(filtered) {
    if (filtered.length === 0) {
      return '<div class="dev-portal-empty">No resources match your filters.</div>';
    }
    return (
      '<div class="dev-portal-grid dev-portal-view-' + state.viewMode + '">' +
      filtered.map(function (r) { return renderResourceCard(r); }).join('') +
      '</div>'
    );
  }

  function renderModal() {
    var r = state.selectedResource;
    if (!r) return '';
    var prereqHtml = (r.prerequisites || []).map(function (p) { return '<span class="dev-portal-chip outline">' + escapeHtml(p) + '</span>'; }).join('');
    var platformHtml = (r.platforms || []).map(function (p) { return '<span class="dev-portal-chip outline">' + escapeHtml(p) + '</span>'; }).join('');
    var langFw = [].concat(r.languages || [], r.frameworks || []).map(function (x) { return '<span class="dev-portal-chip outline">' + escapeHtml(x) + '</span>'; }).join('');
    var codeHtml = (r.codeExamples || []).length
      ? '<div class="dev-portal-code-block"><pre>' + escapeHtml((r.codeExamples || []).join('\n')) + '</pre></div>'
      : '';
    return (
      '<div id="dev-portal-modal" class="dev-portal-modal' + (state.modalOpen ? ' open' : '') + '">' +
      '<div class="dev-portal-modal-backdrop"></div>' +
      '<div class="dev-portal-modal-content">' +
      '<div class="dev-portal-modal-header">' +
      '<h2 class="dev-portal-modal-title">' + escapeHtml(r.title) + '</h2>' +
      '<button type="button" class="dev-portal-modal-close" aria-label="Close">&times;</button>' +
      '</div>' +
      '<div class="dev-portal-modal-body">' +
      '<p class="dev-portal-modal-desc">' + escapeHtml(r.description) + '</p>' +
      '<div class="dev-portal-modal-chips">' +
      '<span class="dev-portal-chip">v' + escapeHtml(r.version) + '</span>' +
      '<span class="dev-portal-chip outline">' + escapeHtml(r.size) + '</span>' +
      '<span class="dev-portal-chip ' + getDifficultyClass(r.difficulty) + '">' + escapeHtml(r.difficulty) + '</span>' +
      '</div>' +
      '<h4>Prerequisites</h4><div class="dev-portal-chip-wrap">' + prereqHtml + '</div>' +
      '<h4>Supported Platforms</h4><div class="dev-portal-chip-wrap">' + platformHtml + '</div>' +
      '<h4>Languages & Frameworks</h4><div class="dev-portal-chip-wrap">' + langFw + '</div>' +
      (codeHtml ? '<h4>Code Examples</h4>' + codeHtml : '') +
      '</div>' +
      '<div class="dev-portal-modal-footer">' +
      '<button type="button" class="dev-portal-btn dev-portal-btn-secondary cancel-modal-btn">Cancel</button>' +
      '<button type="button" class="dev-portal-btn dev-portal-btn-primary download-modal-btn">Download Now</button>' +
      '</div>' +
      '</div>' +
      '</div>'
    );
  }

  function render() {
    var container = document.getElementById('dev-portal-content');
    if (!container) return;
    var filtered = filterResources();
    var modalHtml = (state.selectedResource && state.modalOpen) ? renderModal() : '';
    container.innerHTML =
      '<div class="dev-portal">' +
      '<div class="dev-portal-header">' +
      '<h1 class="dev-portal-title">🚀 OASIS Dev Portal</h1>' +
      '<p class="dev-portal-subtitle">Everything you need to build on the OASIS ecosystem</p>' +
      '</div>' +
      (state.loading ? '<div class="dev-portal-loading">Loading...</div>' : '') +
      (!state.loading ? renderStats() : '') +
      (!state.loading ? renderFilters() : '') +
      (!state.loading ? renderResourcesGrid(filtered) : '') +
      modalHtml +
      '</div>';
    if (state.modalOpen && state.selectedResource) {
      attachModalEvents();
    }
    attachFilters();
    attachCardClicks();
  }

  function openModal(resource) {
    state.selectedResource = resource;
    state.modalOpen = true;
    render();
    var modalEl = document.getElementById('dev-portal-modal');
    if (modalEl) modalEl.classList.add('open');
    attachModalEvents();
  }

  function closeModal() {
    state.modalOpen = false;
    state.selectedResource = null;
    var modal = document.getElementById('dev-portal-modal');
    if (modal) modal.classList.remove('open');
    render();
  }

  function attachModalEvents() {
    var modal = document.getElementById('dev-portal-modal');
    if (!modal) return;
    var backdrop = modal.querySelector('.dev-portal-modal-backdrop');
    var closeBtn = modal.querySelector('.dev-portal-modal-close');
    var cancelBtn = modal.querySelector('.cancel-modal-btn');
    var downloadBtn = modal.querySelector('.download-modal-btn');
    if (backdrop) backdrop.addEventListener('click', closeModal);
    if (closeBtn) closeBtn.addEventListener('click', closeModal);
    if (cancelBtn) cancelBtn.addEventListener('click', closeModal);
    if (downloadBtn) downloadBtn.addEventListener('click', function () {
      var r = state.selectedResource;
      if (r && r.downloadUrl) {
        try { window.open(r.downloadUrl, '_blank', 'noopener,noreferrer'); } catch (e) {}
      }
      closeModal();
    });
    document.addEventListener('keydown', function onEsc(e) {
      if (e.key === 'Escape') { closeModal(); document.removeEventListener('keydown', onEsc); }
    });
  }

  function attachFilters() {
    var search = document.getElementById('dev-portal-search');
    var cat = document.getElementById('dev-portal-category');
    var type = document.getElementById('dev-portal-type');
    var diff = document.getElementById('dev-portal-difficulty');
    function update() {
      state.searchTerm = search ? search.value : '';
      state.selectedCategory = cat ? cat.value : 'all';
      state.selectedType = type ? type.value : 'all';
      state.selectedDifficulty = diff ? diff.value : 'all';
      render();
    }
    if (search) search.addEventListener('input', update);
    if (cat) cat.addEventListener('change', update);
    if (type) type.addEventListener('change', update);
    if (diff) diff.addEventListener('change', update);
    var viewBtns = document.querySelectorAll('.dev-portal-view-btn');
    viewBtns.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var v = btn.getAttribute('data-view');
        if (v) { state.viewMode = v; render(); }
      });
    });
  }

  function attachCardClicks() {
    document.querySelectorAll('.view-details-btn, .dev-portal-card').forEach(function (el) {
      el.addEventListener('click', function (e) {
        var id = el.getAttribute('data-resource-id') || (e.target.closest && e.target.closest('.dev-portal-card')?.getAttribute('data-resource-id'));
        if (!id) return;
        var resource = state.resources.find(function (r) { return r.id === id; });
        if (resource) openModal(resource);
      });
    });
  }

  function loadData() {
    state.loading = true;
    render();
    var statsPromise =
      typeof oasisAPI !== 'undefined' && oasisAPI.getDevPortalStats
        ? oasisAPI.getDevPortalStats().then(function (res) {
            if (res && !res.isError && res.result) return res.result;
            return DEMO_STATS;
          })
        : Promise.resolve(DEMO_STATS);
    var resourcesPromise =
      typeof oasisAPI !== 'undefined' && oasisAPI.getDevPortalResources
        ? oasisAPI.getDevPortalResources().then(function (res) {
            if (res && !res.isError && res.result && Array.isArray(res.result)) return res.result;
            return DEMO_RESOURCES;
          })
        : Promise.resolve(DEMO_RESOURCES);
    Promise.all([statsPromise, resourcesPromise])
      .then(function (results) {
        state.stats = results[0];
        state.resources = Array.isArray(results[1]) ? results[1] : DEMO_RESOURCES;
        state.loading = false;
        render();
      })
      .catch(function () {
        state.stats = DEMO_STATS;
        state.resources = DEMO_RESOURCES;
        state.loading = false;
        render();
      });
  }

  function loadDevPortal() {
    var container = document.getElementById('dev-portal-content');
    if (!container) return;
    if (container.querySelector('.dev-portal')) {
      return;
    }
    loadData();
  }

  if (typeof window !== 'undefined') {
    window.loadDevPortal = loadDevPortal;
  }
})();
