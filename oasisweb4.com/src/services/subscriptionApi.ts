export async function createCheckoutSession(planId: string): Promise<void> {
  // Calls existing SubscriptionController endpoint
  const apiBase = import.meta.env.VITE_WEB4_API_BASE ?? 'http://localhost:5000/api';
  try {
    const res = await fetch(`${apiBase}/subscription/checkout/session`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        planId,
        successUrl: `${window.location.origin}/checkout/success`,
        cancelUrl: `${window.location.origin}/plans`
      })
    });
    if (!res.ok) throw new Error(`Checkout failed: ${res.status}`);
    const data = await res.json();
    const url = data?.sessionUrl || data?.url;
    if (url) {
      window.location.href = url;
    }
  } catch (e) {
    // eslint-disable-next-line no-console
    console.error(e);
    alert('Checkout not configured yet. Please set VITE_WEB4_API_BASE and backend endpoints.');
  }
}


