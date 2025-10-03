class OASISNotifications extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.notifications = [];
  }

  connectedCallback() {
    this.render();
    this.loadNotifications();
  }

  async loadNotifications() {
    const response = await fetch('/api/notifications');
    this.notifications = await response.json();
    this.render();
  }

  async markAsRead(id) {
    await fetch(`/api/notifications/${id}/read`, { method: 'POST' });
    this.loadNotifications();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; }
        .notification { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
        .notification:hover { background: #f5f5f5; }
        .notification.unread { background: #e3f2fd; }
        .icon { font-size: 24px; }
        .content { flex: 1; }
        h4 { margin: 0 0 4px 0; font-size: 14px; }
        p { margin: 0; font-size: 13px; color: #666; }
      </style>
      ${this.notifications.map(n => `
        <div class="notification ${n.read ? '' : 'unread'}" onclick="this.getRootNode().host.markAsRead('${n.id}')">
          <div class="icon">${n.type === 'karma' ? '⭐' : '📢'}</div>
          <div class="content">
            <h4>${n.title}</h4>
            <p>${n.message}</p>
          </div>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-notifications', OASISNotifications);



