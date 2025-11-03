class OASISProvider extends HTMLElement {
  constructor() {
    super();
    this.apiUrl = this.getAttribute('api-url') || 'https://api.oasis.earth/api/v1';
    this.apiKey = this.getAttribute('api-key') || '';
  }

  connectedCallback() {
    // Make client available globally
    window.oasisClient = {
      get: (url) => fetch(this.apiUrl + url, { headers: { 'X-API-Key': this.apiKey } }).then(r => r.json()),
      post: (url, data) => fetch(this.apiUrl + url, { 
        method: 'POST', 
        headers: { 'Content-Type': 'application/json', 'X-API-Key': this.apiKey },
        body: JSON.stringify(data)
      }).then(r => r.json())
    };
  }
}

customElements.define('oasis-provider', OASISProvider);



