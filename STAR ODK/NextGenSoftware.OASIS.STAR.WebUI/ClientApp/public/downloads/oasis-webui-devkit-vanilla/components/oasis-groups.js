class OASISGroups extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.groups = [];
  }

  connectedCallback() {
    this.render();
    this.loadGroups();
  }

  async loadGroups() {
    const response = await fetch('/api/groups');
    this.groups = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .group { padding: 20px; background: white; margin-bottom: 16px; border-radius: 8px; }
        .emoji { font-size: 36px; }
        button { padding: 8px 16px; background: #4A90E2; color: white; border: none; border-radius: 4px; cursor: pointer; }
      </style>
      ${this.groups.map(g => `
        <div class="group">
          <div class="emoji">${g.emoji}</div>
          <h3>${g.name}</h3>
          <p>${g.description}</p>
          <small>${g.memberCount} members</small>
          <button>View</button>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-groups', OASISGroups);



