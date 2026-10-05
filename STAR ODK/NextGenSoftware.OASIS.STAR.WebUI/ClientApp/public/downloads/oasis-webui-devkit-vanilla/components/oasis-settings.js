class OASISSettings extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
  }

  connectedCallback() {
    this.render();
  }

  async saveSettings() {
    const settings = {
      theme: this.shadowRoot.getElementById('theme').value,
      privacy: this.shadowRoot.getElementById('privacy').value
    };
    await fetch('/api/settings', { method: 'POST', body: JSON.stringify(settings) });
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .setting { margin-bottom: 20px; }
        label { display: block; margin-bottom: 8px; font-weight: 600; }
        select, input { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 12px 24px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
      </style>
      <div class="setting">
        <label>Theme</label>
        <select id="theme"><option>Light</option><option>Dark</option></select>
      </div>
      <div class="setting">
        <label>Privacy</label>
        <select id="privacy"><option>Public</option><option>Private</option></select>
      </div>
      <button id="saveBtn">Save Settings</button>
    `;
    this.shadowRoot.getElementById('saveBtn').addEventListener('click', () => this.saveSettings());
  }
}

customElements.define('oasis-settings', OASISSettings);



