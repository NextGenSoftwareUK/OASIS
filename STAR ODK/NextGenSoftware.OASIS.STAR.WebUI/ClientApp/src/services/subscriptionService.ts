// Subscription Service for STAR Web UI
// Handles subscription-related API calls

export interface Plan {
  id: string;
  name: string;
  price: string;
  originalPrice?: string;
  description: string;
  features: string[];
  popular?: boolean;
  recommended?: boolean;
  limits: {
    apiCalls: string;
    storage: string;
    support: string;
  };
}

export interface Subscription {
  id: string;
  planName: string;
  status: 'active' | 'canceled' | 'past_due' | 'incomplete';
  amount: number;
  currency: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
}

export interface PaymentMethod {
  id: string;
  type: 'card';
  last4: string;
  brand: string;
  expMonth: number;
  expYear: number;
  isDefault: boolean;
}

export interface Invoice {
  id: string;
  amount: number;
  currency: string;
  status: 'paid' | 'open' | 'void';
  created: string;
  invoiceUrl: string;
}

export interface CreateCheckoutSessionRequest {
  planId: string;
  successUrl: string;
  cancelUrl: string;
}

export interface CreateCheckoutSessionResponse {
  sessionUrl: string;
  sessionId: string;
}

import { ENV } from '../config/env';

class SubscriptionService {
  private baseUrl: string;

  constructor() {
    this.baseUrl = ENV.API_BASE_URL;
  }

  // Get all available subscription plans
  async getPlans(): Promise<Plan[]> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/plans`);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (error) {
      console.error('Error fetching plans:', error);
      // Return default plans as fallback
      return this.getDefaultPlans();
    }
  }

  // Create a checkout session for a plan
  async createCheckoutSession(request: CreateCheckoutSessionRequest): Promise<CreateCheckoutSessionResponse> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/checkout/session`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error creating checkout session:', error);
      throw error;
    }
  }

  // Get current user's subscription
  async getMySubscription(): Promise<Subscription | null> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/subscriptions/me`, {
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
      });

      if (!response.ok) {
        if (response.status === 404) {
          return null; // No subscription found
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching subscription:', error);
      throw error;
    }
  }

  // Get user's payment methods
  async getPaymentMethods(): Promise<PaymentMethod[]> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/payment-methods`, {
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching payment methods:', error);
      throw error;
    }
  }

  // Get user's invoices
  async getInvoices(): Promise<Invoice[]> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/orders/me`, {
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching invoices:', error);
      throw error;
    }
  }

  // Cancel subscription
  async cancelSubscription(): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/cancel`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Error canceling subscription:', error);
      throw error;
    }
  }

  // Update subscription plan
  async updatePlan(planId: string): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/subscription/update`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
        body: JSON.stringify({ planId }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Error updating plan:', error);
      throw error;
    }
  }

  // Get auth token from localStorage or context
  private getAuthToken(): string {
    // TODO: Implement proper auth token retrieval
    // This should get the token from your auth context or localStorage
    return localStorage.getItem('authToken') || '';
  }

  // Default plans as fallback
  private getDefaultPlans(): Plan[] {
    return [
      {
        id: 'bronze',
        name: 'Bronze',
        price: '$9/mo',
        description: 'Perfect for getting started with OASIS',
        features: [
          '10,000 API calls/month',
          '1GB storage',
          'Community support',
          'Basic analytics',
          'Standard uptime SLA',
        ],
        limits: {
          apiCalls: '10,000/month',
          storage: '1GB',
          support: 'Community',
        },
      },
      {
        id: 'silver',
        name: 'Silver',
        price: '$29/mo',
        originalPrice: '$39/mo',
        description: 'Great for growing applications',
        features: [
          '100,000 API calls/month',
          '10GB storage',
          'Email support',
          'Advanced analytics',
          'Priority uptime SLA',
          'Webhook support',
        ],
        popular: true,
        limits: {
          apiCalls: '100,000/month',
          storage: '10GB',
          support: 'Email',
        },
      },
      {
        id: 'gold',
        name: 'Gold',
        price: '$99/mo',
        originalPrice: '$129/mo',
        description: 'For serious applications',
        features: [
          '1,000,000 API calls/month',
          '100GB storage',
          'Priority support',
          'Advanced analytics & reporting',
          '99.9% uptime SLA',
          'Custom webhooks',
          'API rate limit increases',
        ],
        recommended: true,
        limits: {
          apiCalls: '1,000,000/month',
          storage: '100GB',
          support: 'Priority',
        },
      },
      {
        id: 'enterprise',
        name: 'Enterprise',
        price: 'Contact us',
        description: 'Custom solutions for large organizations',
        features: [
          'Unlimited API calls',
          'Unlimited storage',
          'Dedicated support',
          'Custom analytics',
          '99.99% uptime SLA',
          'SSO integration',
          'Custom integrations',
          'SLA guarantees',
        ],
        limits: {
          apiCalls: 'Unlimited',
          storage: 'Unlimited',
          support: 'Dedicated',
        },
      },
    ];
  }
}

export const subscriptionService = new SubscriptionService();
