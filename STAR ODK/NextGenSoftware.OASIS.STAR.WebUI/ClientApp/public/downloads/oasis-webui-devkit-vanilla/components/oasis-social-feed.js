class OASISSocialFeed extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.posts = [];
  }

  connectedCallback() {
    this.render();
    this.loadFeed();
  }

  async loadFeed() {
    const response = await fetch('/api/social/feed');
    this.posts = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .post { background: white; padding: 20px; margin-bottom: 16px; border-radius: 8px; }
        .post-header { display: flex; gap: 12px; margin-bottom: 12px; }
        .avatar { width: 40px; height: 40px; border-radius: 50%; background: #e3f2fd; }
        .actions { display: flex; gap: 16px; margin-top: 12px; }
        button { background: none; border: none; cursor: pointer; }
      </style>
      ${this.posts.map(p => `
        <div class="post">
          <div class="post-header">
            <div class="avatar"></div>
            <div><strong>${p.author}</strong><br/><small>${p.time}</small></div>
          </div>
          <p>${p.content}</p>
          <div class="actions">
            <button>👍 ${p.likes}</button>
            <button>💬 ${p.comments}</button>
            <button>🔄 Share</button>
          </div>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-social-feed', OASISSocialFeed);



